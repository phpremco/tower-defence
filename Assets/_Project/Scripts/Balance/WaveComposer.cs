using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Turns a wave script into the spawn groups of one wave on one difficulty, with the health multiplier the solver chose. The kind of wave
    /// decides how the budget is spent (a rush of the cheapest creatures, a heavy wave led by the toughest, an air raid);
    /// the wave grammar (round 79, <see cref="WaveGrammar"/>) lays the creatures out in time: clusters with breathers, a
    /// tank with its escort, a combo swarm, a surprise out of a rift or down the second road in the middle of the wave,
    /// and a boss that walks in with the last cluster of a thin crowd. Deterministic:
    /// the same script, health and seed give the same wave in the editor and in the BalanceLab tool.
    /// </summary>
    public static class WaveComposer
    {
        public const int Road = 0;
        public const int NorthAirRoute = 1;
        public const int SouthAirRoute = 2;

        private const float BossDelaySeconds = 14f;
        private const float EscortDelaySeconds = 5f;
        private const float TroopStagger = 2.5f;
        private const float RushBudget = 1.2f;
        private const int RaidCount = 5;
        private const float RaidSpread = 2f;
        private const float HeavyBias = 0.6f;
        private const float MinShare = 0.35f;
        private const int MaxGroupCount = 64;
        private const float RushTempo = 0.6f;
        private const float HeavyShare = 0.55f;
        private const float AirShare = 0.35f;
        private const float AirGroundShare = 0.4f;
        private const float SprinkleShare = 0.15f;
        // The default spacing of flyers in an air raid and in the sprinkle; a script may pack its raids tighter.
        private const float FlyerInterval = 1.6f;
        private const float ClusterInnerIntervalShare = 0.5f;
        private const float ClusterMinInterval = 0.2f;
        private const float PackInnerIntervalShare = 0.35f;
        private const float TeaserDelaySeconds = 6f;
        private const float TeaserInterval = 1.2f;

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
                default:
                    return Kind.Plain;
            }
        }

        public static WaveSpec Compose(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int number, float health, int secondRoad, BalanceRules rules) =>
            Compose(script, enemies, difficulty, number, health, secondRoad, rules, GrowthOn(script, enemies, difficulty, number, secondRoad, rules));

        /// <summary>
        /// What a counter trait's troops gain on one wave (round 68, the growth layer of the combat rules): 1 up to the end of the
        /// grace window after the trait's first wave, then the script's growth factor per wave, capped. A defence without the
        /// counter tower may hold the first waves of a trait and must lose a few waves later; the player's own curve does not
        /// move, because the solver sizes the wave against a player who buys the counter.
        /// </summary>
        public readonly struct TraitGrowth
        {
            public TraitGrowth(float armour, float air)
            {
                Armour = armour;
                Air = air;
            }

            public float Armour { get; }
            public float Air { get; }

            public static TraitGrowth None => new TraitGrowth(1f, 1f);
        }

        /// <summary>The most a trait's troops may gain over what the script writes: past it a wave would be nothing else.</summary>
        public const float MaxTraitGrowth = 6f;

        // The pack a plated troop walks in once it has grown past its grace window.
        private const int GrownPack = 3;

        public static TraitGrowth GrowthOn(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int number, int secondRoad, BalanceRules rules)
        {
            float armour = script.ArmourGrowthFor(difficulty);
            float air = script.AirGrowthFor(difficulty);
            if (armour == 1f && air == 1f)
            {
                return TraitGrowth.None;
            }

            int grace = script.GraceWavesFor(difficulty);
            return new TraitGrowth(
                Grown(armour, number, armour == 1f ? 0 : FirstTraitWave(script, enemies, difficulty, secondRoad, rules, false), grace, script.FallWithinWavesFor(difficulty)),
                Grown(air, number, air == 1f ? 0 : FirstTraitWave(script, enemies, difficulty, secondRoad, rules, true), grace, script.FallWithinWavesFor(difficulty)));
        }

        // The troops grow wave by wave through the window the defence without the counter must fall in, then stay as they are.
        private static float Grown(float growth, int number, int first, int grace, int fallWithin)
        {
            int past = first > 0 ? Math.Min(number - first - grace + 1, Math.Max(1, fallWithin)) : 0;
            return growth == 1f || past <= 0 ? 1f : Math.Min(MaxTraitGrowth, (float)Math.Pow(growth, past));
        }

        /// <summary>
        /// The first wave (from 1) a troop with the trait walks or flies in on this difficulty, as the script composes it without
        /// growth (bosses apart: the window is about the troops a counter tower is bought for); zero when none does. Remembered
        /// on the script per difficulty and raid floor, because the solver composes a wave thousands of times.
        /// </summary>
        public static int FirstTraitWave(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int secondRoad, BalanceRules rules, bool air)
        {
            long key = ((long)script.FlyingTroopFloorFor(difficulty) << 24) | ((long)script.FirstAirWaveFloorFor(difficulty) << 8) | ((long)difficulty << 1) | (air ? 1L : 0L);
            if (script.FirstTraitWaves.TryGetValue(key, out int first))
            {
                return first;
            }

            first = 0;
            for (int number = 1; number <= script.Waves && first == 0; number++)
            {
                foreach (GroupSpec group in Compose(script, enemies, difficulty, number, 1f, secondRoad, rules, TraitGrowth.None).Groups)
                {
                    if (!group.Enemy.IsBoss && (air ? group.Enemy.Flies : group.Enemy.Armor > 0f))
                    {
                        first = number;
                        break;
                    }
                }
            }

            script.FirstTraitWaves[key] = first;
            return first;
        }

        private static WaveSpec Compose(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int number, float health, int secondRoad, BalanceRules rules, TraitGrowth growth)
        {
            var random = new Random(Seed(script.Key, difficulty, number - 1));
            float budget = script.BudgetOf(number);
            var wave = new WaveSpec { HealthMultiplier = health };
            List<GroupSpec> result = wave.Groups;
            BossSpec boss = BossOn(script, difficulty, number);
            if (boss != null)
            {
                float crowd = script.BossCrowdFor(difficulty);
                budget *= crowd > 0f ? crowd : rules.BossWaveShare;
            }

            List<TroopSpec> open = Unlocked(script, enemies, number, difficulty, false);
            EnemyStats flyer = Get(enemies, script.Flyer);
            WaveGrammarRules grammar = rules.Grammar;
            WavePattern pattern = WaveGrammar.PatternOf(script, difficulty, number, grammar);
            SurpriseKind surprise = WaveGrammar.SurpriseOn(script, enemies, difficulty, number, secondRoad, rules);
            float hiddenBudget = surprise != SurpriseKind.None ? budget * grammar.SurpriseShareFor(difficulty) : 0f;
            float frontBudget = budget - hiddenBudget;
            var fill = new FillContext(script, enemies, rules, difficulty, number, secondRoad, random, growth);
            // The creatures at the gate, on a clock of their own until the grammar has cut them into clusters.
            var front = new List<GroupSpec>();
            // A troop capped at the largest group size cannot take its whole share; what it could not buy makes every
            // creature of the wave tougher instead of the crowd larger.
            float shortfall;
            switch (pattern)
            {
                case WavePattern.Swarm:
                    shortfall = Fill(front, frontBudget * RushBudget, Cheapest(open, 2, enemies, rules), RushTempo, fill);
                    break;
                case WavePattern.TankEscort:
                    shortfall = FillTankEscort(front, frontBudget, open, fill);
                    break;
                case WavePattern.AirRaid:
                    AddFlyers(result, flyer, budget * AirShare * script.AirRaidScaleFor(difficulty) * growth.Air, 0f, script.FlyerInterval, rules);
                    shortfall = Fill(front, frontBudget * AirGroundShare, open, 1f, fill);
                    break;
                case WavePattern.ComboSwarm:
                    shortfall = Fill(front, frontBudget, open, grammar.ComboTempo, fill, 0f, 0f);
                    break;
                default:
                    shortfall = Fill(front, frontBudget, open, 1f, fill);
                    break;
            }

            if (script.RaidWaveFor(difficulty) == number)
            {
                AddRaid(front, open, secondRoad, random, enemies, rules);
            }

            // Clusters and breathers. A boss wave's thin crowd comes in a fixed number of clusters with long breathers, and the
            // boss walks in with the last: a defence that bends on the first cluster gets its hero and its gates back before the
            // boss, instead of losing the base to one block (the finale rule, Docs/WAVE_GRAMMAR.md).
            float span = WaveTimeline.Span(front);
            int beats = boss != null ? BossBeats(script, number, span, grammar) : WaveTimeline.Beats(span, grammar.BeatSecondsFor(pattern), grammar.MinBeatShare, grammar.MaxBeats);
            float[] starts = WaveTimeline.Spread(front, beats, boss != null ? grammar.BossBreatherFor(difficulty) : grammar.BreatherFor(difficulty));
            result.AddRange(front);
            wave.Pattern = pattern == WavePattern.SpreadClusters && beats == 1 ? WavePattern.Stream : pattern;
            wave.Beats = beats;

            if (surprise != SurpriseKind.None)
            {
                // Out of the rifts, or down the second road alone, once the front is well under way: the player is committed there.
                var hidden = new List<GroupSpec>();
                List<TroopSpec> troops = surprise == SurpriseKind.Rift ? Unlocked(script, enemies, number, difficulty, true) : open;
                // Sized by its share alone: the growth past a grace window is the front's (seven Cragbacks behind the gate of Glass
                // Meadows on Psycho, where the share asked for two, ended every run of the first measurement).
                var plain = new FillContext(script, enemies, rules, difficulty, number, secondRoad, random, TraitGrowth.None);
                shortfall += Fill(hidden, hiddenBudget, troops, 1f, plain, 0f, TroopStagger, surprise == SurpriseKind.Flank ? new[] { secondRoad } : null);
                if (hidden.Count == 0)
                {
                    // A surprise is never empty: where its share buys no creature yet (an early wave, a heavy rift troop), one comes.
                    AddOne(hidden, Cheapest(troops, 1, enemies, rules), enemies, surprise == SurpriseKind.Flank ? secondRoad : Road);
                }

                WaveTimeline.Shift(hidden, Math.Max(grammar.SurpriseMinDelay, WaveTimeline.Span(front) * grammar.SurpriseAt));
                result.AddRange(hidden);
                wave.Surprise = hidden.Count > 0 ? surprise : SurpriseKind.None;
            }

            if (shortfall > 0f && budget > shortfall)
            {
                wave.HealthMultiplier *= budget / (budget - shortfall);
            }

            // On the harder difficulties the sky stays contested between the air raids.
            if (HasSprinkle(script, difficulty, number))
            {
                AddFlyers(result, flyer, budget * SprinkleShare * growth.Air, TroopStagger * 2f, FlyerInterval, rules);
            }

            if (boss != null)
            {
                AddBoss(result, script, boss, Get(enemies, boss.Enemy), flyer, difficulty, secondRoad, beats > 1 ? starts[beats - 1] : BossDelaySeconds);
            }

            AddTeasers(result, script, number, enemies);
            wave.CounterCheck = growth.Armour > 1f && HasPlatedTroops(result);
            return wave;
        }

        /// <summary>Whether plated troops (bosses apart) come in these groups.</summary>
        public static bool HasPlatedTroops(List<GroupSpec> groups)
        {
            foreach (GroupSpec group in groups)
            {
                if (!group.Enemy.IsBoss && group.Enemy.Armor > 0f)
                {
                    return true;
                }
            }

            return false;
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

        private static void AddBoss(List<GroupSpec> result, WaveScriptSpec script, BossSpec boss, EnemyStats enemy, EnemyStats flyer, int difficulty, int secondRoad, float delay)
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
                Delay = delay,
                Path = route == BossRoute.SecondRoad && secondRoad >= 0 ? secondRoad : Road,
                Source = route == BossRoute.Entrance ? WaveLoad.EntranceSource : 0,
            });

            bool last = boss.Wave == script.Waves;
            int escorts = BossEscortFlyers[Math.Min(difficulty, BossEscortFlyers.Length - 1)];
            if (last && escorts > 0 && flyer != null)
            {
                result.Add(new GroupSpec { Enemy = flyer, Count = escorts, Interval = 1.5f, Delay = delay + EscortDelaySeconds, Path = NorthAirRoute });
            }
        }

        private static EnemyStats Get(IReadOnlyDictionary<string, EnemyStats> enemies, string id) =>
            !string.IsNullOrEmpty(id) && enemies.TryGetValue(id, out EnemyStats enemy) ? enemy : null;

        /// <summary>True when a plain wave also carries a few flyers on this difficulty.</summary>
        public static bool HasSprinkle(WaveScriptSpec script, int difficulty, int number) =>
            KindOf(script, difficulty, number) == Kind.Plain && difficulty >= script.AirSprinkleFrom && number > FirstAirWave(script, difficulty);

        private static bool IsAirWave(WaveScriptSpec script, int difficulty, int number)
        {
            int allowed = AllowedAirWaves(script, difficulty);
            int shift = AirShift(script, difficulty);
            for (int i = 0; i < allowed; i++)
            {
                if (script.AirWaves[i] + shift == number)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The wave of the difficulty's first air raid (int.MaxValue when it has none), the script's floor applied.</summary>
        public static int FirstAirWave(WaveScriptSpec script, int difficulty)
        {
            int first = FirstWrittenAirWave(script, difficulty);
            return first == int.MaxValue ? first : first + AirShift(script, difficulty);
        }

        private static int AllowedAirWaves(WaveScriptSpec script, int difficulty) =>
            Math.Min(script.AirWavesPerDifficulty[Math.Min(difficulty, script.AirWavesPerDifficulty.Length - 1)], script.AirWaves.Length);

        // The earliest raid wave the script writes for the difficulty, before any floor.
        private static int FirstWrittenAirWave(WaveScriptSpec script, int difficulty)
        {
            int first = int.MaxValue;
            int allowed = AllowedAirWaves(script, difficulty);
            for (int i = 0; i < allowed; i++)
            {
                first = Math.Min(first, script.AirWaves[i]);
            }

            return first;
        }

        // The raids of a difficulty move together so the first comes no earlier than the script's floor (WaveScriptSpec.FirstAirWaveFloor);
        // a raid pushed past the last wave is simply gone.
        private static int AirShift(WaveScriptSpec script, int difficulty)
        {
            int floor = script.FirstAirWaveFloorFor(difficulty);
            int first = FirstWrittenAirWave(script, difficulty);
            return floor > 0 && first != int.MaxValue ? Math.Max(0, floor - first) : 0;
        }

        // The troops a wave may draw from. A flying troop also waits for the difficulty's floor of its own (WaveScriptSpec.FlyingTroopFloor),
        // and one out of a rift for the difficulty's first air raid (round 79): a surprise never introduces the sky. The raid does,
        // on the wave the raid rules allow it, and the rifts' flyers follow.
        internal static List<TroopSpec> Unlocked(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int number, int difficulty, bool pools)
        {
            var list = new List<TroopSpec>();
            int floor = script.FlyingTroopFloorFor(difficulty);
            bool skyKnown = !pools || script.AirWaves.Length == 0 || number > FirstAirWave(script, difficulty);
            foreach (TroopSpec troop in script.Troops)
            {
                if (troop.FirstWave <= number && troop.MinDifficulty <= difficulty && difficulty <= troop.MaxDifficulty && (troop.Source != 0) == pools
                    && ((number >= floor && skyKnown) || !(Get(enemies, troop.Enemy)?.Flies ?? false)))
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

        // What every fill of one wave shares: the script and its difficulty, the wave, the roads and the wave's own dice.
        private readonly struct FillContext
        {
            public FillContext(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, BalanceRules rules, int difficulty, int number, int secondRoad, Random random, TraitGrowth growth)
            {
                Script = script;
                Enemies = enemies;
                Rules = rules;
                Difficulty = difficulty;
                Number = number;
                SecondRoad = secondRoad;
                Random = random;
                Growth = growth;
            }

            public WaveScriptSpec Script { get; }
            public IReadOnlyDictionary<string, EnemyStats> Enemies { get; }
            public BalanceRules Rules { get; }
            public int Difficulty { get; }
            public int Number { get; }
            public int SecondRoad { get; }
            public Random Random { get; }
            public TraitGrowth Growth { get; }
        }

        // One creature of the first of the given troops, on the troop's own delay.
        private static void AddOne(List<GroupSpec> output, List<TroopSpec> troops, IReadOnlyDictionary<string, EnemyStats> enemies, int path)
        {
            EnemyStats enemy = troops.Count > 0 ? Get(enemies, troops[0].Enemy) : null;
            if (enemy == null)
            {
                return;
            }

            TroopSpec troop = troops[0];
            output.Add(new GroupSpec { Enemy = enemy, Count = 1, Interval = troop.Interval, Delay = troop.Delay, Source = troop.Source, Path = troop.Source != 0 || enemy.Flies ? Road : path, Nest = troop.Source != 0 ? troop.Nest : 0 });
        }

        // The clusters of a boss wave's crowd: the grammar's number (more for the last wave of the level), fewer for a crowd too thin to cut.
        private static int BossBeats(WaveScriptSpec script, int number, float span, WaveGrammarRules grammar)
        {
            int beats = Math.Max(1, number == script.Waves ? grammar.FinaleBeats : grammar.BossBeats);
            while (beats > 1 && span / beats < grammar.MinBossBeatSeconds)
            {
                beats--;
            }

            return beats;
        }

        // Tank escort: the toughest troop takes the heavy share and walks in first; the others come close together and start as
        // much later as it takes them to run into the tanks' back at the first guns. With front-first targeting the guns are
        // still busy on the tanks when the small ones arrive; nothing in the game forces that (no taunt).
        private static float FillTankEscort(List<GroupSpec> front, float budget, List<TroopSpec> open, FillContext fill)
        {
            TroopSpec heavy = Toughest(open, fill.Enemies, fill.Rules);
            if (heavy == null)
            {
                return 0f;
            }

            float shortfall = Fill(front, budget * HeavyShare, new List<TroopSpec> { heavy }, 1f, fill);
            List<TroopSpec> others = Without(open, heavy);
            var escort = new List<GroupSpec>();
            shortfall += Fill(escort, budget * (1f - HeavyShare), others, fill.Rules.Grammar.EscortTempo, fill);
            WaveTimeline.Shift(escort, EscortLag(heavy, others, fill));
            front.AddRange(escort);
            return shortfall;
        }

        // Seconds the fastest escort starts behind the first tank so that it catches it after the grammar's cells of road.
        private static float EscortLag(TroopSpec heavy, List<TroopSpec> others, FillContext fill)
        {
            EnemyStats tank = Get(fill.Enemies, heavy.Enemy);
            float fastest = 0f;
            foreach (TroopSpec troop in others)
            {
                EnemyStats enemy = Get(fill.Enemies, troop.Enemy);
                fastest = enemy != null ? Math.Max(fastest, enemy.Speed) : fastest;
            }

            if (tank == null || tank.Speed <= 0f || fastest <= tank.Speed)
            {
                return 0f;
            }

            WaveGrammarRules grammar = fill.Rules.Grammar;
            return Math.Min(grammar.MaxEscortLag, grammar.EscortCatchCells * (1f / tank.Speed - 1f / fastest));
        }

        // Spends a budget over troops by weight, leaning towards tougher creatures as the level goes on; gate troops are
        // cut into clusters over the lanes (or the given lanes only), pool and nest troops surface as one group.
        private static float Fill(List<GroupSpec> output, float budget, List<TroopSpec> troops, float tempo, FillContext fill, float extraDelay = 0f, float troopStagger = TroopStagger, int[] lanes = null)
        {
            WaveScriptSpec script = fill.Script;
            IReadOnlyDictionary<string, EnemyStats> enemies = fill.Enemies;
            BalanceRules rules = fill.Rules;
            int difficulty = fill.Difficulty;
            int number = fill.Number;
            TraitGrowth growth = fill.Growth;
            if (troops.Count == 0 || budget <= 0f)
            {
                return 0f;
            }

            float shortfall = 0f;
            float bias = HeavyBias * (number - 1) / Math.Max(1, script.Waves - 1);
            float totalWeight = 0f;
            foreach (TroopSpec troop in troops)
            {
                totalWeight += Weight(troop, bias, enemies, rules) * PlatedLever(troop, script, difficulty, enemies) * SwarmLever(troop, troops, script, difficulty, enemies);
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
                float share = budget * Weight(troop, bias, enemies, rules) * PlatedLever(troop, script, difficulty, enemies) * SwarmLever(troop, troops, script, difficulty, enemies) / totalWeight;
                // Growth past a grace window adds bodies on top of the troop's share: the other troops of the wave keep theirs.
                int wanted = (int)Math.Round(share * Grown(troop, growth, enemies) / threat);
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
                    Interval = troop.Interval * tempo * script.TempoFor(difficulty),
                    Delay = troop.Delay + extraDelay + order * troopStagger,
                    Source = troop.Source,
                    Path = Road,
                    Nest = troop.Source != 0 ? troop.Nest : 0,
                };
                // Round 68: plated troops that have grown past their grace window close ranks (packs at the gate, a tight file from
                // a nest), so a blast catches several and light fire, one target at a time, falls behind.
                bool ranks = enemy.Armor > 0f && growth.Armour > 1f;
                if (troop.Source == 0)
                {
                    Scatter(group, fill.Random.Next(0, 1000), lanes != null && !enemy.Flies ? lanes : LanesFor(enemy, fill.SecondRoad), output, ranks ? Math.Max(troop.Pack, GrownPack) : troop.Pack, troop.Loose && !ranks);
                }
                else
                {
                    group.Interval *= ranks ? PackInnerIntervalShare : 1f;
                    output.Add(group);
                }

                order++;
            }

            return shortfall;
        }

        private static float Weight(TroopSpec troop, float bias, IReadOnlyDictionary<string, EnemyStats> enemies, BalanceRules rules) =>
            troop.Weight * (float)Math.Pow(ThreatOf(troop, enemies, rules), bias);

        // The builder's plated lever (round 22e): armoured troops weigh more in every fill of the difficulty, soft ones as written.
        private static float PlatedLever(TroopSpec troop, WaveScriptSpec script, int difficulty, IReadOnlyDictionary<string, EnemyStats> enemies)
        {
            EnemyStats enemy = Get(enemies, troop.Enemy);
            return enemy != null && enemy.Armor > 0f ? script.PlatedBiasFor(difficulty) : 1f;
        }

        // Round 68, growth: a plated troop and a flying troop come in greater numbers with every wave past their trait's grace window.
        private static float Grown(TroopSpec troop, TraitGrowth growth, IReadOnlyDictionary<string, EnemyStats> enemies)
        {
            EnemyStats enemy = Get(enemies, troop.Enemy);
            return enemy == null ? 1f : (enemy.Armor > 0f ? growth.Armour : 1f) * (enemy.Flies ? growth.Air : 1f);
        }

        // Round 27, the swarm lever: troops without armour and no heavier than twice the lightest troop of the fill weigh more on the
        // difficulty (the lever against a splash row: more small bodies than a blast can catch), the rest as written.
        private const float SwarmHealthShare = 2f;

        private static float SwarmLever(TroopSpec troop, List<TroopSpec> troops, WaveScriptSpec script, int difficulty, IReadOnlyDictionary<string, EnemyStats> enemies)
        {
            float bias = script.SwarmBiasFor(difficulty);
            EnemyStats enemy = Get(enemies, troop.Enemy);
            if (bias == 1f || enemy == null || enemy.Armor > 0f)
            {
                return 1f;
            }

            float lightest = float.MaxValue;
            foreach (TroopSpec other in troops)
            {
                EnemyStats candidate = Get(enemies, other.Enemy);
                lightest = candidate != null ? Math.Min(lightest, candidate.EffectiveHealth) : lightest;
            }

            return enemy.EffectiveHealth <= lightest * SwarmHealthShare ? bias : 1f;
        }

        // The builder's raid lever (round 22e): the wave opens with a few fast, spread runners of the cheapest troop, ahead of its fill.
        private static void AddRaid(List<GroupSpec> output, List<TroopSpec> open, int secondRoad, Random random, IReadOnlyDictionary<string, EnemyStats> enemies, BalanceRules rules)
        {
            List<TroopSpec> runners = Cheapest(open, 1, enemies, rules);
            EnemyStats enemy = runners.Count > 0 ? Get(enemies, runners[0].Enemy) : null;
            if (enemy == null)
            {
                return;
            }

            var group = new GroupSpec { Enemy = enemy, Count = RaidCount, Interval = runners[0].Interval * RaidSpread, Delay = 0f, Source = runners[0].Source, Path = Road };
            Scatter(group, random.Next(0, 1000), LanesFor(enemy, secondRoad), output, 0, true);
        }

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
        private static void Scatter(GroupSpec group, int seed, int[] lanes, List<GroupSpec> output, int pack, bool loose = false)
        {
            if (loose && pack <= 0)
            {
                Single(group, seed, lanes, output);
                return;
            }

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

        // A loose troop walks in one at a time, each a whole interval behind the last, taking turns on the lanes: one group per
        // lane at the lanes' share of the rate, offset so the walkers alternate.
        private static void Single(GroupSpec group, int seed, int[] lanes, List<GroupSpec> output)
        {
            for (int l = 0; l < lanes.Length; l++)
            {
                int count = group.Count / lanes.Length + (l < group.Count % lanes.Length ? 1 : 0);
                if (count <= 0)
                {
                    continue;
                }

                output.Add(new GroupSpec
                {
                    Enemy = group.Enemy,
                    Count = count,
                    Interval = group.Interval * lanes.Length,
                    Delay = group.Delay,
                    Source = group.Source,
                    Stagger = l * group.Interval,
                    Path = lanes[(seed + l) % lanes.Length],
                });
            }
        }
    }
}
