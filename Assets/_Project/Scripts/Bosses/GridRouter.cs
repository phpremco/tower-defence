using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Shortest way over the level grid with a cost per cell (A*, eight directions). A cell that costs infinity is never
    /// entered, and a diagonal step never cuts the corner of such a cell, so a line drawn through the cell centres keeps
    /// at least half a cell from it: a crack line or a boss's walk goes round a tower, never visibly under it. Pure logic;
    /// it allocates only when asked for a route, which happens a few times per boss fight.
    /// </summary>
    public static class GridRouter
    {
        private const float DiagonalFactor = 1.41421356f;

        private static readonly Vector2Int[] Steps =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        /// <summary>
        /// Fills <paramref name="path"/> with the cells from start to goal, both included, and returns true; false when
        /// the goal cannot be reached. <paramref name="cost"/> is the price of entering a cell (at least 1 for a plain one,
        /// infinity for a blocked one). The start is always allowed; so is the goal when <paramref name="goalAlwaysOpen"/>.
        /// <paramref name="cheapest"/> is the lowest price any cell can have (quicksand is cheaper than plain ground), so the
        /// estimate of the way left never overshoots and the shortcut is found.
        /// </summary>
        public static bool TryFind(Vector2Int size, Vector2Int start, Vector2Int goal, Func<Vector2Int, float> cost, List<Vector2Int> path, bool goalAlwaysOpen = false, float cheapest = 1f)
        {
            path.Clear();
            if (!Inside(size, start) || !Inside(size, goal))
            {
                return false;
            }

            var best = new Dictionary<Vector2Int, float> { [start] = 0f };
            var from = new Dictionary<Vector2Int, Vector2Int>();
            var closed = new HashSet<Vector2Int>();
            var open = new List<Vector2Int> { start };
            while (open.Count > 0)
            {
                int pick = Cheapest(open, best, goal, cheapest);
                Vector2Int cell = open[pick];
                open.RemoveAt(pick);
                if (cell == goal)
                {
                    Trace(from, start, goal, path);
                    return true;
                }

                if (!closed.Add(cell))
                {
                    continue;
                }

                foreach (Vector2Int step in Steps)
                {
                    Vector2Int next = cell + step;
                    if (!Inside(size, next) || closed.Contains(next))
                    {
                        continue;
                    }

                    float enter = next == goal && goalAlwaysOpen ? 1f : cost(next);
                    if (float.IsPositiveInfinity(enter))
                    {
                        continue;
                    }

                    bool diagonal = step.x != 0 && step.y != 0;
                    if (diagonal && (Blocked(cost, cell + new Vector2Int(step.x, 0)) || Blocked(cost, cell + new Vector2Int(0, step.y))))
                    {
                        continue;
                    }

                    float total = best[cell] + enter * (diagonal ? DiagonalFactor : 1f);
                    if (best.TryGetValue(next, out float known) && known <= total)
                    {
                        continue;
                    }

                    best[next] = total;
                    from[next] = cell;
                    open.Add(next);
                }
            }

            return false;
        }

        private static bool Blocked(Func<Vector2Int, float> cost, Vector2Int cell) => float.IsPositiveInfinity(cost(cell));

        private static bool Inside(Vector2Int size, Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < size.x && cell.y < size.y;

        // The open cell with the lowest known cost plus the straight-line guess to the goal.
        private static int Cheapest(List<Vector2Int> open, Dictionary<Vector2Int, float> best, Vector2Int goal, float cheapest)
        {
            int pick = 0;
            float lowest = float.PositiveInfinity;
            for (int i = 0; i < open.Count; i++)
            {
                Vector2Int cell = open[i];
                float guess = best[cell] + Octile(cell, goal) * cheapest;
                if (guess < lowest)
                {
                    lowest = guess;
                    pick = i;
                }
            }

            return pick;
        }

        private static float Octile(Vector2Int a, Vector2Int b)
        {
            int dx = Math.Abs(a.x - b.x);
            int dy = Math.Abs(a.y - b.y);
            return Math.Max(dx, dy) + (DiagonalFactor - 1f) * Math.Min(dx, dy);
        }

        private static void Trace(Dictionary<Vector2Int, Vector2Int> from, Vector2Int start, Vector2Int goal, List<Vector2Int> path)
        {
            Vector2Int cell = goal;
            path.Add(cell);
            while (cell != start)
            {
                cell = from[cell];
                path.Add(cell);
            }

            path.Reverse();
        }
    }
}
