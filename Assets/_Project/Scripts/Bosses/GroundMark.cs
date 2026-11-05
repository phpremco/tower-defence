using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Bosses
{
    /// <summary>
    /// Builds the meshes of the boss ground marks (<see cref="BossFxArt"/>, shader <c>Game/BossDecal</c>): a strip along a
    /// path (crack, furrow, sinking road) or one flat piece (a trail patch, a frost cell). A strip's UV.x runs along it in
    /// tiles from a random start, so neighbouring marks never repeat in step; UV2.x is the distance along the path (the
    /// crack opens behind its head) and UV2.y how high a vertex rises as it opens (the heaved lips beside the fissure).
    /// </summary>
    public static class GroundMark
    {
        public static readonly int Progress = Shader.PropertyToID("_Progress");
        public static readonly int OpenDistance = Shader.PropertyToID("_OpenDistance");
        public static readonly int OpenRamp = Shader.PropertyToID("_OpenRamp");
        public static readonly int Fade = Shader.PropertyToID("_Fade");
        public static readonly int GlowFade = Shader.PropertyToID("_GlowFade");
        public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        public static readonly int Tint = Shader.PropertyToID("_Tint");
        private static readonly int BaseMapSt = Shader.PropertyToID("_BaseMap_ST");

        /// <summary>How long a mark's rounded ends are, as a multiple of its own end length (1: as built).</summary>
        public static readonly int EndTaper = Shader.PropertyToID("_EndTaper");

        private const float Lift = 0.03f;

        // Remco 2026-11-05: no ground mark ends square. A strip rounds off over this many of its widths at each end (at most
        // this share of its length); a piece over this share of its half length, so the pieces of a trail, a spacing apart
        // and overlapping, still cover each other completely between their centres.
        private const float StripEndWidths = 0.9f;
        private const float StripEndShare = 0.3f;
        private const float PieceEndShare = 0.47f;
        private const float RoundStep = 0.5f;

        // Across the strip: where each column lies (share of the half width) and how much it rises with the lips. The
        // lips stand just outside the fissure so the dark middle reads as a gap between two heaved edges.
        private static readonly float[] Across = { -1f, -0.62f, -0.3f, 0f, 0.3f, 0.62f, 1f };
        private static readonly float[] Rise = { 0f, 1f, 0.35f, 0f, 0.35f, 1f, 0f };

        /// <summary>A mark's own copy of its material, or null when the art is not registered or has no such mark.</summary>
        public static Material MaterialFor(BossFxArt.Mark mark)
        {
            BossFxArt art = BossFxArt.Active;
            Material shared = art != null ? art.Material(mark) : null;
            return shared != null ? new Material(shared) : null;
        }

        /// <summary>A strip along world points (already on the ground), its tiles starting at a random U.</summary>
        public static MeshRenderer Strip(Transform parent, string name, IReadOnlyList<Vector3> points, float width, float tileLength, Material material, bool lips) =>
            Strip(parent, name, points, width, tileLength, material, lips, null, 0f);

        /// <summary>
        /// A strip whose width is scaled per point (<paramref name="widthScale"/>, null: even), its open distance counted from
        /// <paramref name="alongStart"/> (a branch opens with the line it grows from). UV3 holds each vertex's offset from the
        /// centre line, so the shader can pinch the strip to a point at its open end.
        /// </summary>
        public static MeshRenderer Strip(Transform parent, string name, IReadOnlyList<Vector3> points, float width, float tileLength, Material material, bool lips, IReadOnlyList<float> widthScale, float alongStart)
        {
            int count = points.Count;
            int columns = Across.Length;
            var vertices = new Vector3[count * columns];
            var uvs = new Vector2[vertices.Length];
            var path = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];
            var offsets = new Vector2[vertices.Length];
            var colours = new Color32[vertices.Length];
            var ends = new Vector3[vertices.Length];
            var triangles = new List<int>((count - 1) * (columns - 1) * 6);
            float total = 0f;
            for (int i = 1; i < count; i++)
            {
                total += Vector3.Distance(points[i - 1], points[i]);
            }

            float endLength = Mathf.Max(0.01f, Mathf.Min(width * StripEndWidths, total * StripEndShare));
            float along = alongStart;
            float start = Random.value;
            float lipHeight = lips && material != null && material.HasProperty("_LipHeight") ? material.GetFloat("_LipHeight") : 0f;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    along += Vector3.Distance(points[i - 1], points[i]);
                }

                Vector3 side = Side(points, i);
                float half = width * 0.5f * (widthScale != null ? widthScale[i] : 1f);
                for (int c = 0; c < columns; c++)
                {
                    int v = i * columns + c;
                    Vector3 offset = side * (Across[c] * half);
                    vertices[v] = points[i] + offset + Vector3.up * Lift;
                    offsets[v] = new Vector2(offset.x, offset.z);
                    colours[v] = new Color32(255, 255, 255, 255);
                    uvs[v] = new Vector2(start + along / Mathf.Max(0.01f, tileLength), (Across[c] + 1f) * 0.5f);
                    path[v] = new Vector2(along, lips ? Rise[c] : 0f);
                    float fromStart = along - alongStart;
                    ends[v] = new Vector3(Mathf.Min(fromStart, total - fromStart) / endLength, Across[c], 1f);
                    normals[v] = LipNormal(side, c, width, lipHeight);
                }
            }

            for (int i = 0; i < count - 1; i++)
            {
                for (int c = 0; c < columns - 1; c++)
                {
                    int a = i * columns + c;
                    int b = a + columns;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(b + 1);
                    triangles.Add(a);
                    triangles.Add(b + 1);
                    triangles.Add(a + 1);
                }
            }

            var mesh = new Mesh { name = "GroundMark" + name };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.uv2 = path;
            mesh.normals = normals;
            mesh.uv3 = offsets;
            mesh.colors32 = colours;
            mesh.SetUVs(3, ends);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            Bounds bounds = mesh.bounds;
            bounds.Expand(new Vector3(0f, lipHeight * 2f + 0.5f, 0f));
            mesh.bounds = bounds;
            return Place(parent, name, mesh, material);
        }

        /// <summary>One flat piece centred on a point, its length along a direction, starting at a random U along its strip.</summary>
        public static MeshRenderer Piece(Transform parent, string name, float length, float width, float tileLength, Material material)
        {
            float u0 = Random.value;
            float u1 = u0 + length / Mathf.Max(0.01f, tileLength);
            float hl = length * 0.5f;
            float hw = width * 0.5f;
            // Three rows along it (start, middle, end), so the distance to the nearer end the shader rounds the ends off by
            // rises to the middle and falls again (UV3: that distance in end lengths, the place across, a flag).
            float endLength = Mathf.Max(0.01f, hl * PieceEndShare);
            var vertices = new Vector3[6];
            var uvs = new Vector2[6];
            var ends = new Vector3[6];
            for (int row = 0; row < 3; row++)
            {
                float z = (row - 1) * hl;
                float u = Mathf.Lerp(u0, u1, row * 0.5f);
                float fromEnd = row == 1 ? hl : 0f;
                for (int side = 0; side < 2; side++)
                {
                    int v = row * 2 + side;
                    vertices[v] = new Vector3(side == 0 ? -hw : hw, Lift, z);
                    uvs[v] = new Vector2(u, side);
                    ends[v] = new Vector3(fromEnd / endLength, side == 0 ? -1f : 1f, 1f);
                }
            }

            var mesh = new Mesh { name = "GroundMark" + name };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.uv2 = new Vector2[6];
            mesh.SetUVs(3, ends);
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 3, 1, 0, 2, 3, 2, 5, 3, 2, 4, 5 };
            mesh.RecalculateBounds();
            return Place(parent, name, mesh, material);
        }

        /// <summary>
        /// A path with its corners rounded (Remco 2026-11-05: no hard right-angle bends in a ground mark): resampled every
        /// half metre and averaged over about <paramref name="radius"/> metres either side, three times; the ends stay put.
        /// </summary>
        public static List<Vector3> Rounded(IReadOnlyList<Vector3> points, float radius)
        {
            var result = new List<Vector3>();
            if (points == null || points.Count < 3)
            {
                if (points != null)
                {
                    result.AddRange(points);
                }

                return result;
            }

            float length = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector3.Distance(points[i - 1], points[i]);
            }

            int count = Mathf.Max(2, Mathf.CeilToInt(length / RoundStep) + 1);
            int segment = 1;
            float segmentStart = 0f;
            for (int k = 0; k < count; k++)
            {
                float target = length * k / (count - 1);
                while (segment < points.Count - 1 && segmentStart + Vector3.Distance(points[segment - 1], points[segment]) < target)
                {
                    segmentStart += Vector3.Distance(points[segment - 1], points[segment]);
                    segment++;
                }

                float span = Vector3.Distance(points[segment - 1], points[segment]);
                result.Add(Vector3.Lerp(points[segment - 1], points[segment], span > 0.0001f ? Mathf.Clamp01((target - segmentStart) / span) : 1f));
            }

            int halfWindow = Mathf.Max(1, Mathf.RoundToInt(radius / RoundStep));
            for (int pass = 0; pass < 3; pass++)
            {
                var next = new List<Vector3>(result.Count);
                for (int i = 0; i < result.Count; i++)
                {
                    int reach = Mathf.Min(halfWindow, Mathf.Min(i, result.Count - 1 - i));
                    Vector3 sum = Vector3.zero;
                    for (int k = -reach; k <= reach; k++)
                    {
                        sum += result[i + k];
                    }

                    next.Add(sum / (reach * 2 + 1));
                }

                result = next;
            }

            return result;
        }

        /// <summary>Points a piece's renderer at one cell of a 2 x 2 atlas (the frost) through a property block.</summary>
        public static void UseCell(Renderer renderer, MaterialPropertyBlock block, int cell)
        {
            renderer.GetPropertyBlock(block);
            block.SetVector(BaseMapSt, new Vector4(0.5f, 0.5f, (cell % 2) * 0.5f, (cell / 2 % 2) * 0.5f));
            renderer.SetPropertyBlock(block);
        }

        private static MeshRenderer Place(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        // A lip leans away from the fissure on its outer slope and towards it on its inner one, so the light picks it out.
        private static Vector3 LipNormal(Vector3 side, int column, float width, float lip)
        {
            if (lip <= 0f || column == 0 || column == Across.Length - 1 || column == Across.Length / 2)
            {
                return Vector3.up;
            }

            float sign = Across[column] < 0f ? -1f : 1f;
            bool outer = Mathf.Abs(Across[column]) > 0.5f;
            float slope = lip / (width * 0.2f);
            Vector3 lean = side * (sign * (outer ? 0.4f : -0.9f) * slope);
            return (Vector3.up + lean).normalized;
        }

        private static Vector3 Side(IReadOnlyList<Vector3> points, int i)
        {
            Vector3 incoming = i > 0 ? points[i] - points[i - 1] : Vector3.zero;
            Vector3 outgoing = i < points.Count - 1 ? points[i + 1] - points[i] : Vector3.zero;
            Vector3 along = incoming.normalized + outgoing.normalized;
            along.y = 0f;
            return along.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, along.normalized) : Vector3.right;
        }
    }
}
