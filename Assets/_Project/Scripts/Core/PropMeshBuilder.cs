using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Builds small organic shapes from code: tubes that taper along a curve (stalks, tendrils, crystals) and
    /// bumpy ellipsoids (rocks, caps, eggs). Every shape goes into one of a fixed number of submeshes, so a prop is
    /// one mesh with a body material and a few glow materials.
    /// </summary>
    public sealed class PropMeshBuilder
    {
        public const int SubmeshCount = 4;

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<int>[] _triangles = new List<int>[SubmeshCount];

        public PropMeshBuilder()
        {
            for (int i = 0; i < SubmeshCount; i++)
            {
                _triangles[i] = new List<int>();
            }
        }

        /// <summary>A tube along a curve. Radii taper per point; a radius of zero at the end closes it to a point.</summary>
        public void AddTube(Vector3[] path, float[] radii, int sides, int submesh, bool flat)
        {
            int rings = path.Length;
            var ringPositions = new Vector3[rings * sides];
            var ringNormals = new Vector3[rings * sides];
            Vector3 reference = Vector3.forward;

            for (int r = 0; r < rings; r++)
            {
                Vector3 tangent = r < rings - 1 ? path[r + 1] - path[r] : path[r] - path[r - 1];
                tangent.Normalize();
                if (Mathf.Abs(Vector3.Dot(tangent, reference)) > 0.95f)
                {
                    reference = Vector3.right;
                }

                Vector3 side = Vector3.Cross(tangent, reference).normalized;
                Vector3 up = Vector3.Cross(side, tangent);
                for (int s = 0; s < sides; s++)
                {
                    float angle = s / (float)sides * Mathf.PI * 2f;
                    Vector3 direction = Mathf.Cos(angle) * side + Mathf.Sin(angle) * up;
                    ringPositions[r * sides + s] = path[r] + direction * radii[r];
                    ringNormals[r * sides + s] = direction;
                }
            }

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int next = (s + 1) % sides;
                    int a = r * sides + s;
                    int b = r * sides + next;
                    int c = (r + 1) * sides + s;
                    int d = (r + 1) * sides + next;
                    Emit(ringPositions, ringNormals, new[] { a, c, b }, submesh, flat);
                    Emit(ringPositions, ringNormals, new[] { b, c, d }, submesh, flat);
                }
            }

            // Cap the end with a point (or a flat lid when it still has width).
            Vector3 end = path[rings - 1];
            Vector3 axis = (path[rings - 1] - path[rings - 2]).normalized;
            Vector3 tip = end + axis * Mathf.Max(radii[rings - 1], 0.02f);
            int last = (rings - 1) * sides;
            for (int s = 0; s < sides; s++)
            {
                int next = (s + 1) % sides;
                var cap = new[] { ringPositions[last + s], tip, ringPositions[last + next] };
                var capNormals = new[] { ringNormals[last + s], axis, ringNormals[last + next] };
                Emit(cap, capNormals, new[] { 0, 1, 2 }, submesh, flat);
            }
        }

        /// <summary>
        /// A bumpy ellipsoid. <paramref name="phiMax"/> is how much of it from the top is built (pi = all of it),
        /// which turns it into a cap or a dome.
        /// </summary>
        public void AddBlob(Vector3 center, Vector3 radii, int rows, int columns, float bump, int seed, int submesh, bool flat, float phiMax = Mathf.PI)
        {
            var positions = new Vector3[(rows + 1) * columns];
            var normals = new Vector3[positions.Length];

            for (int r = 0; r <= rows; r++)
            {
                float phi = r / (float)rows * phiMax;
                for (int c = 0; c < columns; c++)
                {
                    float theta = c / (float)columns * Mathf.PI * 2f;
                    var direction = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    float noise = Mathf.PerlinNoise(direction.x * 1.9f + seed * 7.1f, direction.y * 1.7f + direction.z * 1.5f + seed * 3.3f);
                    float scale = 1f + (noise - 0.5f) * 2f * bump;
                    positions[r * columns + c] = center + Vector3.Scale(direction, radii) * scale;
                    normals[r * columns + c] = new Vector3(direction.x / radii.x, direction.y / radii.y, direction.z / radii.z).normalized;
                }
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int next = (c + 1) % columns;
                    int a = r * columns + c;
                    int b = r * columns + next;
                    int d = (r + 1) * columns + c;
                    int e = (r + 1) * columns + next;
                    Emit(positions, normals, new[] { a, b, d }, submesh, flat);
                    Emit(positions, normals, new[] { b, e, d }, submesh, flat);
                }
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(_positions);
            mesh.SetNormals(_normals);
            mesh.subMeshCount = SubmeshCount;
            for (int i = 0; i < SubmeshCount; i++)
            {
                mesh.SetTriangles(_triangles[i], i);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        // Every triangle gets its own vertices: that is what makes flat shading possible, and smooth shading
        // still looks smooth because the vertex normals are shared in value.
        private void Emit(Vector3[] positions, Vector3[] normals, int[] triangle, int submesh, bool flat)
        {
            Vector3 a = positions[triangle[0]];
            Vector3 b = positions[triangle[1]];
            Vector3 c = positions[triangle[2]];
            Vector3 face = Vector3.Cross(b - a, c - a).normalized;

            // Winding follows the outward normals, so the generators do not have to get it right by hand.
            Vector3 outward = normals[triangle[0]] + normals[triangle[1]] + normals[triangle[2]];
            int second = 1;
            int third = 2;
            if (Vector3.Dot(face, outward) < 0f)
            {
                face = -face;
                second = 2;
                third = 1;
            }

            int[] order = { 0, second, third };
            for (int i = 0; i < 3; i++)
            {
                int source = triangle[order[i]];
                _triangles[submesh].Add(_positions.Count);
                _positions.Add(positions[source]);
                _normals.Add(flat ? face : normals[source]);
            }
        }
    }
}
