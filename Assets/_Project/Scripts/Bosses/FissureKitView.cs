using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Bosses
{
    /// <summary>
    /// Gravemaw's crack as real geometry (ArtSource README round 34): the modular fissure kit laid along the drawn crack
    /// line, a <c>Fissure_Start</c> at the dive hole, straights behind it and a <c>Fissure_Head</c> coming to a jagged point
    /// at the far end, with a side crack every few metres. Each module is bent along the line vertex by vertex (spline
    /// deform), so the crack follows the rounded path without a kink, never ends square and never overlaps itself. All
    /// modules go into one mesh: the rock (<c>Game/FissureRock</c>) and, twice, the gap opening that punches the
    /// ground's depth in two stages (<c>Game/DepthPunch</c>). Opening, cooling and healing only move material values; the mesh is built
    /// once. Look only.
    /// </summary>
    public sealed class FissureKitView : MonoBehaviour
    {
        private const string StartModule = "Fissure_Start";
        private const string HeadModule = "Fissure_Head";
        private const string BranchLeft = "Fissure_Branch_L";
        private const string BranchRight = "Fissure_Branch_R";
        private static readonly string[] Straights = { "Fissure_Straight_A", "Fissure_Straight_B", "Fissure_Straight_C" };
        private const string FarSuffix = "_Far";

        // Mean modelled length of a straight (2, 3 and 2 m), to choose how many fit between the start and the head.
        private const float MeanStraight = 2.33f;

        // Modules are stretched or squeezed along the line at most this much rather than cut (never a square end).
        private const float MinSqueeze = 0.45f;

        // README round 34: one side crack every 6-10 m, alternating sides, never right at the ends.
        private const float BranchEveryMin = 6f;
        private const float BranchEveryMax = 10f;
        private const float BranchEndMargin = 3f;
        private const float HotLength = 6f;
        private const float ResidualHeat = 0.12f;
        private const float BoundsPadding = 1.5f;

        private static readonly int FrontId = Shader.PropertyToID("_Front");
        private static readonly int OpenLengthId = Shader.PropertyToID("_OpenLength");
        private static readonly int CloseId = Shader.PropertyToID("_Close");
        private static readonly int HeatId = Shader.PropertyToID("_Heat");
        private static readonly int HotLengthId = Shader.PropertyToID("_HotLength");

        private static readonly Dictionary<GameObject, Dictionary<string, Module>> Kits = new Dictionary<GameObject, Dictionary<string, Module>>();

        private Vector3[] _points;
        private float[] _along;
        private Vector3[] _tangents;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private Mesh _mesh;
        private float _coolSeconds;
        private float _openedFor = -1f;
        private float _heat = 1f;
        private float _fadeHeat = 1f;
        private float _close;
        private float _front;

        // One module of the kit in game space (README "Unity" axes: +Z forward along the path, +X right, +Y up).
        private sealed class Module
        {
            public float Length;
            public Vector3[] Positions;
            public Vector3[] Normals;
            public Vector4[] Tangents;
            public Vector2[] Uvs;
            public float[] Along;
            public Vector3[] Close;
            public Color32[] Colours;
            public int[] Rock;
            public int[] Punch;
        }

        /// <summary>Total length of the line the crack follows.</summary>
        public float Length => _along[_along.Length - 1];

        /// <summary>
        /// Lays the kit along a drawn crack line (points on the ground, from the dive hole to the far end); null when the
        /// art is not registered. A vertex over a spot <paramref name="blocked"/> names fades out.
        /// </summary>
        public static FissureKitView Create(Transform parent, IReadOnlyList<Vector3> points, System.Func<Vector3, bool> blocked)
        {
            BossFxArt art = BossFxArt.Active;
            if (art == null || !art.HasFissure || points == null || points.Count < 2)
            {
                return null;
            }

            Dictionary<string, Module> kit = Load(art.FissureKit);
            if (kit == null)
            {
                return null;
            }

            var go = new GameObject("Fissure");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<FissureKitView>();
            view.Build(points, kit, art, blocked);
            return view;
        }

        /// <summary>Tears the crack open up to a distance along its line.</summary>
        public void OpenTo(float distance)
        {
            if (_openedFor < 0f && distance > 0f)
            {
                _openedFor = 0f;
            }

            _front = distance;
            Apply();
        }

        /// <summary>
        /// The crack cooling away: <paramref name="left"/> runs from 1 (just started) to 0 (gone). The glow goes first, then
        /// the lips fold flat and the gap shuts.
        /// </summary>
        public void Fade(float left)
        {
            _fadeHeat = Mathf.Clamp01(left * 2f - 1f);
            _close = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(left / 0.45f));
            Apply();
        }

        private void Update()
        {
            if (_openedFor < 0f)
            {
                return;
            }

            _openedFor += Time.deltaTime;
            _heat = Mathf.Lerp(1f, ResidualHeat, Mathf.Clamp01(_openedFor / _coolSeconds));
            Apply();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }

        private void Apply()
        {
            _block.SetFloat(FrontId, _front);
            _block.SetFloat(CloseId, _close);
            _block.SetFloat(HeatId, _heat * _fadeHeat);
            _block.SetFloat(HotLengthId, _fadeHeat < 1f ? 0.001f : HotLength);
            _renderer.SetPropertyBlock(_block);
        }

        private void Build(IReadOnlyList<Vector3> points, Dictionary<string, Module> kit, BossFxArt art, System.Func<Vector3, bool> blocked)
        {
            BuildLine(points);
            _coolSeconds = art.FissureCoolSeconds;
            float scale = art.FissureScale;
            var mesh = new MeshData();
            float length = Length;
            List<(Module Module, float Stretch)> order = Plan(kit, length, scale);
            float total = 0f;
            foreach ((Module module, float stretch) in order)
            {
                total += module.Length * scale * stretch;
            }

            // Whatever rounding is left over is shared out over every module, so the head's point lands on the end.
            float fit = total > 0.001f ? length / total : 1f;
            float at = 0f;
            foreach ((Module module, float stretch) in order)
            {
                float along = scale * stretch * fit;
                Lay(mesh, module, at, along, scale, blocked);
                at += module.Length * along;
            }

            LayBranches(mesh, kit, length, scale, blocked);
            _mesh = mesh.ToMesh();
            Bounds bounds = _mesh.bounds;
            bounds.Expand(BoundsPadding);
            _mesh.bounds = bounds;
            gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterials = new[] { art.FissureRock, art.FissurePunch, art.FissureClear };
            _renderer.shadowCastingMode = ShadowCastingMode.On;
            _renderer.receiveShadows = true;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _block = new MaterialPropertyBlock();
            _block.SetFloat(OpenLengthId, art.FissureOpenLength);
            Apply();
        }

        // The start, as many straights as fit, the head; each stretched alike so they fill the line exactly. A line too
        // short for the start and the head squeezes those two (still pointed at both ends).
        private static List<(Module, float)> Plan(Dictionary<string, Module> kit, float length, float scale)
        {
            var order = new List<(Module, float)>();
            Module start = kit[StartModule];
            Module head = kit[HeadModule];
            float ends = (start.Length + head.Length) * scale;
            float middle = length - ends;
            int count = middle > 0f ? Mathf.RoundToInt(middle / (MeanStraight * scale)) : 0;
            var straights = new List<Module>(count);
            float straightLength = 0f;
            int last = -1;
            for (int i = 0; i < count; i++)
            {
                int pick = Random.Range(0, Straights.Length);
                if (pick == last)
                {
                    pick = (pick + 1) % Straights.Length;
                }

                last = pick;
                straights.Add(kit[Straights[pick]]);
                straightLength += kit[Straights[pick]].Length * scale;
            }

            float endStretch = count == 0 ? Mathf.Max(MinSqueeze, length / ends) : 1f;
            float straightStretch = count > 0 ? Mathf.Max(MinSqueeze, middle / straightLength) : 1f;
            order.Add((start, endStretch));
            foreach (Module straight in straights)
            {
                order.Add((straight, straightStretch));
            }

            order.Add((head, endStretch));
            return order;
        }

        // A side crack off the lip now and then, alternating sides, pivoted on the line like a module start.
        private void LayBranches(MeshData mesh, Dictionary<string, Module> kit, float length, float scale, System.Func<Vector3, bool> blocked)
        {
            if (!kit.TryGetValue(BranchLeft, out Module left) || !kit.TryGetValue(BranchRight, out Module right))
            {
                return;
            }

            bool leftSide = Random.value < 0.5f;
            float at = BranchEndMargin + Random.Range(0f, BranchEveryMin * 0.5f);
            while (at < length - BranchEndMargin - left.Length * scale)
            {
                Lay(mesh, leftSide ? left : right, at, scale, scale, blocked);
                leftSide = !leftSide;
                at += Random.Range(BranchEveryMin, BranchEveryMax);
            }
        }

        // Bends one module onto the line from a distance along it: each vertex keeps its place across and above the line
        // at the distance its own depth along the module gives, and its closed position bends the same way.
        private void Lay(MeshData mesh, Module module, float start, float alongScale, float scale, System.Func<Vector3, bool> blocked)
        {
            int first = mesh.Positions.Count;
            for (int i = 0; i < module.Positions.Length; i++)
            {
                Vector3 local = module.Positions[i];
                Vector3 world = Bend(local, start, alongScale, scale, out Vector3 right, out Vector3 forward);
                Vector3 closed = Bend(local + module.Close[i], start, alongScale, scale, out _, out _);
                Vector3 n = module.Normals[i];
                Vector4 t = module.Tangents[i];
                Vector3 tangent = right * t.x + Vector3.up * t.y + forward * t.z;
                Color32 colour = module.Colours[i];
                if (blocked != null && blocked(world))
                {
                    colour.a = 0;
                }

                Vector3 normal = transform.InverseTransformDirection((right * n.x + Vector3.up * n.y + forward * n.z).normalized);
                tangent = transform.InverseTransformDirection(tangent);
                mesh.Positions.Add(transform.InverseTransformPoint(world));
                mesh.Normals.Add(normal);
                mesh.Tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, t.w));
                mesh.Uvs.Add(module.Uvs[i]);
                mesh.Reveal.Add(new Vector2(start + module.Along[i] * alongScale, 0f));
                Vector3 delta = transform.InverseTransformVector(closed - world);
                mesh.CloseXY.Add(new Vector2(delta.x, delta.y));
                mesh.CloseZ.Add(new Vector2(delta.z, 0f));
                mesh.Colours.Add(colour);
            }

            foreach (int index in module.Rock)
            {
                mesh.Rock.Add(first + index);
            }

            foreach (int index in module.Punch)
            {
                mesh.Punch.Add(first + index);
            }
        }

        private Vector3 Bend(Vector3 local, float start, float alongScale, float scale, out Vector3 right, out Vector3 forward)
        {
            Vector3 on = Sample(start + local.z * alongScale, out forward);
            right = Vector3.Cross(Vector3.up, forward);
            return on + right * (local.x * scale) + Vector3.up * (local.y * scale);
        }

        // The line's point and direction at a distance; past either end it runs on straight (hairlines reach beyond).
        private Vector3 Sample(float distance, out Vector3 forward)
        {
            int last = _points.Length - 1;
            if (distance <= 0f)
            {
                forward = _tangents[0];
                return _points[0] + forward * distance;
            }

            if (distance >= _along[last])
            {
                forward = _tangents[last];
                return _points[last] + forward * (distance - _along[last]);
            }

            int i = 1;
            while (i < last && _along[i] < distance)
            {
                i++;
            }

            float span = _along[i] - _along[i - 1];
            float t = span > 0.0001f ? (distance - _along[i - 1]) / span : 0f;
            forward = Vector3.Slerp(_tangents[i - 1], _tangents[i], t).normalized;
            return Vector3.Lerp(_points[i - 1], _points[i], t);
        }

        private void BuildLine(IReadOnlyList<Vector3> points)
        {
            _points = new Vector3[points.Count];
            _along = new float[points.Count];
            _tangents = new Vector3[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                _points[i] = points[i];
                _along[i] = i > 0 ? _along[i - 1] + Vector3.Distance(points[i - 1], points[i]) : 0f;
            }

            // Smooth directions per point (the average of both legs), so a bent module shows no crease at a point.
            for (int i = 0; i < _points.Length; i++)
            {
                Vector3 along = _points[Mathf.Min(i + 1, _points.Length - 1)] - _points[Mathf.Max(i - 1, 0)];
                along.y = 0f;
                _tangents[i] = along.sqrMagnitude > 0.000001f ? along.normalized : (i > 0 ? _tangents[i - 1] : Vector3.forward);
            }
        }

        // The kit's meshes read once per model, turned from the export's axes into the README's game axes
        // (x right = -mesh x, y up = -mesh z, z forward = -mesh y); the closed offsets are given in game axes already.
        private static Dictionary<string, Module> Load(GameObject model)
        {
            if (model == null)
            {
                return null;
            }

            if (Kits.TryGetValue(model, out Dictionary<string, Module> cached))
            {
                return cached;
            }

            var modules = new Dictionary<string, Module>();
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || !mesh.isReadable || mesh.subMeshCount < 2)
                {
                    continue;
                }

                string name = filter.name.EndsWith(FarSuffix, System.StringComparison.Ordinal) ? filter.name.Substring(0, filter.name.Length - FarSuffix.Length) : filter.name;
                modules[name] = Read(mesh);
            }

            bool complete = modules.ContainsKey(StartModule) && modules.ContainsKey(HeadModule);
            foreach (string straight in Straights)
            {
                complete &= modules.ContainsKey(straight);
            }

            Kits[model] = complete ? modules : null;
            return Kits[model];
        }

        private static Module Read(Mesh mesh)
        {
            Vector3[] positions = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            var reveal = new List<Vector2>();
            var closeXY = new List<Vector2>();
            var closeZ = new List<Vector2>();
            mesh.GetUVs(1, reveal);
            mesh.GetUVs(2, closeXY);
            mesh.GetUVs(3, closeZ);
            var module = new Module
            {
                Positions = new Vector3[positions.Length],
                Normals = new Vector3[positions.Length],
                Tangents = new Vector4[positions.Length],
                Uvs = mesh.uv,
                Along = new float[positions.Length],
                Close = new Vector3[positions.Length],
                Colours = mesh.colors32,
                Rock = mesh.GetTriangles(0),
                Punch = mesh.GetTriangles(1),
            };

            for (int i = 0; i < positions.Length; i++)
            {
                module.Positions[i] = ToGame(positions[i]);
                module.Normals[i] = ToGame(normals[i]);
                Vector3 t = ToGame(tangents[i]);
                module.Tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
                module.Along[i] = i < reveal.Count ? reveal[i].x : 0f;
                module.Close[i] = i < closeXY.Count && i < closeZ.Count ? new Vector3(closeXY[i].x, closeXY[i].y, closeZ[i].x) : Vector3.zero;
                module.Length = Mathf.Max(module.Length, module.Along[i]);
            }

            if (module.Colours == null || module.Colours.Length != positions.Length)
            {
                module.Colours = new Color32[positions.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    module.Colours[i] = new Color32(0, 0, 0, 255);
                }
            }

            return module;
        }

        private static Vector3 ToGame(Vector3 v) => new Vector3(-v.x, -v.z, -v.y);

        private sealed class MeshData
        {
            public readonly List<Vector3> Positions = new List<Vector3>(16384);
            public readonly List<Vector3> Normals = new List<Vector3>(16384);
            public readonly List<Vector4> Tangents = new List<Vector4>(16384);
            public readonly List<Vector2> Uvs = new List<Vector2>(16384);
            public readonly List<Vector2> Reveal = new List<Vector2>(16384);
            public readonly List<Vector2> CloseXY = new List<Vector2>(16384);
            public readonly List<Vector2> CloseZ = new List<Vector2>(16384);
            public readonly List<Color32> Colours = new List<Color32>(16384);
            public readonly List<int> Rock = new List<int>(32768);
            public readonly List<int> Punch = new List<int>(2048);

            public Mesh ToMesh()
            {
                var mesh = new Mesh { name = "Fissure", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(Positions);
                mesh.SetNormals(Normals);
                mesh.SetTangents(Tangents);
                mesh.SetUVs(0, Uvs);
                mesh.SetUVs(1, Reveal);
                mesh.SetUVs(2, CloseXY);
                mesh.SetUVs(3, CloseZ);
                mesh.SetColors(Colours);
                // The opening twice: once for each stage of the depth punch.
                mesh.subMeshCount = 3;
                mesh.SetTriangles(Rock, 0);
                mesh.SetTriangles(Punch, 1);
                mesh.SetTriangles(Punch, 2);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
