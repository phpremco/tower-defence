using UnityEngine;

namespace Game.Levels
{
    /// <summary>Moves a cursor over the grid with a stick, d-pad or WASD, one cell at a time.</summary>
    public static class GridCursor
    {
        /// <summary>Returns the cell one step in the dominant direction, kept inside the grid.</summary>
        public static Vector2Int Step(Vector2Int cell, Vector2 direction, GridMap grid)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return cell;
            }

            Vector2Int step = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? new Vector2Int(direction.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, direction.y > 0f ? 1 : -1);

            Vector2Int next = cell + step;
            return grid.Contains(next) ? next : cell;
        }
    }
}
