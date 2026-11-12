using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// Draws the imported environment (kerbs, pads, banks, cliffs, decor, scatter) with GPU instancing instead of one
    /// object per piece: the pieces are grouped by mesh, material and a square of the map, and each group that the main
    /// camera can see (with a margin for shadows) is drawn in one call per frame. A level holds thousands of pieces;
    /// as objects they cost thousands of draw calls, as groups a few hundred. Nothing allocates per frame.
    /// </summary>
    public sealed class EnvironmentInstancer : MonoBehaviour
    {
        private const int MaxPerDraw = 1023;
        private const float ChunkSize = 10f;
        private const float CullMargin = 3f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private sealed class Batch
        {
            public Mesh Mesh;
            public Material Material;
            public bool Shadows;
            public readonly List<Matrix4x4> Building = new List<Matrix4x4>();
            public Matrix4x4[] Matrices;

            // Every piece of the batch; Matrices is all of them or those left after a cleared sight line.
            public Matrix4x4[] All;
            public Bounds Bounds;
            public bool HasBounds;
        }

        private readonly Dictionary<(Mesh, Material, bool, Vector2Int), Batch> _lookup = new Dictionary<(Mesh, Material, bool, Vector2Int), Batch>();
        private readonly List<Batch> _batches = new List<Batch>();

        // A remains piece is fitted to this share of the smashed piece's reach: the rubble spreads well wider than the piece
        // stood and heaps a little higher than modelled, so it reads at the game camera on a dark field (round 15).
        private const float RemainsFit = 1.7f;
        private const float MinRemainsScale = 0.8f;
        private const float MaxRemainsScale = 4.5f;
        private const float RemainsHeap = 1.35f;

        // Crystal shards glow in the colour family of the level's crystals (their light pools), a little lifted, so they read.
        private const float CrystalRemainsGlow = 2.4f;
        private const float RemainsAlbedoLift = 1.3f;

        // Decoration a boss smashes when its body overlaps it (DecorContact.Breakable), with its reach and its remains.
        private readonly List<(Batch Batch, Matrix4x4 Matrix, float Radius, DecorRemains Remains)> _breakable = new List<(Batch, Matrix4x4, float, DecorRemains)>();

        // The remains are added after Finish, one group per mesh and square of the map.
        private readonly Dictionary<(Mesh, Vector2Int), Batch> _remainsLookup = new Dictionary<(Mesh, Vector2Int), Batch>();
        private EnvironmentSet _remainsSet;
        private bool _remainsLow;
        private Color _crystalGlow;
        private Material _crystalRemains;
        private Batch _lastBatch;
        private Matrix4x4 _lastMatrix;
        private Bounds _lastBounds;
        private readonly List<DecorFormation> _formations = new List<DecorFormation>();
        private readonly Plane[] _planes = new Plane[6];

        // Round 39 (Remco: crystals stuck inside rocks): the brittle pieces (crystals, flora, small rocks) and the solid ones
        // (boulders, spires, cliffs, terraces) as added, so a brittle piece buried in a solid one can be left out before the
        // groups freeze. A brittle piece's attachment (its glow pool and light) goes with it.
        private const float SolidMinHeight = 0.6f;
        private const float BuriedCore = 0.85f;
        private const float BuriedShare = 0.15f;
        private const int LatticeAcross = 3;
        private const int LatticeUp = 5;
        private const float LatticeReach = 0.6f;
        private readonly List<Brittle> _brittle = new List<Brittle>();
        private readonly List<(Mesh Mesh, Matrix4x4 Matrix, Bounds Bounds)> _solids = new List<(Mesh, Matrix4x4, Bounds)>();

        private sealed class Brittle
        {
            public Batch Batch;
            public Matrix4x4 Matrix;
            public Bounds Bounds;
            public bool Formation;
            public Transform Attachment;
        }
        private readonly List<Material> _ownMaterials = new List<Material>();

        /// <summary>How many pieces were added, for tests and reports.</summary>
        public int Count { get; private set; }

        /// <summary>How many groups (draws when all are in view) the pieces fell into.</summary>
        public int BatchCount => _batches.Count;

        /// <summary>When set (by tests), the world bounds of every decoration piece added through <see cref="AddDecor"/>.</summary>
        public List<Bounds> DecorLog { get; set; }

        public static EnvironmentInstancer Create(Transform parent)
        {
            var go = new GameObject("Environment");
            go.transform.SetParent(parent, false);
            return go.AddComponent<EnvironmentInstancer>();
        }

        /// <summary>
        /// Adds one piece at a placement in the piece's own frame (y up, as the README's placement frames describe it). The
        /// material is the model's for the profile unless the caller passes its own copy.
        /// </summary>
        /// <param name="castShadows">False keeps a piece that would throw a shadow out of the shadow pass (far decor).</param>
        public void Add(EnvironmentMesh model, Matrix4x4 matrix, bool low, Material material = null, bool castShadows = true)
        {
            if (model == null || !model.IsValid)
            {
                return;
            }

            Material wear = material != null ? material : model.MaterialFor(low);
            bool shadows = model.CastShadows && castShadows;
            Vector3 position = matrix.GetColumn(3);
            matrix *= model.Node;
            var chunk = new Vector2Int(Mathf.FloorToInt(position.x / ChunkSize), Mathf.FloorToInt(position.z / ChunkSize));
            var key = (model.Mesh, wear, shadows, chunk);
            if (!_lookup.TryGetValue(key, out Batch batch))
            {
                batch = new Batch { Mesh = model.Mesh, Material = wear, Shadows = shadows };
                _lookup[key] = batch;
                _batches.Add(batch);
            }

            batch.Building.Add(matrix);
            _lastBatch = batch;
            _lastMatrix = matrix;
            Bounds bounds = Transform(model.Mesh.bounds, matrix);
            _lastBounds = bounds;
            if (model.Contact != DecorContact.Breakable && bounds.size.y >= SolidMinHeight)
            {
                _solids.Add((model.Mesh, matrix, bounds));
            }

            if (batch.HasBounds)
            {
                batch.Bounds.Encapsulate(bounds);
            }
            else
            {
                batch.Bounds = bounds;
                batch.HasBounds = true;
            }

            Count++;
        }

        /// <summary>Adds a piece of decoration (rock, plant, cliff, scatter, silhouette): as <see cref="Add"/>, and logged when a test asks.</summary>
        /// The piece's <see cref="EnvironmentMesh.Contact"/> says what a boss does with it.
        /// <param name="formation">True for the formation standing on a blocked cell of the field, which the climb layer reads.</param>
        public void AddDecor(EnvironmentMesh model, Matrix4x4 matrix, bool low, Material material = null, bool castShadows = true, bool formation = false)
        {
            _lastBatch = null;
            Add(model, matrix, low, material, castShadows);
            if (_lastBatch == null)
            {
                return;
            }

            if (formation)
            {
                _formations.Add(new DecorFormation(_lastBounds.center, _lastBounds.extents, model.Contact));
            }

            if (model.Contact == DecorContact.Breakable)
            {
                float reach = Mathf.Max(_lastBounds.extents.x, _lastBounds.extents.z);
                _breakable.Add((_lastBatch, _lastMatrix, reach, model.Remains));
                _brittle.Add(new Brittle { Batch = _lastBatch, Matrix = _lastMatrix, Bounds = _lastBounds, Formation = formation });
            }

            if (DecorLog != null && model != null && model.IsValid)
            {
                DecorLog.Add(Transform(model.Mesh.bounds, matrix * model.Node));
            }
        }

        /// <summary>Ties an object (a glow pool, a light) to the brittle piece added last, so it goes when that piece is left out.</summary>
        public void AttachToLast(Transform attachment)
        {
            if (_brittle.Count > 0 && _brittle[_brittle.Count - 1].Batch == _lastBatch && _brittle[_brittle.Count - 1].Matrix == _lastMatrix)
            {
                _brittle[_brittle.Count - 1].Attachment = attachment;
            }
        }

        /// <summary>
        /// Leaves out every brittle piece (crystal, plant, small rock) that stands buried in a solid one (boulder, spire, cliff,
        /// terrace, silhouette): more than 15 % of a lattice through its middle inside the solid's core (its mesh bounds shrunk to 85 %, in the
        /// solid's own frame). Formation pieces stay, the climb layer reads them. Call before <see cref="Finish"/>; returns how
        /// many were left out. Bounds and point tests only, run once per level.
        /// </summary>
        public int LeaveOutBuriedBrittle()
        {
            var points = new Dictionary<Mesh, Vector3[]>();
            int left = 0;
            for (int i = _brittle.Count - 1; i >= 0; i--)
            {
                Brittle piece = _brittle[i];
                if (piece.Formation || !IsBuried(piece, points))
                {
                    continue;
                }

                piece.Batch.Building.Remove(piece.Matrix);
                _breakable.RemoveAll(entry => entry.Batch == piece.Batch && entry.Matrix == piece.Matrix);
                if (piece.Attachment != null)
                {
                    DestroyImmediate(piece.Attachment.gameObject);
                }

                _brittle.RemoveAt(i);
                left++;
            }

            return left;
        }

        private bool IsBuried(Brittle piece, Dictionary<Mesh, Vector3[]> points)
        {
            if (!points.TryGetValue(piece.Batch.Mesh, out Vector3[] samples))
            {
                samples = Samples(piece.Batch.Mesh);
                points[piece.Batch.Mesh] = samples;
            }

            foreach ((Mesh mesh, Matrix4x4 matrix, Bounds bounds) in _solids)
            {
                if (!bounds.Intersects(piece.Bounds))
                {
                    continue;
                }

                Matrix4x4 into = matrix.inverse * piece.Matrix;
                Bounds core = mesh.bounds;
                core.extents *= BuriedCore;
                int inside = 0;
                foreach (Vector3 sample in samples)
                {
                    if (core.Contains(into.MultiplyPoint3x4(sample)))
                    {
                        inside++;
                    }
                }

                if (inside > samples.Length * BuriedShare)
                {
                    return true;
                }
            }

            return false;
        }

        // A lattice of points through the middle of a mesh's bounds, standing in for its body: the imported meshes are not
        // readable at run time (no Read/Write), so their vertices cannot be sampled.
        private static Vector3[] Samples(Mesh mesh)
        {
            Bounds bounds = mesh.bounds;
            var samples = new Vector3[LatticeAcross * LatticeUp * LatticeAcross];
            int i = 0;
            for (int x = 0; x < LatticeAcross; x++)
            {
                for (int y = 0; y < LatticeUp; y++)
                {
                    for (int z = 0; z < LatticeAcross; z++)
                    {
                        var share = new Vector3(Cell(x, LatticeAcross), Cell(y, LatticeUp), Cell(z, LatticeAcross));
                        samples[i++] = bounds.center + Vector3.Scale(bounds.extents * LatticeReach, share);
                    }
                }
            }

            return samples;
        }

        // -1..1 across the lattice's cells, at their middles.
        private static float Cell(int index, int count) => (index + 0.5f) / count * 2f - 1f;

        /// <summary>A copy of a material this level changes (a tinted glow); destroyed with the level.</summary>
        public Material Own(Material source)
        {
            var copy = new Material(source) { name = source.name + " (level)" };
            _ownMaterials.Add(copy);
            return copy;
        }

        /// <summary>Freezes the groups once everything is added.</summary>
        public void Finish()
        {
            foreach (Batch batch in _batches)
            {
                batch.All = batch.Building.ToArray();
                batch.Matrices = batch.All;
                batch.Building.Clear();
                batch.Bounds.Expand(CullMargin);
            }

            _lookup.Clear();
        }

        /// <summary>
        /// Leaves out every piece standing (in plan) within <paramref name="radius"/> of a sight line from a camera spot to
        /// what it looks at, so a close shot of a character is never blocked by a rock between them; an empty list draws
        /// everything again. Recomputed only when called (once per shot).
        /// </summary>
        public void ClearSightLines(IReadOnlyList<Vector3> eyes, IReadOnlyList<Vector3> targets, float radius)
        {
            foreach (Batch batch in _batches)
            {
                if (batch.All == null)
                {
                    continue;
                }

                if (eyes.Count == 0)
                {
                    batch.Matrices = batch.All;
                    continue;
                }

                var kept = new List<Matrix4x4>(batch.All.Length);
                foreach (Matrix4x4 matrix in batch.All)
                {
                    if (!Blocks(matrix.GetPosition(), eyes, targets, radius))
                    {
                        kept.Add(matrix);
                    }
                }

                batch.Matrices = kept.Count == batch.All.Length ? batch.All : kept.ToArray();
            }
        }

        /// <summary>
        /// True when no drawn piece taller than <paramref name="minHeight"/> stands (in plan) within <paramref name="radius"/>
        /// of <paramref name="point"/>, each piece measured by its mesh bounds through its matrix. Ground cover lower than
        /// that does not count, so a cutscene drone may set down in grass but never inside a rock.
        /// </summary>
        public bool IsClear(Vector3 point, float radius, float minHeight)
        {
            foreach (Batch batch in _batches)
            {
                if (batch.All == null || batch.Mesh == null || !NearInPlan(batch.Bounds, point, radius))
                {
                    continue;
                }

                foreach (Matrix4x4 matrix in batch.All)
                {
                    Bounds piece = Transform(batch.Mesh.bounds, matrix);
                    if (piece.size.y >= minHeight && NearInPlan(piece, point, radius))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Adds the world bounds of every drawn piece at least <paramref name="minHeight"/> tall (rocks, boulders, crystals; no ground cover).</summary>
        public void CollectTall(float minHeight, List<Bounds> into)
        {
            foreach (Batch batch in _batches)
            {
                if (batch.All == null || batch.Mesh == null)
                {
                    continue;
                }

                foreach (Matrix4x4 matrix in batch.All)
                {
                    Bounds piece = Transform(batch.Mesh.bounds, matrix);
                    if (piece.size.y >= minHeight)
                    {
                        into.Add(piece);
                    }
                }
            }
        }

        // Whether a circle on the ground touches a box seen from above.
        private static bool NearInPlan(Bounds bounds, Vector3 point, float radius)
        {
            float dx = Mathf.Max(0f, Mathf.Abs(point.x - bounds.center.x) - bounds.extents.x);
            float dz = Mathf.Max(0f, Mathf.Abs(point.z - bounds.center.z) - bounds.extents.z);
            return dx * dx + dz * dz < radius * radius;
        }

        /// <summary>The formations on the field's blocked cells as drawn: where, how big, and what a boss does with them.</summary>
        public IReadOnlyList<DecorFormation> Formations => _formations;

        /// <summary>How many breakable pieces still stand.</summary>
        public int BreakableCount => _breakable.Count;

        /// <summary>How many remains lie on the field (smashed pieces), for probes and tests.</summary>
        public int RemainsCount { get; private set; }

        /// <summary>Where a smashed piece's remains come from (the level's environment set); none leaves nothing behind.</summary>
        public void UseRemains(EnvironmentSet set, bool low)
        {
            _remainsSet = set;
            _remainsLow = low;
            EnvironmentDecor crystal = set != null ? set.Decor(PropKind.Crystal) ?? set.Decor(PropKind.IceShard) : null;
            _crystalGlow = crystal != null ? crystal.Glow : Color.clear;
        }

        /// <summary>
        /// Smashes every breakable piece a body of <paramref name="radius"/> at a point overlaps (the piece's own reach
        /// counts half, so a glancing pass at a wide crystal does not break it): it is no longer drawn, its remains are
        /// laid in its place for the rest of the level, and it is added to <paramref name="broken"/> for the shatter view.
        /// Returns how many broke.
        /// </summary>
        public int Break(Vector3 centre, float radius, List<BrokenDecor> broken = null)
        {
            int count = 0;
            for (int i = _breakable.Count - 1; i >= 0; i--)
            {
                (Batch batch, Matrix4x4 matrix, float reach, DecorRemains remains) = _breakable[i];
                Vector3 at = matrix.GetPosition();
                float dx = at.x - centre.x;
                float dz = at.z - centre.z;
                float touch = radius + reach * 0.5f;
                if (dx * dx + dz * dz > touch * touch)
                {
                    continue;
                }

                _breakable.RemoveAt(i);
                Remove(batch, matrix);
                LayRemains(remains, matrix, reach);
                broken?.Add(new BrokenDecor(at, reach, remains));
                count++;
            }

            return count;
        }

        /// <summary>
        /// Leaves out every piece (solid, breakable or remains) that stands on a rectangle of ground for the rest of the level,
        /// with nothing laid in its place: a structure raised there later (the ruin gate on its plinth) then stands on clear
        /// ground instead of through boulders. The rectangle is centred on <paramref name="centre"/>, its depth along
        /// <paramref name="forward"/>; a piece counts when its centre lies within the rectangle grown by half its reach (as
        /// <see cref="Break"/> measures). Returns how many were left out.
        /// </summary>
        public int ClearFootprint(Vector3 centre, Vector3 forward, Vector2 halfSize)
        {
            Vector3 along = new Vector3(forward.x, 0f, forward.z).normalized;
            Vector3 across = new Vector3(along.z, 0f, -along.x);
            int count = 0;
            foreach (Batch batch in _batches)
            {
                if (batch.All == null || batch.Mesh == null)
                {
                    continue;
                }

                foreach (Matrix4x4 matrix in batch.All)
                {
                    Bounds piece = Transform(batch.Mesh.bounds, matrix);
                    Vector3 offset = piece.center - centre;
                    float grow = Mathf.Max(piece.extents.x, piece.extents.z) * 0.5f;
                    if (Mathf.Abs(Vector3.Dot(offset, across)) > halfSize.x + grow || Mathf.Abs(Vector3.Dot(offset, along)) > halfSize.y + grow)
                    {
                        continue;
                    }

                    Forget(batch, matrix);
                    count++;
                }
            }

            return count;
        }

        // Drops a piece from its group and from the breakable and brittle lists (its glow pool goes with it).
        private void Forget(Batch batch, Matrix4x4 matrix)
        {
            Remove(batch, matrix);
            _breakable.RemoveAll(entry => entry.Batch == batch && entry.Matrix == matrix);
            for (int i = _brittle.Count - 1; i >= 0; i--)
            {
                Brittle piece = _brittle[i];
                if (piece.Batch != batch || piece.Matrix != matrix)
                {
                    continue;
                }

                if (piece.Attachment != null)
                {
                    Destroy(piece.Attachment.gameObject);
                }

                _brittle.RemoveAt(i);
            }
        }

        // The remains turned as the piece stood (its yaw read off the placed frame) and fitted to its reach.
        private void LayRemains(DecorRemains kind, Matrix4x4 piece, float reach)
        {
            EnvironmentMesh model = _remainsSet != null ? _remainsSet.Remains(kind) : null;
            if (model == null)
            {
                return;
            }

            Bounds own = Transform(model.Mesh.bounds, model.Node);
            float span = Mathf.Max(own.extents.x, own.extents.z);
            float scale = span > 0.001f ? Mathf.Clamp(reach * RemainsFit / span, MinRemainsScale, MaxRemainsScale) : 1f;
            Vector3 side = piece.GetColumn(0);
            float yaw = Mathf.Atan2(side.z, side.x) * Mathf.Rad2Deg;
            Matrix4x4 matrix = Matrix4x4.TRS(piece.GetPosition(), Quaternion.Euler(0f, -yaw, 0f), new Vector3(scale, scale * RemainsHeap, scale)) * model.Node;
            Bounds bounds = Transform(model.Mesh.bounds, matrix);
            var chunk = new Vector2Int(Mathf.FloorToInt(bounds.center.x / ChunkSize), Mathf.FloorToInt(bounds.center.z / ChunkSize));
            if (!_remainsLookup.TryGetValue((model.Mesh, chunk), out Batch batch))
            {
                batch = new Batch { Mesh = model.Mesh, Material = RemainsMaterial(kind, model), Shadows = false, All = new Matrix4x4[0], Bounds = bounds, HasBounds = true };
                _remainsLookup[(model.Mesh, chunk)] = batch;
                _batches.Add(batch);
            }

            var grown = new Matrix4x4[batch.All.Length + 1];
            System.Array.Copy(batch.All, grown, batch.All.Length);
            grown[grown.Length - 1] = matrix;
            batch.All = grown;
            batch.Matrices = grown;
            bounds.Expand(CullMargin);
            batch.Bounds.Encapsulate(bounds);
            RemainsCount++;
        }

        // Stone and plant debris wear their own material; crystal shards a level copy lit in the level's crystal colour.
        private Material RemainsMaterial(DecorRemains kind, EnvironmentMesh model)
        {
            Material source = model.MaterialFor(_remainsLow);
            if (kind != DecorRemains.Crystal || _crystalGlow.maxColorComponent <= 0f)
            {
                return source;
            }

            if (_crystalRemains == null)
            {
                Color hue = _crystalGlow / _crystalGlow.maxColorComponent;
                hue.a = 1f;
                _crystalRemains = Own(source);
                _crystalRemains.SetColor(BaseColorId, source.GetColor(BaseColorId) * RemainsAlbedoLift);
                _crystalRemains.SetColor(EmissionColorId, hue * CrystalRemainsGlow);
            }

            return _crystalRemains;
        }

        private static void Remove(Batch batch, Matrix4x4 matrix)
        {
            if (batch.All == null)
            {
                batch.Building.Remove(matrix);
                return;
            }

            int index = System.Array.IndexOf(batch.All, matrix);
            if (index < 0)
            {
                return;
            }

            var kept = new Matrix4x4[batch.All.Length - 1];
            System.Array.Copy(batch.All, 0, kept, 0, index);
            System.Array.Copy(batch.All, index + 1, kept, index, kept.Length - index);
            batch.All = kept;
            batch.Matrices = kept;
        }

        private static bool Blocks(Vector3 position, IReadOnlyList<Vector3> eyes, IReadOnlyList<Vector3> targets, float radius)
        {
            var point = new Vector2(position.x, position.z);
            for (int i = 0; i < eyes.Count; i++)
            {
                var a = new Vector2(eyes[i].x, eyes[i].z);
                var b = new Vector2(targets[i].x, targets[i].z);
                Vector2 line = b - a;
                float t = line.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(point - a, line) / line.sqrMagnitude) : 0f;
                if ((a + line * t - point).sqrMagnitude < radius * radius)
                {
                    return true;
                }
            }

            return false;
        }

        private void LateUpdate()
        {
            Camera view = Camera.main;
            bool cull = view != null;
            if (cull)
            {
                GeometryUtility.CalculateFrustumPlanes(view, _planes);
            }

            int layer = gameObject.layer;
            for (int i = 0; i < _batches.Count; i++)
            {
                Batch batch = _batches[i];
                if (batch.Matrices == null || (cull && !GeometryUtility.TestPlanesAABB(_planes, batch.Bounds)))
                {
                    continue;
                }

                var parameters = new RenderParams(batch.Material)
                {
                    layer = layer,
                    shadowCastingMode = batch.Shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = true,
                    worldBounds = batch.Bounds,
                };
                for (int start = 0; start < batch.Matrices.Length; start += MaxPerDraw)
                {
                    Graphics.RenderMeshInstanced(parameters, batch.Mesh, 0, batch.Matrices, Mathf.Min(MaxPerDraw, batch.Matrices.Length - start), start);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _ownMaterials)
            {
                Destroy(material);
            }
        }

        private static Bounds Transform(Bounds local, Matrix4x4 matrix)
        {
            Vector3 centre = matrix.MultiplyPoint3x4(local.center);
            Vector3 extents = local.extents;
            Vector3 x = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
            Vector3 y = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
            Vector3 z = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
            var size = new Vector3(
                Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
            return new Bounds(centre, size * 2f);
        }
    }
}
