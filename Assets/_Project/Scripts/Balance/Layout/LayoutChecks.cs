using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Decides whether a generated layout plays fairly, before any asset exists: map size, road length, the build rows
    /// between parallel legs, gates (count, straight cells, spacing, build plots and screen cover), fronts, the junction of
    /// a second road, nests and terrain shares. Pure and deterministic, so it chooses the generator's attempt in the
    /// editor and in a dotnet tool alike; the game's <c>LevelValidator</c> asserts the result afterwards.
    /// </summary>
    public static class LayoutChecks
    {
        public const int MinRoadLength = 74;
        public const int MaxRoadLength = 141;
        public const int MinGates = 3;
        public const int MaxGates = 5;
        public const int MinPlotsPerGate = 8;
        public const float MaxNestShare = 0.6f;
        public const int NestBaseClearance = 6;
        public const int MinWidth = 28;
        public const int MaxWidth = 44;
        public const int MinHeight = 16;
        public const int MaxHeight = 26;
        public const float MinAspect = 1.4f;
        public const float MaxAspect = 2.2f;

        /// <summary>
        /// Half the ground a play-zoomed screen shows, in cells: about 21 by 16 cells is 85% of a 16:9 view (Docs/STATUS,
        /// defence-zone rule). The validator measures the real camera; this is the same rule without the engine.
        /// </summary>
        public const float ScreenHalfX = 10f;
        public const float ScreenHalfY = 7.5f;

        private const float TerrainShareLow = 0.5f;
        private const float TerrainShareHigh = 1.6f;
        private const int MinTerrainCells = 6;
        private const float MinJunctionProgress = 0.5f;
        private const int GateTurnClearance = 2;
        private const int GateSpacing = 6;

        public static List<string> Check(RoadLayout layout, LayoutSpec spec, BalanceRules rules)
        {
            var errors = new List<string>();
            if (layout.Problem != null)
            {
                errors.Add(layout.Problem);
                return errors;
            }

            CheckSize(layout, errors);
            List<Cell> walk = LayoutGeometry.Walk(layout.Road);
            CheckRoad(layout, walk, errors);
            CheckSpacing(layout, errors);
            CheckGates(layout, spec, walk, errors);
            CheckScreens(layout, errors);
            CheckFronts(layout, spec, rules, errors);
            CheckJunction(layout, walk, errors);
            CheckNests(layout, spec, walk, errors);
            CheckTerrain(layout, spec, errors);
            return errors;
        }

        private static void CheckSize(RoadLayout layout, List<string> errors)
        {
            float aspect = layout.Width / (float)Math.Max(1, layout.Height);
            if (layout.Width < MinWidth || layout.Width > MaxWidth || layout.Height < MinHeight || layout.Height > MaxHeight || aspect < MinAspect || aspect > MaxAspect)
            {
                errors.Add($"map {layout.Width}x{layout.Height} does not fit the screen ({MinWidth}-{MaxWidth} by {MinHeight}-{MaxHeight}, aspect {MinAspect}-{MaxAspect})");
            }
        }

        // The road runs from the west edge to the base on the east edge, in straight legs, and never leaves the grid.
        private static void CheckRoad(RoadLayout layout, List<Cell> walk, List<string> errors)
        {
            int length = walk.Count - 1;
            if (length < MinRoadLength || length > MaxRoadLength)
            {
                errors.Add($"road is {length} cells long, not {MinRoadLength}-{MaxRoadLength}");
            }

            if (layout.Road[0].X != 0 || layout.Base.X != layout.Width - 1)
            {
                errors.Add("road must enter at the west edge and end at the base on the east edge");
            }

            CheckLegs(layout.Road, "road", errors);
            if (layout.SecondRoad != null)
            {
                CheckLegs(layout.SecondRoad, "second road", errors);
            }

            var corridor = AllRoadCells(layout);
            foreach (long key in corridor)
            {
                int x = (int)(key >> 32);
                int y = (int)(key & 0xffffffffL);
                if (x < 0 || y < 0 || x >= layout.Width || y >= layout.Height)
                {
                    errors.Add($"road cell ({x},{y}) lies off the grid");
                    return;
                }
            }
        }

        private static void CheckLegs(Cell[] road, string name, List<string> errors)
        {
            for (int i = 1; i < road.Length; i++)
            {
                bool straight = road[i].X == road[i - 1].X || road[i].Y == road[i - 1].Y;
                bool moves = road[i].X != road[i - 1].X || road[i].Y != road[i - 1].Y;
                if (!straight || !moves)
                {
                    errors.Add($"{name} leg {i - 1}-{i} is not a straight step");
                }

                if (i >= 2)
                {
                    Cell a = LayoutGeometry.Step(road[i - 2], road[i - 1]);
                    Cell b = LayoutGeometry.Step(road[i - 1], road[i]);
                    if (a.X == -b.X && a.Y == -b.Y)
                    {
                        errors.Add($"{name} turns back on itself at {road[i - 1]}");
                    }
                }
            }
        }

        // Two road corridors that are not joined keep at least two free build rows between them, and so does a plaza.
        private static void CheckSpacing(RoadLayout layout, List<string> errors)
        {
            var segments = new List<Segment>();
            AddSegments(segments, layout.Road, 0, layout.RoadWidth);
            if (layout.SecondRoad != null)
            {
                AddSegments(segments, layout.SecondRoad, 1, layout.RoadWidth);
            }

            for (int a = 0; a < segments.Count; a++)
            {
                for (int b = a + 1; b < segments.Count; b++)
                {
                    if (segments[a].Touches(segments[b]))
                    {
                        continue;
                    }

                    int free = LayoutGeometry.Distance(segments[a].Cells, segments[b].Cells) - 1;
                    if (free < RoadGenerator.MinBuildRows)
                    {
                        errors.Add($"legs {segments[a]} and {segments[b]} leave {free} build rows between them, need {RoadGenerator.MinBuildRows}");
                    }
                }
            }

            if (layout.PlazaCells.Count == 0)
            {
                return;
            }

            foreach (Segment segment in segments)
            {
                int distance = LayoutGeometry.Distance(segment.Cells, layout.PlazaCells);
                if (distance > 1 && distance - 1 < RoadGenerator.MinBuildRows)
                {
                    errors.Add($"plaza leaves {distance - 1} build rows to leg {segment}, need {RoadGenerator.MinBuildRows}");
                }
            }
        }

        private static void AddSegments(List<Segment> segments, Cell[] road, int roadIndex, int width)
        {
            for (int i = 1; i < road.Length; i++)
            {
                segments.Add(new Segment(roadIndex, road[i - 1], road[i], LayoutGeometry.SegmentCorridor(road[i - 1], road[i], width)));
            }
        }

        private static void CheckGates(RoadLayout layout, LayoutSpec spec, List<Cell> walk, List<string> errors)
        {
            if (layout.Gates.Count < MinGates || layout.Gates.Count > MaxGates || layout.Gates.Count != spec.Gates)
            {
                errors.Add($"{layout.Gates.Count} gates placed, the spec asks {spec.Gates} ({MinGates}-{MaxGates})");
            }

            var indices = new List<int>();
            foreach (Cell gate in layout.Gates)
            {
                int index = LayoutGeometry.IndexOf(walk, gate);
                if (index < 0)
                {
                    errors.Add($"gate {gate} is not on the main road's first row");
                    continue;
                }

                if (!Straight(walk, index))
                {
                    errors.Add($"gate {gate} stands on or next to a turn");
                }

                foreach (Cell plaza in layout.PlazaCells)
                {
                    if (LayoutGeometry.Chebyshev(gate, plaza) <= 1)
                    {
                        errors.Add($"gate {gate} touches the plaza");
                        break;
                    }
                }

                int plots = PlotsAround(layout, gate);
                if (plots < MinPlotsPerGate)
                {
                    errors.Add($"gate {gate} has {plots} build cells within one screen, needs {MinPlotsPerGate}");
                }

                indices.Add(index);
            }

            indices.Sort();
            for (int i = 1; i < indices.Count; i++)
            {
                if (indices[i] - indices[i - 1] < GateSpacing)
                {
                    errors.Add($"gates at walk cells {indices[i - 1]} and {indices[i]} stand closer than {GateSpacing}");
                }
            }
        }

        private static bool Straight(List<Cell> walk, int index)
        {
            for (int k = Math.Max(1, index - GateTurnClearance); k <= Math.Min(walk.Count - 2, index + GateTurnClearance); k++)
            {
                Cell before = LayoutGeometry.Step(walk[k - 1], walk[k]);
                Cell after = LayoutGeometry.Step(walk[k], walk[k + 1]);
                if (before.X != after.X || before.Y != after.Y)
                {
                    return false;
                }
            }

            return index > 0 && index < walk.Count - 1;
        }

        // Ground in view of a gate that is neither road nor liquid; rocks are not known yet, so the count is a little generous.
        private static int PlotsAround(RoadLayout layout, Cell gate)
        {
            HashSet<long> road = AllRoadCells(layout);
            var liquid = new HashSet<long>();
            foreach (LayoutPatch patch in layout.Terrain)
            {
                if (LayoutTerrain.IsLiquid(patch.Kind))
                {
                    liquid.Add(LayoutGeometry.Key(patch.Cell));
                }
            }

            int plots = 0;
            for (int x = 0; x < layout.Width; x++)
            {
                for (int y = 0; y < layout.Height; y++)
                {
                    long key = LayoutGeometry.Key(x, y);
                    if (InScreen(new Cell(x, y), gate) && !road.Contains(key) && !liquid.Contains(key))
                    {
                        plots++;
                    }
                }
            }

            return plots;
        }

        private static bool InScreen(Cell point, Cell centre) => Math.Abs(point.X - centre.X) <= ScreenHalfX && Math.Abs(point.Y - centre.Y) <= ScreenHalfY;

        // Every lane, flight routes included, passes some gate's screen, and any two roads share one, so one defence covers both.
        private static void CheckScreens(RoadLayout layout, List<string> errors)
        {
            var lanes = new List<(string Name, List<Cell> Cells, bool Ground)>
            {
                ("road", LayoutGeometry.Walk(layout.Road), true),
                ("north air route", Fly(layout.NorthAir), false),
                ("south air route", Fly(layout.SouthAir), false),
            };
            if (layout.SecondRoad != null)
            {
                lanes.Add(("second road", LayoutGeometry.Walk(layout.SecondRoadToBase), true));
            }

            var covered = new List<HashSet<int>>();
            foreach (Cell gate in layout.Gates)
            {
                var set = new HashSet<int>();
                for (int l = 0; l < lanes.Count; l++)
                {
                    foreach (Cell cell in lanes[l].Cells)
                    {
                        if (InScreen(cell, gate))
                        {
                            set.Add(l);
                            break;
                        }
                    }
                }

                covered.Add(set);
            }

            for (int l = 0; l < lanes.Count; l++)
            {
                if (!covered.Exists(s => s.Contains(l)))
                {
                    errors.Add($"the {lanes[l].Name} never passes within a screen of a gate");
                }

                for (int m = l + 1; m < lanes.Count; m++)
                {
                    if (lanes[l].Ground && lanes[m].Ground && !covered.Exists(s => s.Contains(l) && s.Contains(m)))
                    {
                        errors.Add($"the {lanes[l].Name} and the {lanes[m].Name} share no gate's screen");
                    }
                }
            }
        }

        // Points along a flight route, one per cell of its longest axis, so a screen it only clips still counts.
        private static List<Cell> Fly(Cell[] route)
        {
            var cells = new List<Cell>();
            for (int i = 1; i < route.Length; i++)
            {
                int steps = Math.Max(1, Math.Max(Math.Abs(route[i].X - route[i - 1].X), Math.Abs(route[i].Y - route[i - 1].Y)));
                for (int s = 0; s <= steps; s++)
                {
                    cells.Add(new Cell(route[i - 1].X + (route[i].X - route[i - 1].X) * s / steps, route[i - 1].Y + (route[i].Y - route[i - 1].Y) * s / steps));
                }
            }

            return cells;
        }

        private static void CheckFronts(RoadLayout layout, LayoutSpec spec, BalanceRules rules, List<string> errors)
        {
            var roads = new List<RoadInput> { new RoadInput { PathIndex = 0, Waypoints = layout.Road } };
            if (layout.SecondRoad != null)
            {
                roads.Add(new RoadInput { PathIndex = 3, Waypoints = layout.SecondRoadToBase });
            }

            LevelShape shape = LevelShape.Build(roads, layout.Gates, new Cell[0], null);
            int fronts = shape.GroundFronts(rules.TrunkReference);
            if (fronts > spec.MaxFronts)
            {
                errors.Add($"{fronts} ground fronts, the tempo allows {spec.MaxFronts}");
            }
        }

        // A second road ends on a waypoint of the main road, past the middle of it, and does not join where gates stand.
        private static void CheckJunction(RoadLayout layout, List<Cell> walk, List<string> errors)
        {
            if (layout.SecondRoad == null)
            {
                return;
            }

            Cell junction = layout.SecondRoad[layout.SecondRoad.Length - 1];
            if (LayoutGeometry.IndexOf(layout.Road, junction) < 0)
            {
                errors.Add($"second road ends at {junction}, which is no waypoint of the main road");
                return;
            }

            float progress = LayoutGeometry.IndexOf(walk, junction) / (float)Math.Max(1, walk.Count - 1);
            if (progress < MinJunctionProgress)
            {
                errors.Add($"second road joins at {progress:P0} of the main road, before half of it");
            }

            bool downstream = false;
            int index = LayoutGeometry.IndexOf(walk, junction);
            foreach (Cell gate in layout.Gates)
            {
                int gateIndex = LayoutGeometry.IndexOf(walk, gate);
                downstream |= gateIndex > index && gateIndex - index <= RoadGenerator.JunctionGateReach;
            }

            if (!downstream)
            {
                errors.Add($"no gate within {RoadGenerator.JunctionGateReach} cells downstream of the junction");
            }
        }

        // Nests: the asked number, off the road, in their own liquid, joining the road early, never beside the base.
        private static void CheckNests(RoadLayout layout, LayoutSpec spec, List<Cell> walk, List<string> errors)
        {
            int wanted = 0;
            foreach (NestCount count in spec.Nests)
            {
                wanted += count.Count;
            }

            if (layout.Nests.Count != wanted)
            {
                errors.Add($"{layout.Nests.Count} nests placed, the spec asks {wanted}");
            }

            HashSet<long> road = AllRoadCells(layout);
            var terrain = new Dictionary<long, int>();
            foreach (LayoutPatch patch in layout.Terrain)
            {
                terrain[LayoutGeometry.Key(patch.Cell)] = patch.Kind;
            }

            foreach (LayoutNest nest in layout.Nests)
            {
                long key = LayoutGeometry.Key(nest.Cell);
                if (road.Contains(key))
                {
                    errors.Add($"{nest.Element} nest at {nest.Cell} lies on the road");
                }

                int kind = LayoutTerrain.KindOf(nest.Element);
                terrain.TryGetValue(key, out int under);
                if (LayoutTerrain.IsLiquid(kind) ? under != kind : LayoutTerrain.IsLiquid(under))
                {
                    errors.Add($"{nest.Element} nest at {nest.Cell} sits in the wrong ground");
                }

                float share = LayoutGeometry.Nearest(walk, nest.Cell, out _) / (float)Math.Max(1, walk.Count - 1);
                if (share > MaxNestShare)
                {
                    errors.Add($"{nest.Element} nest at {nest.Cell} joins the road {share:P0} of the way, at most {MaxNestShare:P0}");
                }

                if (LayoutGeometry.Chebyshev(nest.Cell, layout.Base) < NestBaseClearance)
                {
                    errors.Add($"{nest.Element} nest at {nest.Cell} lies next to the base");
                }
            }
        }

        // Terrain stays off the road, liquid keeps clear of the gates, and every element covers about the share asked.
        private static void CheckTerrain(RoadLayout layout, LayoutSpec spec, List<string> errors)
        {
            HashSet<long> road = AllRoadCells(layout);
            var counts = new Dictionary<int, int>();
            foreach (LayoutPatch patch in layout.Terrain)
            {
                if (road.Contains(LayoutGeometry.Key(patch.Cell)))
                {
                    errors.Add($"terrain at {patch.Cell} lies on the road");
                }

                if (LayoutTerrain.IsLiquid(patch.Kind))
                {
                    foreach (Cell gate in layout.Gates)
                    {
                        if (LayoutGeometry.Chebyshev(gate, patch.Cell) <= 1)
                        {
                            errors.Add($"liquid at {patch.Cell} touches the gate at {gate}");
                        }
                    }
                }

                counts.TryGetValue(patch.Kind, out int count);
                counts[patch.Kind] = count + 1;
            }

            int cells = layout.Width * layout.Height;
            foreach (TerrainShare share in spec.Terrain)
            {
                int kind = LayoutTerrain.KindOf(share.Element);
                int target = (int)Math.Round(share.Share * cells);
                if (kind == LayoutTerrain.Ground || target < MinTerrainCells)
                {
                    continue;
                }

                counts.TryGetValue(kind, out int count);
                if (count < target * TerrainShareLow || count > target * TerrainShareHigh)
                {
                    errors.Add($"{share.Element} covers {count} cells, the spec asks about {target}");
                }
            }
        }

        private static HashSet<long> AllRoadCells(RoadLayout layout)
        {
            HashSet<long> cells = LayoutGeometry.Corridor(layout.Road, layout.RoadWidth);
            if (layout.SecondRoad != null)
            {
                cells.UnionWith(LayoutGeometry.Corridor(layout.SecondRoad, layout.RoadWidth));
            }

            foreach (Cell cell in layout.PlazaCells)
            {
                cells.Add(LayoutGeometry.Key(cell));
            }

            return cells;
        }

        /// <summary>One straight leg of a road and the cells its corridor covers.</summary>
        private sealed class Segment
        {
            private readonly int _road;
            private readonly Cell _from;
            private readonly Cell _to;

            public Segment(int road, Cell from, Cell to, List<Cell> cells)
            {
                _road = road;
                _from = from;
                _to = to;
                Cells = cells;
            }

            public List<Cell> Cells { get; }

            // Legs that share an end cell are joined (one after the other, or at a junction), so they may touch.
            public bool Touches(Segment other) =>
                LayoutGeometry.Same(_from, other._from) || LayoutGeometry.Same(_from, other._to) ||
                LayoutGeometry.Same(_to, other._from) || LayoutGeometry.Same(_to, other._to) ||
                (_road != other._road && (Contains(other._to) || other.Contains(_to)));

            private bool Contains(Cell cell)
            {
                int minX = Math.Min(_from.X, _to.X);
                int maxX = Math.Max(_from.X, _to.X);
                int minY = Math.Min(_from.Y, _to.Y);
                int maxY = Math.Max(_from.Y, _to.Y);
                return cell.X >= minX && cell.X <= maxX && cell.Y >= minY && cell.Y <= maxY;
            }

            public override string ToString() => $"{_from}-{_to}";
        }
    }
}
