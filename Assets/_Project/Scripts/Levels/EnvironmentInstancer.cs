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

        // A remains piece is fitted to this share of the smashed piece's reach: the rubble spreads a little wider than the piece stood, so it reads at the game camera.
        private const float RemainsFit = 1.2f;
        private const float MinRemainsScale = 0.3f;
        private const float MaxRemainsScale = 3f;

        // Decoration a boss smashes when its body overlaps it (DecorContact.Breakable), with its reach and its remains.
        private readonly List<(Batch Batch, Matrix4x4 Matrix, float Radius, DecorRemains Remains)> _breakable = new List<(Batch, Matrix4x4, float, DecorRemains)>();

        // The remains are added after Finish, one group per mesh and square of the map.
        private readonly Dictionary<(Mesh, Vector2Int), Batch> _remainsLookup = new Dictionary<(Mesh, Vector2Int), Batch>();
        private EnvironmentSet _remainsSet;
        private bool _remainsLow;
        private Batch _lastBatch;
        private Matrix4x4 _lastMatrix;
        private Bounds _lastBounds;
        private readonly List<DecorFormation> _formations = new List<DecorFormation>();
        private readonly Plane[] _planes = new Plane[6];
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
            }

            if (DecorLog != null && model != null && model.IsValid)
            {
                DecorLog.Add(Transform(model.Mesh.bounds, matrix * model.Node));
            }
        }

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
            Matrix4x4 matrix = Matrix4x4.TRS(piece.GetPosition(), Quaternion.Euler(0f, -yaw, 0f), Vector3.one * scale) * model.Node;
            Bounds bounds = Transform(model.Mesh.bounds, matrix);
            var chunk = new Vector2Int(Mathf.FloorToInt(bounds.center.x / ChunkSize), Mathf.FloorToInt(bounds.center.z / ChunkSize));
            if (!_remainsLookup.TryGetValue((model.Mesh, chunk), out Batch batch))
            {
                batch = new Batch { Mesh = model.Mesh, Material = model.MaterialFor(_remainsLow), Shadows = false, All = new Matrix4x4[0], Bounds = bounds, HasBounds = true };
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
