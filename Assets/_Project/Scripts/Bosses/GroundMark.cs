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

        private const float Lift = 0.03f;

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
        public static MeshRenderer Strip(Transform parent, string name, IReadOnlyList<Vector3> points, float width, float tileLength, Material material, bool lips)
        {
            int count = points.Count;
            int columns = Across.Length;
            var vertices = new Vector3[count * columns];
            var uvs = new Vector2[vertices.Length];
            var path = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];
            var triangles = new List<int>((count - 1) * (columns - 1) * 6);
            float along = 0f;
            float start = Random.value;
            float lipHeight = lips && material != null && material.HasProperty("_LipHeight") ? material.GetFloat("_LipHeight") : 0f;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    along += Vector3.Distance(points[i - 1], points[i]);
                }

                Vector3 side = Side(points, i);
                for (int c = 0; c < columns; c++)
                {
                    int v = i * columns + c;
                    vertices[v] = points[i] + side * (Across[c] * width * 0.5f) + Vector3.up * Lift;
                    uvs[v] = new Vector2(start + along / Mathf.Max(0.01f, tileLength), (Across[c] + 1f) * 0.5f);
                    path[v] = new Vector2(along, lips ? Rise[c] : 0f);
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
            var mesh = new Mesh { name = "GroundMark" + name };
            mesh.vertices = new[] { new Vector3(-hw, Lift, -hl), new Vector3(hw, Lift, -hl), new Vector3(hw, Lift, hl), new Vector3(-hw, Lift, hl) };
            mesh.uv = new[] { new Vector2(u0, 0f), new Vector2(u0, 1f), new Vector2(u1, 1f), new Vector2(u1, 0f) };
            mesh.uv2 = new[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return Place(parent, name, mesh, material);
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
