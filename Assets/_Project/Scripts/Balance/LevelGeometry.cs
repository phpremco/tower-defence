using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>What one build spot covers of a level, as the ledger counts a tower's time on target (round 22).</summary>
    public struct SpotCover
    {
        /// <summary>Cells of walking route in reach, weighted by the share of the creatures that walk each cell.</summary>
        public float GroundPass;

        /// <summary>Cells of flight route in reach, weighted the same way.</summary>
        public float AirPass;

        /// <summary>True when the defence gate's queue is in reach: the gun fires the whole time the gate holds the walkers.</summary>
        public bool Gate;

        /// <summary>Share of the ground route in reach where walkers bunch (the queue before a gate, the inside of a bend).</summary>
        public float Bunch;

        /// <summary>The ledger's spotless assumption: a straight pass through the whole reach, the gate queue in reach.</summary>
        public static SpotCover Ideal(float reach) => new SpotCover { GroundPass = 2f * reach, AirPass = 2f * reach, Gate = true, Bunch = 0f };
    }

    /// <summary>
    /// The map as the plan model needs it: the walking and flight routes sampled cell by cell with the share of creatures
    /// that pass each cell, the gates, the cells a tower may stand on beside the road, and the road cells a gadget may lie
    /// on. From it a spot's cover follows (<see cref="CoverAt"/>), so a tower in a bend or at the queue counts for more
    /// than one on a side lane, and the same numbers serve the EditMode plan search and the PlayMode replay. Pure C#, built
    /// from the level's cells by the game side (<c>BalanceLedger.Geometry</c>).
    /// </summary>
    public sealed class LevelGeometry
    {
        /// <summary>Cells around a gate where walkers queue, and how sharp a turn bunches them (cos 45 degrees).</summary>
        public const float QueueCells = 3f;
        public const float BendCos = 0.707f;
        public const float BendReachCells = 1.5f;

        private struct Sample
        {
            public float X;
            public float Y;
            public float Weight;
            public bool Bunched;
        }

        private readonly List<Sample> _ground = new List<Sample>();
        private readonly List<Sample> _air = new List<Sample>();

        public readonly List<Cell> Gates = new List<Cell>();
        public readonly List<Cell> Spots = new List<Cell>();
        public readonly List<Cell> MainRoad = new List<Cell>();

        // Per-spot reach multipliers from the map (a raised platform, say): the data hook every lens and the plan search respect.
        private readonly Dictionary<long, float> _spotRange = new Dictionary<long, float>();
        private readonly Dictionary<(long, int), SpotCover> _covers = new Dictionary<(long, int), SpotCover>();

        /// <summary>The first gate every walker passes (the hero's post and the ledger's kill point), or null.</summary>
        public Cell? DefenceGate;

        /// <summary>World units per cell: ranges are in world units, routes in cells.</summary>
        public float CellSize = 1f;

        /// <summary>Walking route cells, weighted by the share of the walkers that pass them (one per cell).</summary>
        public int GroundSamples => _ground.Count;

        public static LevelGeometry Build(IReadOnlyList<RoadInput> roads, IReadOnlyList<RoadInput> flights, IReadOnlyList<Cell> gates, Cell? defenceGate, IReadOnlyList<Cell> spots, float cellSize)
        {
            var geometry = new LevelGeometry { CellSize = cellSize <= 0f ? 1f : cellSize };
            geometry.Gates.AddRange(gates);
            geometry.Spots.AddRange(spots);
            geometry.DefenceGate = defenceGate;
            if (roads.Count > 0)
            {
                geometry.MainRoad.AddRange(LevelShape.Expand(roads[0].Waypoints));
            }

            SampleRoutes(roads, gates, geometry._ground);
            SampleRoutes(flights, new List<Cell>(), geometry._air);
            return geometry;
        }

        /// <summary>A spot's own reach multiplier (1 for plain ground); set by the game side from the map's terrain.</summary>
        public void SetSpotRange(Cell spot, float multiplier) => _spotRange[Key(spot)] = multiplier;

        public float SpotRange(Cell spot) => _spotRange.TryGetValue(Key(spot), out float m) ? m : 1f;

        /// <summary>A tower's reach from a spot: its range times the spot's multiplier.</summary>
        public float ReachAt(Cell spot, float range) => range * SpotRange(spot);

        /// <summary>What a tower with this range (world units) covers from a spot.</summary>
        public SpotCover CoverAt(Cell spot, float reach)
        {
            (long, int) key = (Key(spot), (int)Math.Round(reach * 1000f));
            if (_covers.TryGetValue(key, out SpotCover known))
            {
                return known;
            }

            SpotCover cover = Compute(spot, reach);
            _covers[key] = cover;
            return cover;
        }

        private SpotCover Compute(Cell spot, float reach)
        {
            float cells = ReachAt(spot, reach) / CellSize;
            float radius = cells * cells;
            var cover = new SpotCover();
            float bunched = 0f;
            foreach (Sample sample in _ground)
            {
                if (Distance(spot, sample) <= radius)
                {
                    cover.GroundPass += sample.Weight;
                    bunched += sample.Bunched ? sample.Weight : 0f;
                }
            }

            foreach (Sample sample in _air)
            {
                if (Distance(spot, sample) <= radius)
                {
                    cover.AirPass += sample.Weight;
                }
            }

            cover.Bunch = cover.GroundPass > 0f ? bunched / cover.GroundPass : 0f;
            cover.Gate = DefenceGate.HasValue && Distance(spot, DefenceGate.Value) <= (cells + QueueCells) * (cells + QueueCells);
            return cover;
        }

        private readonly Dictionary<(int, bool, bool, int), SpotCover> _typical = new Dictionary<(int, bool, bool, int), SpotCover>();

        /// <summary>
        /// What a competent player's tower of this reach covers: the mean cover of the best <paramref name="count"/> spots for it
        /// (round 22). The spotless ledger (solver, bands) buys from these instead of an ideal spot, so it reckons with the same
        /// placement the calibration was fitted on; the gate counts when most of those spots reach the queue.
        /// </summary>
        public SpotCover TypicalCover(float reach, bool air, bool bunch, int count)
        {
            (int, bool, bool, int) key = ((int)Math.Round(reach * 1000f), air, bunch, count);
            if (_typical.TryGetValue(key, out SpotCover known))
            {
                return known;
            }

            List<Cell> ranked = Rank(reach, air, bunch);
            int n = Math.Min(count, ranked.Count);
            if (n == 0)
            {
                return SpotCover.Ideal(reach);
            }

            var mean = new SpotCover();
            int gates = 0;
            for (int i = 0; i < n; i++)
            {
                SpotCover cover = CoverAt(ranked[i], reach);
                mean.GroundPass += cover.GroundPass / n;
                mean.AirPass += cover.AirPass / n;
                mean.Bunch += cover.Bunch / n;
                gates += cover.Gate ? 1 : 0;
            }

            mean.Gate = gates * 2 >= n;
            _typical[key] = mean;
            return mean;
        }

        /// <summary>The spots ranked for a tower of this reach: most weighted route in reach first (air guns by flight route).</summary>
        public List<Cell> Rank(float reach, bool air, bool bunch)
        {
            var scored = new List<(Cell Cell, float Score)>();
            foreach (Cell spot in Spots)
            {
                SpotCover cover = CoverAt(spot, reach);
                float score = air ? cover.AirPass : cover.GroundPass * (bunch ? 1f + cover.Bunch : 1f) + (cover.Gate ? QueueCells : 0f);
                scored.Add((spot, score));
            }

            // A stable order: equal scores keep the spot list's order (nearest the gate first on the game side).
            for (int i = 1; i < scored.Count; i++)
            {
                (Cell Cell, float Score) item = scored[i];
                int j = i - 1;
                while (j >= 0 && scored[j].Score < item.Score)
                {
                    scored[j + 1] = scored[j];
                    j--;
                }

                scored[j + 1] = item;
            }

            var result = new List<Cell>(scored.Count);
            foreach ((Cell cell, float _) in scored)
            {
                result.Add(cell);
            }

            return result;
        }

        /// <summary>Road cells of the main road within a few cells before the defence gate (the queue), nearest the gate first.</summary>
        public List<Cell> QueueCellsBeforeGate(int count)
        {
            var cells = new List<Cell>();
            if (!DefenceGate.HasValue || MainRoad.Count == 0)
            {
                return cells;
            }

            int at = 0;
            for (int i = 1; i < MainRoad.Count; i++)
            {
                at = Distance(MainRoad[i], DefenceGate.Value) < Distance(MainRoad[at], DefenceGate.Value) ? i : at;
            }

            for (int k = 2; k <= count + 1 && at - k >= 0; k++)
            {
                cells.Add(MainRoad[at - k]);
            }

            return cells;
        }

        /// <summary>Whether a tower at a spot with this reach covers a road cell (a slow field there multiplies its damage).</summary>
        public bool Reaches(Cell spot, float reach, Cell road)
        {
            float cells = ReachAt(spot, reach) / CellSize;
            return Distance(spot, road) <= cells * cells;
        }

        private static void SampleRoutes(IReadOnlyList<RoadInput> roads, IReadOnlyList<Cell> gates, List<Sample> output)
        {
            if (roads.Count == 0)
            {
                return;
            }

            // How many lanes walk each cell: the trunk every lane shares weighs as the whole crowd, a lane of its own as its share.
            var lanes = new Dictionary<long, int>();
            var expanded = new List<List<Cell>>();
            foreach (RoadInput road in roads)
            {
                List<Cell> cells = LevelShape.Expand(road.Waypoints);
                expanded.Add(cells);
                var seen = new HashSet<long>();
                foreach (Cell cell in cells)
                {
                    long key = Key(cell);
                    if (seen.Add(key))
                    {
                        lanes[key] = (lanes.TryGetValue(key, out int n) ? n : 0) + 1;
                    }
                }
            }

            var done = new HashSet<long>();
            for (int r = 0; r < roads.Count; r++)
            {
                List<Cell> cells = expanded[r];
                for (int i = 0; i < cells.Count; i++)
                {
                    Cell cell = cells[i];
                    if (!done.Add(Key(cell)))
                    {
                        continue;
                    }

                    output.Add(new Sample
                    {
                        X = cell.X,
                        Y = cell.Y,
                        Weight = lanes[Key(cell)] / (float)roads.Count,
                        Bunched = NearGate(cell, gates) || NearBend(roads[r].Waypoints, cell),
                    });
                }
            }
        }

        private static bool NearGate(Cell cell, IReadOnlyList<Cell> gates)
        {
            foreach (Cell gate in gates)
            {
                if (Distance(cell, gate) <= QueueCells * QueueCells)
                {
                    return true;
                }
            }

            return false;
        }

        // Within reach of a corner sharper than BendCos: walkers bunch on the inside of a turn.
        private static bool NearBend(Cell[] waypoints, Cell cell)
        {
            for (int i = 1; i + 1 < waypoints.Length; i++)
            {
                float ix = waypoints[i].X - waypoints[i - 1].X;
                float iy = waypoints[i].Y - waypoints[i - 1].Y;
                float ox = waypoints[i + 1].X - waypoints[i].X;
                float oy = waypoints[i + 1].Y - waypoints[i].Y;
                float li = (float)Math.Sqrt(ix * ix + iy * iy);
                float lo = (float)Math.Sqrt(ox * ox + oy * oy);
                if (li <= 0f || lo <= 0f)
                {
                    continue;
                }

                float cos = (ix * ox + iy * oy) / (li * lo);
                if (cos < BendCos && Distance(cell, waypoints[i]) <= BendReachCells * BendReachCells)
                {
                    return true;
                }
            }

            return false;
        }

        private static float Distance(Cell a, Sample b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private static float Distance(Cell a, Cell b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private static long Key(Cell cell) => ((long)cell.X << 32) ^ (uint)cell.Y;
    }
}
