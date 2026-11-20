using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// What the paper ledger reports about a level's stored waves: the human lens per difficulty, the ramp within a
    /// continent, whether a level is beatable with the hero, a boss's mechanic. Reports and warnings: since round 87 a
    /// wave's health is a formula (WaveFormula) and the level as played is measured (the closed loop).
    /// </summary>
    public static class BalanceChecks
    {
        public const int OpeningWaves = 4;

        /// <summary>
        /// Mean pressure of a level's plain waves after the opening: what the player meets on an ordinary wave. A boss wave is
        /// none, and neither is a wave that asks for a counter tower (round 68): both aim above the curve on purpose. An air
        /// raid is none either (round 80b): it aims below the curve and is capped at the sky guns the player holds, and as a
        /// plain wave every softer raid was paid for by tougher ground waves.
        /// </summary>
        public static float PlainMean(IReadOnlyList<WaveLine> lines)
        {
            float sum = 0f;
            int count = 0;
            for (int i = OpeningWaves; i < lines.Count; i++)
            {
                if (!lines[i].Boss && !lines[i].CounterCheck && !lines[i].AirRaid)
                {
                    sum += lines[i].Pressure;
                    count++;
                }
            }

            if (count > 0)
            {
                return sum / count;
            }

            // A level whose every wave after the opening asks for a counter has no plain wave left: its mean is theirs.
            for (int i = OpeningWaves; i < lines.Count; i++)
            {
                if (!lines[i].Boss)
                {
                    sum += lines[i].Pressure;
                    count++;
                }
            }

            return count > 0 ? sum / count : 0f;
        }

        /// <summary>One line for the ledger table: a competent player's firepower over need on plain waves, per difficulty.</summary>
        public static string DescribeHuman(string level, IReadOnlyList<float> plainMeans)
        {
            var text = new System.Text.StringBuilder($"{level} human lens, towers alone, firepower over need on plain waves:");
            for (int d = 0; d < plainMeans.Count; d++)
            {
                text.Append(plainMeans[d] > 0f ? $" {1f / plainMeans[d]:0.00}x" : " -");
            }

            if (plainMeans.Count > 1 && plainMeans[1] > 0f)
            {
                text.Append($" (Easy asks {plainMeans[0] / plainMeans[1]:0.00} of Medium)");
            }

            return text.ToString();
        }

        /// <summary>
        /// The human lens (<see cref="BalanceRules.HumanTowersOnly"/>) on one level: <paramref name="plainMeans"/> holds the
        /// <see cref="PlainMean"/> of the level's waves per difficulty, Easy first, for a competent player's towers alone.
        /// Easy may not hand that player more than <see cref="BalanceRules.MaxEasySurplus"/> times the firepower it needs,
        /// must stay clearly lighter than Medium, and every difficulty must ask more than the one below it.
        /// </summary>
        public static List<string> CheckHuman(string level, IReadOnlyList<float> plainMeans, BalanceRules rules, CampaignLevel tuned = null)
        {
            var failures = new List<string>();
            if (plainMeans.Count == 0)
            {
                return failures;
            }

            float easy = plainMeans[0];
            if (easy > 0f && 1f / easy > rules.MaxEasySurplus)
            {
                failures.Add($"{level} Easy: a competent player has {1f / easy:0.00}x the firepower the plain waves need (most {rules.MaxEasySurplus:0.00}x)");
            }

            if (plainMeans.Count > 1 && easy > plainMeans[1] * rules.MaxEasyShareOfMedium)
            {
                failures.Add($"{level} Easy asks {easy / plainMeans[1]:0.00} of Medium on plain waves (most {rules.MaxEasyShareOfMedium:0.00})");
            }

            for (int d = 1; d < plainMeans.Count; d++)
            {
                // Judged at the curve: the builder's target scale is play's correction of the paper (round 31: Fault Line Psycho at 0.66 is the
                // harder cell in play while the paper, which underrates its swarm waves, reads it under Hard).
                float above = plainMeans[d] / (tuned != null ? tuned.TargetScaleFor(d) : 1f);
                float below = plainMeans[d - 1] / (tuned != null ? tuned.TargetScaleFor(d - 1) : 1f);
                if (above <= below)
                {
                    failures.Add($"{level}: difficulty {d} asks no more than difficulty {d - 1} on plain waves at the curve ({above:0.00} vs {below:0.00}; stored {plainMeans[d]:0.00} vs {plainMeans[d - 1]:0.00})");
                }
            }

            return failures;
        }

        /// <summary>
        /// The campaign ramp: within a continent each level asks at least as much of a competent player as the one before it
        /// (net of the upgrades expected there), on every difficulty. <paramref name="surplus"/> holds per level, in play order,
        /// the human-lens surplus per difficulty; a level may be easier than the one before by at most
        /// <see cref="BalanceRules.RampTolerance"/>, so no level is a dip.
        /// </summary>
        public static List<string> CheckRamp(IReadOnlyList<string> levels, IReadOnlyList<float[]> surplus, BalanceRules rules)
        {
            var failures = new List<string>();
            for (int i = 1; i < surplus.Count; i++)
            {
                for (int d = 0; d < surplus[i].Length && d < surplus[i - 1].Length; d++)
                {
                    if (surplus[i][d] > surplus[i - 1][d] * (1f + rules.RampTolerance))
                    {
                        failures.Add($"{levels[i]} difficulty {d}: easier than {levels[i - 1]} ({surplus[i][d]:0.00}x against {surplus[i - 1][d]:0.00}x)");
                    }
                }
            }

            return failures;
        }

        /// <summary>
        /// The rule for a continent boss's mechanic (Docs/BOSSES.md, "no wipe-outs"): over a fight of the given seconds in reach of
        /// the defence it may take away at most <see cref="BalanceRules.MaxBossMechanicShare"/> of the firepower. Returns the
        /// failure, or null when it keeps inside.
        /// </summary>
        public static string CheckBossMechanic(string boss, float lossSeconds, float fightSeconds, BalanceRules rules)
        {
            float share = fightSeconds > 0f ? lossSeconds / fightSeconds : 1f;
            return share > rules.MaxBossMechanicShare
                ? $"{boss}: its mechanic takes {share:P0} of the defence's firepower over a {fightSeconds:0}s fight (most {rules.MaxBossMechanicShare:P0})"
                : null;
        }

        /// <summary>
        /// Beatable: a competent player with the hero (<see cref="BalanceRules.HumanWithHero"/>) and the permanent upgrades the
        /// campaign expects there holds the plain waves with at least <see cref="BalanceRules.MinBeatableSurplusFor"/>, and no
        /// wave, bosses included, asks more than the difficulty's <see cref="BalanceRules.HeroCap"/> (plus a little tolerance).
        /// </summary>
        public static List<string> CheckBeatable(string level, string difficultyName, int difficulty, IReadOnlyList<WaveLine> withHero, BalanceRules rules)
        {
            var failures = new List<string>();
            float mean = PlainMean(withHero);
            float floor = rules.MinBeatableSurplusFor(difficulty);
            if (mean > 0f && 1f / mean < floor)
            {
                failures.Add($"{level} {difficultyName}: with the hero a competent player has only {1f / mean:0.00}x the firepower on plain waves (at least {floor:0.00}x)");
            }

            float most = rules.HeroCapFor(difficulty) * (1f + rules.BeatableTolerance);
            foreach (WaveLine line in withHero)
            {
                if (line.Pressure > most)
                {
                    failures.Add($"{level} {difficultyName} wave {line.Wave}{(line.Boss ? " (boss)" : string.Empty)}: pressure {line.Pressure:0.00} with the hero (most {most:0.00})");
                }
            }

            return failures;
        }
    }
}
