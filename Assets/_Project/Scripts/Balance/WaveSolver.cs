using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// Chooses how tough the creatures of every wave are, so the ledger's pressure on Medium follows the target curve: a
    /// ramp over the opening waves, a slow rise through the level, a factor per wave kind so the rhythm stays, a peak on
    /// boss waves and a breather after them. The script fixes the size of the crowd (and so the kill money); the solver
    /// only sets the wave's health multiplier. Tougher creatures pay no more, so the purse cannot snowball into ever
    /// bigger waves. Waves are solved in order because the kills of one wave buy the defence of the next. The other
    /// difficulties use the same multipliers with their profiles on top, and are checked against their own bands.
    /// A level generator calls this with a new script and map and gets balanced waves without hand tuning.
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

        /// <summary>The step (a factor of health) the search for a raid's lowest crossing of its target walks up in.</summary>
        private const float CrossingStep = 1.05f;

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

        /// <summary>One health multiplier per wave, solved on Medium against the player the level expects there.</summary>
        public static float[] Solve(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, LevelInput level, DifficultyStats medium, int secondRoad, BalanceRules rules)
        {
            var healths = new float[script.Waves];
            var recent = new List<float>();
            var purse = new KillPurse(level, medium, rules);
            var held = new Ledger.AirHoldings();
            for (int i = 0; i < script.Waves; i++)
            {
                int number = i + 1;
                // The crowd and its timing do not depend on the health multiplier, so any multiplier sets the clock.
                WaveSpec timing = WaveComposer.Compose(script, enemies, SolveDifficulty, number, 1f, secondRoad, rules);
                purse.Begin(timing);
                float carried = purse.Carried(timing);
                int money = purse.Money(i, script.Waves);
                float target = Target(script, number, rules, WaveComposer.GrowthOn(script, enemies, SolveDifficulty, number, secondRoad, rules).Air) * ArmourAim(script, enemies, SolveDifficulty, number, secondRoad, rules);
                bool boss = WaveComposer.BossOn(script, SolveDifficulty, number) != null;
                // The same creature should not change much from one wave to the next: the multiplier follows the recent
                // trend within a band, so a wave that is light for its kind stays light instead of growing a wall.
                float trend = 0f;
                foreach (float value in recent)
                {
                    trend = Math.Max(trend, value);
                }

                // Air raids and boss waves are a different kind of wave (other creatures, other guns), so they may drop as
                // far as they need below the ground trend.
                WaveComposer.Kind kind = WaveComposer.KindOf(script, SolveDifficulty, number);
                bool free = boss || kind == WaveComposer.Kind.Air;
                float low = recent.Count == 0 || free ? MinHealth : Math.Max(MinHealth, trend * MaxDrop);
                float high = recent.Count == 0 ? MaxHealth : Math.Min(MaxHealth, trend * MaxRise);
                // Rounded so the editor and the BalanceLab tool land on the same numbers.
                float health = (float)Math.Round(BisectOn(script, enemies, level, medium, secondRoad, rules, number, money, held, carried, target, low, high), 3);
                WaveSpec solved = WaveComposer.Compose(script, enemies, SolveDifficulty, number, health, secondRoad, rules);
                var next = new Ledger.AirHoldings();
                WaveLine line = Ledger.EvaluateWave(level, solved, medium, money, rules, held, next, carried);
                // The trend floor must not push a wave out of its band (a thin purse after the air guns of a raid): such a wave
                // may drop below the trend as far as it needs.
                if (!boss && low > MinHealth && line.Pressure > rules.MaxPressure(SolveDifficulty))
                {
                    health = (float)Math.Round(BisectOn(script, enemies, level, medium, secondRoad, rules, number, money, held, carried, target, MinHealth, health), 3);
                    solved = WaveComposer.Compose(script, enemies, SolveDifficulty, number, health, secondRoad, rules);
                    next = new Ledger.AirHoldings();
                    line = Ledger.EvaluateWave(level, solved, medium, money, rules, held, next, carried);
                }

                healths[i] = health;
                held = next;
                purse.Add(solved, line);
                if (!free)
                {
                    recent.Add(health);
                    if (recent.Count > TrendMemory)
                    {
                        recent.RemoveAt(0);
                    }
                }
            }

            return healths;
        }

        /// <summary>
        /// The waves a harder or easier difficulty adds or changes (an extra air raid, an extra boss, a surprise of another
        /// size or on another wave) have no Medium counterpart to copy; they are solved on their own difficulty against the Medium target times the
        /// difficulty's share. Every other wave keeps the Medium multiplier, with the profile on top.
        /// </summary>
        public static float[] SolveVariant(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, LevelInput level, DifficultyStats difficulty, float[] medium, int secondRoad, BalanceRules rules)
        {
            var healths = (float[])medium.Clone();
            int d = difficulty.Index;
            float share = rules.DifficultyPressure[Math.Min(d, rules.DifficultyPressure.Length - 1)];
            var purse = new KillPurse(level, difficulty, rules);
            var held = new Ledger.AirHoldings();
            for (int i = 0; i < script.Waves; i++)
            {
                int number = i + 1;
                WaveSpec timing = WaveComposer.Compose(script, enemies, d, number, 1f, secondRoad, rules);
                purse.Begin(timing);
                float carried = purse.Carried(timing);
                int money = purse.Money(i, script.Waves);
                float airGrowth = WaveComposer.GrowthOn(script, enemies, d, number, secondRoad, rules).Air;
                float armourAim = ArmourAim(script, enemies, d, number, secondRoad, rules);
                bool changed = WaveComposer.KindOf(script, d, number) != WaveComposer.KindOf(script, SolveDifficulty, number)
                    || WaveComposer.BossOn(script, d, number) != WaveComposer.BossOn(script, SolveDifficulty, number)
                    || WaveComposer.HasSprinkle(script, d, number) != WaveComposer.HasSprinkle(script, SolveDifficulty, number)
                    || (WaveComposer.KindOf(script, d, number) == WaveComposer.Kind.Air && script.AirRaidScaleFor(d) * airGrowth != script.AirRaidScaleFor(SolveDifficulty) * WaveComposer.GrowthOn(script, enemies, SolveDifficulty, number, secondRoad, rules).Air)
                    || armourAim != ArmourAim(script, enemies, SolveDifficulty, number, secondRoad, rules)
                    // A surprise is sized per difficulty (its share of the wave, and which waves carry one), so it has no Medium twin.
                    || WaveGrammar.SurpriseOn(script, enemies, d, number, secondRoad, rules) != SurpriseKind.None
                    || WaveGrammar.SurpriseOn(script, enemies, SolveDifficulty, number, secondRoad, rules) != SurpriseKind.None;
                if (changed)
                {
                    float target = TargetOn(script, d, number, rules, airGrowth) * share * armourAim;
                    healths[i] = (float)Math.Round(BisectOn(script, enemies, level, difficulty, secondRoad, rules, number, money, held, carried, target, MinHealth, MaxHealth), 3);
                }

                WaveLine line = Ledger.EvaluateWave(level, WaveComposer.Compose(script, enemies, d, number, healths[i], secondRoad, rules), difficulty, money, rules, held, null, carried);
                // A copied Medium multiplier can still break this difficulty's band when its purse is thinner than Medium's
                // at that point (an extra boss wave earns less); such a wave is solved on its own like a changed one.
                if (!changed && WaveComposer.BossOn(script, d, number) == null && line.Pressure > rules.MaxPressure(d))
                {
                    float target = TargetOn(script, d, number, rules, airGrowth) * share * armourAim;
                    healths[i] = (float)Math.Round(BisectOn(script, enemies, level, difficulty, secondRoad, rules, number, money, held, carried, target, MinHealth, healths[i]), 3);
                    line = Ledger.EvaluateWave(level, WaveComposer.Compose(script, enemies, d, number, healths[i], secondRoad, rules), difficulty, money, rules, held, null, carried);
                }

                var next = new Ledger.AirHoldings();
                Ledger.EvaluateWave(level, WaveComposer.Compose(script, enemies, d, number, healths[i], secondRoad, rules), difficulty, money, rules, held, next, carried);
                held = next;
                purse.Add(WaveComposer.Compose(script, enemies, d, number, healths[i], secondRoad, rules), line);
            }

            return healths;
        }

        // Pressure grows with health, so a bisection on its logarithm finds the multiplier that meets the target. Not on an air
        // raid, though: the ledger's player buys a sky gun's next step only once the flyers' pressure is above
        // BalanceRules.SkyHoldPressure, a raid aims below that, and with the step bought the pressure drops under the target
        // again. So a raid meets its target with the sky guns held and once more after every step, and a bisection over the
        // whole range lands on either by a hair of the purse (the raid of Quartz Flats wave 13: x1.18 in round 78, x2.46 in
        // round 79 with the same crowd and nearly the same purse, and every difficulty copied it). With
        // BalanceRules.RaidsCappedAtHeldGuns a raid walks up from the low end in steps and bisects inside the first step that
        // crosses: the raid sized against the sky guns the player holds.
        private static float BisectOn(WaveScriptSpec script, IReadOnlyDictionary<string, EnemyStats> enemies, LevelInput level, DifficultyStats difficulty, int secondRoad, BalanceRules rules, int number, int money, Ledger.AirHoldings held, float carried, float target, float low, float high)
        {
            double lo = Math.Log(low);
            double hi = Math.Log(Math.Max(low, high));
            float Pressure(double logHealth) =>
                Ledger.EvaluateWave(level, WaveComposer.Compose(script, enemies, difficulty.Index, number, (float)Math.Exp(logHealth), secondRoad, rules), difficulty, money, rules, held, null, carried).Pressure;

            if (Pressure(lo) >= target)
            {
                return (float)Math.Exp(lo);
            }

            if (rules.RaidsCappedAtHeldGuns && WaveComposer.KindOf(script, difficulty.Index, number) == WaveComposer.Kind.Air)
            {
                double step = Math.Log(CrossingStep);
                double from = lo;
                bool crossed = false;
                while (from < hi && !crossed)
                {
                    double to = Math.Min(hi, from + step);
                    if (Pressure(to) >= target)
                    {
                        lo = from;
                        hi = to;
                        crossed = true;
                    }

                    from = to;
                }

                if (!crossed)
                {
                    return (float)Math.Exp(hi);
                }
            }
            else if (Pressure(hi) <= target)
            {
                return (float)Math.Exp(hi);
            }

            for (int i = 0; i < Iterations; i++)
            {
                double mid = (lo + hi) * 0.5;
                if (Pressure(mid) < target)
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
