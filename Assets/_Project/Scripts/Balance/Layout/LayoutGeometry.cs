using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Grid helpers for the layout generator and its checks: roads walked cell by cell and the corridor a wide road takes.
    /// The extra rows lie to the right of the direction of travel, with mitred corners, the same rule as the game's
    /// <c>RoadCorridor</c> (facing east the right hand points south).
    /// </summary>
    public static class LayoutGeometry
    {
        public static long Key(Cell cell) => ((long)cell.X << 32) ^ (uint)cell.Y;

        public static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;

        public static bool Same(Cell a, Cell b) => a.X == b.X && a.Y == b.Y;

        public static int IndexOf(IReadOnlyList<Cell> cells, Cell cell)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (Same(cells[i], cell))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The unit step from one cell towards another on the same row or column.</summary>
        public static Cell Step(Cell from, Cell to) => new Cell(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y));

        /// <summary>The right hand of a step: facing east (+x) it points south (-y).</summary>
        public static Cell RightOf(Cell step) => new Cell(step.Y, -step.X);

        /// <summary>True when a turn from one step to the next is clockwise (a right turn).</summary>
        public static bool IsClockwise(Cell incoming, Cell outgoing)
        {
            Cell right = RightOf(incoming);
            return right.X == outgoing.X && right.Y == outgoing.Y;
        }

        /// <summary>Every cell along the straight legs between corner cells, as the creatures walk them.</summary>
        public static List<Cell> Walk(IReadOnlyList<Cell> waypoints)
        {
            var cells = new List<Cell>();
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (i == 0)
                {
                    cells.Add(waypoints[0]);
                    continue;
                }

                Cell from = waypoints[i - 1];
                Cell to = waypoints[i];
                int steps = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
                for (int s = 1; s <= steps; s++)
                {
                    cells.Add(new Cell(from.X + (to.X - from.X) * s / steps, from.Y + (to.Y - from.Y) * s / steps));
                }
            }

            return cells;
        }

        /// <summary>The cells one straight leg of a wide road covers, its end cells included.</summary>
        public static List<Cell> SegmentCorridor(Cell from, Cell to, int width)
        {
            var cells = new List<Cell>();
            Cell step = Step(from, to);
            Cell right = RightOf(step);
            int length = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
            for (int s = 0; s <= length; s++)
            {
                for (int row = 0; row < width; row++)
                {
                    cells.Add(new Cell(from.X + step.X * s + right.X * row, from.Y + step.Y * s + right.Y * row));
                }
            }

            return cells;
        }

        /// <summary>Every cell a wide road covers, mitred corners included.</summary>
        public static HashSet<long> Corridor(IReadOnlyList<Cell> waypoints, int width)
        {
            var cells = new HashSet<long>();
            for (int i = 1; i < waypoints.Count; i++)
            {
                foreach (Cell cell in SegmentCorridor(waypoints[i - 1], waypoints[i], width))
                {
                    cells.Add(Key(cell));
                }
            }

            // The outer corner of a turn belongs to neither leg's rows; the mitre fills the square between them.
            for (int i = 1; i < waypoints.Count - 1; i++)
            {
                Cell rightIn = RightOf(Step(waypoints[i - 1], waypoints[i]));
                Cell rightOut = RightOf(Step(waypoints[i], waypoints[i + 1]));
                for (int r = 0; r < width; r++)
                {
                    for (int s = 0; s < width; s++)
                    {
                        cells.Add(Key(waypoints[i].X + rightIn.X * r + rightOut.X * s, waypoints[i].Y + rightIn.Y * r + rightOut.Y * s));
                    }
                }
            }

            return cells;
        }

        /// <summary>Smallest Chebyshev distance between two sets of cells (1 means they touch).</summary>
        public static int Distance(List<Cell> a, List<Cell> b)
        {
            int best = int.MaxValue;
            foreach (Cell p in a)
            {
                foreach (Cell q in b)
                {
                    int d = Math.Max(Math.Abs(p.X - q.X), Math.Abs(p.Y - q.Y));
                    if (d < best)
                    {
                        best = d;
                    }
                }
            }

            return best;
        }

        public static int Chebyshev(Cell a, Cell b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        /// <summary>Index of the walked road cell nearest to a point, and its squared distance.</summary>
        public static int Nearest(IReadOnlyList<Cell> walk, Cell cell, out int distanceSquared)
        {
            int best = -1;
            distanceSquared = int.MaxValue;
            for (int i = 0; i < walk.Count; i++)
            {
                int dx = walk[i].X - cell.X;
                int dy = walk[i].Y - cell.Y;
                int d = dx * dx + dy * dy;
                if (d < distanceSquared)
                {
                    distanceSquared = d;
                    best = i;
                }
            }

            return best;
        }
    }
}
