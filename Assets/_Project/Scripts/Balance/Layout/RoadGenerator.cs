using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Lays out a map from a <see cref="LayoutSpec"/>: the template's leg skeleton with lengths, turn columns and the entry
    /// side jittered by the seed and the attempt, an optional second road joining the last leg at a junction waypoint, gates
    /// spread along the walk, the two flight routes, nests and terrain. Parallel legs keep exactly the build rows the wide
    /// road needs: a U-turn to the right (clockwise) has its second rows facing each other, so it needs two more rows than a
    /// turn to the left. Whether a layout is good enough is for <see cref="LayoutChecks"/> to say; the caller tries attempts
    /// in a fixed order and keeps the first that passes, so the result is deterministic.
    /// </summary>
    public static class RoadGenerator
    {
        /// <summary>Attempts the generator is given per level before the level fails.</summary>
        public const int Attempts = 24;

        /// <summary>Free build rows between two road corridors that run side by side.</summary>
        public const int MinBuildRows = 2;

        private const int EdgeMargin = 1;
        private const int SideRoadRow = 2;
        private const int MaxExtraGap = 3;
        private const int TurnInset = 4;
        private const int HookEntryMin = 3;
        private const int HookFinalRun = 8;
        private const int HookEndJitter = 2;
        private const int ForkJoinMin = 4;
        private const int ForkJoinMax = 8;
        private const int LateJoinMin = 9;
        private const int LateJoinMax = 12;
        private const int JoinClearance = 3;
        private const int JunctionBaseClearance = 6;
        private const int PlazaDepth = 2;
        private const int PlazaOverhang = 2;

        private const int GateCornerClearance = 3;
        private const int GateSpacing = 6;
        private const int LastGateMin = 5;
        private const int LastGateMax = 7;
        private const int JunctionGateMin = 4;
        private const int JunctionGateMax = 7;

        /// <summary>Walk cells downstream of a junction within which a gate still covers both roads from one screen.</summary>
        public const int JunctionGateReach = 10;
        private const int GateJitter = 2;

        private const int NestRoadMin = 3;
        private const int NestRoadMax = 5;
        private const double NestEarliest = 0.06;
        private const double NestLatest = 0.5;
        private const int NestBaseClearance = 8;
        private const int NestSpacing = 5;
        private const double NestPondMin = 1.6;
        private const double NestPondMax = 2.2;

        private const int TerrainTries = 80;
        private const double BlobMin = 1.5;
        private const double BlobMax = 3.0;
        private const int BlobLobes = 8;
        private const double LobeMin = 0.75;
        private const double LobeMax = 1.25;
        private const int LiquidRoadClearance = 2;
        private const int RaisedRoadClearance = 1;
        private const int LiquidGateClearance = 3;
        private const int BaseClearance = 4;
        private const int AirJitter = 3;

        public static RoadLayout Build(LayoutSpec spec, int attempt)
        {
            var random = new LayoutRandom(spec.Key, spec.Seed, attempt);
            var layout = new RoadLayout { Key = spec.Key, Attempt = attempt, Width = spec.Width, Height = spec.Height, RoadWidth = Math.Max(1, spec.RoadWidth) };
            string problem;
            List<Cell> road = spec.Template == RoadTemplate.Hook ? Hook(spec, random, out problem) : Serpentine(spec, random, spec.HasSecondRoad, out problem);
            if (road == null)
            {
                layout.Problem = problem;
                return layout;
            }

            layout.Road = road.ToArray();
            if (spec.HasSecondRoad)
            {
                AddSecondRoad(layout, spec, random);
            }

            AddAirRoutes(layout, random);
            PlaceGates(layout, spec, random);
            var field = new Field(layout);
            PlaceNests(layout, spec, random, field);
            PlaceTerrain(layout, spec, random, field);
            return layout;
        }

        /// <summary>Waypoint distance between two parallel legs joined by a U-turn, so the build rows between them stay free.</summary>
        public static int UTurnGap(bool clockwise, int roadWidth) => MinBuildRows + 1 + (clockwise ? 2 * (Math.Max(1, roadWidth) - 1) : 0);

        // ---- roads ---------------------------------------------------------------------------------------------------

        private static List<Cell> Serpentine(LayoutSpec spec, LayoutRandom random, bool sideRoad, out string problem)
        {
            int width = Math.Max(1, spec.RoadWidth);
            int legs = spec.Legs < 3 ? 3 : spec.Legs % 2 == 0 ? spec.Legs + 1 : spec.Legs;
            bool up = !sideRoad && random.Chance(0.5);
            var vertical = new Cell(0, up ? 1 : -1);
            var gaps = new int[legs - 1];
            int span = 0;
            for (int k = 0; k < gaps.Length; k++)
            {
                var along = new Cell(k % 2 == 0 ? 1 : -1, 0);
                gaps[k] = UTurnGap(LayoutGeometry.IsClockwise(along, vertical), width);
                span += gaps[k];
            }

            // The first and the last leg run east, with their extra rows to the south. A second road runs under the last.
            int lowest = sideRoad ? SideRoadRow + width + MinBuildRows : EdgeMargin + width - 1;
            int highest = spec.Height - 1 - EdgeMargin;
            int leftover = highest - lowest - span;
            if (leftover < 0)
            {
                problem = $"{legs} legs need {span + lowest + EdgeMargin + 1} rows, the grid has {spec.Height}";
                return null;
            }

            for (int k = 0; k < gaps.Length; k++)
            {
                int extra = Math.Min(leftover, random.Range(0, MaxExtraGap));
                gaps[k] += extra;
                leftover -= extra;
            }

            int margin = random.Range(0, leftover);
            int y = up ? lowest + margin : highest - margin;
            int right = spec.Width - 1 - EdgeMargin - (width - 1);
            int left = EdgeMargin + width - 1;
            var points = new List<Cell> { new Cell(0, y) };
            for (int k = 0; k < gaps.Length; k++)
            {
                int x = k % 2 == 0 ? right - random.Range(0, TurnInset) : left + random.Range(0, TurnInset);
                points.Add(new Cell(x, y));
                y += vertical.Y * gaps[k];
                points.Add(new Cell(x, y));
            }

            points.Add(new Cell(spec.Width - 1, y));
            problem = null;
            return points;
        }

        // Columns that wind down and up from west to east, entered by a short leg from the west edge; the last column turns
        // into a long run east to the base.
        private static List<Cell> Hook(LayoutSpec spec, LayoutRandom random, out string problem)
        {
            int width = Math.Max(1, spec.RoadWidth);
            int columns = Math.Max(3, spec.Legs);
            bool down = random.Chance(0.5);
            int first = down ? -1 : 1;
            var east = new Cell(1, 0);
            var gaps = new int[columns - 1];
            int span = 0;
            for (int k = 0; k < gaps.Length; k++)
            {
                var along = new Cell(0, k % 2 == 0 ? first : -first);
                gaps[k] = UTurnGap(LayoutGeometry.IsClockwise(along, east), width);
                span += gaps[k];
            }

            int x0 = HookEntryMin + random.Range(0, 2);
            int lastColumn = spec.Width - 1 - HookFinalRun;
            int leftover = lastColumn - x0 - span;
            if (leftover < 0)
            {
                problem = $"{columns} columns need {span + x0 + HookFinalRun + 1} cells across, the grid has {spec.Width}";
                return null;
            }

            // Spread the spare width evenly, so the columns cover the map instead of crowding its west side.
            int share = leftover / gaps.Length;
            for (int k = 0; k < gaps.Length; k++)
            {
                int extra = Math.Min(leftover, share + random.Range(-1, 1));
                extra = Math.Max(0, extra);
                gaps[k] += extra;
                leftover -= extra;
            }

            int high = spec.Height - 1 - EdgeMargin;
            int low = EdgeMargin + width - 1;
            if (high - low < HookEndJitter * 2 + MinBuildRows + 2)
            {
                problem = $"the grid is too low for columns ({spec.Height} rows)";
                return null;
            }

            int y = down ? high - random.Range(0, HookEndJitter) : low + random.Range(0, HookEndJitter);
            int x = x0;
            var points = new List<Cell> { new Cell(0, y), new Cell(x, y) };
            for (int k = 0; k < columns; k++)
            {
                bool goingDown = (k % 2 == 0) == down;
                y = goingDown ? low + random.Range(0, HookEndJitter) : high - random.Range(0, HookEndJitter);
                points.Add(new Cell(x, y));
                if (k < gaps.Length)
                {
                    x += gaps[k];
                    points.Add(new Cell(x, y));
                }
            }

            points.Add(new Cell(spec.Width - 1, y));
            problem = null;
            return points;
        }

        // A road from the south-west that runs east under the last leg and turns north onto it. The junction becomes a
        // waypoint of the main road, so the second road can carry on along it to the base.
        private static void AddSecondRoad(RoadLayout layout, LayoutSpec spec, LayoutRandom random)
        {
            Cell[] main = layout.Road;
            Cell end = main[main.Length - 1];
            Cell lastTurn = main[main.Length - 2];
            int joinX = spec.Template == RoadTemplate.Fork
                ? lastTurn.X + random.Range(ForkJoinMin, ForkJoinMax)
                : spec.Width - 1 - random.Range(LateJoinMin, LateJoinMax);
            joinX = Math.Max(lastTurn.X + JoinClearance, Math.Min(spec.Width - 1 - JunctionBaseClearance, joinX));
            var junction = new Cell(joinX, end.Y);

            var road = new List<Cell>(main);
            road.Insert(road.Count - 1, junction);
            layout.Road = road.ToArray();
            layout.SecondRoad = new[] { new Cell(0, SideRoadRow), new Cell(joinX, SideRoadRow), junction };

            if (spec.Plazas <= 0)
            {
                return;
            }

            // The square sits in the elbow under the last leg, around the place the second road comes up.
            int width = layout.RoadWidth;
            int top = end.Y - width;
            for (int px = joinX - PlazaOverhang; px <= joinX + width - 1 + PlazaOverhang; px++)
            {
                for (int py = top - PlazaDepth + 1; py <= top; py++)
                {
                    layout.PlazaCells.Add(new Cell(px, py));
                }
            }
        }

        private static void AddAirRoutes(RoadLayout layout, LayoutRandom random)
        {
            int middle = layout.Width / 2;
            layout.NorthAir = new[] { new Cell(0, layout.Height - 1), new Cell(middle + random.Range(-AirJitter, AirJitter), layout.Height - 2), layout.Base };
            layout.SouthAir = new[] { new Cell(0, 0), new Cell(middle + random.Range(-AirJitter, AirJitter), 1), layout.Base };
        }

        // ---- gates ---------------------------------------------------------------------------------------------------

        // One gate a few cells before the base (the last stand, and the screen the flight routes cross), one a little past
        // a junction (so one defence covers both roads), the rest spread over the walk; all on straight cells.
        private static void PlaceGates(RoadLayout layout, LayoutSpec spec, LayoutRandom random)
        {
            List<Cell> walk = LayoutGeometry.Walk(layout.Road);
            int count = walk.Count;
            var valid = new bool[count];
            List<int> turns = TurnIndices(layout.Road, walk);
            HashSet<long> side = layout.SecondRoad != null ? LayoutGeometry.Corridor(layout.SecondRoad, layout.RoadWidth) : new HashSet<long>();
            for (int i = 0; i < count; i++)
            {
                valid[i] = i >= 2 && i <= count - 3 && FarFromTurns(i, turns) && FarFrom(walk[i], layout.PlazaCells, 2) && !Near(walk[i], side, 1);
            }

            var chosen = new List<int>();
            int last = Pick(count - 1 - random.Range(LastGateMin, LastGateMax), valid, chosen, count);
            int earliestFixed = last;
            if (layout.SecondRoad != null)
            {
                // A late junction already has the last gate within a screen downstream of it.
                int junction = LayoutGeometry.IndexOf(walk, layout.SecondRoad[layout.SecondRoad.Length - 1]);
                bool covered = last >= 0 && last - junction >= JunctionGateMin && last - junction <= JunctionGateReach;
                int afterJunction = covered ? -1 : Pick(junction + random.Range(JunctionGateMin, JunctionGateMax), valid, chosen, count);
                if (afterJunction >= 0)
                {
                    earliestFixed = Math.Min(earliestFixed, afterJunction);
                }
            }

            int rest = Math.Max(0, spec.Gates - chosen.Count);
            double from = count * spec.FirstGateShare;
            double to = Math.Max(from, earliestFixed - GateSpacing);
            for (int r = 0; r < rest; r++)
            {
                double target = rest == 1 ? (from + to) * 0.5 : from + (to - from) * r / (rest - 1);
                Pick((int)Math.Round(target) + random.Range(-GateJitter, GateJitter), valid, chosen, count);
            }

            chosen.Sort();
            foreach (int index in chosen)
            {
                layout.Gates.Add(walk[index]);
            }
        }

        private static List<int> TurnIndices(Cell[] road, List<Cell> walk)
        {
            var turns = new List<int>();
            for (int i = 1; i < road.Length - 1; i++)
            {
                Cell inStep = LayoutGeometry.Step(road[i - 1], road[i]);
                Cell outStep = LayoutGeometry.Step(road[i], road[i + 1]);
                if (inStep.X != outStep.X || inStep.Y != outStep.Y)
                {
                    turns.Add(LayoutGeometry.IndexOf(walk, road[i]));
                }
            }

            return turns;
        }

        private static bool FarFromTurns(int index, List<int> turns)
        {
            foreach (int turn in turns)
            {
                if (Math.Abs(turn - index) < GateCornerClearance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FarFrom(Cell cell, List<Cell> cells, int distance)
        {
            foreach (Cell other in cells)
            {
                if (LayoutGeometry.Chebyshev(cell, other) < distance)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Near(Cell cell, HashSet<long> cells, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    if (cells.Contains(LayoutGeometry.Key(cell.X + dx, cell.Y + dy)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // The valid walk index nearest to a target that keeps its distance from the gates already chosen, or -1.
        private static int Pick(int target, bool[] valid, List<int> chosen, int count)
        {
            for (int offset = 0; offset < count; offset++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    int index = target + offset * sign;
                    if (index >= 0 && index < count && valid[index] && Spaced(index, chosen))
                    {
                        chosen.Add(index);
                        return index;
                    }
                }
            }

            return -1;
        }

        private static bool Spaced(int index, List<int> chosen)
        {
            foreach (int other in chosen)
            {
                if (Math.Abs(other - index) < GateSpacing)
                {
                    return false;
                }
            }

            return true;
        }

        // ---- nests and terrain ---------------------------------------------------------------------------------------

        private static void PlaceNests(RoadLayout layout, LayoutSpec spec, LayoutRandom random, Field field)
        {
            foreach (NestCount nests in spec.Nests)
            {
                int kind = LayoutTerrain.KindOf(nests.Element);
                for (int n = 0; n < nests.Count; n++)
                {
                    var candidates = new List<Cell>();
                    for (int x = EdgeMargin; x < layout.Width - EdgeMargin; x++)
                    {
                        for (int y = EdgeMargin; y < layout.Height - EdgeMargin; y++)
                        {
                            var cell = new Cell(x, y);
                            if (IsNestCandidate(layout, field, cell))
                            {
                                candidates.Add(cell);
                            }
                        }
                    }

                    if (candidates.Count == 0)
                    {
                        return;
                    }

                    Cell nest = candidates[random.Range(0, candidates.Count - 1)];
                    layout.Nests.Add(new LayoutNest(nest, nests.Element));
                    if (LayoutTerrain.IsLiquid(kind))
                    {
                        Blob(layout, field, nest, random.Range(NestPondMin, NestPondMax), kind, random);
                        field.Set(nest, kind);
                    }
                }
            }
        }

        // A nest stands a few cells off the main road, early enough that its creatures still walk most of the way.
        private static bool IsNestCandidate(RoadLayout layout, Field field, Cell cell)
        {
            int road = field.RoadDistance(cell);
            if (road < NestRoadMin || road > NestRoadMax || field.Kind(cell) != LayoutTerrain.Ground)
            {
                return false;
            }

            if (LayoutGeometry.Chebyshev(cell, layout.Base) < NestBaseClearance || !FarFrom(cell, layout.Gates, LiquidGateClearance))
            {
                return false;
            }

            foreach (LayoutNest other in layout.Nests)
            {
                if (LayoutGeometry.Chebyshev(cell, other.Cell) < NestSpacing)
                {
                    return false;
                }
            }

            double progress = field.MainProgress(cell, out bool nearestIsMain);
            return nearestIsMain && progress >= NestEarliest && progress <= NestLatest;
        }

        private static void PlaceTerrain(RoadLayout layout, LayoutSpec spec, LayoutRandom random, Field field)
        {
            int cells = layout.Width * layout.Height;
            foreach (TerrainShare share in spec.Terrain)
            {
                int kind = LayoutTerrain.KindOf(share.Element);
                if (kind == LayoutTerrain.Ground)
                {
                    continue;
                }

                int target = (int)Math.Round(share.Share * cells);
                bool liquid = LayoutTerrain.IsLiquid(kind);
                for (int tries = 0; tries < TerrainTries && field.Count(kind) < target; tries++)
                {
                    var centre = new Cell(random.Range(0, layout.Width - 1), random.Range(0, layout.Height - 1));
                    if (field.RoadDistance(centre) < (liquid ? LiquidRoadClearance + 1 : RaisedRoadClearance + 1) || field.Kind(centre) != LayoutTerrain.Ground)
                    {
                        continue;
                    }

                    Blob(layout, field, centre, random.Range(BlobMin, BlobMax), kind, random);
                }
            }

            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    int kind = field.Kind(new Cell(x, y));
                    if (kind != LayoutTerrain.Ground)
                    {
                        layout.Terrain.Add(new LayoutPatch(new Cell(x, y), kind));
                    }
                }
            }
        }

        // A rounded patch with a lumpy outline: the radius swells and shrinks around the centre.
        private static void Blob(RoadLayout layout, Field field, Cell centre, double radius, int kind, LayoutRandom random)
        {
            var lobes = new double[BlobLobes];
            for (int i = 0; i < lobes.Length; i++)
            {
                lobes[i] = random.Range(LobeMin, LobeMax);
            }

            bool liquid = LayoutTerrain.IsLiquid(kind);
            int reach = (int)Math.Ceiling(radius * LobeMax);
            for (int dx = -reach; dx <= reach; dx++)
            {
                for (int dy = -reach; dy <= reach; dy++)
                {
                    var cell = new Cell(centre.X + dx, centre.Y + dy);
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance > radius * Lobe(lobes, Math.Atan2(dy, dx)) || !field.Inside(cell) || field.Kind(cell) != LayoutTerrain.Ground)
                    {
                        continue;
                    }

                    if (field.RoadDistance(cell) < (liquid ? LiquidRoadClearance : RaisedRoadClearance) || LayoutGeometry.Chebyshev(cell, layout.Base) < BaseClearance)
                    {
                        continue;
                    }

                    if (liquid && (!FarFrom(cell, layout.Gates, LiquidGateClearance) || NearDryNest(layout, cell)))
                    {
                        continue;
                    }

                    field.Set(cell, kind);
                }
            }
        }

        private static double Lobe(double[] lobes, double angle)
        {
            double position = (angle + Math.PI) / (2.0 * Math.PI) * lobes.Length;
            int a = (int)Math.Floor(position) % lobes.Length;
            int b = (a + 1) % lobes.Length;
            double t = position - Math.Floor(position);
            return lobes[a] + (lobes[b] - lobes[a]) * t;
        }

        // Nests on dry ground (a hollow tree, a rock cleft) must stay dry, with a step of ground around them.
        private static bool NearDryNest(RoadLayout layout, Cell cell)
        {
            foreach (LayoutNest nest in layout.Nests)
            {
                if (!LayoutTerrain.IsLiquid(LayoutTerrain.KindOf(nest.Element)) && LayoutGeometry.Chebyshev(cell, nest.Cell) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The grid as the terrain pass sees it: what each cell holds and how far it lies from the nearest road cell.</summary>
        private sealed class Field
        {
            private readonly int _width;
            private readonly int _height;
            private readonly int[] _kind;
            private readonly int[] _roadDistance;
            private readonly List<Cell> _mainWalk;
            private readonly List<Cell> _sideWalk;
            private readonly int[] _counts = new int[LayoutTerrain.Last + 1];

            public Field(RoadLayout layout)
            {
                _width = layout.Width;
                _height = layout.Height;
                _kind = new int[_width * _height];
                _roadDistance = new int[_width * _height];
                _mainWalk = LayoutGeometry.Walk(layout.Road);
                _sideWalk = layout.SecondRoad != null ? LayoutGeometry.Walk(layout.SecondRoad) : new List<Cell>();

                var road = LayoutGeometry.Corridor(layout.Road, layout.RoadWidth);
                if (layout.SecondRoad != null)
                {
                    road.UnionWith(LayoutGeometry.Corridor(layout.SecondRoad, layout.RoadWidth));
                }

                foreach (Cell cell in layout.PlazaCells)
                {
                    road.Add(LayoutGeometry.Key(cell));
                }

                MeasureRoadDistance(road);
            }

            public bool Inside(Cell cell) => cell.X >= 0 && cell.Y >= 0 && cell.X < _width && cell.Y < _height;

            public int Kind(Cell cell) => Inside(cell) ? _kind[cell.X * _height + cell.Y] : LayoutTerrain.Ground;

            public int Count(int kind) => _counts[kind];

            public void Set(Cell cell, int kind)
            {
                if (!Inside(cell))
                {
                    return;
                }

                int index = cell.X * _height + cell.Y;
                _counts[_kind[index]]--;
                _kind[index] = kind;
                _counts[kind]++;
            }

            public int RoadDistance(Cell cell) => Inside(cell) ? _roadDistance[cell.X * _height + cell.Y] : 0;

            /// <summary>Share of the main road walked where a creature from this cell would join it, and whether the main road is the nearest.</summary>
            public double MainProgress(Cell cell, out bool nearestIsMain)
            {
                int index = LayoutGeometry.Nearest(_mainWalk, cell, out int mainDistance);
                nearestIsMain = true;
                if (_sideWalk.Count > 0)
                {
                    LayoutGeometry.Nearest(_sideWalk, cell, out int sideDistance);
                    nearestIsMain = mainDistance <= sideDistance;
                }

                return index / (double)Math.Max(1, _mainWalk.Count - 1);
            }

            // Eight-way breadth-first search from every road cell gives the Chebyshev distance to the nearest one.
            private void MeasureRoadDistance(HashSet<long> road)
            {
                _counts[LayoutTerrain.Ground] = _width * _height;
                var open = new Queue<int>();
                for (int i = 0; i < _roadDistance.Length; i++)
                {
                    int x = i / _height;
                    int y = i % _height;
                    if (road.Contains(LayoutGeometry.Key(x, y)))
                    {
                        _roadDistance[i] = 0;
                        open.Enqueue(i);
                    }
                    else
                    {
                        _roadDistance[i] = int.MaxValue;
                    }
                }

                while (open.Count > 0)
                {
                    int current = open.Dequeue();
                    int cx = current / _height;
                    int cy = current % _height;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = cx + dx;
                            int ny = cy + dy;
                            if (nx < 0 || ny < 0 || nx >= _width || ny >= _height)
                            {
                                continue;
                            }

                            int next = nx * _height + ny;
                            if (_roadDistance[next] > _roadDistance[current] + 1)
                            {
                                _roadDistance[next] = _roadDistance[current] + 1;
                                open.Enqueue(next);
                            }
                        }
                    }
                }
            }
        }
    }
}
