using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// A flat ribbon of light along a route (README round 25b): the road's light streaks and the flyers' beads of light,
    /// a strip mesh built once along the points with U running along the route (tiled per world unit) so the marker
    /// material's scroll carries the streaks towards the base. The vertex alpha fades the ends in and out.
    /// </summary>
    public static class RouteTrail
    {
        private const float FadeLength = 1.5f;
        // Miter joints get very long at sharp bends; past this factor the corner is clipped.
        private const float MaxMiter = 2f;

        /// <summary>A trail along <paramref name="points"/> (world space, already at their height); null without the piece.</summary>
        public static MeshRenderer Build(MarkerArt art, MarkerKind kind, string name, Transform parent, IReadOnlyList<Vector3> points, float width, float tilesPerUnit, Color colour)
        {
            if (art == null || !art.Has(kind) || points.Count < 2)
            {
                return null;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Strip(points, width, tilesPerUnit, colour);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = art.Material(kind);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return renderer;
        }

        private static Mesh Strip(IReadOnlyList<Vector3> points, float width, float tilesPerUnit, Color colour)
        {
            int count = points.Count;
            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            var colours = new Color[count * 2];
            var triangles = new int[(count - 1) * 6];
            float total = 0f;
            for (int i = 1; i < count; i++)
            {
                total += Vector3.Distance(points[i - 1], points[i]);
            }

            float along = 0f;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    along += Vector3.Distance(points[i - 1], points[i]);
                }

                Vector3 side = Side(points, i) * (width * 0.5f);
                vertices[i * 2] = points[i] - side;
                vertices[i * 2 + 1] = points[i] + side;
                float u = along * tilesPerUnit;
                uvs[i * 2] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
                float fade = Mathf.Clamp01(Mathf.Min(along, total - along) / FadeLength);
                Color c = new Color(colour.r, colour.g, colour.b, colour.a * fade);
                colours[i * 2] = c;
                colours[i * 2 + 1] = c;
            }

            for (int i = 0; i < count - 1; i++)
            {
                int v = i * 2;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 1;
                triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
            }

            var mesh = new Mesh { name = "RouteTrail" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colours;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // The sideways direction at a point: the average of the two legs' sides, lengthened at a bend so the strip keeps its width.
        private static Vector3 Side(IReadOnlyList<Vector3> points, int i)
        {
            Vector3 incoming = i > 0 ? Flat(points[i] - points[i - 1]) : Vector3.zero;
            Vector3 outgoing = i < points.Count - 1 ? Flat(points[i + 1] - points[i]) : Vector3.zero;
            Vector3 legIn = incoming != Vector3.zero ? Vector3.Cross(Vector3.up, incoming) : Vector3.zero;
            Vector3 legOut = outgoing != Vector3.zero ? Vector3.Cross(Vector3.up, outgoing) : Vector3.zero;
            if (legIn == Vector3.zero)
            {
                return legOut;
            }

            if (legOut == Vector3.zero)
            {
                return legIn;
            }

            Vector3 mid = (legIn + legOut).normalized;
            float cos = Vector3.Dot(mid, legIn);
            return mid * Mathf.Min(MaxMiter, cos > 0.01f ? 1f / cos : MaxMiter);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }
    }
}
