using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The road as a smooth surface for drawing: a ribbon with rounded corners along the centre line of every road,
    /// plus the plaza cells as one flat area. Enemies still follow the exact routes; the corner rounding is small
    /// enough that they never leave the ribbon. Distances are measured as if everything were a ribbon, so a point
    /// on the edge of a plaza reads the same as a point on the edge of a road.
    /// </summary>
    public sealed class RoadShape
    {
        private const float CornerRadiusCells = 0.6f;
        private const int CornerSamples = 7;

        private static readonly Vector2Int[] Sides = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        private readonly List<Vector4> _segments = new List<Vector4>();
        private readonly List<Vector2> _plazaCentres = new List<Vector2>();
        private readonly RoadCorridor _corridor;
        private readonly GridMap _grid;
        private readonly float _cellSize;

        public RoadShape(LevelDefinition level)
        {
            _corridor = level.Road;
            _grid = level.Grid;
            _cellSize = level.CellSize;
            HalfWidth = _corridor.HalfWidthCells * _cellSize;

            for (int p = 0; p < level.Paths.Length; p++)
            {
                if (level.Paths[p].Flying)
                {
                    continue;
                }

                Vector3[] route = _corridor.Route(p);
                if (route.Length < 2)
                {
                    continue;
                }

                var points = new Vector2[route.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new Vector2(route[i].x, route[i].z);
                }

                AddSmoothed(points, CornerRadiusCells * _cellSize);
            }

            foreach (Vector2Int cell in _corridor.PlazaCells)
            {
                Vector3 centre = _grid.CellToWorld(cell);
                _plazaCentres.Add(new Vector2(centre.x, centre.z));
            }
        }

        public IReadOnlyList<Vector4> Segments => _segments;

        /// <summary>Distance from the centre line to the edge of the painted road, in world units.</summary>
        public float HalfWidth { get; }

        /// <summary>
        /// Distance from a world position on the ground plane to the nearest road centre line, where a plaza counts
        /// as a ribbon whose edge runs an inset inside the plaza's outline: below <see cref="HalfWidth"/> is road.
        /// </summary>
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

            best = Mathf.Sqrt(best);
            return _plazaCentres.Count > 0 ? Mathf.Min(best, PlazaDistance(x, z)) : best;
        }

        // Inside a plaza the value falls below the half width by how deep the point lies behind the plaza's outline
        // (sides and corners that border no road); outside it grows with the distance to the nearest plaza cell.
        private float PlazaDistance(float x, float z)
        {
            float edge = HalfWidth + RoadCorridor.EdgeInsetCells * _cellSize;
            Vector2Int cell = _grid.WorldToCell(new Vector3(x, 0f, z));
            if (!_corridor.IsPlaza(cell))
            {
                return edge + OutsideDistance(x, z);
            }

            Vector3 centre = _grid.CellToWorld(cell);
            float half = _cellSize * 0.5f;
            float localX = x - centre.x;
            float localZ = z - centre.z;
            float depth = half;
            for (int i = 0; i < Sides.Length; i++)
            {
                if (!_corridor.IsRoad(cell + Sides[i]))
                {
                    float along = Sides[i].x != 0 ? localX * Sides[i].x : localZ * Sides[i].y;
                    depth = Mathf.Min(depth, half - along);
                }

                // A diagonal neighbour that is not road leaves a corner point the plaza wraps around.
                Vector2Int next = Sides[(i + 1) % Sides.Length];
                if (!_corridor.IsRoad(cell + Sides[i] + next) && _corridor.IsRoad(cell + Sides[i]) && _corridor.IsRoad(cell + next))
                {
                    float cx = (Sides[i].x + next.x) * half;
                    float cz = (Sides[i].y + next.y) * half;
                    depth = Mathf.Min(depth, Mathf.Sqrt((localX - cx) * (localX - cx) + (localZ - cz) * (localZ - cz)));
                }
            }

            return edge - depth;
        }

        private float OutsideDistance(float x, float z)
        {
            float half = _cellSize * 0.5f;
            float best = float.MaxValue;
            for (int i = 0; i < _plazaCentres.Count; i++)
            {
                float dx = Mathf.Max(0f, Mathf.Abs(x - _plazaCentres[i].x) - half);
                float dz = Mathf.Max(0f, Mathf.Abs(z - _plazaCentres[i].y) - half);
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
