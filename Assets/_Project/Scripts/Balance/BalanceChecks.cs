using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// The checks a level's waves must pass on every difficulty. The solver aims Medium at its target; these bands are
    /// what it does not optimise: Easy must stay holdable with margin, each step up may ask more, no wave may be a cliff
    /// against the few before it, and late Easy waves must still ask something. Bosses get a looser band.
    /// </summary>
    public static class BalanceChecks
    {
        private const int CliffMemory = 3;
        public const int OpeningWaves = 4;
        private const float LateShare = 0.65f;

        public static List<string> Check(string level, string difficultyName, int difficulty, IReadOnlyList<WaveLine> lines, BalanceRules rules)
        {
            var failures = new List<string>();
            float lateSum = 0f;
            int lateCount = 0;
            var recent = new List<float>();
            float band = rules.MaxPressure(difficulty);
            for (int i = 0; i < lines.Count; i++)
            {
                WaveLine line = lines[i];
                string where = $"{level} {difficultyName} wave {line.Wave}";
                float limit = band * (line.Boss ? rules.BossAllowance : 1f);
                if (line.Pressure > limit)
                {
                    failures.Add($"{where}: pressure {line.Pressure:0.00} over {limit:0.00}");
                }

                float recentMax = 0f;
                foreach (float value in recent)
                {
                    recentMax = Math.Max(recentMax, value);
                }

                // The opening waves may ramp as steeply as they like: the first pack of a new species is meant to be felt.
                if (!line.Boss && i >= OpeningWaves && recentMax > 0f && line.Pressure > recentMax * rules.MaxCliff)
                {
                    failures.Add($"{where}: pressure jumps from {recentMax:0.00} to {line.Pressure:0.00}");
                }

                if (!line.Boss)
                {
                    recent.Add(line.Pressure);
                    if (recent.Count > CliffMemory)
                    {
                        recent.RemoveAt(0);
                    }
                }

                if (difficulty == 0 && !line.Boss && i >= lines.Count * LateShare)
                {
                    lateSum += line.Pressure;
                    lateCount++;
                }
            }

            if (difficulty == 0 && lateCount > 0 && lateSum / lateCount < rules.MinLateEasyPressure)
            {
                failures.Add($"{level} Easy: late waves average pressure {lateSum / lateCount:0.00}, too easy");
            }

            return failures;
        }

        /// <summary>Mean pressure of a level's plain waves after the opening: what the player meets on an ordinary wave.</summary>
        public static float PlainMean(IReadOnlyList<WaveLine> lines)
        {
            float sum = 0f;
            int count = 0;
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
                // harder cell in play while the paper, which underrates its swarm waves, reads it under Hard), as CheckHumanTarget does.
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
        /// The human lens against the target curve (<see cref="BalanceRules.HumanSurplusAt"/>): on every difficulty a level's
        /// stored waves leave a competent player within <see cref="BalanceRules.HumanTargetTolerance"/> of the surplus the
        /// curve asks for at its place in the continent. <paramref name="plainMeans"/> as in <see cref="CheckHuman"/>.
        /// </summary>
        public static List<string> CheckHumanTarget(string level, int continentIndex, IReadOnlyList<float> plainMeans, BalanceRules rules, CampaignLevel tuned = null)
        {
            var failures = new List<string>();
            for (int d = 0; d < plainMeans.Count; d++)
            {
                // The builder's target scale (WaveTuning) is part of the curve the level was calibrated to.
                float target = rules.HumanSurplusAt(d, continentIndex) / (tuned != null ? tuned.TargetScaleFor(d) : 1f);
                float surplus = plainMeans[d] > 0f ? 1f / plainMeans[d] : float.PositiveInfinity;
                if (Math.Abs(surplus - target) > target * rules.HumanTargetTolerance)
                {
                    failures.Add($"{level} difficulty {d}: a competent player has {surplus:0.00}x the firepower, the curve asks {target:0.00}x");
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
