using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>A grid cell, without Unity types so the model also runs outside the engine.</summary>
    public readonly struct Cell
    {
        public readonly int X;
        public readonly int Y;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X},{Y})";
    }

    /// <summary>A ground road as the creatures walk it: its index among the level's paths and its corner cells.</summary>
    public sealed class RoadInput
    {
        public int PathIndex;
        public Cell[] Waypoints = new Cell[0];
    }

    /// <summary>
    /// What the balance model needs to know about a map: how long the roads are, where a second road joins the main one,
    /// where the gates stand, and how far down the road creatures from a nest, pool or boss entrance join it. Built from
    /// the level's cells, so a procedurally made map gets the same numbers as a hand-made one.
    /// </summary>
    public sealed class LevelShape
    {
        /// <summary>A front the defence has to cover: one ground lane up to where it meets the main road.</summary>
        public sealed class Lane
        {
            public int PathIndex;

            /// <summary>Cells walked from the gate to the base.</summary>
            public float Length;

            /// <summary>Share of the walk that is shared with every other lane (the trunk up to the base).</summary>
            public float TrunkShare;
        }

        /// <summary>A gate (blockade) on a lane: the hero's post and the place where walkers are held.</summary>
        public sealed class Gate
        {
            public int PathIndex;

            /// <summary>Share of the lane walked before the gate, from 0 at the spawn to 1 at the base.</summary>
            public float Progress;

            /// <summary>True when the gate stands on the trunk, so it holds every lane.</summary>
            public bool OnTrunk;
        }

        public readonly List<Lane> Lanes = new List<Lane>();
        public readonly List<Gate> Gates = new List<Gate>();

        /// <summary>Share of the main road still ahead where nest creatures join it, per nest.</summary>
        public readonly List<float> NestRemaining = new List<float>();

        /// <summary>Share of the main road still ahead where a boss from the level's own entrance joins it.</summary>
        public float EntranceRemaining = 1f;

        public float MainLength => Lanes.Count > 0 ? Lanes[0].Length : 0f;

        /// <summary>
        /// Number of separate ground fronts: one while every lane walks a long shared trunk, otherwise one per lane, since
        /// towers on one lane's own stretch do nothing for the other.
        /// </summary>
        public int GroundFronts(float trunkReference)
        {
            foreach (Lane lane in Lanes)
            {
                if (lane.TrunkShare < trunkReference)
                {
                    return Lanes.Count;
                }
            }

            return Lanes.Count > 0 ? 1 : 0;
        }

        public Lane LaneFor(int pathIndex)
        {
            foreach (Lane lane in Lanes)
            {
                if (lane.PathIndex == pathIndex)
                {
                    return lane;
                }
            }

            return Lanes.Count > 0 ? Lanes[0] : null;
        }

        /// <summary>
        /// Builds the shape from the roads (the first is the main road, ending at the base), the gate cells and the cells
        /// where nests and the boss entrance lie. Off-road spawns join the road at its nearest cell.
        /// </summary>
        public static LevelShape Build(IReadOnlyList<RoadInput> roads, IReadOnlyList<Cell> gates, IReadOnlyList<Cell> nests, Cell? entrance)
        {
            var shape = new LevelShape();
            if (roads.Count == 0)
            {
                return shape;
            }

            List<Cell> main = Expand(roads[0].Waypoints);
            var trunk = new HashSet<long>();
            foreach (Cell cell in main)
            {
                trunk.Add(Key(cell));
            }

            // The trunk is the part of the main road every lane walks: from the first cell any other lane shares with it.
            int trunkStart = 0;
            var expanded = new List<List<Cell>> { main };
            for (int r = 1; r < roads.Count; r++)
            {
                List<Cell> cells = Expand(roads[r].Waypoints);
                expanded.Add(cells);
                int join = cells.FindIndex(c => trunk.Contains(Key(c)));
                if (join >= 0)
                {
                    trunkStart = Math.Max(trunkStart, main.FindIndex(c => Same(c, cells[join])));
                }
            }

            for (int r = 0; r < roads.Count; r++)
            {
                List<Cell> cells = expanded[r];
                float length = Math.Max(1, cells.Count - 1);
                int mainTail = main.Count - 1 - trunkStart;
                shape.Lanes.Add(new Lane
                {
                    PathIndex = roads[r].PathIndex,
                    Length = length,
                    TrunkShare = Clamp01(mainTail / length),
                });
            }

            foreach (Cell gate in gates)
            {
                int best = -1;
                int bestRoad = 0;
                float bestDistance = float.MaxValue;
                for (int r = 0; r < expanded.Count; r++)
                {
                    int index = Nearest(expanded[r], gate, out float distance);
                    if (distance < bestDistance - 0.01f)
                    {
                        bestDistance = distance;
                        best = index;
                        bestRoad = r;
                    }
                }

                if (best < 0)
                {
                    continue;
                }

                List<Cell> road = expanded[bestRoad];
                bool onTrunk = bestRoad == 0 ? best >= trunkStart : trunk.Contains(Key(road[best]));
                shape.Gates.Add(new Gate
                {
                    PathIndex = onTrunk ? roads[0].PathIndex : roads[bestRoad].PathIndex,
                    Progress = best / (float)Math.Max(1, road.Count - 1),
                    OnTrunk = onTrunk,
                });
            }

            foreach (Cell nest in nests)
            {
                shape.NestRemaining.Add(Remaining(main, nest));
            }

            if (entrance.HasValue)
            {
                shape.EntranceRemaining = Remaining(main, entrance.Value);
            }

            return shape;
        }

        // Share of the main road still ahead from the road cell nearest to a point off the road.
        private static float Remaining(List<Cell> main, Cell from)
        {
            int index = Nearest(main, from, out _);
            return 1f - index / (float)Math.Max(1, main.Count - 1);
        }

        private static int Nearest(List<Cell> road, Cell cell, out float distance)
        {
            int best = -1;
            distance = float.MaxValue;
            for (int i = 0; i < road.Count; i++)
            {
                float dx = road[i].X - cell.X;
                float dy = road[i].Y - cell.Y;
                float d = dx * dx + dy * dy;
                if (d < distance)
                {
                    distance = d;
                    best = i;
                }
            }

            return best;
        }

        // Every cell along the straight legs between waypoints, as the creatures walk them.
        private static List<Cell> Expand(Cell[] waypoints)
        {
            var cells = new List<Cell>();
            for (int i = 0; i < waypoints.Length; i++)
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

        private static bool Same(Cell a, Cell b) => a.X == b.X && a.Y == b.Y;

        private static long Key(Cell cell) => ((long)cell.X << 32) ^ (uint)cell.Y;

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
