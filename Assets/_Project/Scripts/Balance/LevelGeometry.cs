using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>What one build spot covers of a level, as the ledger counts a tower's time on target (round 22).</summary>
    public struct SpotCover
    {
        /// <summary>Cells of walking route in reach, weighted by the share of the creatures that walk each cell.</summary>
        public float GroundPass;

        /// <summary>
        /// The part of <see cref="GroundPass"/> that lies before the defence gate (round 26): the only road a walker walks while
        /// the gate stands, so the only cover a gun kills on the walk from. The rest of its reach is the queue and the road behind
        /// the gate (<see cref="BehindPass"/>), walked only once the gate is down. Without a defence gate the whole route is approach.
        /// </summary>
        public float ApproachPass;

        /// <summary>Weighted route cells in reach past the defence gate: a gun there shoots what gets through, on the way to the next gate.</summary>
        public float BehindPass;

        /// <summary>Share of the ground cover that is approach (one without a defence gate or with nothing in reach).</summary>
        public float ApproachShare => GroundPass > 0f ? ApproachPass / GroundPass : 1f;

        public float BehindShare => GroundPass > 0f ? BehindPass / GroundPass : 0f;

        /// <summary>Cells of flight route in reach, weighted the same way.</summary>
        public float AirPass;

        /// <summary>True when the defence gate's queue is in reach: the gun fires the whole time the gate holds the walkers.</summary>
        public bool Gate;

        /// <summary>Share of the ground route in reach where walkers bunch (the queue before a gate, the inside of a bend).</summary>
        public float Bunch;

        /// <summary>True when the base's doorstep (the last road cells) is in reach: the gun shoots what lays siege to the base.</summary>
        public bool Base;

        /// <summary>The spot this cover was computed from (round 27: the route model walks the gun from it); none for an ideal or typical cover.</summary>
        public Cell? Spot;

        /// <summary>The ledger's spotless assumption: a straight pass through the whole reach, the gate queue and the doorstep in reach.</summary>
        public static SpotCover Ideal(float reach) => new SpotCover { GroundPass = 2f * reach, ApproachPass = 2f * reach, AirPass = 2f * reach, Gate = true, Bunch = 0f, Base = true };
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

        /// <summary>Road cells before the base where leaked walkers stand and hammer it.</summary>
        public const int DoorstepCells = 3;

        private struct Sample
        {
            public float X;
            public float Y;
            public float Weight;
            public bool Bunched;

            /// <summary>Past the defence gate along its road: not walked while that gate stands.</summary>
            public bool Behind;
        }

        private readonly List<Sample> _ground = new List<Sample>();
        private readonly List<Sample> _air = new List<Sample>();

        /// <summary>One road or flight path in walking order, for the route-sampled kill model (round 27).</summary>
        public sealed class Route
        {
            public int PathIndex;
            public readonly List<Cell> Cells = new List<Cell>();

            /// <summary>Index of the cell the defence gate stands on along this route, or -1 when the route does not pass it (all approach).</summary>
            public int GateIndex = -1;

            /// <summary>The last index walked while the defence gate stands (the gate's own cell, or the end of a route without one).</summary>
            public int ApproachEnd => GateIndex >= 0 ? GateIndex : Cells.Count - 1;
        }

        /// <summary>The walking routes (the main road first) and the flight routes, cell by cell in travel order.</summary>
        public readonly List<Route> GroundRoutes = new List<Route>();
        public readonly List<Route> AirRoutes = new List<Route>();
        private readonly Dictionary<(long, int, int), int[]> _inReach = new Dictionary<(long, int, int), int[]>();

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

            SampleRoutes(roads, gates, defenceGate, geometry._ground);
            SampleRoutes(flights, new List<Cell>(), null, geometry._air);
            foreach (RoadInput road in roads)
            {
                var route = new Route { PathIndex = road.PathIndex };
                route.Cells.AddRange(LevelShape.Expand(road.Waypoints));
                route.GateIndex = GateIndex(route.Cells, defenceGate);
                geometry.GroundRoutes.Add(route);
            }

            foreach (RoadInput flight in flights)
            {
                var route = new Route { PathIndex = flight.PathIndex };
                route.Cells.AddRange(LevelShape.Expand(flight.Waypoints));
                geometry.AirRoutes.Add(route);
            }

            return geometry;
        }

        /// <summary>The walking route creatures of a path take: the route with that path index, else the main road.</summary>
        public Route GroundRouteFor(int pathIndex) => RouteFor(GroundRoutes, pathIndex);

        public Route AirRouteFor(int pathIndex) => RouteFor(AirRoutes, pathIndex);

        private static Route RouteFor(List<Route> routes, int pathIndex)
        {
            foreach (Route route in routes)
            {
                if (route.PathIndex == pathIndex)
                {
                    return route;
                }
            }

            return routes.Count > 0 ? routes[0] : null;
        }

        /// <summary>The indices of a route's cells within a tower's reach from a spot, in travel order (cached per spot, reach and route).</summary>
        public int[] InReach(Cell spot, float reach, Route route)
        {
            int routeIndex = GroundRoutes.IndexOf(route);
            routeIndex = routeIndex >= 0 ? routeIndex : GroundRoutes.Count + AirRoutes.IndexOf(route);
            (long, int, int) key = (Key(spot), (int)Math.Round(reach * 1000f), routeIndex);
            if (_inReach.TryGetValue(key, out int[] known))
            {
                return known;
            }

            float cells = ReachAt(spot, reach) / CellSize;
            float radius = cells * cells;
            var hits = new List<int>();
            for (int i = 0; i < route.Cells.Count; i++)
            {
                if (Distance(spot, route.Cells[i]) <= radius)
                {
                    hits.Add(i);
                }
            }

            int[] result = hits.ToArray();
            _inReach[key] = result;
            return result;
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
            var cover = new SpotCover { Spot = spot };
            float bunched = 0f;
            foreach (Sample sample in _ground)
            {
                if (Distance(spot, sample) <= radius)
                {
                    cover.GroundPass += sample.Weight;
                    cover.ApproachPass += sample.Behind ? 0f : sample.Weight;
                    cover.BehindPass += sample.Behind ? sample.Weight : 0f;
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
            foreach (Cell door in Doorstep())
            {
                cover.Base |= Distance(spot, door) <= radius;
            }

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
            int bases = 0;
            for (int i = 0; i < n; i++)
            {
                SpotCover cover = CoverAt(ranked[i], reach);
                mean.GroundPass += cover.GroundPass / n;
                mean.ApproachPass += cover.ApproachPass / n;
                mean.BehindPass += cover.BehindPass / n;
                mean.AirPass += cover.AirPass / n;
                mean.Bunch += cover.Bunch / n;
                gates += cover.Gate ? 1 : 0;
                bases += cover.Base ? 1 : 0;
            }

            mean.Gate = gates * 2 >= n;
            mean.Base = bases * 2 >= n;
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

        /// <summary>The last cells of the main road: where walkers that got through stand and hit the base.</summary>
        public List<Cell> Doorstep()
        {
            var cells = new List<Cell>();
            for (int k = 1; k <= DoorstepCells && MainRoad.Count - k >= 0; k++)
            {
                cells.Add(MainRoad[MainRoad.Count - k]);
            }

            return cells;
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

        private static void SampleRoutes(IReadOnlyList<RoadInput> roads, IReadOnlyList<Cell> gates, Cell? defenceGate, List<Sample> output)
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
                int gateAt = GateIndex(cells, defenceGate);
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
                        Behind = gateAt >= 0 && i > gateAt,
                    });
                }
            }
        }

        // The index of the road cell the defence gate stands on (the nearest within a cell), or -1 when the road does not pass it:
        // a lane that joins the trunk behind the gate, or a level without one.
        private static int GateIndex(List<Cell> cells, Cell? defenceGate)
        {
            if (!defenceGate.HasValue)
            {
                return -1;
            }

            int at = -1;
            float best = float.MaxValue;
            for (int i = 0; i < cells.Count; i++)
            {
                float distance = Distance(cells[i], defenceGate.Value);
                if (distance < best)
                {
                    best = distance;
                    at = i;
                }
            }

            return best <= QueueCells * QueueCells ? at : -1;
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
