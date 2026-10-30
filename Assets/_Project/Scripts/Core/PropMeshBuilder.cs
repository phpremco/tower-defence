using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Builds small organic shapes from code: tubes that taper along a curve (stalks, tendrils, crystals), bumpy
    /// ellipsoids (rocks, caps, eggs), boxes and flat blades. Every shape goes into one of a fixed number of
    /// submeshes, so a prop is one mesh with a body material and a few glow materials. Shapes carry texture
    /// coordinates that wrap around tubes and blobs, so tiling textures can dress them.
    /// </summary>
    public sealed class PropMeshBuilder
    {
        public const int SubmeshCount = 4;

        private const float TubeTextureAround = 2f;
        private const float TubeTexturePerUnit = 2.5f;
        private const float BlobTextureAround = 3f;
        private const float BlobTextureDown = 1.5f;
        private const float BoxTexturePerUnit = 2f;
        private const float BladeTexturePerUnit = 2f;
        private const float PointCapLength = 0.02f;

        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int>[] _triangles;

        /// <param name="submeshCount">How many material slots the mesh gets; four is the shared machine and creature palette.</param>
        public PropMeshBuilder(int submeshCount = SubmeshCount)
        {
            _triangles = new List<int>[submeshCount];
            for (int i = 0; i < submeshCount; i++)
            {
                _triangles[i] = new List<int>();
            }
        }

        /// <summary>A tube along a curve. Radii taper per point; a radius of zero at the end closes it to a point.</summary>
        public void AddTube(Vector3[] path, float[] radii, int sides, int submesh, bool flat)
        {
            int rings = path.Length;
            int columns = sides + 1;
            var ringPositions = new Vector3[rings * columns];
            var ringNormals = new Vector3[rings * columns];
            var ringUvs = new Vector2[rings * columns];
            Vector3 reference = Vector3.forward;
            float along = 0f;

            for (int r = 0; r < rings; r++)
            {
                Vector3 tangent = r < rings - 1 ? path[r + 1] - path[r] : path[r] - path[r - 1];
                if (r > 0)
                {
                    along += Vector3.Distance(path[r], path[r - 1]);
                }

                tangent.Normalize();
                if (Mathf.Abs(Vector3.Dot(tangent, reference)) > 0.95f)
                {
                    reference = Vector3.right;
                }

                Vector3 side = Vector3.Cross(tangent, reference).normalized;
                Vector3 up = Vector3.Cross(side, tangent);
                for (int s = 0; s < columns; s++)
                {
                    float angle = s / (float)sides * Mathf.PI * 2f;
                    Vector3 direction = Mathf.Cos(angle) * side + Mathf.Sin(angle) * up;
                    ringPositions[r * columns + s] = path[r] + direction * radii[r];
                    ringNormals[r * columns + s] = direction;
                    ringUvs[r * columns + s] = new Vector2(s / (float)sides * TubeTextureAround, along * TubeTexturePerUnit);
                }
            }

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = r * columns + s;
                    int b = a + 1;
                    int c = (r + 1) * columns + s;
                    int d = c + 1;
                    Emit(ringPositions, ringNormals, ringUvs, new[] { a, c, b }, submesh, flat);
                    Emit(ringPositions, ringNormals, ringUvs, new[] { b, c, d }, submesh, flat);
                }
            }

            // Cap the end: a point when the tube has tapered to nothing, otherwise a flat lid.
            Vector3 end = path[rings - 1];
            Vector3 axis = (path[rings - 1] - path[rings - 2]).normalized;
            Vector3 tip = end + axis * (radii[rings - 1] <= 0f ? PointCapLength : 0f);
            int last = (rings - 1) * columns;
            for (int s = 0; s < sides; s++)
            {
                var cap = new[] { ringPositions[last + s], tip, ringPositions[last + s + 1] };
                var capNormals = new[] { ringNormals[last + s], axis, ringNormals[last + s + 1] };
                var capUvs = new[] { ringUvs[last + s], new Vector2(ringUvs[last + s].x + 0.5f / sides, ringUvs[last + s].y + 0.2f), ringUvs[last + s + 1] };
                Emit(cap, capNormals, capUvs, new[] { 0, 1, 2 }, submesh, flat);
            }
        }

        /// <summary>
        /// A bumpy ellipsoid. <paramref name="phiMax"/> is how much of it from the top is built (pi = all of it),
        /// which turns it into a cap or a dome.
        /// </summary>
        public void AddBlob(Vector3 center, Vector3 radii, int rows, int columns, float bump, int seed, int submesh, bool flat, float phiMax = Mathf.PI)
        {
            int stride = columns + 1;
            var positions = new Vector3[(rows + 1) * stride];
            var normals = new Vector3[positions.Length];
            var uvs = new Vector2[positions.Length];

            for (int r = 0; r <= rows; r++)
            {
                float phi = r / (float)rows * phiMax;
                for (int c = 0; c < stride; c++)
                {
                    float theta = c / (float)columns * Mathf.PI * 2f;
                    var direction = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    float noise = Mathf.PerlinNoise(direction.x * 1.9f + seed * 7.1f, direction.y * 1.7f + direction.z * 1.5f + seed * 3.3f);
                    float scale = 1f + (noise - 0.5f) * 2f * bump;
                    int i = r * stride + c;
                    positions[i] = center + Vector3.Scale(direction, radii) * scale;
                    normals[i] = new Vector3(direction.x / radii.x, direction.y / radii.y, direction.z / radii.z).normalized;
                    uvs[i] = new Vector2(c / (float)columns * BlobTextureAround, phi / Mathf.PI * BlobTextureDown);
                }
            }

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    int a = r * stride + c;
                    int b = a + 1;
                    int d = (r + 1) * stride + c;
                    int e = d + 1;
                    Emit(positions, normals, uvs, new[] { a, b, d }, submesh, flat);
                    Emit(positions, normals, uvs, new[] { b, e, d }, submesh, flat);
                }
            }
        }

        /// <summary>A flat-shaded box, optionally turned around the vertical axis. Hard edges suit machines and walls.</summary>
        public void AddBox(Vector3 center, Vector3 size, int submesh, float yawDegrees = 0f) =>
            AddBox(center, size, submesh, Quaternion.Euler(0f, yawDegrees, 0f));

        /// <summary>A flat-shaded box turned any way: tilted barrels, braces and plates.</summary>
        public void AddBox(Vector3 center, Vector3 size, int submesh, Quaternion yaw)
        {
            Vector3 h = size * 0.5f;
            var corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
                corners[i] = center + yaw * c;
            }

            // Each face lists its four corners; the outward normal comes from the box centre.
            int[][] faces =
            {
                new[] { 0, 1, 3, 2 }, new[] { 4, 6, 7, 5 }, new[] { 0, 4, 5, 1 },
                new[] { 2, 3, 7, 6 }, new[] { 0, 2, 6, 4 }, new[] { 1, 5, 7, 3 },
            };
            foreach (int[] face in faces)
            {
                Vector3 mid = (corners[face[0]] + corners[face[2]]) * 0.5f;
                Vector3 normal = (mid - center).normalized;
                var positions = new[] { corners[face[0]], corners[face[1]], corners[face[2]], corners[face[3]] };
                var normals = new[] { normal, normal, normal, normal };
                float width = Vector3.Distance(positions[0], positions[1]) * BoxTexturePerUnit;
                float height = Vector3.Distance(positions[1], positions[2]) * BoxTexturePerUnit;
                var uvs = new[] { new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(width, height), new Vector2(0f, height) };
                Emit(positions, normals, uvs, new[] { 0, 1, 2 }, submesh, true);
                Emit(positions, normals, uvs, new[] { 0, 2, 3 }, submesh, true);
            }
        }

        /// <summary>A ring of tube around <paramref name="axis"/>: coils, rims, magnetic rings and gear collars.</summary>
        public void AddTorus(Vector3 center, Vector3 axis, float radius, float thickness, int segments, int sides, int submesh, bool flat = false)
        {
            Vector3 up = axis.normalized;
            Vector3 reference = Mathf.Abs(Vector3.Dot(up, Vector3.forward)) > 0.95f ? Vector3.right : Vector3.forward;
            Vector3 a = Vector3.Cross(up, reference).normalized;
            Vector3 b = Vector3.Cross(up, a);
            var path = new Vector3[segments + 1];
            var radii = new float[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                path[i] = center + (Mathf.Cos(angle) * a + Mathf.Sin(angle) * b) * radius;
                radii[i] = thickness;
            }

            AddTube(path, radii, sides, submesh, flat);
        }

        /// <summary>
        /// A flat ribbon along a curve, like a leaf or a blade of grass: it widens and narrows per point and is
        /// visible from both sides. <paramref name="side"/> is the direction across the blade.
        /// </summary>
        public void AddBlade(Vector3[] spine, float[] widths, Vector3 side, int submesh)
        {
            int count = spine.Length;
            var positions = new Vector3[count * 2];
            var normals = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            float along = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 tangent = i < count - 1 ? spine[i + 1] - spine[i] : spine[i] - spine[i - 1];
                if (i > 0)
                {
                    along += Vector3.Distance(spine[i], spine[i - 1]);
                }

                Vector3 across = Vector3.ProjectOnPlane(side, tangent).normalized * (widths[i] * 0.5f);
                Vector3 normal = Vector3.Cross(tangent, across).normalized;
                positions[i * 2] = spine[i] - across;
                positions[i * 2 + 1] = spine[i] + across;
                normals[i * 2] = normal;
                normals[i * 2 + 1] = normal;
                uvs[i * 2] = new Vector2(0f, along * BladeTexturePerUnit);
                uvs[i * 2 + 1] = new Vector2(1f, along * BladeTexturePerUnit);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                Emit(positions, normals, uvs, new[] { a, c, b }, submesh, false);
                Emit(positions, normals, uvs, new[] { b, c, d }, submesh, false);
            }

            // The underside, so the blade does not vanish when it curls over.
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = -normals[i];
            }

            for (int i = 0; i < count - 1; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                Emit(positions, normals, uvs, new[] { a, b, c }, submesh, false);
                Emit(positions, normals, uvs, new[] { b, d, c }, submesh, false);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(_positions);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.subMeshCount = _triangles.Length;
            for (int i = 0; i < _triangles.Length; i++)
            {
                mesh.SetTriangles(_triangles[i], i);
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        // Every triangle gets its own vertices: that is what makes flat shading possible, and smooth shading
        // still looks smooth because the vertex normals are shared in value.
        private void Emit(Vector3[] positions, Vector3[] normals, Vector2[] uvs, int[] triangle, int submesh, bool flat)
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
                _uvs.Add(uvs[source]);
            }
        }
    }
}
