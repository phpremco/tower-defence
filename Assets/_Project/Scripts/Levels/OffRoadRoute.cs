using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The way for a creature that starts off the road, in a pool or out of the ground: straight to the nearest
    /// road cell, then along that road to the base. Progress along the road is counted from the road's own start,
    /// so towers rank such a creature against the others fairly.
    /// </summary>
    public static class OffRoadRoute
    {
        /// <summary>Builds the route from a world position and tells how far along the road the join point lies.</summary>
        public static Vector3[] Build(LevelDefinition level, Vector3 start, out float progressOffset)
        {
            RoadCorridor road = level.Road;
            Vector2Int join = NearestRoadCell(level, start);
            if (!road.TryLocate(join, out Vector3[] route, out int next))
            {
                progressOffset = 0f;
                return new[] { start, LevelVisualBuilder.BasePosition(level) };
            }

            Vector3 joinPoint = road.CentreOf(join);
            var result = new Vector3[route.Length - next + 2];
            result[0] = new Vector3(start.x, 0f, start.z);
            result[1] = joinPoint;
            for (int i = next; i < route.Length; i++)
            {
                result[i - next + 2] = route[i];
            }

            progressOffset = 0f;
            for (int i = 1; i < next; i++)
            {
                progressOffset += Vector3.Distance(route[i - 1], route[i]);
            }

            if (next > 0)
            {
                progressOffset += Vector3.Distance(route[next - 1], joinPoint);
            }

            return result;
        }

        /// <summary>The road cell closest to a world position, by straight-line distance.</summary>
        public static Vector2Int NearestRoadCell(LevelDefinition level, Vector3 position)
        {
            GridMap grid = level.Grid;
            Vector2Int best = default;
            float bestSqr = float.MaxValue;
            foreach (Vector2Int cell in level.Road.Cells)
            {
                Vector3 world = grid.CellToWorld(cell);
                float dx = world.x - position.x;
                float dz = world.z - position.z;
                float sqr = dx * dx + dz * dz;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = cell;
                }
            }

            return best;
        }
    }
}
