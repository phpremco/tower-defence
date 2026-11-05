using System.Collections.Generic;
using Game.Levels;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>Small sums over a boss's planned routes (lengths, points along them, flat directions). Pure logic.</summary>
    public static class BossGeometry
    {
        public static float PathLength(List<Vector3> points)
        {
            float total = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                total += Vector3.Distance(Flat3(points[i - 1]), Flat3(points[i]));
            }

            return total;
        }

        /// <summary>The point a distance along the route on the ground plane, and the direction of the leg it lies on.</summary>
        public static Vector3 PointAlong(List<Vector3> points, float distance, out Vector3 dir)
        {
            dir = Vector3.forward;
            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = Flat3(points[i - 1]);
                Vector3 b = Flat3(points[i]);
                float span = Vector3.Distance(a, b);
                if (span <= 0.0001f)
                {
                    continue;
                }

                dir = (b - a) / span;
                if (distance <= span)
                {
                    return a + dir * distance;
                }

                distance -= span;
            }

            return Flat3(points[points.Count - 1]);
        }

        /// <summary>The cell centre of a group nearest a point (on the ground plane), and how far it is.</summary>
        public static Vector3 Nearest(GridMap grid, IReadOnlyList<Vector2Int> cells, Vector3 position, out float distance)
        {
            Vector3 best = position;
            float bestSqr = float.PositiveInfinity;
            foreach (Vector2Int cell in cells)
            {
                Vector3 at = grid.CellToWorld(cell);
                float sqr = (at - new Vector3(position.x, 0f, position.z)).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = at;
                }
            }

            distance = Mathf.Sqrt(bestSqr);
            return best;
        }

        public static Vector3 Flat3(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /// <summary>The direction on the ground plane, or forward for a zero vector.</summary>
        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        /// <summary>Distance between two points on the ground plane.</summary>
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
