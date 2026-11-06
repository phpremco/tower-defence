using System;
using Game.Levels;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Exact walk of a straight line over the level grid: every cell the segment passes through is visited, and where it
    /// runs exactly through a cell corner both cells beside the corner count. Sampling the line at fixed steps misses a
    /// cell whose corner the line only clips (round 20: a burrow leg clipped a tower cell's corner by 0.05 m and the boss,
    /// stepped finely, stood in it). Pure geometry.
    /// </summary>
    public static class GridLine
    {
        // Two crossings closer than this (in cells along the line) are one crossing through a corner.
        private const float CornerTolerance = 1e-5f;

        /// <summary>True when <paramref name="hit"/> is true for any cell the segment from a to b touches (ends included).</summary>
        public static bool Touches(GridMap grid, Vector3 a, Vector3 b, Func<Vector2Int, bool> hit)
        {
            // Cell coordinates in which cell c spans [c - 0.5, c + 0.5], matching GridMap.WorldToCell.
            Vector2 from = ToGrid(grid, a);
            Vector2 to = ToGrid(grid, b);
            Vector2Int cell = grid.WorldToCell(a);
            if (hit(cell))
            {
                return true;
            }

            Vector2 delta = to - from;
            int stepX = Math.Sign(delta.x);
            int stepY = Math.Sign(delta.y);
            float nextX = stepX != 0 ? (cell.x + stepX * 0.5f - from.x) / delta.x : float.PositiveInfinity;
            float nextY = stepY != 0 ? (cell.y + stepY * 0.5f - from.y) / delta.y : float.PositiveInfinity;
            float spanX = stepX != 0 ? Mathf.Abs(1f / delta.x) : float.PositiveInfinity;
            float spanY = stepY != 0 ? Mathf.Abs(1f / delta.y) : float.PositiveInfinity;
            while (Mathf.Min(nextX, nextY) <= 1f)
            {
                if (Mathf.Abs(nextX - nextY) <= CornerTolerance)
                {
                    if (hit(new Vector2Int(cell.x + stepX, cell.y)) || hit(new Vector2Int(cell.x, cell.y + stepY)))
                    {
                        return true;
                    }

                    cell += new Vector2Int(stepX, stepY);
                    nextX += spanX;
                    nextY += spanY;
                }
                else if (nextX < nextY)
                {
                    cell.x += stepX;
                    nextX += spanX;
                }
                else
                {
                    cell.y += stepY;
                    nextY += spanY;
                }

                if (hit(cell))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 ToGrid(GridMap grid, Vector3 position) =>
            new Vector2(position.x / grid.CellSize + (grid.Size.x - 1) * 0.5f, position.z / grid.CellSize + (grid.Size.y - 1) * 0.5f);
    }
}
