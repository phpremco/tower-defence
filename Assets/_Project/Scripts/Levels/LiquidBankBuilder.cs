using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// Sweeps the continent's bendable bank segments (ArtSource README round 24b) along every pool's smoothed shore: the
    /// loop is cut into whole segments stretched to fit, the variants alternate (never the same twice in a row) and each
    /// vertex is bent onto the shore's frame once at level build, so the bank follows the round outline without kinks.
    /// One mesh per pool (a sub-mesh per variant), shore rocks on most joints to hide the seams. View only.
    /// </summary>
    public static class LiquidBankBuilder
    {
        // The segment's dry side is +z and its liquid side -z, so walking +x the liquid lies on the right: the shore loops
        // keep the liquid on the left, so the sweep walks them backwards.
        private const float SegmentLength = 1f;
        private const float AccentDryOffset = 0.08f;
        private const float AccentScaleMin = 0.8f;
        private const float AccentScaleSpread = 0.4f;
        private const float DryProbe = 0.25f;
        private const int MinSegments = 3;

        /// <summary>Lays the banks of every pool; returns how many segments were swept (for tests and reports).</summary>
        public static int Build(TerrainField field, EnvironmentSet set, EnvironmentInstancer instancer, Transform parent, bool low)
        {
            var variants = new List<EnvironmentMesh>();
            foreach (EnvironmentMesh sweep in set.BankSweeps)
            {
                if (sweep != null && sweep.IsValid)
                {
                    variants.Add(sweep);
                }
            }

            if (variants.Count == 0)
            {
                return 0;
            }

            var sources = new SegmentSource[variants.Count];
            for (int v = 0; v < variants.Count; v++)
            {
                sources[v] = new SegmentSource(variants[v]);
            }

            int swept = 0;
            for (int p = 0; p < field.Pools.Count; p++)
            {
                LiquidShape pool = field.Pools[p];
                var combined = new CombinedBank(variants.Count);
                for (int l = 0; l < pool.Loops.Count; l++)
                {
                    swept += Sweep(Reversed(pool.Loops[l]), field, sources, combined, set, instancer, low, p * 97 + l);
                }

                Place(combined, variants, pool, parent, low);
            }

            return swept;
        }

        private static Vector2[] Reversed(Vector2[] loop)
        {
            var result = new Vector2[loop.Length];
            for (int i = 0; i < loop.Length; i++)
            {
                result[i] = loop[loop.Length - 1 - i];
            }

            return result;
        }

        private static int Sweep(Vector2[] loop, TerrainField field, SegmentSource[] sources, CombinedBank combined, EnvironmentSet set, EnvironmentInstancer instancer, bool low, int seed)
        {
            var frame = new LoopFrame(loop);
            int count = Mathf.Max(MinSegments, Mathf.RoundToInt(frame.Perimeter / SegmentLength));
            float length = frame.Perimeter / count;
            int previous = -1;
            int first = -1;
            for (int i = 0; i < count; i++)
            {
                int variant = PickVariant(sources.Length, seed, i, previous, i == count - 1 ? first : -1);
                first = i == 0 ? variant : first;
                previous = variant;
                Bend(sources[variant], combined, variant, frame, field, i, length);
                MaybeAccent(set, instancer, frame, field, i * length, TerrainNoise.Hash(seed, i, 71), low, seed, i);
            }

            for (int k = 0; k < set.LooseBankAccents; k++)
            {
                float u = TerrainNoise.Hash(seed, k, 73) * frame.Perimeter;
                PlaceAccent(set, instancer, frame, field, u, low, seed, 1000 + k);
            }

            return count;
        }

        // A random variant, never the one before it (nor, for the last segment, the first one it closes on).
        private static int PickVariant(int variants, int seed, int index, int previous, int closing)
        {
            if (variants == 1)
            {
                return 0;
            }

            int pick = (int)(TerrainNoise.Hash(seed, index, 70) * variants) % variants;
            for (int tries = 0; tries < variants && (pick == previous || pick == closing); tries++)
            {
                pick = (pick + 1) % variants;
            }

            return pick;
        }

        private static void Bend(SegmentSource source, CombinedBank combined, int variant, LoopFrame frame, TerrainField field, int index, float length)
        {
            int start = combined.Vertices.Count;
            for (int v = 0; v < source.Positions.Length; v++)
            {
                Vector3 local = source.Positions[v];
                float u = (index + local.x / SegmentLength + 0.5f) * length;
                frame.Sample(u, out Vector2 point, out Vector2 along, out Vector2 dry);
                float ground = Mathf.Max(0f, field.HeightAt(point.x + dry.x * DryProbe, point.y + dry.y * DryProbe));
                Vector3 tangent3 = new Vector3(along.x, 0f, along.y);
                Vector3 dry3 = new Vector3(dry.x, 0f, dry.y);
                combined.Vertices.Add(new Vector3(point.x, ground, point.y) + dry3 * local.z + Vector3.up * local.y);
                Vector3 n = source.Normals[v];
                combined.Normals.Add((tangent3 * n.x + Vector3.up * n.y + dry3 * n.z).normalized);
                Vector4 t = source.Tangents[v];
                Vector3 bentTangent = (tangent3 * t.x + Vector3.up * t.y + dry3 * t.z).normalized;
                combined.Tangents.Add(new Vector4(bentTangent.x, bentTangent.y, bentTangent.z, t.w));
                combined.Uv0.Add(source.Uv0[v]);
                combined.Uv1.Add(source.Uv1.Length > v ? new Vector2(source.Uv1[v].x + index, source.Uv1[v].y) : Vector2.zero);
            }

            List<int> triangles = combined.Triangles[variant];
            for (int i = 0; i < source.Triangles.Length; i++)
            {
                triangles.Add(start + source.Triangles[i]);
            }
        }

        private static void MaybeAccent(EnvironmentSet set, EnvironmentInstancer instancer, LoopFrame frame, TerrainField field, float u, float roll, bool low, int seed, int index)
        {
            if (roll < set.BankAccentShare)
            {
                PlaceAccent(set, instancer, frame, field, u, low, seed, index);
            }
        }

        private static void PlaceAccent(EnvironmentSet set, EnvironmentInstancer instancer, LoopFrame frame, TerrainField field, float u, bool low, int seed, int index)
        {
            EnvironmentMesh accent = set.BankAccent;
            if (accent == null || !accent.IsValid || instancer == null)
            {
                return;
            }

            frame.Sample(u, out Vector2 point, out Vector2 _, out Vector2 dry);
            Vector2 spot = point + dry * (TerrainNoise.Hash(seed, index, 74) * AccentDryOffset);
            float ground = Mathf.Max(0f, field.HeightAt(spot.x, spot.y));
            float yaw = TerrainNoise.Hash(seed, index, 75) * 360f;
            float scale = AccentScaleMin + TerrainNoise.Hash(seed, index, 76) * AccentScaleSpread;
            instancer.AddDecor(accent, Matrix4x4.TRS(new Vector3(spot.x, ground, spot.y), Quaternion.Euler(0f, yaw, 0f), Vector3.one * scale), low);
        }

        private static void Place(CombinedBank combined, List<EnvironmentMesh> variants, LiquidShape pool, Transform parent, bool low)
        {
            if (combined.Vertices.Count == 0)
            {
                return;
            }

            var mesh = new Mesh { name = $"Bank_{pool.Kind}", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(combined.Vertices);
            mesh.SetNormals(combined.Normals);
            mesh.SetTangents(combined.Tangents);
            mesh.SetUVs(0, combined.Uv0);
            mesh.SetUVs(1, combined.Uv1);
            mesh.subMeshCount = variants.Count;
            var materials = new Material[variants.Count];
            for (int v = 0; v < variants.Count; v++)
            {
                mesh.SetTriangles(combined.Triangles[v], v);
                materials[v] = variants[v].MaterialFor(low);
            }

            mesh.RecalculateBounds();
            var go = new GameObject(mesh.name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = variants[0].CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        // A segment's vertices in its own frame (the FBX node's axis change applied): x along, y up, z to the dry side.
        private sealed class SegmentSource
        {
            public SegmentSource(EnvironmentMesh model)
            {
                Mesh mesh = model.Mesh;
                Matrix4x4 node = model.Node;
                Vector3[] positions = mesh.vertices;
                Vector3[] normals = mesh.normals;
                Vector4[] tangents = mesh.tangents;
                Positions = new Vector3[positions.Length];
                Normals = new Vector3[positions.Length];
                Tangents = new Vector4[positions.Length];
                for (int i = 0; i < positions.Length; i++)
                {
                    Positions[i] = node.MultiplyPoint3x4(positions[i]);
                    Normals[i] = normals.Length > i ? node.MultiplyVector(normals[i]).normalized : Vector3.up;
                    Vector4 t = tangents.Length > i ? tangents[i] : new Vector4(1f, 0f, 0f, 1f);
                    Vector3 turned = node.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                    Tangents[i] = new Vector4(turned.x, turned.y, turned.z, t.w);
                }

                Uv0 = mesh.uv;
                Uv1 = mesh.uv2;
                Triangles = mesh.triangles;
            }

            public Vector3[] Positions { get; }
            public Vector3[] Normals { get; }
            public Vector4[] Tangents { get; }
            public Vector2[] Uv0 { get; }
            public Vector2[] Uv1 { get; }
            public int[] Triangles { get; }
        }

        private sealed class CombinedBank
        {
            public CombinedBank(int variants)
            {
                Triangles = new List<int>[variants];
                for (int i = 0; i < variants; i++)
                {
                    Triangles[i] = new List<int>();
                }
            }

            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector4> Tangents = new List<Vector4>();
            public readonly List<Vector2> Uv0 = new List<Vector2>();
            public readonly List<Vector2> Uv1 = new List<Vector2>();
            public List<int>[] Triangles { get; }
        }
    }
}
