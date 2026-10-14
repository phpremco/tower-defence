using UnityEngine;

namespace Game.Levels
{
    /// <summary>Finds where a road cell sits on the level's road paths, so rolling path objects know where to head.</summary>
    public static class RoadLocator
    {
        /// <summary>
        /// Finds the first road path that crosses the cell. Returns the path and the index of the waypoint that comes
        /// after the cell in the direction of travel. False when the cell is not on a road.
        /// </summary>
        public static bool TryLocate(LevelDefinition level, Vector2Int cell, out Vector2Int[] waypoints, out int nextWaypoint)
        {
            foreach (EnemyPath path in level.Paths)
            {
                if (path.Flying || path.Waypoints == null)
                {
                    continue;
                }

                for (int i = 1; i < path.Waypoints.Length; i++)
                {
                    Vector2Int from = path.Waypoints[i - 1];
                    Vector2Int to = path.Waypoints[i];
                    bool insideX = cell.x >= Mathf.Min(from.x, to.x) && cell.x <= Mathf.Max(from.x, to.x);
                    bool insideY = cell.y >= Mathf.Min(from.y, to.y) && cell.y <= Mathf.Max(from.y, to.y);
                    if (insideX && insideY)
                    {
                        waypoints = path.Waypoints;
                        nextWaypoint = i;
                        return true;
                    }
                }
            }

            waypoints = null;
            nextWaypoint = 0;
            return false;
        }
    }
}
