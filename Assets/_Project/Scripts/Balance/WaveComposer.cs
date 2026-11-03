using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Turns a wave script into the spawn groups of one wave on one difficulty, with the health multiplier the solver chose. The kind of wave
    /// decides how the budget is spent (a rush of the cheapest creatures, a heavy wave led by the toughest, an air raid,
    /// an ambush from the pools and nests); a boss wave keeps the ground thin so the boss is the event. Deterministic:
    /// the same script, health and seed give the same wave in the editor and in the BalanceLab tool.
    /// </summary>
    public static class WaveComposer
    {
        public const int Road = 0;
        public const int NorthAirRoute = 1;
        public const int SouthAirRoute = 2;

        private const float BossDelaySeconds = 14f;
        private const float EscortDelaySeconds = 5f;
        private const float AmbushDelaySeconds = 12f;
        private const float TroopStagger = 2.5f;
        private const float RushBudget = 1.2f;
        private const float HeavyBias = 0.6f;
        private const float MinShare = 0.35f;
        private const int MaxGroupCount = 64;
        private const float RushTempo = 0.6f;
        private const float HeavyShare = 0.55f;
        private const float HeavyTempo = 1.4f;
        private const float AirShare = 0.35f;
        private const float AirGroundShare = 0.4f;
        private const float BossWaveShare = 0.6f;
        private const float SprinkleShare = 0.15f;
        // The default spacing of flyers in an air raid and in the sprinkle; a script may pack its raids tighter.
        private const float FlyerInterval = 1.6f;
        private const float ClusterInnerIntervalShare = 0.5f;
        private const float ClusterMinInterval = 0.2f;
        private const float PackInnerIntervalShare = 0.35f;
        private const float TeaserDelaySeconds = 6f;
        private const float TeaserInterval = 1.2f;


        // Share of an ambush wave that surfaces from pools and nests, per difficulty: easy keeps most of it at the gate.
        private static readonly float[] AmbushSharePerDifficulty = { 0.3f, 0.45f, 0.55f, 0.65f };

        // Flyers that sweep in behind the last boss, per difficulty.
        private static readonly int[] BossEscortFlyers = { 0, 1, 2, 4 };

        // Cluster sizes a stream is cut into; several patterns keep neighbouring streams from marching in step.
        private static readonly int[][] ClusterPatterns =
        {
            new[] { 1, 2, 1, 3 },
            new[] { 2, 1, 3 },
            new[] { 1, 1, 2, 4 },
            new[] { 3, 1, 2 },
        };

        public enum Kind
        {
            Plain = 0,
            Rush = 1,
            Heavy = 2,
            Air = 3,
            Ambush = 4,
        }

        /// <summary>A stable hash of the level key (FNV-1a), so seeds do not depend on the runtime's string hashing.</summary>
        public static int StableHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text)
                {
                    hash = (hash ^ c) * 16777619;
                }

                return (int)hash;
            }
        }

        public static int Seed(string levelKey, int difficulty, int waveIndex)
        {
            unchecked
            {
                return StableHash(levelKey) * 31 + difficulty * 131 + waveIndex;
            }
        }

        /// <summary>The boss on this wave for this difficulty, or null.</summary>
        public static BossSpec BossOn(WaveScriptSpec script, int difficulty, int number)
        {
            foreach (BossSpec boss in script.Bosses)
            {
                if (boss.Wave == number && boss.MinDifficulty <= difficulty)
                {
                    return boss;
                }
            }

            return null;
        }

        public static Kind KindOf(WaveScriptSpec script, int difficulty, int number)
        {
            if (IsAirWave(script, difficulty, number))
            {
                return Kind.Air;
            }

            switch (script.Rhythm.Length == 0 ? 'P' : script.Rhythm[(number - 1) % script.Rhythm.Length])
            {
                case 'R':
                    return Kind.Rush;
                case 'H':
                    return Kind.Heavy;
                case 'M':
                    return Kind.Ambush;
                default:
                    return Kind.Plain;
            }
        }

        public static WaveSpec Compose(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int number, float health, int secondRoad, BalanceRules rules)
        {
            var random = new Random(Seed(script.Key, difficulty, number - 1));
            float budget = script.BudgetOf(number);
            var wave = new WaveSpec { HealthMultiplier = health };
            List<GroupSpec> result = wave.Groups;
            BossSpec boss = BossOn(script, difficulty, number);
            Kind kind = KindOf(script, difficulty, number);
            if (boss != null)
            {
                budget *= BossWaveShare;
            }

            List<TroopSpec> open = Unlocked(script.Troops, number, difficulty, false);
            List<TroopSpec> pools = Unlocked(script.Troops, number, difficulty, true);
            EnemyStats flyer = Get(enemies, script.Flyer);
            // A troop capped at the largest group size cannot take its whole share; what it could not buy makes every
            // creature of the wave tougher instead of the crowd larger.
            float shortfall = 0f;
            switch (kind)
            {
                case Kind.Rush:
                    shortfall = Fill(result, budget * RushBudget, Cheapest(open, 2, enemies, rules), RushTempo, secondRoad, random, script, number, enemies, rules);
                    break;
                case Kind.Heavy:
                    TroopSpec heavy = Toughest(open, enemies, rules);
                    if (heavy != null)
                    {
                        shortfall = Fill(result, budget * HeavyShare, new List<TroopSpec> { heavy }, HeavyTempo, secondRoad, random, script, number, enemies, rules);
                        shortfall += Fill(result, budget * (1f - HeavyShare), Without(open, heavy), 1f, secondRoad, random, script, number, enemies, rules);
                    }

                    break;
                case Kind.Air:
                    AddFlyers(result, flyer, budget * AirShare * script.AirRaidScaleFor(difficulty), 0f, script.FlyerInterval, rules);
                    shortfall = Fill(result, budget * AirGroundShare, open, 1f, secondRoad, random, script, number, enemies, rules);
                    break;
                case Kind.Ambush when pools.Count > 0:
                    float ambush = AmbushSharePerDifficulty[Math.Min(difficulty, AmbushSharePerDifficulty.Length - 1)];
                    shortfall = Fill(result, budget * (1f - ambush), open, 1f, secondRoad, random, script, number, enemies, rules);
                    shortfall += Fill(result, budget * ambush, pools, 1f, secondRoad, random, script, number, enemies, rules, AmbushDelaySeconds);
                    break;
                default:
                    shortfall = Fill(result, budget, open, 1f, secondRoad, random, script, number, enemies, rules);
                    break;
            }

            if (shortfall > 0f && budget > shortfall)
            {
                wave.HealthMultiplier *= budget / (budget - shortfall);
            }

            // On the harder difficulties the sky stays contested between the air raids.
            if (HasSprinkle(script, difficulty, number))
            {
                AddFlyers(result, flyer, budget * SprinkleShare, TroopStagger * 2f, FlyerInterval, rules);
            }

            if (boss != null)
            {
                AddBoss(result, script, boss, Get(enemies, boss.Enemy), flyer, difficulty, secondRoad);
            }

            AddTeasers(result, script, number, enemies);
            return wave;
        }

        // A teaser walks in a little after the wave opens, through the main gate or along the north flight route.
        private static void AddTeasers(List<GroupSpec> result, WaveScriptSpec script, int number, IReadOnlyDictionary<string, EnemyStats> enemies)
        {
            foreach (TeaserSpec teaser in script.Teasers)
            {
                EnemyStats enemy = Get(enemies, teaser.Enemy);
                if (teaser.Wave != number || enemy == null || teaser.Count <= 0)
                {
                    continue;
                }

                result.Add(new GroupSpec
                {
                    Enemy = enemy,
                    Count = teaser.Count,
                    Interval = TeaserInterval,
                    Delay = TeaserDelaySeconds,
                    Path = enemy.Flies ? NorthAirRoute : Road,
                });
            }
        }

        private static void AddBoss(List<GroupSpec> result, WaveScriptSpec script, BossSpec boss, EnemyStats enemy, EnemyStats flyer, int difficulty, int secondRoad)
        {
            if (enemy == null)
            {
                return;
            }

            BossRoute route = boss.RouteFor(difficulty);
            result.Add(new GroupSpec
            {
                Enemy = enemy,
                Count = 1,
                Interval = 1f,
                Delay = BossDelaySeconds,
                Path = route == BossRoute.SecondRoad && secondRoad >= 0 ? secondRoad : Road,
                Source = route == BossRoute.Entrance ? WaveLoad.EntranceSource : 0,
            });

            bool last = boss.Wave == script.Waves;
            int escorts = BossEscortFlyers[Math.Min(difficulty, BossEscortFlyers.Length - 1)];
            if (last && escorts > 0 && flyer != null)
            {
                result.Add(new GroupSpec { Enemy = flyer, Count = escorts, Interval = 1.5f, Delay = BossDelaySeconds + EscortDelaySeconds, Path = NorthAirRoute });
            }
        }

        private static EnemyStats Get(IReadOnlyDictionary<string, EnemyStats> enemies, string id) =>
            !string.IsNullOrEmpty(id) && enemies.TryGetValue(id, out EnemyStats enemy) ? enemy : null;

        /// <summary>True when a plain wave also carries a few flyers on this difficulty.</summary>
        public static bool HasSprinkle(WaveScriptSpec script, int difficulty, int number) =>
            KindOf(script, difficulty, number) == Kind.Plain && difficulty >= script.AirSprinkleFrom && number > FirstAirWave(script, difficulty);

        private static bool IsAirWave(WaveScriptSpec script, int difficulty, int number)
        {
            int allowed = Math.Min(script.AirWavesPerDifficulty[Math.Min(difficulty, script.AirWavesPerDifficulty.Length - 1)], script.AirWaves.Length);
            for (int i = 0; i < allowed; i++)
            {
                if (script.AirWaves[i] == number)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FirstAirWave(WaveScriptSpec script, int difficulty) =>
            script.AirWavesPerDifficulty[Math.Min(difficulty, script.AirWavesPerDifficulty.Length - 1)] > 0 && script.AirWaves.Length > 0 ? script.AirWaves[0] : int.MaxValue;

        private static List<TroopSpec> Unlocked(TroopSpec[] troops, int number, int difficulty, bool pools)
        {
            var list = new List<TroopSpec>();
            foreach (TroopSpec troop in troops)
            {
                if (troop.FirstWave <= number && troop.MinDifficulty <= difficulty && (troop.Source != 0) == pools)
                {
                    list.Add(troop);
                }
            }

            return list;
        }

        private static float ThreatOf(TroopSpec troop, IReadOnlyDictionary<string, EnemyStats> enemies, BalanceRules rules)
        {
            EnemyStats enemy = Get(enemies, troop.Enemy);
            return enemy == null ? float.MaxValue : Threat.Of(enemy, rules);
        }

        private static List<TroopSpec> Cheapest(List<TroopSpec> troops, int count, IReadOnlyDictionary<string, EnemyStats> enemies, BalanceRules rules)
        {
            var sorted = new List<TroopSpec>(troops);
            sorted.Sort((a, b) => ThreatOf(a, enemies, rules).CompareTo(ThreatOf(b, enemies, rules)));
            return sorted.GetRange(0, Math.Min(count, sorted.Count));
        }

        private static TroopSpec Toughest(List<TroopSpec> troops, IReadOnlyDictionary<string, EnemyStats> enemies, BalanceRules rules)
        {
            TroopSpec best = troops.Count > 0 ? troops[0] : null;
            foreach (TroopSpec troop in troops)
            {
                if (ThreatOf(troop, enemies, rules) > ThreatOf(best, enemies, rules))
                {
                    best = troop;
                }
            }

            return best;
        }

        private static List<TroopSpec> Without(List<TroopSpec> troops, TroopSpec excluded)
        {
            var list = new List<TroopSpec>(troops);
            list.Remove(excluded);
            return list.Count > 0 ? list : troops;
        }

        // Spends a budget over troops by weight, leaning towards tougher creatures as the level goes on; gate troops are
        // cut into clusters over the lanes, pool and nest troops surface as one group.
        private static float Fill(List<GroupSpec> output, float budget, List<TroopSpec> troops, float tempo, int secondRoad, Random random, WaveScriptSpec script, int number, IReadOnlyDictionary<string, EnemyStats> enemies, BalanceRules rules, float extraDelay = 0f)
        {
            if (troops.Count == 0 || budget <= 0f)
            {
                return 0f;
            }

            float shortfall = 0f;
            float bias = HeavyBias * (number - 1) / Math.Max(1, script.Waves - 1);
            float totalWeight = 0f;
            foreach (TroopSpec troop in troops)
            {
                totalWeight += Weight(troop, bias, enemies, rules);
            }

            int order = 0;
            foreach (TroopSpec troop in troops)
            {
                EnemyStats enemy = Get(enemies, troop.Enemy);
                if (enemy == null)
                {
                    continue;
                }

                float threat = Threat.Of(enemy, rules);
                float share = budget * Weight(troop, bias, enemies, rules) / totalWeight;
                int wanted = (int)Math.Round(share / threat);
                int count = Math.Min(MaxGroupCount, Math.Max(share >= threat * MinShare ? 1 : 0, wanted));
                if (wanted > MaxGroupCount)
                {
                    shortfall += (wanted - MaxGroupCount) * threat;
                }

                // A pack species waits until the budget buys at least half a pack, then always sends a whole one.
                if (count > 0 && troop.Pack > 0 && count < troop.Pack)
                {
                    count = count * 2 >= troop.Pack ? troop.Pack : 0;
                }

                if (count <= 0)
                {
                    continue;
                }

                var group = new GroupSpec
                {
                    Enemy = enemy,
                    Count = count,
                    Interval = troop.Interval * tempo,
                    Delay = troop.Delay + extraDelay + order * TroopStagger,
                    Source = troop.Source,
                    Path = Road,
                };
                if (troop.Source == 0)
                {
                    Scatter(group, random.Next(0, 1000), LanesFor(enemy, secondRoad), output, troop.Pack);
                }
                else
                {
                    output.Add(group);
                }

                order++;
            }

            return shortfall;
        }

        private static float Weight(TroopSpec troop, float bias, IReadOnlyDictionary<string, EnemyStats> enemies, BalanceRules rules) =>
            troop.Weight * (float)Math.Pow(ThreatOf(troop, enemies, rules), bias);

        // Flyers split over both flight routes, the second route a little behind the first.
        private static void AddFlyers(List<GroupSpec> output, EnemyStats flyer, float budget, float delay, float interval, BalanceRules rules)
        {
            if (flyer == null)
            {
                return;
            }

            int count = Math.Max(1, (int)Math.Round(budget / Threat.Of(flyer, rules)));
            int north = (count + 1) / 2;
            output.Add(new GroupSpec { Enemy = flyer, Count = north, Interval = interval, Delay = delay, Path = NorthAirRoute });
            if (count - north > 0)
            {
                output.Add(new GroupSpec { Enemy = flyer, Count = count - north, Interval = interval, Delay = delay + TroopStagger, Path = SouthAirRoute });
            }
        }

        private static int[] LanesFor(EnemyStats enemy, int secondRoad) =>
            !enemy.Flies && secondRoad >= 0 ? new[] { Road, secondRoad } : new[] { Road };

        // Cuts one stream into clusters that take turns on the lanes; a cluster of n claims n stream intervals.
        private static void Scatter(GroupSpec group, int seed, int[] lanes, List<GroupSpec> output, int pack)
        {
            int[] pattern = pack > 0 ? new[] { pack, pack + 1, pack, pack + 2 } : ClusterPatterns[seed % ClusterPatterns.Length];
            float inner = Math.Max(ClusterMinInterval, group.Interval * (pack > 0 ? PackInnerIntervalShare : ClusterInnerIntervalShare));
            int remaining = group.Count;
            int step = 0;
            float offset = 0f;

            while (remaining > 0)
            {
                int size = Math.Min(pattern[step % pattern.Length], remaining);
                output.Add(new GroupSpec
                {
                    Enemy = group.Enemy,
                    Count = size,
                    Interval = inner,
                    Delay = group.Delay,
                    Source = group.Source,
                    Stagger = offset,
                    Path = lanes[(seed + step) % lanes.Length],
                });

                offset += size * group.Interval;
                remaining -= size;
                step++;
            }
        }
    }
}
