using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The road as a smooth ribbon: the corner points of every ground path with rounded corners, as line segments in
    /// world space. Enemies still follow the exact waypoints; the ribbon is only how the road is drawn, and the corner
    /// rounding is small enough that they never leave it.
    /// </summary>
    public sealed class RoadShape
    {
        private const float CornerRadiusCells = 0.6f;
        private const int CornerSamples = 7;

        private readonly List<Vector4> _segments = new List<Vector4>();

        public RoadShape(LevelDefinition level)
        {
            GridMap grid = level.Grid;
            foreach (EnemyPath path in level.Paths)
            {
                if (path.Flying || path.Waypoints == null || path.Waypoints.Length < 2)
                {
                    continue;
                }

                var points = new Vector2[path.Waypoints.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 world = grid.CellToWorld(path.Waypoints[i]);
                    points[i] = new Vector2(world.x, world.z);
                }

                AddSmoothed(points, CornerRadiusCells * level.CellSize);
            }
        }

        public IReadOnlyList<Vector4> Segments => _segments;

        /// <summary>Distance from a world position on the ground plane to the nearest piece of road.</summary>
        public float DistanceTo(float x, float z)
        {
            float best = float.MaxValue;
            for (int i = 0; i < _segments.Count; i++)
            {
                Vector4 s = _segments[i];
                float abx = s.z - s.x;
                float abz = s.w - s.y;
                float apx = x - s.x;
                float apz = z - s.y;
                float lengthSqr = abx * abx + abz * abz;
                float t = lengthSqr > 0f ? Mathf.Clamp01((apx * abx + apz * abz) / lengthSqr) : 0f;
                float dx = apx - abx * t;
                float dz = apz - abz * t;
                best = Mathf.Min(best, dx * dx + dz * dz);
            }

            return Mathf.Sqrt(best);
        }

        private void AddSmoothed(Vector2[] points, float radius)
        {
            var line = new List<Vector2> { points[0] };
            for (int i = 1; i < points.Length - 1; i++)
            {
                Vector2 corner = points[i];
                float before = Vector2.Distance(points[i - 1], corner);
                float after = Vector2.Distance(corner, points[i + 1]);
                float r = Mathf.Min(radius, Mathf.Min(before, after) * 0.5f);
                Vector2 start = corner + (points[i - 1] - corner).normalized * r;
                Vector2 end = corner + (points[i + 1] - corner).normalized * r;

                // A quadratic curve whose control point is the corner itself.
                for (int s = 0; s <= CornerSamples; s++)
                {
                    float t = s / (float)CornerSamples;
                    Vector2 p = (1f - t) * (1f - t) * start + 2f * (1f - t) * t * corner + t * t * end;
                    line.Add(p);
                }
            }

            line.Add(points[points.Length - 1]);
            for (int i = 1; i < line.Count; i++)
            {
                _segments.Add(new Vector4(line[i - 1].x, line[i - 1].y, line[i].x, line[i].y));
            }
        }
    }
}
