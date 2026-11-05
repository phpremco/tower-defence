using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Bosses
{
    /// <summary>
    /// Shows a <see cref="LavaTrail"/>. With the round-29 art the trail is ONE ribbon of the painted trail strip (lava:
    /// basalt crust over a molten channel that cools from its edges in; rot: slime and mould that decays from its edges)
    /// laid along the boss's smoothed path, rebuilt each frame from the live patches into reused buffers. Its U runs along
    /// the distance walked, so the strip flows on without repeating in tiles; each vertex carries its own age (vertex colour
    /// R: how far it has cooled, A: its fade), so it cools along its length; both ends taper through the UV3 end data.
    /// Remco 2026-11-05: the overlapping pieces painted over each other in turns. A ribbon that crosses itself (a tight turn,
    /// a U-turn) draws each pixel once: the material sets a stencil bit where it draws, newest part first, and skips pixels
    /// already set. Without the art each patch is a pool of light. A few embers or spores rise while it is fresh.
    /// </summary>
    public sealed class LavaTrailView : MonoBehaviour
    {
        private const float PoolScale = 1.5f;
        private const float EmberRate = 6f;
        // The hot lava starts a little cooled (README round 29: the fresh strip glows over too much of its crack net).
        private const float FirstCool = 0.2f;
        private const float FadeFrom = 0.7f;

        // Samples of the smoothed path between two patches, and the columns across the ribbon (edge, centre, edge).
        private const int Subdivisions = 3;
        private const int Columns = 3;

        // Patches further apart than this many spacings belong to separate stretches (a jump the trail never walked).
        private const float BreakSpacings = 2.2f;
        private const float HeadMinGap = 0.05f;

        // GroundMark rule (no square ends): the head rounds off over this many widths, the tail over more, and the tail
        // also narrows over its last metres so the trail thins out instead of stopping. Neither end takes more than this
        // share of a short stretch.
        private const float HeadEndWidths = 0.7f;
        private const float TailEndWidths = 1.6f;
        private const float EndShareMax = 0.45f;
        private const float TailNarrowest = 0.4f;
        private const float TailNarrowWidths = 1.4f;

        // Draw once: stencil bit 32 (Game/DepthPunch owns 64 and clears it), and the alpha under which a pixel is not
        // claimed, so a faint fringe of the newer part never cuts a seam into the older part it crosses.
        private const int DrawOnceBit = 32;
        private const float DrawOnceClip = 0.03f;
        private const float BoundsHeight = 0.5f;
        private const float GroundMarkLift = 0.03f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int VertexProgress = Shader.PropertyToID("_VertexProgress");
        private static readonly int ClipBelow = Shader.PropertyToID("_ClipBelow");
        private static readonly int StencilRef = Shader.PropertyToID("_StencilRef");
        private static readonly int StencilComp = Shader.PropertyToID("_StencilComp");
        private static readonly int StencilPass = Shader.PropertyToID("_StencilPass");
        private static readonly Color Hot = new Color(1f, 0.45f, 0.08f, 1f) * 2.2f;
        private static readonly Color Ember = new Color(1f, 0.6f, 0.2f, 0.9f);
        private static readonly float[] Across = { -1f, 0f, 1f };

        private LavaTrail _trail;
        private Color _hot;
        private MaterialPropertyBlock _block;
        private ParticleSystem _embers;
        private bool _marked;
        private bool _heat;

        // Without the art: one pool of light per slot of the ring.
        private Renderer[] _pools;

        // The ribbon.
        private Mesh _mesh;
        private Transform _ribbon;
        private Matrix4x4 _toLocal;
        private float _width;
        private float _tileLength;
        private float _uStart;
        private float[] _along;
        private float _walked;
        private Vector3 _lastLaid;
        private bool _hasLast;

        // Reused buffers: the live slots in laid order, where each stretch starts, one stretch's points, and the mesh.
        private int[] _order;
        private int[] _runStarts;
        private Vector3[] _points;
        private float[] _pointAge;
        private float[] _pointAlong;
        private Vector3[] _vertices;
        private Vector3[] _normals;
        private Vector2[] _uvs;
        private Vector3[] _ends;
        private Color32[] _colours;
        private int[] _triangles;

        public static LavaTrailView Create(Transform parent, LavaTrail trail, float radius) => Create(parent, trail, radius, Hot, Ember, "LavaTrail", BossFxArt.Mark.LavaTrail);

        /// <summary>A trail in other colours and another mark (the rot an uprooted flower leaves behind it).</summary>
        public static LavaTrailView Create(Transform parent, LavaTrail trail, float radius, Color hot, Color ember, string name, BossFxArt.Mark mark = BossFxArt.Mark.RotTrail)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<LavaTrailView>();
            view._trail = trail;
            view._hot = hot;
            view._block = new MaterialPropertyBlock();
            view._heat = mark == BossFxArt.Mark.LavaTrail;
            BossFxArt art = BossFxArt.Active;
            Material material = GroundMark.MaterialFor(mark);
            view._marked = material != null;
            if (view._marked)
            {
                view.BuildRibbon(material, art.Width(mark), art.TileLength(mark));
            }
            else
            {
                view.BuildPools(hot, radius);
            }

            view._embers = ParticleKit.Create(go.transform, "Embers", Vector3.zero, ember, 0.6f, 1.2f, 0.04f, 0.1f, 0.4f, 1f, -0.4f, 80, radius);
            ParticleKit.SetRate(view._embers, 0f);
            trail.Laid += view.OnLaid;
            return view;
        }

        private void BuildRibbon(Material material, float width, float tileLength)
        {
            material.SetFloat(VertexProgress, 1f);
            material.SetFloat(GroundMark.Progress, 1f);
            material.SetFloat(GroundMark.Fade, 1f);
            material.SetFloat(GroundMark.EndTaper, 1f);
            material.SetFloat(ClipBelow, DrawOnceClip);
            material.SetFloat(StencilRef, DrawOnceBit);
            material.SetFloat(StencilComp, (float)CompareFunction.NotEqual);
            material.SetFloat(StencilPass, (float)StencilOp.Replace);
            _width = width;
            _tileLength = Mathf.Max(0.01f, tileLength);
            _uStart = Random.value;

            int capacity = _trail.Capacity;
            _along = new float[capacity];
            _order = new int[capacity];
            _runStarts = new int[capacity + 1];
            _points = new Vector3[capacity + 1];
            _pointAge = new float[capacity + 1];
            _pointAlong = new float[capacity + 1];
            int rows = (capacity + 1) * Subdivisions + capacity + 1;
            _vertices = new Vector3[rows * Columns];
            _normals = new Vector3[_vertices.Length];
            _uvs = new Vector2[_vertices.Length];
            _ends = new Vector3[_vertices.Length];
            _colours = new Color32[_vertices.Length];
            _triangles = new int[rows * (Columns - 1) * 6];
            for (int i = 0; i < _normals.Length; i++)
            {
                _normals[i] = Vector3.up;
            }

            _mesh = new Mesh { name = "GroundMarkTrailRibbon" };
            _mesh.MarkDynamic();
            var ribbon = new GameObject("Ribbon");
            ribbon.transform.SetParent(transform, false);
            _ribbon = ribbon.transform;
            ribbon.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = ribbon.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void BuildPools(Color hot, float radius)
        {
            _pools = new Renderer[_trail.Capacity];
            for (int i = 0; i < _pools.Length; i++)
            {
                var slot = new GameObject("Patch").transform;
                slot.SetParent(transform, false);
                _pools[i] = GlowDecal.PlacePool(slot, hot, radius * PoolScale).GetComponent<MeshRenderer>();
                slot.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_trail != null)
            {
                _trail.Laid -= OnLaid;
            }

            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        private void OnLaid(int slot)
        {
            Vector3 at = _trail.PointAt(slot);
            if (_marked)
            {
                float step = _hasLast ? Vector3.Distance(at, _lastLaid) : 0f;
                // A new stretch starts somewhere else along the strip's tiles, so two stretches never repeat in step.
                _walked += _trail.StartsRun(slot) || !_hasLast ? Random.Range(0f, _tileLength) : step;
                _along[slot] = _walked;
            }
            else
            {
                Transform pool = _pools[slot].transform.parent;
                pool.position = at;
                pool.gameObject.SetActive(true);
            }

            _lastLaid = at;
            _hasLast = true;
            _embers.transform.position = at;
            _embers.Emit(2);
        }

        private void Update()
        {
            if (_marked)
            {
                RebuildRibbon();
            }
            else
            {
                UpdatePools();
            }
        }

        private void RebuildRibbon()
        {
            // The patches are in world space; the ribbon lives under the effects root, wherever that stands.
            _toLocal = _ribbon.worldToLocalMatrix;
            int live = CollectLive();
            int runs = SplitRuns(live);
            int rows = 0;
            int indices = 0;
            // Newest stretch first, each from its head back: where the trail crosses itself, the fresher part wins the pixel.
            for (int r = runs - 1; r >= 0; r--)
            {
                int count = FillRun(_runStarts[r], _runStarts[r + 1], r == runs - 1);
                if (count >= 2)
                {
                    AddRun(count, ref rows, ref indices);
                }
            }

            _mesh.Clear(true);
            if (indices == 0)
            {
                return;
            }

            int vertexCount = rows * Columns;
            _mesh.SetVertices(_vertices, 0, vertexCount);
            _mesh.SetNormals(_normals, 0, vertexCount);
            _mesh.SetUVs(0, _uvs, 0, vertexCount);
            _mesh.SetUVs(3, _ends, 0, vertexCount);
            _mesh.SetColors(_colours, 0, vertexCount);
            _mesh.SetTriangles(_triangles, 0, indices, 0, false);
            _mesh.RecalculateBounds();
            Bounds bounds = _mesh.bounds;
            bounds.Expand(new Vector3(0f, BoundsHeight, 0f));
            _mesh.bounds = bounds;
        }

        // The live slots in the order they were laid (all patches live equally long, so the dead ones are the oldest).
        private int CollectLive()
        {
            int live = 0;
            for (int k = 0; k < _trail.Count; k++)
            {
                int slot = _trail.SlotInOrder(k);
                if (_trail.IsLive(slot))
                {
                    _order[live++] = slot;
                }
            }

            return live;
        }

        private int SplitRuns(int live)
        {
            int runs = 0;
            float breakSqr = BreakSpacings * BreakSpacings * _trail.Spacing * _trail.Spacing;
            for (int k = 0; k < live; k++)
            {
                int slot = _order[k];
                bool starts = k == 0 || _trail.StartsRun(slot) || (_trail.PointAt(slot) - _trail.PointAt(_order[k - 1])).sqrMagnitude > breakSqr;
                if (starts)
                {
                    _runStarts[runs++] = k;
                }
            }

            _runStarts[runs] = live;
            return runs;
        }

        // One stretch's points from its head back to its tail; the newest stretch reaches on to where the boss walks now.
        private int FillRun(int from, int to, bool newest)
        {
            int count = 0;
            int head = _order[to - 1];
            if (newest && _trail.IsWalking)
            {
                Vector3 now = _trail.Head;
                float gap = Vector3.Distance(now, _trail.PointAt(head));
                if (gap > HeadMinGap && gap < BreakSpacings * _trail.Spacing)
                {
                    _points[count] = now;
                    _pointAge[count] = 0f;
                    _pointAlong[count] = _along[head] + gap;
                    count++;
                }
            }

            for (int k = to - 1; k >= from; k--)
            {
                int slot = _order[k];
                _points[count] = _trail.PointAt(slot);
                _pointAge[count] = _trail.AgeAt(slot);
                _pointAlong[count] = _along[slot];
                count++;
            }

            return count;
        }

        private void AddRun(int count, ref int rows, ref int indices)
        {
            float headAlong = _pointAlong[0];
            float tailAlong = _pointAlong[count - 1];
            float length = Mathf.Max(0.01f, headAlong - tailAlong);
            float headEnd = Mathf.Max(0.01f, Mathf.Min(_width * HeadEndWidths, length * EndShareMax));
            float tailEnd = Mathf.Max(0.01f, Mathf.Min(_width * TailEndWidths, length * EndShareMax));
            float narrowOver = _width * TailNarrowWidths;
            int first = rows;
            int samples = (count - 1) * Subdivisions + 1;
            for (int s = 0; s < samples; s++)
            {
                int i = Mathf.Min(s / Subdivisions, count - 2);
                float t = (s - i * Subdivisions) / (float)Subdivisions;
                Vector3 at = Smoothed(i, t, count);
                float along = Mathf.Lerp(_pointAlong[i], _pointAlong[i + 1], t);
                float age = Mathf.Lerp(_pointAge[i], _pointAge[i + 1], t) / _trail.Lifetime;
                Vector3 side = SideAt(s, samples, count);
                float fromTail = along - tailAlong;
                float half = _width * 0.5f * Mathf.Lerp(TailNarrowest, 1f, Mathf.Clamp01(fromTail / narrowOver));
                float ends = Mathf.Min((headAlong - along) / headEnd, fromTail / tailEnd);
                Color32 colour = AgeColour(age);
                for (int c = 0; c < Columns; c++)
                {
                    int v = rows * Columns + c;
                    _vertices[v] = _toLocal.MultiplyPoint3x4(at + side * (Across[c] * half));
                    _uvs[v] = new Vector2(_uStart + along / _tileLength, (Across[c] + 1f) * 0.5f);
                    _ends[v] = new Vector3(ends, Across[c], 1f);
                    _colours[v] = colour;
                }

                rows++;
            }

            for (int row = first; row < rows - 1; row++)
            {
                for (int c = 0; c < Columns - 1; c++)
                {
                    int a = row * Columns + c;
                    int b = a + Columns;
                    _triangles[indices++] = a;
                    _triangles[indices++] = b;
                    _triangles[indices++] = b + 1;
                    _triangles[indices++] = a;
                    _triangles[indices++] = b + 1;
                    _triangles[indices++] = a + 1;
                }
            }
        }

        // Catmull-Rom through the patches, so a turn bends round instead of kinking at each patch.
        private Vector3 Smoothed(int i, float t, int count)
        {
            Vector3 p1 = _points[i];
            Vector3 p2 = _points[i + 1];
            Vector3 p0 = i > 0 ? _points[i - 1] : p1 * 2f - p2;
            Vector3 p3 = i + 2 < count ? _points[i + 2] : p2 * 2f - p1;
            float t2 = t * t;
            float t3 = t2 * t;
            Vector3 point = 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
            point.y = GroundMarkLift;
            return point;
        }

        private Vector3 SideAt(int s, int samples, int count)
        {
            int before = Mathf.Max(0, s - 1);
            int after = Mathf.Min(samples - 1, s + 1);
            Vector3 along = SampleAt(before, count) - SampleAt(after, count);
            along.y = 0f;
            return along.sqrMagnitude > 0.000001f ? Vector3.Cross(Vector3.up, along.normalized) : Vector3.right;
        }

        private Vector3 SampleAt(int s, int count)
        {
            int i = Mathf.Min(s / Subdivisions, count - 2);
            return Smoothed(i, (s - i * Subdivisions) / (float)Subdivisions, count);
        }

        private Color32 AgeColour(float age)
        {
            float progress = _heat ? Mathf.Lerp(FirstCool, 1f, age) : age;
            float fade = 1f - Mathf.Clamp01((age - FadeFrom) / (1f - FadeFrom));
            return new Color32((byte)(Mathf.Clamp01(progress) * 255f), 255, 255, (byte)(fade * 255f));
        }

        private void UpdatePools()
        {
            for (int i = 0; i < _pools.Length; i++)
            {
                Transform pool = _pools[i].transform.parent;
                if (!pool.gameObject.activeSelf)
                {
                    continue;
                }

                if (!_trail.IsLive(i))
                {
                    pool.gameObject.SetActive(false);
                    continue;
                }

                float heat = 1f - _trail.AgeAt(i) / _trail.Lifetime;
                Color glow = _hot * (heat * heat);
                _pools[i].GetPropertyBlock(_block);
                _block.SetColor(MarkerArt.Tint, glow);
                _block.SetColor(EmissionColorId, glow);
                _pools[i].SetPropertyBlock(_block);
            }
        }

        /// <summary>Puts the whole trail out at once (the boss left the field).</summary>
        public void Clear()
        {
            _hasLast = false;
            if (_mesh != null)
            {
                _mesh.Clear();
            }

            if (_pools != null)
            {
                foreach (Renderer pool in _pools)
                {
                    pool.transform.parent.gameObject.SetActive(false);
                }
            }
        }

        public void SetEmbers(bool on) => ParticleKit.SetRate(_embers, on ? EmberRate : 0f);
    }
}
