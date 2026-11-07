using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>What the plan search needs of one level and difficulty.</summary>
    public sealed class PlanSearchInput
    {
        public string LevelKey = string.Empty;
        public int Difficulty;
        public LevelInput Level;
        public IReadOnlyList<WaveSpec> Waves;
        public DifficultyStats Stats;

        /// <summary>The rules the plans are judged by (the human lens with the hero) and the difficulty's hero cap.</summary>
        public BalanceRules Judge;
        public float Cap;

        /// <summary>Selling and rebuilding is a last resort: only Psycho plans may try it (Remco 2026-11-06).</summary>
        public bool AllowSell;
        public int Seed = 1;
        public int Samples = 3000;
        public int Keep = 30;
        public int LocalRounds = 3;
        public int LocalMutations = 24;
        public int SpotsPerType = 6;
        public int ShowPlans = 5;
    }

    /// <summary>The search's findings for one cell.</summary>
    public sealed class PlanSearchResult
    {
        public PlanSearchInput Input;

        /// <summary>The best plans by score, after the local improvement.</summary>
        public readonly List<PlanOutcome> Top = new List<PlanOutcome>();

        /// <summary>Of the top plans, the ones that hold and are distinct from each other (several ways to win).</summary>
        public readonly List<PlanOutcome> DistinctWinners = new List<PlanOutcome>();

        /// <summary>The obvious family on paper: a row of each type and the careless near-gate mix.</summary>
        public readonly List<PlanOutcome> Obvious = new List<PlanOutcome>();

        /// <summary>Per tower type, its best spots with their cover.</summary>
        public readonly Dictionary<string, List<(Cell Cell, SpotCover Cover)>> BestSpots = new Dictionary<string, List<(Cell, SpotCover)>>();
        public int Sampled;
        public int Holding;
    }

    /// <summary>
    /// The tester's stage A (round 22): an omniscient plan search on paper. It knows every wave, origin and route, as a replaying
    /// player learns them. A plan is an ordered list of builds (type, spot, wave), upgrades, mines and slow fields, drone buys
    /// and, on Psycho only, a sale. Spots come from the map (<see cref="LevelGeometry.Rank"/>: the best few per tower type),
    /// waves are coarse. The constructive family (a primary and a secondary type on their best spots, taken as the purse allows)
    /// and thousands of random plans are judged by <see cref="PlanEvaluator"/> (purse walk, placement, gadget windows, the leak
    /// model: a plan holds when the base survives), the best few are improved by small changes (another spot, an earlier or later step, an upgrade more or less),
    /// and the output is readable: the distinct winning plans, the obvious plans on the same scale, the best spots per type.
    /// Everything comes from the level input and the waves; nothing is typed for a level.
    /// </summary>
    public static class PlanSearch
    {
        private const int MinBuilds = 3;
        private const int MaxBuilds = 14;
        private const int MaxSkyGuns = 4;
        private const float UpgradeShare = 0.5f;
        private const float MineChance = 0.5f;
        private const float FieldChance = 0.35f;
        private const float HarvestChance = 0.5f;
        private const float AttackDroneChance = 0.25f;
        private const float SellChance = 0.2f;
        private const float RepairDroneChance = 0.3f;
        private const float RepairChance = 0.4f;
        private const int MaxRepairs = 3;
        private const int RepairDroneWave = 3;
        private const int MaxMines = 4;
        private const int QueueCellCount = 4;
        private const int MonoUpgradeAfterTowers = 4;
        private const int NaiveSkyEvery = 3;

        public static PlanSearchResult Run(PlanSearchInput input)
        {
            var result = new PlanSearchResult { Input = input };
            var random = new Random(input.Seed * 7919 + input.Difficulty);
            var context = new Context(input);
            foreach (KeyValuePair<string, List<Cell>> pair in context.Spots)
            {
                var list = new List<(Cell, SpotCover)>();
                TowerStats stats = input.Level.Towers.Find(t => t.Id == pair.Key);
                foreach (Cell cell in pair.Value)
                {
                    list.Add((cell, PlanEvaluator.CoverOf(input.Level, cell, stats.Levels[0].Range)));
                }

                result.BestSpots[pair.Key] = list;
            }

            var top = new List<PlanOutcome>();
            // The constructive plans first (what a player who read the preview would do), then the random sample.
            foreach (PlanSpec plan in ConstructivePlans(context))
            {
                PlanOutcome outcome = Evaluate(input, plan);
                result.Sampled++;
                result.Holding += outcome.Holds ? 1 : 0;
                Insert(top, outcome, input.Keep);
            }

            for (int i = 0; i < input.Samples; i++)
            {
                PlanSpec plan = Sample(context, random, i);
                PlanOutcome outcome = Evaluate(input, plan);
                result.Sampled++;
                result.Holding += outcome.Holds ? 1 : 0;
                Insert(top, outcome, input.Keep);
            }

            // Local improvement of the best few: keep a change when it scores better.
            int improve = Math.Min(input.ShowPlans, top.Count);
            for (int p = 0; p < improve; p++)
            {
                PlanOutcome best = top[p];
                for (int round = 0; round < input.LocalRounds; round++)
                {
                    for (int m = 0; m < input.LocalMutations; m++)
                    {
                        PlanSpec mutated = Mutate(context, random, best.Plan);
                        PlanOutcome outcome = Evaluate(input, mutated);
                        if (outcome.Score > best.Score)
                        {
                            best = outcome;
                        }
                    }
                }

                top[p] = best;
            }

            top.Sort((a, b) => b.Score.CompareTo(a.Score));
            result.Top.AddRange(top);
            foreach (PlanOutcome outcome in top)
            {
                if (!outcome.Holds)
                {
                    continue;
                }

                bool distinct = true;
                foreach (PlanOutcome kept in result.DistinctWinners)
                {
                    distinct &= outcome.Plan.DistinctFrom(kept.Plan);
                }

                if (distinct)
                {
                    result.DistinctWinners.Add(outcome);
                }
            }

            for (int i = 0; i < result.DistinctWinners.Count; i++)
            {
                result.DistinctWinners[i].Plan.Name = $"{input.LevelKey}_{input.Difficulty}_plan{i + 1}";
            }

            foreach (PlanSpec plan in ObviousPlans(context))
            {
                result.Obvious.Add(Evaluate(input, plan));
            }

            return result;
        }

        /// <summary>The obvious family on paper: a row of each ground type on its role spots, and the careless near-gate mix.</summary>
        public static List<PlanSpec> ObviousPlans(PlanSearchInput input) => ObviousPlans(new Context(input));

        private static PlanOutcome Evaluate(PlanSearchInput input, PlanSpec plan) =>
            PlanEvaluator.Evaluate(input.Level, input.Waves, input.Stats, input.Judge, plan, input.Cap);

        private static void Insert(List<PlanOutcome> top, PlanOutcome outcome, int keep)
        {
            int at = top.Count;
            while (at > 0 && top[at - 1].Score < outcome.Score)
            {
                at--;
            }

            if (at < keep)
            {
                top.Insert(at, outcome);
                if (top.Count > keep)
                {
                    top.RemoveAt(top.Count - 1);
                }
            }
        }

        // ---- The plan space --------------------------------------------------------------------------------------------

        private sealed class Context
        {
            public readonly PlanSearchInput Input;
            public readonly List<TowerStats> Ground = new List<TowerStats>();
            public readonly List<TowerStats> Sky = new List<TowerStats>();
            public readonly Dictionary<string, List<Cell>> Spots = new Dictionary<string, List<Cell>>();
            public readonly List<Cell> Queue = new List<Cell>();
            public readonly List<Cell> NearGate = new List<Cell>();
            public readonly List<int> BossWaves = new List<int>();
            public readonly List<int> CrowdWaves = new List<int>();
            public readonly bool Air;

            /// <summary>The first wave with flyers (0 = none) and how many waves bring them.</summary>
            public readonly int FirstAirWave;
            public readonly int AirWaves;
            public readonly GadgetStats Mine;
            public readonly GadgetStats Field;
            public readonly DroneStats Harvester;
            public readonly DroneStats Attack;
            public readonly DroneStats Repair;

            /// <summary>True when the level has trunk gates the plan model wears and repairs.</summary>
            public readonly bool Gates;
            public readonly int WaveCount;

            public Context(PlanSearchInput input)
            {
                Input = input;
                WaveCount = input.Waves.Count;
                LevelGeometry geometry = input.Level.Geometry;
                foreach (TowerStats tower in input.Level.Towers)
                {
                    if (!tower.IsGun)
                    {
                        continue;
                    }

                    bool airOnly = Layer.Has(tower.Targets, Layer.Air) && !Layer.Has(tower.Targets, Layer.Ground);
                    (airOnly ? Sky : Ground).Add(tower);
                    List<Cell> ranked = geometry != null ? geometry.Rank(tower.Levels[0].Range, airOnly, tower.Splash > 0f) : new List<Cell>();
                    Spots[tower.Id] = ranked.GetRange(0, Math.Min(input.SpotsPerType, ranked.Count));
                }

                Ground.Sort((a, b) => a.Levels[0].Cost.CompareTo(b.Levels[0].Cost));
                if (geometry != null)
                {
                    Queue.AddRange(geometry.QueueCellsBeforeGate(QueueCellCount));
                    NearGate.AddRange(geometry.Spots);
                    if (geometry.DefenceGate.HasValue)
                    {
                        Cell gate = geometry.DefenceGate.Value;
                        NearGate.Sort((a, b) => Distance(a, gate).CompareTo(Distance(b, gate)));
                    }
                }

                Mine = input.Level.Gadgets.Find(g => g.Kind == GadgetKind.Mine);
                Field = input.Level.Gadgets.Find(g => g.Kind == GadgetKind.SlowField);
                Harvester = input.Level.Drones.Find(d => d.Role == DroneRole.Harvest);
                Attack = input.Level.Drones.Find(d => d.Role == DroneRole.Attack);
                Repair = input.Level.Drones.Find(d => d.Role == DroneRole.Repair);
                Gates = PlanEvaluator.TrunkGates(input.Level) > 0;
                var purse = new KillPurse(input.Level, input.Stats, input.Judge);
                for (int i = 0; i < input.Waves.Count; i++)
                {
                    purse.Begin(input.Waves[i]);
                    WaveLoad load = WaveLoad.Of(input.Level, input.Waves[i], input.Stats, input.Judge);
                    Air |= load.AirHealth > 0f;
                    if (load.AirHealth > 0f)
                    {
                        FirstAirWave = FirstAirWave == 0 ? i + 1 : FirstAirWave;
                        AirWaves++;
                    }

                    if (load.HasBoss)
                    {
                        BossWaves.Add(i + 1);
                    }

                    if (load.GroundDensity > 0f && 1f + 2f * (Mine?.Radius ?? 1f) * load.GroundDensity >= 2f)
                    {
                        CrowdWaves.Add(i + 1);
                    }

                    purse.Add(input.Waves[i], new WaveLine { GroundPressure = 1f });
                }

            }

            public int Bucket(Random random, int from, int to) => Math.Max(1, Math.Min(WaveCount, from + random.Next(Math.Max(1, to - from + 1))));
        }

        // A random plan: a handful of builds on the good spots of each type (sky guns only where flyers come), upgrades on some
        // of them later, mines before the boss and crowd waves, a slow field at the queue, a harvester early, an attack drone late.
        private static PlanSpec Sample(Context context, Random random, int index)
        {
            var steps = new List<PlanStep>();
            var used = new HashSet<long>();
            int builds = MinBuilds + random.Next(MaxBuilds - MinBuilds + 1);
            int skyGuns = context.Air && context.Sky.Count > 0 ? 1 + random.Next(MaxSkyGuns) : 0;
            // A bias per plan towards one type or another, so the mixes differ.
            var weights = new float[context.Ground.Count];
            for (int t = 0; t < weights.Length; t++)
            {
                weights[t] = 0.3f + (float)random.NextDouble();
            }

            var built = new List<(TowerStats Tower, Cell Cell, int Wave)>();
            for (int b = 0; b < builds; b++)
            {
                TowerStats tower = Pick(context.Ground, weights, random);
                if (tower == null || !Free(context.Spots[tower.Id], used, random, out Cell cell))
                {
                    continue;
                }

                int wave = b < 2 ? 1 : context.Bucket(random, 1, (int)Math.Ceiling(context.WaveCount * (b < builds / 2 ? 0.5f : 0.85f)));
                built.Add((tower, cell, wave));
            }

            for (int s = 0; s < skyGuns; s++)
            {
                TowerStats tower = context.Sky[random.Next(context.Sky.Count)];
                if (!Free(context.Spots[tower.Id], used, random, out Cell cell))
                {
                    continue;
                }

                built.Add((tower, cell, context.Bucket(random, 1, Math.Max(1, context.WaveCount * 2 / 3))));
            }

            built.Sort((a, b) => a.Wave.CompareTo(b.Wave));
            foreach ((TowerStats tower, Cell cell, int wave) in built)
            {
                steps.Add(new PlanStep { Kind = PlanStep.Build, Item = tower.Id, X = cell.X, Y = cell.Y, Wave = wave });
            }

            foreach ((TowerStats tower, Cell cell, int wave) in built)
            {
                int levels = tower.Levels.Length - 1;
                int ups = random.NextDouble() < UpgradeShare ? 1 + random.Next(Math.Max(1, levels)) : 0;
                for (int u = 0; u < Math.Min(ups, levels); u++)
                {
                    steps.Add(new PlanStep { Kind = PlanStep.Upgrade, X = cell.X, Y = cell.Y, Wave = context.Bucket(random, wave + 1 + u * 2, context.WaveCount) });
                }
            }

            if (context.Mine != null && context.Queue.Count > 0 && random.NextDouble() < MineChance)
            {
                var targets = new List<int>(context.BossWaves);
                targets.AddRange(context.CrowdWaves);
                int mines = 1 + random.Next(MaxMines);
                for (int m = 0; m < mines && targets.Count > 0; m++)
                {
                    Cell cell = context.Queue[random.Next(context.Queue.Count)];
                    steps.Add(new PlanStep { Kind = PlanStep.Mine, Item = context.Mine.Id, X = cell.X, Y = cell.Y, Wave = targets[random.Next(targets.Count)] });
                }
            }

            if (context.Field != null && context.Queue.Count > 0 && random.NextDouble() < FieldChance)
            {
                Cell cell = context.Queue[0];
                int wave = context.BossWaves.Count > 0 && random.NextDouble() < 0.5 ? context.BossWaves[random.Next(context.BossWaves.Count)] : context.Bucket(random, context.WaveCount / 2, context.WaveCount);
                steps.Add(new PlanStep { Kind = PlanStep.Field, Item = context.Field.Id, X = cell.X, Y = cell.Y, Wave = wave });
            }

            if (context.Harvester != null && context.Input.Level.MineCount > 0 && random.NextDouble() < HarvestChance)
            {
                steps.Add(new PlanStep { Kind = PlanStep.Drone, Item = context.Harvester.Id, Wave = context.Bucket(random, 1, Math.Max(1, context.WaveCount / 2)) });
            }

            if (context.Attack != null && random.NextDouble() < AttackDroneChance)
            {
                steps.Add(new PlanStep { Kind = PlanStep.Drone, Item = context.Attack.Id, Wave = context.Bucket(random, context.WaveCount / 2, context.WaveCount) });
            }

            // Repairs are a trade (Remco): a repair drone early or mid-level, or a few gate repairs by hand that the evaluator takes only by the bot's rule.
            if (context.Repair != null && random.NextDouble() < RepairDroneChance)
            {
                steps.Add(new PlanStep { Kind = PlanStep.Drone, Item = context.Repair.Id, Wave = context.Bucket(random, 2, Math.Max(2, context.WaveCount * 2 / 3)) });
            }

            if (context.Gates && random.NextDouble() < RepairChance)
            {
                int repairs = 1 + random.Next(MaxRepairs);
                for (int r = 0; r < repairs; r++)
                {
                    steps.Add(new PlanStep { Kind = PlanStep.Repair, Item = PlanStep.Gates, Wave = context.Bucket(random, 3, context.WaveCount) });
                }
            }

            if (context.Input.AllowSell && built.Count > 2 && random.NextDouble() < SellChance)
            {
                (TowerStats tower, Cell cell, int _) = built[random.Next(built.Count)];
                steps.Add(new PlanStep { Kind = PlanStep.Sell, X = cell.X, Y = cell.Y, Wave = context.Bucket(random, context.WaveCount / 2, context.WaveCount) });
                TowerStats dearest = context.Ground[context.Ground.Count - 1];
                steps.Add(new PlanStep { Kind = PlanStep.Build, Item = dearest.Id, X = cell.X, Y = cell.Y, Wave = context.WaveCount });
            }

            Order(steps);
            return new PlanSpec { Level = context.Input.LevelKey, Difficulty = context.Input.Difficulty, Name = $"sample{index}", Steps = steps.ToArray() };
        }

        // The constructive family (round 22b): a primary type on its best spots and a secondary type beside it, two guns before the first
        // wave and then a priority list taken as the purse allows (upgrade the best-placed gun, add a gun, in turn), a sky gun the wave
        // before the flyers come, the harvester early, a mine before every boss wave. Two orders per pair: upgrades first, or towers first.
        private static List<PlanSpec> ConstructivePlans(Context context)
        {
            var plans = new List<PlanSpec>();
            foreach (TowerStats primary in context.Ground)
            {
                foreach (TowerStats secondary in context.Ground)
                {
                    for (int variant = 0; variant < 4; variant++)
                    {
                        // Two orders (upgrades first, towers first), each with and without a repair drone, when the level offers one.
                        bool repairDrone = variant >= 2;
                        if (repairDrone && context.Repair == null)
                        {
                            continue;
                        }

                        PlanSpec plan = Constructive(context, primary, secondary, variant % 2 == 0, repairDrone);
                        if (plan != null)
                        {
                            plans.Add(plan);
                        }
                    }
                }
            }

            return plans;
        }

        private static PlanSpec Constructive(Context context, TowerStats primary, TowerStats secondary, bool upgradesFirst, bool repairDrone)
        {
            var steps = new List<PlanStep>();
            var used = new HashSet<long>();
            var builtPrimary = new List<Cell>();
            var builtSecondary = new List<Cell>();
            int wave = 1;
            int clock = 0;
            // Two purchases a wave from the second wave on, as the purse of a plain wave roughly allows.
            int Next() => Math.Min(context.WaveCount, 1 + (clock++ + 2) / 2);

            bool Build(TowerStats tower, List<Cell> built, int at)
            {
                if (!FirstFree(context.Spots[tower.Id], used, out Cell cell))
                {
                    return false;
                }

                steps.Add(new PlanStep { Kind = PlanStep.Build, Item = tower.Id, X = cell.X, Y = cell.Y, Wave = at });
                built.Add(cell);
                return true;
            }

            if (!Build(primary, builtPrimary, wave))
            {
                return null;
            }

            if (secondary != primary)
            {
                Build(secondary, builtSecondary, wave);
            }

            var upgrades = new Dictionary<long, int>();
            bool Upgrade(List<Cell> built, TowerStats tower)
            {
                foreach (Cell cell in built)
                {
                    long key = ((long)cell.X << 32) ^ (uint)cell.Y;
                    int have = upgrades.TryGetValue(key, out int n) ? n : 0;
                    if (have < tower.Levels.Length - 1)
                    {
                        upgrades[key] = have + 1;
                        steps.Add(new PlanStep { Kind = PlanStep.Upgrade, X = cell.X, Y = cell.Y, Wave = Next() });
                        return true;
                    }
                }

                return false;
            }

            int builds = 2;
            for (int round = 0; round < MaxBuilds && builds < MaxBuilds; round++)
            {
                bool primaryTurn = round % 2 == 0 || secondary == primary;
                TowerStats tower = primaryTurn ? primary : secondary;
                List<Cell> built = primaryTurn ? builtPrimary : builtSecondary;
                if (upgradesFirst)
                {
                    if (!Upgrade(built, tower) && !Build(tower, built, Next()))
                    {
                        break;
                    }
                }
                else if (!Build(tower, built, Next()) && !Upgrade(built, tower))
                {
                    break;
                }

                builds++;
            }

            if (context.Air && context.Sky.Count > 0 && context.FirstAirWave > 0)
            {
                TowerStats sky = context.Sky[0];
                int at = Math.Max(1, context.FirstAirWave - 1);
                if (FirstFree(context.Spots[sky.Id], used, out Cell cell))
                {
                    steps.Add(new PlanStep { Kind = PlanStep.Build, Item = sky.Id, X = cell.X, Y = cell.Y, Wave = at });
                }

                if (context.AirWaves > 2 && FirstFree(context.Spots[sky.Id], used, out Cell second))
                {
                    steps.Add(new PlanStep { Kind = PlanStep.Build, Item = sky.Id, X = second.X, Y = second.Y, Wave = Math.Min(context.WaveCount, context.FirstAirWave + 2) });
                }
            }

            if (context.Harvester != null && context.Input.Level.MineCount > 0)
            {
                steps.Add(new PlanStep { Kind = PlanStep.Drone, Item = context.Harvester.Id, Wave = 2 });
            }

            if (context.Mine != null && context.Queue.Count > 0)
            {
                foreach (int boss in context.BossWaves)
                {
                    steps.Add(new PlanStep { Kind = PlanStep.Mine, Item = context.Mine.Id, X = context.Queue[0].X, Y = context.Queue[0].Y, Wave = Math.Max(1, boss - 1) });
                }
            }

            if (repairDrone)
            {
                steps.Add(new PlanStep { Kind = PlanStep.Drone, Item = context.Repair.Id, Wave = RepairDroneWave });
            }
            else if (context.Gates)
            {
                // Without a drone the gates are repaired by hand before every boss and at the last third, where the bot's rule allows it.
                foreach (int boss in context.BossWaves)
                {
                    steps.Add(new PlanStep { Kind = PlanStep.Repair, Item = PlanStep.Gates, Wave = Math.Max(1, boss - 1) });
                }

                steps.Add(new PlanStep { Kind = PlanStep.Repair, Item = PlanStep.Gates, Wave = Math.Max(1, context.WaveCount * 2 / 3) });
            }

            Order(steps);
            string name = $"{primary.Id}+{secondary.Id}{(upgradesFirst ? " upgrades first" : " towers first")}{(repairDrone ? " repair drone" : string.Empty)}";
            return new PlanSpec { Level = context.Input.LevelKey, Difficulty = context.Input.Difficulty, Name = name, Steps = steps.ToArray() };
        }

        // The first free spot of a list in rank order (the constructive plans take the best spot first).
        private static bool FirstFree(List<Cell> spots, HashSet<long> used, out Cell cell)
        {
            foreach (Cell candidate in spots)
            {
                if (used.Add(((long)candidate.X << 32) ^ (uint)candidate.Y))
                {
                    cell = candidate;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        // One small change: another spot of the same type, a step two waves earlier or later, an upgrade more or less.
        private static PlanSpec Mutate(Context context, Random random, PlanSpec plan)
        {
            var steps = new List<PlanStep>();
            foreach (PlanStep step in plan.Steps)
            {
                steps.Add(new PlanStep { Kind = step.Kind, Item = step.Item, X = step.X, Y = step.Y, Wave = step.Wave });
            }

            if (steps.Count == 0)
            {
                return plan;
            }

            int pick = random.Next(steps.Count);
            PlanStep chosen = steps[pick];
            switch (random.Next(4))
            {
                case 0 when chosen.Kind == PlanStep.Build && context.Spots.TryGetValue(chosen.Item, out List<Cell> spots) && spots.Count > 1:
                    var used = new HashSet<long>(plan.SpotKeys());
                    if (Free(spots, used, random, out Cell cell))
                    {
                        foreach (PlanStep step in steps)
                        {
                            if (step.X == chosen.X && step.Y == chosen.Y && (step.Kind == PlanStep.Build || step.Kind == PlanStep.Upgrade || step.Kind == PlanStep.Sell))
                            {
                                step.X = cell.X;
                                step.Y = cell.Y;
                            }
                        }
                    }

                    break;
                case 1:
                    chosen.Wave = Math.Max(1, Math.Min(context.WaveCount, chosen.Wave + (random.Next(2) == 0 ? -2 : 2)));
                    break;
                case 2 when chosen.Kind == PlanStep.Build:
                    TowerStats tower = context.Input.Level.Towers.Find(t => t.Id == chosen.Item);
                    int have = 0;
                    foreach (PlanStep step in steps)
                    {
                        have += step.Kind == PlanStep.Upgrade && step.X == chosen.X && step.Y == chosen.Y ? 1 : 0;
                    }

                    if (tower != null && have < tower.Levels.Length - 1)
                    {
                        steps.Add(new PlanStep { Kind = PlanStep.Upgrade, X = chosen.X, Y = chosen.Y, Wave = context.Bucket(random, chosen.Wave + 1, context.WaveCount) });
                    }

                    break;
                default:
                    if (chosen.Kind != PlanStep.Build && steps.Count > 1)
                    {
                        steps.RemoveAt(pick);
                    }

                    break;
            }

            Order(steps);
            return new PlanSpec { Level = plan.Level, Difficulty = plan.Difficulty, Name = plan.Name, Family = plan.Family, Steps = steps.ToArray() };
        }

        // The obvious family as the PlayMode bots play it: a row of each type on its role spots (towers first, then alternating
        // upgrades), and the careless mix (types in turn, nearest the gate, an air gun every third tower where flyers come).
        private static List<PlanSpec> ObviousPlans(Context context)
        {
            var plans = new List<PlanSpec>();
            foreach (TowerStats tower in context.Ground)
            {
                var steps = new List<PlanStep>();
                List<Cell> spots = context.Spots[tower.Id];
                int upgrades = 0;
                for (int i = 0; i < spots.Count; i++)
                {
                    // The row first; from the fourth tower on an upgrade of an earlier tower goes between the builds, as the mono bot plays.
                    if (i >= MonoUpgradeAfterTowers)
                    {
                        Cell up = spots[upgrades % spots.Count];
                        steps.Add(new PlanStep { Kind = PlanStep.Upgrade, X = up.X, Y = up.Y, Wave = 1 });
                        upgrades++;
                    }

                    steps.Add(new PlanStep { Kind = PlanStep.Build, Item = tower.Id, X = spots[i].X, Y = spots[i].Y, Wave = 1 });
                }

                for (int u = 0; u < spots.Count * (tower.Levels.Length - 1); u++)
                {
                    Cell up = spots[u % spots.Count];
                    steps.Add(new PlanStep { Kind = PlanStep.Upgrade, X = up.X, Y = up.Y, Wave = 1 });
                }

                plans.Add(new PlanSpec { Level = context.Input.LevelKey, Difficulty = context.Input.Difficulty, Name = "mono " + tower.Id, Family = "mono", Steps = steps.ToArray() });
            }

            if (context.Ground.Count > 0 && context.NearGate.Count > 0)
            {
                var steps = new List<PlanStep>();
                var used = new HashSet<long>();
                int purchases = 0;
                int spot = 0;
                var built = new List<Cell>();
                while (spot < context.NearGate.Count && built.Count < MaxBuilds)
                {
                    bool skyTurn = context.Air && context.Sky.Count > 0 && purchases % NaiveSkyEvery == NaiveSkyEvery - 1;
                    TowerStats tower = skyTurn ? context.Sky[0] : context.Ground[purchases % context.Ground.Count];
                    Cell cell = context.NearGate[spot++];
                    if (!used.Add(((long)cell.X << 32) ^ (uint)cell.Y))
                    {
                        continue;
                    }

                    steps.Add(new PlanStep { Kind = PlanStep.Build, Item = tower.Id, X = cell.X, Y = cell.Y, Wave = 1 });
                    built.Add(cell);
                    purchases++;
                    if (purchases % 3 == 0 && built.Count > 0)
                    {
                        Cell up = built[purchases / 3 % built.Count];
                        steps.Add(new PlanStep { Kind = PlanStep.Upgrade, X = up.X, Y = up.Y, Wave = 1 });
                    }
                }

                plans.Add(new PlanSpec { Level = context.Input.LevelKey, Difficulty = context.Input.Difficulty, Name = "naive", Family = "naive", Steps = steps.ToArray() });
            }

            return plans;
        }

        private static void Order(List<PlanStep> steps)
        {
            // Stable: by wave, builds before anything else in the same wave.
            for (int i = 1; i < steps.Count; i++)
            {
                PlanStep item = steps[i];
                int j = i - 1;
                while (j >= 0 && Later(steps[j], item))
                {
                    steps[j + 1] = steps[j];
                    j--;
                }

                steps[j + 1] = item;
            }
        }

        private static bool Later(PlanStep a, PlanStep b) => a.Wave > b.Wave || (a.Wave == b.Wave && a.Kind != PlanStep.Build && b.Kind == PlanStep.Build);

        private static TowerStats Pick(List<TowerStats> towers, float[] weights, Random random)
        {
            if (towers.Count == 0)
            {
                return null;
            }

            float total = 0f;
            foreach (float weight in weights)
            {
                total += weight;
            }

            float roll = (float)random.NextDouble() * total;
            for (int i = 0; i < towers.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                {
                    return towers[i];
                }
            }

            return towers[towers.Count - 1];
        }

        private static bool Free(List<Cell> spots, HashSet<long> used, Random random, out Cell cell)
        {
            int start = spots.Count > 0 ? random.Next(spots.Count) : 0;
            for (int i = 0; i < spots.Count; i++)
            {
                Cell candidate = spots[(start + i) % spots.Count];
                if (used.Add(((long)candidate.X << 32) ^ (uint)candidate.Y))
                {
                    cell = candidate;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private static float Distance(Cell a, Cell b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
    }
}
