using UnityEngine;

namespace Game.Levels
{
    /// <summary>Which way the first road path runs through a cell, so a blockade can stand across it.</summary>
    public static class RoadDirection
    {
        /// <summary>Unit direction on the ground plane (x, z), or forward when the cell is not on a road path.</summary>
        public static Vector3 At(LevelDefinition level, Vector2Int cell)
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
                        return new Vector3(Mathf.Sign(to.x - from.x) * (to.x != from.x ? 1f : 0f), 0f, Mathf.Sign(to.y - from.y) * (to.y != from.y ? 1f : 0f));
                    }
                }
            }

            return Vector3.forward;
        }
    }
}
