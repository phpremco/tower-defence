using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Chooses how tough the creatures of every wave are (round 83: one pass per difficulty, every wave to its own target).
    /// The script fixes the size of the crowd (and so the kill money); the solver only sets the wave's health multiplier
    /// and the health of a level's boss. Waves are solved in order, because the kills of one wave buy the defence of the
    /// next. A wave's target is the level's target (what a competent player's towers alone are asked on a plain wave)
    /// times the wave's share of the curve: a ramp over the opening waves, a slow rise through the level, a factor per
    /// wave kind, a peak on boss waves and a breather after them. The ledger's player decides its purchase before it
    /// knows the wave's toughness (<see cref="Ledger.PlanOf"/>), so a wave's pressure only rises with its multiplier and
    /// a plain bisection finds the one that meets the target. Nothing is held on a level mean: a wave that a cap or the
    /// band lowers is simply lower, and no other wave answers for it.
    /// A level generator calls this with a new script and map and gets shaped waves without hand tuning; the level of a
    /// cell as played is then one scalar found against the probe (Docs/WAVE_GRAMMAR.md, "How to balance a new level").
    /// </summary>
    public static class WaveSolver
    {
        public const int SolveDifficulty = 1;
        private const int Iterations = 24;
        private const int TrendMemory = 3;

        private const float MinHealth = 0.4f;
        private const float MaxHealth = 60f;

        /// <summary>A creature does not grow tougher from one wave to the next by more than this, nor lose more than this.</summary>
        private const float MaxRise = 1.3f;
        private const float MaxDrop = 0.7f;

        /// <summary>
        /// No ground wave is tougher than this many times the median of the ground waves before it, once there are
        /// <see cref="MedianFrom"/> of them: the band alone lets three rises in a row add up (a late swarm of Crysium Vein
        /// Psycho at 2.8 times its level's median). The gate on the stored waves allows 2.5.
        /// </summary>
        private const float MostOverMedian = 2.2f;
        private const int MedianFrom = 4;

        /// <summary>No ground wave drops under this share of that median (the gate on the stored waves allows 0.4).</summary>
        private const float LeastOfMedian = 0.45f;

        /// <summary>The pressure the solver aims a wave at, on Medium.</summary>
        public static float Target(WaveScriptSpec script, int number, BalanceRules rules, float airGrowth = 1f) => TargetOn(script, SolveDifficulty, number, rules, airGrowth);

        /// <summary>
        /// The Medium target curve, read with the wave kinds and bosses of the given difficulty. A raid that has grown past its
        /// grace window (<paramref name="airGrowth"/>, <see cref="WaveComposer.GrowthOn"/>) aims as much higher as it has more
        /// flyers, so they keep their toughness, as with the raid scale of the script.
        /// </summary>
        public static float TargetOn(WaveScriptSpec script, int difficulty, int number, BalanceRules rules, float airGrowth = 1f)
        {
            float phase = script.Waves > 1 ? (number - 1) / (float)(script.Waves - 1) : 0f;
            float target = rules.TargetPressure * (rules.EarlyTargetShare + (rules.LateTargetShare - rules.EarlyTargetShare) * phase);
            if (number <= rules.OpeningWaves)
            {
                target *= rules.OpeningShare + (1f - rules.OpeningShare) * (number - 1) / Math.Max(1f, rules.OpeningWaves);
            }

            if (WaveComposer.BossOn(script, difficulty, number) != null)
            {
                return target * rules.BossFactor;
            }

            if (WaveComposer.BossOn(script, difficulty, number - 1) != null)
            {
                target *= rules.AfterBossFactor;
            }

            switch (WaveComposer.KindOf(script, difficulty, number))
            {
                case WaveComposer.Kind.Rush:
                    return target * rules.RushFactor;
                case WaveComposer.Kind.Heavy:
                    return target * rules.HeavyFactor;
                case WaveComposer.Kind.Air:
                    return target * rules.AirFactor * script.AirRaidScaleFor(difficulty) * airGrowth;
                default:
                    return target;
            }
        }

        /// <summary>
        /// How much higher a wave aims once its plated troops have grown past their grace window (round 68): as much as they
        /// have grown in number (<see cref="WaveComposer.GrowthOn"/>), so the extra bodies keep the toughness of the first
        /// ones, as with a raid and its scale. One for a wave without plated troops. The human calibration then sets the level
        /// of the whole curve, so the level asks no more in all; it asks it where the armour is.
        /// </summary>
        public static float ArmourAim(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int number, int secondRoad, BalanceRules rules)
        {
            float growth = WaveComposer.GrowthOn(script, enemies, difficulty, number, secondRoad, rules).Armour;
            if (growth <= 1f)
            {
                return 1f;
            }

            return WaveComposer.Compose(script, enemies, difficulty, number, 1f, secondRoad, rules).CounterCheck ? growth : 1f;
        }

        /// <summary>The multipliers of one difficulty and, per wave, the health of a level's boss on top of its wave's.</summary>
        public sealed class Solution
        {
            public float[] Healths;
            public float[] BossScales;

            /// <summary>Per wave the multiplier its creatures walk in with: the solved one times what a capped troop could not buy.</summary>
            public float[] WalkIn;

        }

        /// <summary>
        /// A wave's share of the level's target: the Medium target curve read with the wave kinds and bosses of the
        /// difficulty, over its mean on the plain waves (after the opening; no boss, raid or counter check), so the plain
        /// waves of a level average its target when nothing caps them.
        /// </summary>
        public static float[] Shares(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, int difficulty, int secondRoad, BalanceRules rules)
        {
            var shares = new float[script.Waves];
            float sum = 0f;
            int plain = 0;
            for (int i = 0; i < script.Waves; i++)
            {
                int number = i + 1;
                float air = WaveComposer.GrowthOn(script, enemies, difficulty, number, secondRoad, rules).Air;
                shares[i] = TargetOn(script, difficulty, number, rules, air) * ArmourAim(script, enemies, difficulty, number, secondRoad, rules);
                bool counted = i >= BalanceChecks.OpeningWaves && WaveComposer.BossOn(script, difficulty, number) == null
                    && WaveComposer.KindOf(script, difficulty, number) != WaveComposer.Kind.Air
                    && !WaveComposer.Compose(script, enemies, difficulty, number, 1f, secondRoad, rules).CounterCheck;
                if (counted)
                {
                    sum += shares[i];
                    plain++;
                }
            }

            float mean = plain > 0 ? sum / plain : rules.TargetPressure;
            for (int i = 0; i < shares.Length; i++)
            {
                shares[i] /= mean;
            }

            return shares;
        }

        /// <summary>
        /// One difficulty of a level, wave by wave. <paramref name="lens"/> is the player the targets are read on (its towers
        /// alone), <paramref name="withHero"/> the same player with the hero, whom no wave may ask more than the
        /// difficulty's hero cap; <paramref name="levelTarget"/> is the pressure a plain wave asks of the lens on average.
        /// <paramref name="stored"/> is the factor the caller stores the multipliers with (play's correction of the paper):
        /// the floor of a raid is a floor on the health as stored. <paramref name="calm"/> is
        /// <see cref="Solution.WalkIn"/> of the same difficulty solved without surprises, or null: what a wave that
        /// carries a surprise counts as for the waves after it.
        /// </summary>
        public static Solution Solve(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, LevelInput level, DifficultyStats difficulty, int secondRoad, BalanceRules lens, BalanceRules withHero, float levelTarget, float stored = 1f, float[] calm = null)
        {
            stored = Math.Max(0.001f, stored);
            int d = difficulty.Index;
            var solution = new Solution { Healths = new float[script.Waves], BossScales = new float[script.Waves], WalkIn = new float[script.Waves] };
            float[] shares = Shares(script, enemies, d, secondRoad, lens);
            var recent = new List<float>();
            var ground = new List<float>();
            // The toughest ordinary creatures met so far, as they walked in: what a boss of the level is held against.
            var yardstick = new BossYardstick(lens);
            var purse = new KillPurse(level, difficulty, lens);
            var held = new Ledger.AirHoldings();
            float cap = lens.HeroCapFor(d);
            for (int i = 0; i < script.Waves; i++)
            {
                int number = i + 1;
                // The crowd and its timing do not depend on the health multiplier, so any multiplier sets the clock. The clock
                // is the stored waves' (the first wave's longer countdown), so the solve reads what the ledger reads back.
                float delay = i == 0 ? lens.FirstWaveDelay : lens.WaveDelay;
                WaveSpec timing = WaveComposer.Compose(script, enemies, d, number, 1f, secondRoad, lens);
                timing.StartDelay = delay;
                purse.Begin(timing);
                float carried = purse.Carried(timing);
                int money = purse.Money(i, script.Waves);
                Ledger.AirHoldings standing = held;

                WaveSpec Wave(float health, float bossScale)
                {
                    WaveSpec wave = WaveComposer.Compose(script, enemies, d, number, health, secondRoad, lens);
                    wave.StartDelay = delay;
                    wave.BossHealthScale = bossScale;
                    return wave;
                }

                float Pressure(BalanceRules rules, float health, float bossScale) =>
                    Ledger.EvaluateWave(level, Wave(health, bossScale), difficulty, money, rules, standing, null, carried).Pressure;

                BossSpec boss = WaveComposer.BossOn(script, d, number);
                bool raid = WaveComposer.KindOf(script, d, number) == WaveComposer.Kind.Air && boss == null;
                bool levelBoss = boss != null && enemies.TryGetValue(boss.Enemy, out EnemyStats bossEnemy) && bossEnemy.IsBoss && !bossEnemy.IsContinentBoss;
                float target = levelTarget * shares[i];

                // The same creature does not change much from one wave to the next: a ground wave stays inside a band of the
                // toughest of the ground waves just before it, as its creatures walk in (a troop capped at the largest group
                // size makes every creature of its wave tougher by what it could not buy, timing.HealthMultiplier). The lower
                // edge is for a wave whose pressure is carried in by earlier waves: no health of its own brings it to its target,
                // and it stays on the edge instead of becoming paper; a wave its own health can bring down (a thin purse after
                // the sky guns of a raid) drops as far as it needs (Reach). A raid is another kind of wave (other creatures,
                // other guns) and is free; its flyers keep their book toughness where the rule says.
                float lift = timing.HealthMultiplier;
                float trend = Trend(recent);
                // The floor is on the creatures as they walk in too: a capped swarm carries its unbought budget as health.
                float bottom = MinHealth / lift;
                float low = raid ? Math.Max(MinHealth, lens.RaidHealthFloorFor(d) / stored) : recent.Count == 0 ? bottom : Math.Max(bottom, trend * MaxDrop / lift);
                float high = raid || recent.Count == 0 ? MaxHealth : Math.Max(low, Math.Min(MaxHealth, trend * MaxRise / lift));
                if (!raid && ground.Count >= MedianFrom)
                {
                    high = Math.Max(low, Math.Min(high, Median(ground) * MostOverMedian / lift));
                }

                // A level's boss is sized by one rule and before its crowd (round 86): it costs the player's guns
                // BossOverRegular times what the toughest ordinary creature met so far cost them, under shells and under
                // light fire alike (BossYardstick), and never walks in under BossBookShare of its book health. Nothing else
                // sizes it: no share of the defence, no cap, no floor on the health as stored, so a cell's scalar moves the
                // boss exactly as it moves every other creature. The multiple is on the boss as it walks in, so it is taken
                // off the wave's lift (a capped troop makes every creature of its wave tougher, the boss included). A boss
                // with nothing before it is held against the toughest creature of its own wave, at that wave's multiplier.
                float bossWalksIn = 0f;
                float bossOverCrowd = 0f;
                if (levelBoss && lens.BossOverRegularFor(d) > 0f)
                {
                    EnemyStats stats = enemies[boss.Enemy];
                    if (yardstick.HasMet(stats.Flies))
                    {
                        bossWalksIn = Math.Max(lens.BossBookShare, yardstick.Multiple(stats, lens.BossOverRegularFor(d)));
                    }
                    else
                    {
                        var own = new BossYardstick(lens);
                        own.Meet(timing, 1f);
                        bossWalksIn = lens.BossBookShare;
                        bossOverCrowd = own.HasMet(stats.Flies) ? own.Multiple(stats, lens.BossOverRegularFor(d)) / lift : 0f;
                    }
                }

                // The scale of the boss over its wave's multiplier: the fixed size it walks in with, or the multiple of its
                // own crowd where that is more.
                float BossScale(float health) => !levelBoss || bossWalksIn <= 0f ? 1f : Math.Max(bossWalksIn / (lift * health), bossOverCrowd);

                // A plain wave that drops under its band (a thin purse) still keeps a share of the median of the ground waves
                // before it, the mirror of the cap over the median.
                float least = raid ? low : bottom;
                if (!raid && boss == null && ground.Count >= MedianFrom)
                {
                    least = Math.Min(low, Math.Max(least, Median(ground) * LeastOfMedian / lift));
                }

                float WithHero(float health) => Pressure(withHero, health, BossScale(health));
                float crowd;
                if (levelBoss)
                {
                    // The boss is the fight of its wave, and the rule above is all that sizes it. Its crowd comes on the
                    // lower edge of its band: the creatures of the waves before it, a little softer (round 86). Until then
                    // the crowd was filled up to a paper target around the boss and gave way under a paper cap, and it
                    // jumped between a floor and the edge of the band when that cap was met or missed by a hair (Glass
                    // Meadows Psycho wave 5: x0.41, x1.75 and x0.55 on three neighbouring scalars); on the stored waves
                    // of round 85 the cap had put nearly every boss wave's crowd on this edge already. How tough the cell
                    // is as played is the closed loop to set.
                    crowd = low;
                }
                else
                {
                    crowd = Reach(health => Pressure(lens, health, BossScale(health)), target, least, low, high);
                    // No wave asks more of the player with the hero than the cap of the difficulty. The creatures of the
                    // wave give way: inside the band, and under it when their own health is what still asks too much. A
                    // plain wave that what an earlier wave left on the road holds over the cap (the wave after a boss)
                    // gives way to its share of the median, which is no paper crowd; kept on the edge of the band it was
                    // a cliff (round 85: Glass Meadows Psycho wave 10 stood x1.58 with the scalar of the cell at 0.96 and
                    // x2.65 at 0.94).
                    if (WithHero(crowd) > cap)
                    {
                        crowd = Meet(WithHero, cap, Math.Min(low, crowd), crowd);
                        if (least < crowd && WithHero(crowd) > cap)
                        {
                            crowd = WithHero(least) < cap ? Meet(WithHero, cap, least, crowd) : boss == null && ground.Count >= MedianFrom ? least : crowd;
                        }
                    }
                }

                // Rounded so the editor and the BalanceLab tool land on the same numbers.
                crowd = (float)Math.Round(crowd, 3);
                solution.Healths[i] = crowd;
                solution.BossScales[i] = (float)Math.Round(BossScale(crowd), 3);
                WaveSpec solved = Wave(crowd, solution.BossScales[i]);
                var next = new Ledger.AirHoldings();
                WaveLine line = Ledger.EvaluateWave(level, solved, difficulty, money, lens, held, next, carried);
                held = next;
                purse.Add(solved, line);
                solution.WalkIn[i] = solved.HealthMultiplier;
                // What the waves after this one are held against (the band, the median, the yardstick of a boss): its
                // creatures as they walk in. A wave that carries a surprise stands there as the same wave of the level
                // without surprises (round 84): its creatures are fewer and tougher, and a band, a median or a boss they lift
                // would let the waves after it follow (a late swarm lives on the upper edge of its band: Crysium Vein Psycho
                // wave 16 stood x1.59 above the same level solved without surprises). So a surprise takes from its own wave
                // and moves no other. Its creatures do count for the yardstick, as kinds the player has met, at that calm
                // multiplier (round 86; at their own they made every boss after a surprise bigger).
                float reference = calm != null && solved.Surprise != SurpriseKind.None ? calm[i] : solved.HealthMultiplier;
                // A raid stays off the yardstick: it is another kind of wave with a multiplier of its own (free of the band),
                // and under light fire its flyers cost five times their bar.
                if (!raid)
                {
                    yardstick.Meet(solved, reference);
                }

                // The median is read on every ground wave, a boss wave's crowd included, as the gate on the stored waves reads it.
                if (!raid)
                {
                    ground.Add(reference);
                }

                if (!raid && boss == null)
                {
                    recent.Add(reference);
                    if (recent.Count > TrendMemory)
                    {
                        recent.RemoveAt(0);
                    }
                }
            }

            return solution;
        }

        private static float Median(List<float> values)
        {
            var sorted = new List<float>(values);
            sorted.Sort();
            return sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) * 0.5f;
        }

        // The toughest of the ground waves just solved: what the next one is held against.
        private static float Trend(List<float> recent)
        {
            float trend = 0f;
            foreach (float value in recent)
            {
                trend = Math.Max(trend, value);
            }

            return trend;
        }

        // The health between the band's edges that meets the target; under the lower edge only when a health down to the
        // floor does meet it. Where even the floor is over the target the wave's own health is not what asks, and it keeps
        // the lower edge.
        private static float Reach(Func<float, float> pressure, float target, float floor, float low, float high)
        {
            if (pressure(low) <= target)
            {
                return Meet(pressure, target, low, high);
            }

            return floor < low && pressure(floor) < target ? Meet(pressure, target, floor, low) : low;
        }

        // Pressure never falls as health rises (the purchase is decided on the planned wave, Ledger.PlanOf), so a bisection
        // on the logarithm finds the health that meets the target: the low end when the wave is over it already (what
        // earlier waves left on the road), the high end when nothing in the range reaches it.
        private static float Meet(Func<float, float> pressure, float target, float low, float high)
        {
            if (high <= low || pressure(low) >= target)
            {
                return low;
            }

            if (pressure(high) <= target)
            {
                return high;
            }

            double lo = Math.Log(low);
            double hi = Math.Log(high);
            for (int i = 0; i < Iterations; i++)
            {
                double mid = (lo + hi) * 0.5;
                if (pressure((float)Math.Exp(mid)) < target)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return (float)Math.Exp((lo + hi) * 0.5);
        }
    }
}
