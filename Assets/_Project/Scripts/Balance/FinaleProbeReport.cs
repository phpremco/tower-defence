using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Balance
{
    /// <summary>One continent boss on one difficulty as the finale probe played it (round 22f).</summary>
    public sealed class FinaleProbeCell
    {
        public string Boss = string.Empty;
        public int Difficulty;

        /// <summary>The largest health multiple of the boss (1 = the data) at which the finale player still won.</summary>
        public float Holds = 1f;

        /// <summary>Whether the run at the data's health was won, and the base left then (a share of its maximum).</summary>
        public bool WonAsIs;
        public float BaseAsIs;

        /// <summary>The difficulty's boss health share and the boss's data health when the probe ran.</summary>
        public float ShareNow = 1f;
        public float HealthNow;
        public int Runs;
    }

    /// <summary>
    /// The finale probe's findings as a file the EditMode checks and the apply stage read (Tools/PlayTest/finale_probe.txt):
    /// per boss and difficulty the health multiple that just holds in play. Derived from it: the boss health the data should
    /// carry (Medium's holding multiple with <see cref="BalanceRules.FinaleHoldShare"/> of margin) and the boss health share
    /// per difficulty that puts every difficulty at its own holding point relative to Medium. Pure C#.
    /// </summary>
    public static class FinaleProbeReport
    {
        public const string Path = "Tools/PlayTest/finale_probe.txt";
        public const int Medium = 1;
        public const int Hard = 2;
        public const int Psycho = 3;
        private const string CellMark = "cell";

        public static string Write(string header, IReadOnlyList<FinaleProbeCell> cells)
        {
            var text = new StringBuilder();
            text.Append(header).Append('\n');
            text.Append("Per boss and difficulty: holds = the largest boss health multiple (1 = the data) the finale player still beat; as-is = the run at the data's health;\n");
            text.Append("share and health as the data had them when the probe ran. Lines starting with 'cell' are read by ContinentFinaleTests and the finale apply stage.\n");
            foreach (FinaleProbeCell cell in cells)
            {
                text.Append(string.Join("|", CellMark, cell.Boss, I(cell.Difficulty), F(cell.Holds), cell.WonAsIs ? "won" : "lost", F(cell.BaseAsIs), F(cell.ShareNow), F(cell.HealthNow), I(cell.Runs))).Append('\n');
            }

            return text.ToString();
        }

        public static List<FinaleProbeCell> Read(IEnumerable<string> lines)
        {
            var cells = new List<FinaleProbeCell>();
            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length < 9 || parts[0] != CellMark)
                {
                    continue;
                }

                cells.Add(new FinaleProbeCell
                {
                    Boss = parts[1],
                    Difficulty = int.Parse(parts[2], CultureInfo.InvariantCulture),
                    Holds = float.Parse(parts[3], CultureInfo.InvariantCulture),
                    WonAsIs = parts[4] == "won",
                    BaseAsIs = float.Parse(parts[5], CultureInfo.InvariantCulture),
                    ShareNow = float.Parse(parts[6], CultureInfo.InvariantCulture),
                    HealthNow = float.Parse(parts[7], CultureInfo.InvariantCulture),
                    Runs = int.Parse(parts[8], CultureInfo.InvariantCulture),
                });
            }

            return cells;
        }

        public static FinaleProbeCell Find(IReadOnlyList<FinaleProbeCell> cells, string boss, int difficulty)
        {
            foreach (FinaleProbeCell cell in cells)
            {
                if (cell.Boss == boss && cell.Difficulty == difficulty)
                {
                    return cell;
                }
            }

            return null;
        }

        /// <summary>True when the probe beat the boss as the data has it on that difficulty (its holding multiple is at least one).</summary>
        public static bool HoldsAsIs(IReadOnlyList<FinaleProbeCell> cells, string boss, int difficulty)
        {
            FinaleProbeCell cell = Find(cells, boss, difficulty);
            return cell != null && cell.Holds >= 1f;
        }

        /// <summary>
        /// Round 30 (Remco: Psycho's boss share is never below Hard's): the largest boss share at which the Psycho finale player
        /// still beats every boss as the data will stand after <see cref="DerivedHealth"/> moves it (its hold divided by Medium's
        /// hold and the hold share), times the share it was probed at. -1 when no boss was probed on both Medium and Psycho.
        /// </summary>
        public static float PsychoCeiling(IReadOnlyList<FinaleProbeCell> cells, BalanceRules rules)
        {
            float ceiling = float.PositiveInfinity;
            foreach (FinaleProbeCell cell in cells)
            {
                FinaleProbeCell medium = cell.Difficulty == Psycho ? Find(cells, cell.Boss, Medium) : null;
                if (medium == null || medium.Holds <= 0f)
                {
                    continue;
                }

                ceiling = Math.Min(ceiling, cell.ShareNow * cell.Holds / (medium.Holds * rules.FinaleHoldShare));
            }

            return float.IsPositiveInfinity(ceiling) ? -1f : ceiling;
        }

        /// <summary>
        /// The one share Hard and Psycho carry under <see cref="BalanceRules.PsychoBossShareAtLeastHard"/>: Hard's derived share,
        /// pulled down to the Psycho ceiling when the Psycho finale player could not beat every boss at it. -1 without a Hard
        /// derivation.
        /// </summary>
        public static float HardPsychoShare(IReadOnlyList<FinaleProbeCell> cells, BalanceRules rules)
        {
            float hard = DerivedShare(cells, Hard);
            float ceiling = PsychoCeiling(cells, rules);
            return hard <= 0f ? -1f : ceiling > 0f ? Math.Min(hard, ceiling) : hard;
        }

        /// <summary>
        /// The health the data should carry for a boss: its health when probed times Medium's holding multiple, with the hold share
        /// of margin for a competent human (the finale bot is the reference, not the ceiling). -1 without a Medium cell.
        /// </summary>
        public static float DerivedHealth(IReadOnlyList<FinaleProbeCell> cells, string boss, BalanceRules rules)
        {
            FinaleProbeCell medium = Find(cells, boss, Medium);
            return medium == null ? -1f : medium.HealthNow * medium.Holds * rules.FinaleHoldShare;
        }

        /// <summary>
        /// The boss health share a difficulty should carry so its finale sits at its own holding point as Medium does at its
        /// own: the probed share times the difficulty's holding multiple over Medium's, averaged over the bosses probed on both.
        /// -1 when no boss was probed on both.
        /// </summary>
        public static float DerivedShare(IReadOnlyList<FinaleProbeCell> cells, int difficulty)
        {
            float sum = 0f;
            int count = 0;
            foreach (FinaleProbeCell cell in cells)
            {
                FinaleProbeCell medium = cell.Difficulty == difficulty ? Find(cells, cell.Boss, Medium) : null;
                if (medium == null || medium.Holds <= 0f)
                {
                    continue;
                }

                sum += cell.ShareNow * cell.Holds / medium.Holds;
                count++;
            }

            return count > 0 ? sum / count : -1f;
        }

        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
