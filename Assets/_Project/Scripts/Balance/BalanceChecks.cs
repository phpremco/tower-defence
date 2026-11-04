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
        private const int OpeningWaves = 4;
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
        public static List<string> CheckHuman(string level, IReadOnlyList<float> plainMeans, BalanceRules rules)
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
                if (plainMeans[d] <= plainMeans[d - 1])
                {
                    failures.Add($"{level}: difficulty {d} asks no more than difficulty {d - 1} on plain waves ({plainMeans[d]:0.00} vs {plainMeans[d - 1]:0.00})");
                }
            }

            return failures;
        }
    }
}
