using System.Collections.Generic;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Builds an armour plate: a curved shell whose crown sits at the origin's +Z and whose edges curl back round the limb
    /// it covers. Three submeshes: the face, a raised ridge down its middle for the glowing trim, and the frame (the
    /// face's outer band, the rim and the underside) in the suit's dark colour, so the plate reads as a framed panel and
    /// not as a bare shape. X runs across the limb, Y along it. Meshes are cached by size.
    /// </summary>
    public static class ArmourPlateMesh
    {
        private const int Across = 10;
        private const int Along = 6;

        // The plate's corners are cut away round, so it reads as a pressed shell and not as a bent rectangle.
        private const float CornerPower = 4f;

        // How much less the plate curls along the limb than across it.
        private const float AlongCurl = 0.3f;
        private const float RidgeHalfWidth = 0.1f;
        private const float RidgeLength = 0.72f;
        private const float RidgeLift = 0.004f;

        private static readonly Dictionary<Vector4, Mesh> Cache = new Dictionary<Vector4, Mesh>();

        /// <summary>The plate for a size (x width, y length, z bulge) and a rim thickness; submesh 0 is the face, 1 the ridge, 2 the frame.</summary>
        public static Mesh For(Vector3 size, float thickness)
        {
            var key = new Vector4(size.x, size.y, size.z, thickness);
            if (Cache.TryGetValue(key, out Mesh cached) && cached != null)
            {
                return cached;
            }

            Mesh mesh = Build(size, thickness);
            Cache[key] = mesh;
            return mesh;
        }

        private static Mesh Build(Vector3 size, float thickness)
        {
            var vertices = new List<Vector3>();
            var face = new List<int>();
            var ridge = new List<int>();
            var frame = new List<int>();

            // The face and the frame band each get their own sheet, so the colour changes on a crisp line.
            int front = AddSheet(vertices, size, thickness, 1f, 1f);
            int band = AddSheet(vertices, size, thickness, 1f, 1f);
            int back = AddSheet(vertices, size, 0f, 1f, 1f);
            AddSheetTriangles(face, null, front, outward: true);
            AddSheetTriangles(null, frame, band, outward: true);
            AddSheetTriangles(frame, frame, back, outward: false);
            AddRim(vertices, frame, size, thickness);

            int ridgeStart = AddSheet(vertices, size, thickness + RidgeLift, RidgeHalfWidth, RidgeLength);
            AddSheetTriangles(ridge, ridge, ridgeStart, outward: true);

            var mesh = new Mesh { name = "ArmourPlate", subMeshCount = 3 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(face, 0);
            mesh.SetTriangles(ridge, 1);
            mesh.SetTriangles(frame, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // A sheet of (Across + 1) x (Along + 1) points over the plate's outline, shrunk to a share of it for the ridge.
        private static int AddSheet(List<Vector3> vertices, Vector3 size, float lift, float acrossShare, float alongShare)
        {
            int start = vertices.Count;
            for (int j = 0; j <= Along; j++)
            {
                float v = Mathf.Lerp(-1f, 1f, j / (float)Along) * alongShare;
                for (int i = 0; i <= Across; i++)
                {
                    float u = Mathf.Lerp(-1f, 1f, i / (float)Across) * acrossShare;
                    vertices.Add(Point(size, u, v, lift));
                }
            }

            return start;
        }

        // The outline is a superellipse: the sheet's square of (u, v) is pulled in at its corners.
        private static Vector3 Point(Vector3 size, float u, float v, float lift)
        {
            float reach = Mathf.Pow(Mathf.Pow(Mathf.Abs(u), CornerPower) + Mathf.Pow(Mathf.Abs(v), CornerPower), 1f / CornerPower);
            float square = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
            float pull = reach > 0.0001f ? square / reach : 1f;
            float x = u * pull;
            float y = v * pull;
            float z = lift - size.z * (x * x + AlongCurl * y * y);
            return new Vector3(x * size.x * 0.5f, y * size.y * 0.5f, z);
        }

        // The sheet's quads go to one list in its middle and to another in its outer band; a null list drops that part.
        private static void AddSheetTriangles(List<int> inner, List<int> outer, int start, bool outward)
        {
            int row = Across + 1;
            for (int j = 0; j < Along; j++)
            {
                for (int i = 0; i < Across; i++)
                {
                    bool edge = i == 0 || j == 0 || i == Across - 1 || j == Along - 1;
                    List<int> triangles = edge ? outer : inner;
                    if (triangles == null)
                    {
                        continue;
                    }

                    int a = start + j * row + i;
                    int b = a + 1;
                    int c = a + row;
                    int d = c + 1;
                    if (outward)
                    {
                        triangles.Add(a); triangles.Add(b); triangles.Add(c);
                        triangles.Add(b); triangles.Add(d); triangles.Add(c);
                    }
                    else
                    {
                        triangles.Add(a); triangles.Add(c); triangles.Add(b);
                        triangles.Add(b); triangles.Add(c); triangles.Add(d);
                    }
                }
            }
        }

        // The rim joins the front and back sheets with its own vertices, so the edge stays crisp under smooth normals.
        private static void AddRim(List<Vector3> vertices, List<int> triangles, Vector3 size, float thickness)
        {
            var outline = new List<Vector2>();
            for (int i = 0; i <= Across; i++) { outline.Add(new Vector2(Mathf.Lerp(-1f, 1f, i / (float)Across), -1f)); }
            for (int j = 1; j <= Along; j++) { outline.Add(new Vector2(1f, Mathf.Lerp(-1f, 1f, j / (float)Along))); }
            for (int i = Across - 1; i >= 0; i--) { outline.Add(new Vector2(Mathf.Lerp(-1f, 1f, i / (float)Across), 1f)); }
            for (int j = Along - 1; j >= 1; j--) { outline.Add(new Vector2(-1f, Mathf.Lerp(-1f, 1f, j / (float)Along))); }

            int start = vertices.Count;
            foreach (Vector2 point in outline)
            {
                vertices.Add(Point(size, point.x, point.y, thickness));
                vertices.Add(Point(size, point.x, point.y, 0f));
            }

            for (int i = 0; i < outline.Count; i++)
            {
                int a = start + i * 2;
                int b = a + 1;
                int c = start + (i + 1) % outline.Count * 2;
                int d = c + 1;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(c); triangles.Add(b); triangles.Add(d);
            }
        }
    }
}
