using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// The liquid sheet of one pool, cut from its signed shore distance on a fine grid (marching squares, every square
    /// clipped to the part past the cut line), so its edge is the smoothed shore pushed a little under the bank and never
    /// a row of cell squares. Each vertex carries its distance from the bank's shelf edge (UV1.x), which the liquid shader
    /// reads for depth, foam bands and frost rings on every profile (no depth texture needed). World-space positions:
    /// the object sits at the origin, raised to the liquid's level.
    /// </summary>
    public static class LiquidSurfaceMesh
    {
        /// <summary>Grid step of the sheet; fine enough for the shader's vertex waves.</summary>
        public const float Step = 0.25f;

        /// <summary>The sheet reaches this far outside the shore, under the bank and the dry ground.</summary>
        public const float OuterReach = 0.18f;

        /// <summary>The bank's shelf covers this much of the liquid beside the shore line (README: about 0.06).</summary>
        public const float ShelfInset = 0.06f;

        private const float Padding = OuterReach + Step;

        public static Mesh Build(LiquidShape shape)
        {
            Rect bounds = shape.Bounds;
            int nx = Mathf.CeilToInt((bounds.width + 2f * Padding) / Step) + 1;
            int nz = Mathf.CeilToInt((bounds.height + 2f * Padding) / Step) + 1;
            float x0 = bounds.xMin - Padding;
            float z0 = bounds.yMin - Padding;
            var values = new float[nx * nz];
            for (int iz = 0; iz < nz; iz++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    values[iz * nx + ix] = shape.SignedDistance(x0 + ix * Step, z0 + iz * Step) + OuterReach;
                }
            }

            var builder = new Builder(shape, values, nx, x0, z0);
            for (int iz = 0; iz < nz - 1; iz++)
            {
                for (int ix = 0; ix < nx - 1; ix++)
                {
                    builder.Square(ix, iz);
                }
            }

            return builder.ToMesh();
        }

        // Collects the clipped squares with shared vertices: grid points and edge crossings each exist once.
        private sealed class Builder
        {
            private readonly LiquidShape _shape;
            private readonly float[] _values;
            private readonly int _nx;
            private readonly float _x0;
            private readonly float _z0;
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector2> _shore = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();
            private readonly Dictionary<long, int> _ids = new Dictionary<long, int>();
            private readonly List<int> _polygon = new List<int>(8);

            public Builder(LiquidShape shape, float[] values, int nx, float x0, float z0)
            {
                _shape = shape;
                _values = values;
                _nx = nx;
                _x0 = x0;
                _z0 = z0;
            }

            // The square's corners counter-clockwise; kept corners and the crossings between kept and dropped ones form
            // a convex polygon (a saddle square keeps both corners and the band between, which is fine at this step).
            public void Square(int ix, int iz)
            {
                _polygon.Clear();
                Corner(ix, iz, ix + 1, iz);
                Corner(ix + 1, iz, ix + 1, iz + 1);
                Corner(ix + 1, iz + 1, ix, iz + 1);
                Corner(ix, iz + 1, ix, iz);
                for (int i = 1; i < _polygon.Count - 1; i++)
                {
                    _triangles.Add(_polygon[0]);
                    _triangles.Add(_polygon[i + 1]);
                    _triangles.Add(_polygon[i]);
                }
            }

            private void Corner(int ax, int az, int bx, int bz)
            {
                float a = Value(ax, az);
                float b = Value(bx, bz);
                if (a >= 0f)
                {
                    _polygon.Add(GridVertex(ax, az));
                }

                if ((a >= 0f) != (b >= 0f))
                {
                    _polygon.Add(EdgeVertex(ax, az, bx, bz, a / (a - b)));
                }
            }

            private float Value(int ix, int iz) => _values[iz * _nx + ix];

            private int GridVertex(int ix, int iz)
            {
                long key = ((long)iz * _nx + ix) * 3;
                return Vertex(key, _x0 + ix * Step, _z0 + iz * Step);
            }

            private int EdgeVertex(int ax, int az, int bx, int bz, float t)
            {
                int lowX = Mathf.Min(ax, bx);
                int lowZ = Mathf.Min(az, bz);
                long key = ((long)lowZ * _nx + lowX) * 3 + (ax != bx ? 1 : 2);
                float x = _x0 + Mathf.Lerp(ax, bx, t) * Step;
                float z = _z0 + Mathf.Lerp(az, bz, t) * Step;
                return Vertex(key, x, z);
            }

            private int Vertex(long key, float x, float z)
            {
                if (_ids.TryGetValue(key, out int id))
                {
                    return id;
                }

                id = _vertices.Count;
                _ids[key] = id;
                _vertices.Add(new Vector3(x, 0f, z));
                _shore.Add(new Vector2(_shape.SignedDistance(x, z) - ShelfInset, 0f));
                return id;
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh { name = $"Liquid_{_shape.Kind}" };
                mesh.indexFormat = _vertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(_vertices);
                var normals = new Vector3[_vertices.Count];
                var uvs = new Vector2[_vertices.Count];
                for (int i = 0; i < normals.Length; i++)
                {
                    normals[i] = Vector3.up;
                    uvs[i] = new Vector2(_vertices[i].x, _vertices[i].z);
                }

                mesh.normals = normals;
                mesh.uv = uvs;
                mesh.SetUVs(1, _shore);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
