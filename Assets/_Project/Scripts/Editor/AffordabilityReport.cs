using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Economy;
using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The affordability table of the first counter tower (<see cref="CounterAffordability"/>) for the first continent:
    /// written to Tools/PlayTest/affordability.txt, every cell that is not payable from bounties logged as a warning.
    /// A warning, not an error: whether a first flyer may need starting money kept back is a design decision per difficulty.
    /// </summary>
    public static class AffordabilityReport
    {
        public const string ReportPath = "Tools/PlayTest/affordability.txt";
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";

        /// <summary>The levels of the first continent (Crystal Plains): the campaign's first nodes.</summary>
        public const int FirstContinentNodes = 6;

        private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Psycho" };

        [MenuItem("Tools/Validate/Counter Affordability (first continent)")]
        public static void Run()
        {
            List<string> warnings = Write();
            Debug.Log($"Counter affordability: {warnings.Count} warnings, see {ReportPath}");
        }

        /// <summary>Writes the table and logs the warnings; returns them.</summary>
        public static List<string> Write()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            List<CounterAffordability.Row> rows = CounterAffordability.Rows(catalog, unlocks.StartingTowerIds, FirstContinentNodes);
            var warnings = new List<string>();
            File.WriteAllText(ReportPath, Table(rows, warnings), new UTF8Encoding(false));
            foreach (string warning in warnings)
            {
                Debug.LogWarning("[Validator] counter affordability: " + warning);
            }

            return warnings;
        }

        /// <summary>The table as Markdown; the warnings are added to <paramref name="warnings"/>.</summary>
        public static string Table(List<CounterAffordability.Row> rows, List<string> warnings)
        {
            var text = new StringBuilder();
            text.Append("Counter affordability from the level data (no bot, no model): can the first counter tower be paid by the wave it must stand in?\n");
            text.Append("Deadline = the trait's first wave + the grace waves of the difficulty (Easy 2, Medium 1, Hard 1, Psycho 0). Bounties = every creature of the waves\n");
            text.Append("before the deadline wave, killed, through the income share; no early-call bonus, no harvest. Start share = the part of the starting money the\n");
            text.Append("counter takes when the bounties fall short.\n\n");
            text.Append("| Cell | Trait | First wave | Deadline | Counter | Price | Starting money | Bounties by deadline | Start share | Verdict |\n");
            text.Append("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |\n");
            foreach (CounterAffordability.Row row in rows)
            {
                string warning = CounterAffordability.Warning(row);
                if (warning != null)
                {
                    warnings.Add(warning);
                }

                string name = row.Difficulty < DifficultyNames.Length ? DifficultyNames[row.Difficulty] : I(row.Difficulty);
                string share = row.Verdict == CounterAffordability.Verdict.NeedsStartingMoney ? row.StartingMoneyShare.ToString("0%", CultureInfo.InvariantCulture) : "-";
                text.Append($"| {row.Level.Replace("Level_", string.Empty)} {name} | {row.Trait} | w{I(row.FirstWave)} | w{I(row.DeadlineWave)} | {(row.CounterId.Length > 0 ? row.CounterId : "none")} | {I(row.Price)} | {I(row.StartingMoney)} | {I(row.KillCoins)} | {share} | {Verdict(row.Verdict)} |\n");
            }

            text.Append($"\n{I(warnings.Count)} warnings of {I(rows.Count)} rows.\n");
            return text.ToString();
        }

        private static string Verdict(CounterAffordability.Verdict verdict)
        {
            switch (verdict)
            {
                case CounterAffordability.Verdict.Payable:
                    return "payable from bounties";
                case CounterAffordability.Verdict.NeedsStartingMoney:
                    return "WARNING: only from the starting money";
                case CounterAffordability.Verdict.OutOfReach:
                    return "WARNING: out of reach";
                default:
                    return "WARNING: no counter owned";
            }
        }

        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
