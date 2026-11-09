using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>The search's findings for one cell: the best strategies on paper, the distinct winners, the creative point and the obvious family.</summary>
    public sealed class StrategySearchResult
    {
        public PlanSearchInput Input;
        public readonly List<(StrategySpec Strategy, PlanOutcome Outcome)> Top = new List<(StrategySpec, PlanOutcome)>();
        public readonly List<(StrategySpec Strategy, PlanOutcome Outcome)> DistinctWinners = new List<(StrategySpec, PlanOutcome)>();
        public PlanOutcome Creative;
        public readonly List<PlanOutcome> Obvious = new List<PlanOutcome>();
        public readonly Dictionary<string, List<(Cell Cell, SpotCover Cover)>> BestSpots = new Dictionary<string, List<(Cell, SpotCover)>>();
        public int Sampled;
        public int Holding;
    }

    /// <summary>
    /// The tester's stage A over strategies (round 22e): samples the parameter space of <see cref="StrategySpec"/> (spot priority
    /// per type from the geometry's best spots, a favoured type per threat, the upgrade policy, the gadget and drone triggers; a sale
    /// only where the cell allows it), executes each on paper (<see cref="StrategyExecutor"/>), improves the best few by one
    /// parameter at a time, and keeps the distinct winners (different spots, mix or timing). The creative reference bot is one point
    /// of the space and is reported beside the obvious family on the same paper. Nothing is typed for a level.
    /// </summary>
    public static class StrategySearch
    {
        private const float UpgradeBiasMin = 0.6f;
        private const float UpgradeBiasMax = 1.8f;
        private const int TowersBeforeUpgradesMax = 4;
        private const float WeightMin = 0.5f;
        private const float WeightMax = 2.5f;
        private const float FavourChance = 0.6f;
        private static readonly float[] MineCovers = { 0f, 1f, 1f, 1.3f };
        private static readonly float[] FieldCovers = { 0f, 1f, 1f, 1.2f };
        private static readonly float[] HarvesterCovers = { 0f, 1f, 1.2f, 1.2f, 1.5f };
        private static readonly float[] HarvesterShares = { 0.3f, 0.4f, 0.5f, 0.6f };
        private static readonly float[] RepairWears = { 0.05f, 0.1f, 0.25f, 0.5f, 9f };
        private static readonly int[] AttackAfter = { 0, 6, 8, 12 };
        private static readonly float[] GateBiases = { 0f, 0f, 0.8f, 1.2f };

        public static StrategySearchResult Run(PlanSearchInput input)
        {
            var result = new StrategySearchResult { Input = input };
            var random = new Random(input.Seed * 7919 + input.Difficulty * 31 + 7);
            var ground = new List<TowerStats>();
            var all = new List<TowerStats>();
            foreach (TowerStats tower in input.Level.Towers)
            {
                if (!tower.IsGun)
                {
                    continue;
                }

                all.Add(tower);
                if (Layer.Has(tower.Targets, Layer.Ground))
                {
                    ground.Add(tower);
                }
            }

            foreach (KeyValuePair<string, List<(Cell Cell, SpotCover Cover)>> pair in PlanSearch.BestSpots(input))
            {
                result.BestSpots[pair.Key] = pair.Value;
            }

            var groundIds = ground.ConvertAll(t => t.Id);
            var top = new List<(StrategySpec Strategy, PlanOutcome Outcome)>();
            StrategySpec creative = StrategySpec.Creative(input.LevelKey, input.Difficulty);
            result.Creative = StrategyExecutor.Evaluate(input, creative);
            for (int i = 0; i < input.Samples; i++)
            {
                StrategySpec strategy = Sample(input, all, groundIds, random, i);
                PlanOutcome outcome = StrategyExecutor.Evaluate(input, strategy);
                result.Sampled++;
                result.Holding += outcome.Holds ? 1 : 0;
                Insert(top, (strategy, outcome), input.Keep);
            }

            int improve = Math.Min(input.ShowPlans, top.Count);
            for (int p = 0; p < improve; p++)
            {
                (StrategySpec Strategy, PlanOutcome Outcome) best = top[p];
                for (int round = 0; round < input.LocalRounds; round++)
                {
                    for (int m = 0; m < input.LocalMutations; m++)
                    {
                        StrategySpec mutated = Mutate(input, all, groundIds, random, best.Strategy);
                        PlanOutcome outcome = StrategyExecutor.Evaluate(input, mutated);
                        if (outcome.Score > best.Outcome.Score)
                        {
                            best = (mutated, outcome);
                        }
                    }
                }

                top[p] = best;
            }

            top.Sort((a, b) => b.Outcome.Score.CompareTo(a.Outcome.Score));
            result.Top.AddRange(top);
            foreach ((StrategySpec strategy, PlanOutcome outcome) in top)
            {
                if (!outcome.Holds)
                {
                    continue;
                }

                bool distinct = true;
                foreach ((StrategySpec kept, PlanOutcome _) in result.DistinctWinners)
                {
                    distinct &= strategy.DistinctFrom(kept, groundIds);
                }

                if (distinct)
                {
                    result.DistinctWinners.Add((strategy, outcome));
                }
            }

            for (int i = 0; i < result.DistinctWinners.Count; i++)
            {
                result.DistinctWinners[i].Strategy.Name = $"{input.LevelKey}_{input.Difficulty}_strategy{i + 1}";
                result.DistinctWinners[i].Outcome.Plan.Name = result.DistinctWinners[i].Strategy.Name;
            }

            foreach (PlanSpec plan in PlanSearch.ObviousPlans(input))
            {
                result.Obvious.Add(PlanEvaluator.Evaluate(input.Level, input.Waves, input.Stats, input.Judge, plan, input.Cap));
            }

            return result;
        }

        private static void Insert(List<(StrategySpec Strategy, PlanOutcome Outcome)> top, (StrategySpec Strategy, PlanOutcome Outcome) item, int keep)
        {
            int at = top.Count;
            while (at > 0 && top[at - 1].Outcome.Score < item.Outcome.Score)
            {
                at--;
            }

            if (at < keep)
            {
                top.Insert(at, item);
                if (top.Count > keep)
                {
                    top.RemoveAt(top.Count - 1);
                }
            }
        }

        // A random point of the space: per type a priority over its best spots (rank order or a biased shuffle), a favoured type for
        // some threats, and the policy parameters from their grids.
        private static StrategySpec Sample(PlanSearchInput input, List<TowerStats> all, List<string> groundIds, Random random, int index)
        {
            var strategy = new StrategySpec { Level = input.LevelKey, Difficulty = input.Difficulty, Name = $"sample{index}", AllowSell = input.AllowSell };
            var spots = new List<SpotPriority>();
            foreach (TowerStats tower in all)
            {
                List<Cell> ranked = StrategyExecutor.RankedSpots(input.Level, tower, input.SpotsPerType);
                if (random.Next(2) == 1)
                {
                    BiasedShuffle(ranked, random);
                }

                spots.Add(Priority(tower.Id, ranked));
            }

            strategy.Spots = spots.ToArray();
            var weights = new List<ThreatWeight>();
            foreach (string threat in StrategySpec.Threats)
            {
                if (groundIds.Count > 0 && random.NextDouble() < FavourChance)
                {
                    string tower = groundIds[random.Next(groundIds.Count)];
                    weights.Add(new ThreatWeight { Threat = threat, Tower = tower, Weight = WeightMin + (float)random.NextDouble() * (WeightMax - WeightMin) });
                }
            }

            strategy.Weights = weights.ToArray();
            strategy.UpgradeBias = (float)Math.Round(UpgradeBiasMin + (float)random.NextDouble() * (UpgradeBiasMax - UpgradeBiasMin), 2);
            strategy.TowersBeforeUpgrades = 1 + random.Next(TowersBeforeUpgradesMax);
            strategy.MineCoverBelow = Pick(MineCovers, random);
            strategy.MineOnBoss = random.Next(4) != 0;
            strategy.FieldCoverBelow = Pick(FieldCovers, random);
            strategy.HarvesterCover = Pick(HarvesterCovers, random);
            strategy.HarvesterShare = Pick(HarvesterShares, random);
            strategy.RepairDroneWear = Pick(RepairWears, random);
            strategy.AttackDroneAfterTowers = Pick(AttackAfter, random);
            strategy.GateUpgradeBias = Pick(GateBiases, random);
            return strategy;
        }

        // One parameter changed: a spot swapped forward, a threat's favourite or weight, or one policy knob.
        private static StrategySpec Mutate(PlanSearchInput input, List<TowerStats> all, List<string> groundIds, Random random, StrategySpec from)
        {
            StrategySpec strategy = Copy(from);
            switch (random.Next(7))
            {
                case 0 when strategy.Spots.Length > 0:
                    SpotPriority spots = strategy.Spots[random.Next(strategy.Spots.Length)];
                    if (spots.Count > 1)
                    {
                        int i = random.Next(spots.Count);
                        int j = random.Next(spots.Count);
                        (spots.X[i], spots.X[j]) = (spots.X[j], spots.X[i]);
                        (spots.Y[i], spots.Y[j]) = (spots.Y[j], spots.Y[i]);
                    }

                    break;
                case 1 when groundIds.Count > 0:
                    string threat = StrategySpec.Threats[random.Next(StrategySpec.Threats.Length)];
                    var weights = new List<ThreatWeight>(strategy.Weights);
                    weights.RemoveAll(w => w.Threat == threat);
                    if (random.Next(3) != 0)
                    {
                        weights.Add(new ThreatWeight { Threat = threat, Tower = groundIds[random.Next(groundIds.Count)], Weight = WeightMin + (float)random.NextDouble() * (WeightMax - WeightMin) });
                    }

                    strategy.Weights = weights.ToArray();
                    break;
                case 2:
                    strategy.UpgradeBias = (float)Math.Round(Math.Max(UpgradeBiasMin, Math.Min(UpgradeBiasMax, strategy.UpgradeBias + (random.Next(2) == 0 ? -0.2f : 0.2f))), 2);
                    break;
                case 3:
                    strategy.TowersBeforeUpgrades = 1 + random.Next(TowersBeforeUpgradesMax);
                    break;
                case 4:
                    strategy.MineCoverBelow = Pick(MineCovers, random);
                    strategy.MineOnBoss = random.Next(4) != 0;
                    strategy.FieldCoverBelow = Pick(FieldCovers, random);
                    break;
                case 5:
                    strategy.HarvesterCover = Pick(HarvesterCovers, random);
                    strategy.HarvesterShare = Pick(HarvesterShares, random);
                    break;
                default:
                    strategy.RepairDroneWear = Pick(RepairWears, random);
                    strategy.AttackDroneAfterTowers = Pick(AttackAfter, random);
                    strategy.GateUpgradeBias = Pick(GateBiases, random);
                    break;
            }

            return strategy;
        }

        private static StrategySpec Copy(StrategySpec from)
        {
            var copy = new StrategySpec
            {
                Level = from.Level,
                Difficulty = from.Difficulty,
                Name = from.Name,
                Family = from.Family,
                UpgradeBias = from.UpgradeBias,
                TowersBeforeUpgrades = from.TowersBeforeUpgrades,
                MineCoverBelow = from.MineCoverBelow,
                MineOnBoss = from.MineOnBoss,
                FieldCoverBelow = from.FieldCoverBelow,
                HarvesterCover = from.HarvesterCover,
                HarvesterShare = from.HarvesterShare,
                RepairDroneWear = from.RepairDroneWear,
                AttackDroneAfterTowers = from.AttackDroneAfterTowers,
                GateUpgradeBias = from.GateUpgradeBias,
                AllowSell = from.AllowSell,
            };
            var spots = new List<SpotPriority>();
            foreach (SpotPriority priority in from.Spots)
            {
                spots.Add(new SpotPriority { Tower = priority.Tower, X = (int[])priority.X.Clone(), Y = (int[])priority.Y.Clone() });
            }

            copy.Spots = spots.ToArray();
            var weights = new List<ThreatWeight>();
            foreach (ThreatWeight weight in from.Weights)
            {
                weights.Add(new ThreatWeight { Threat = weight.Threat, Tower = weight.Tower, Weight = weight.Weight });
            }

            copy.Weights = weights.ToArray();
            return copy;
        }

        private static SpotPriority Priority(string tower, List<Cell> cells)
        {
            var priority = new SpotPriority { Tower = tower, X = new int[cells.Count], Y = new int[cells.Count] };
            for (int i = 0; i < cells.Count; i++)
            {
                priority.X[i] = cells[i].X;
                priority.Y[i] = cells[i].Y;
            }

            return priority;
        }

        // A few neighbour swaps: the good spots stay near the front, the order differs.
        private static void BiasedShuffle(List<Cell> cells, Random random)
        {
            for (int k = 0; k < cells.Count; k++)
            {
                int i = random.Next(cells.Count);
                int j = Math.Max(0, Math.Min(cells.Count - 1, i + (random.Next(2) == 0 ? -1 : 1)));
                (cells[i], cells[j]) = (cells[j], cells[i]);
            }
        }

        private static float Pick(float[] values, Random random) => values[random.Next(values.Length)];

        private static int Pick(int[] values, Random random) => values[random.Next(values.Length)];
    }
}
