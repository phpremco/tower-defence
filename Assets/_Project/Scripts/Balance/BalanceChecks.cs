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
    }
}
