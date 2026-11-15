using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEditor;
using static System.FormattableString;

namespace Game.Editor
{
    /// <summary>
    /// The growth layer of the combat rules (round 67, Remco: "early in a level a few off-role guns may still handle a new
    /// creature type, a few waves later only the counter tower does"). Per cell and per trait that needs a counter tower
    /// (armour, air): the wave the trait first appears in, and how long each defence without the counter held after that,
    /// as played: the yardstick's rows of one tower type (<c>mono &lt;TowerId&gt;</c> in calibration_records.txt), for every
    /// row whose tower is not made for the trait. The rules' grace window says how many waves such a row may hold and by
    /// when it must have lost. A stage of the balance pipeline (<c>balance_pipeline.sh grace</c>); it reads, never writes data.
    /// </summary>
    public static class GraceWindows
    {
        public const string ReportPath = "Tools/PlayTest/grace_windows.txt";
        private const string RecordsPath = "Tools/PlayTest/calibration_records.txt";
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string TowerFolder = "Assets/_Project/Data/Towers";
        private const string RowPrefix = "mono ";

        // The traits a damage factor exists for: the ones a defence needs a counter tower against.
        private static readonly EnemyTraits[] CounterTraits = { EnemyTraits.Armoured, EnemyTraits.Airborne };

        /// <summary>How one row without the counter did after a trait appeared.</summary>
        public enum Verdict
        {
            /// <summary>Held through the grace window at most and lost in time.</summary>
            InTime,

            /// <summary>Lost before the grace window was over: allowed ("may hold"), but harsher than the window offers.</summary>
            Early,

            /// <summary>Still standing after the window and the waves it must fall within, or it won the level: the counter tower was not needed.</summary>
            Late,

            /// <summary>Lost before the trait appeared, or the trait appears too close to the end to judge.</summary>
            NotJudged,
        }

        public readonly struct Row
        {
            public Row(string level, int difficulty, EnemyTraits trait, int firstWave, int waves, string tower, int runs, int wins, int earliestFall, Verdict verdict)
            {
                Level = level;
                Difficulty = difficulty;
                Trait = trait;
                FirstWave = firstWave;
                Waves = waves;
                Tower = tower;
                Runs = runs;
                Wins = wins;
                EarliestFall = earliestFall;
                Verdict = verdict;
            }

            public string Level { get; }
            public int Difficulty { get; }
            public EnemyTraits Trait { get; }

            /// <summary>The first wave (1-based) a creature with the trait comes in.</summary>
            public int FirstWave { get; }
            public int Waves { get; }
            public string Tower { get; }
            public int Runs { get; }
            public int Wins { get; }

            /// <summary>The earliest wave a lost run of the row fell at; zero when every run won.</summary>
            public int EarliestFall { get; }
            public Verdict Verdict { get; }
        }

        /// <summary>The verdict for one row: the wave it fell at against the trait's first wave and the difficulty's window.</summary>
        public static Verdict Judge(int firstWave, int waveCount, int earliestFall, int wins, int runs, int grace, int fallWithin)
        {
            int deadline = firstWave + grace + fallWithin - 1;
            if (earliestFall > 0 && earliestFall < firstWave)
            {
                return Verdict.NotJudged;
            }

            // Most runs winning means the row is a way to win the level without the counter.
            if (wins * 2 > runs || earliestFall == 0)
            {
                return deadline > waveCount ? Verdict.NotJudged : Verdict.Late;
            }

            int held = earliestFall - firstWave;
            return held < grace ? Verdict.Early : earliestFall > deadline ? Verdict.Late : Verdict.InTime;
        }

        /// <summary>Every row of every cell the records hold, judged.</summary>
        public static List<Row> Rows(CombatRules rules)
        {
            var rows = new List<Row>();
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            if (catalog == null || !File.Exists(RecordsPath))
            {
                return rows;
            }

            var runs = new List<string[]>();
            foreach (string line in File.ReadAllLines(RecordsPath))
            {
                string[] fields = line.Split('|');
                if (fields.Length >= 10 && fields[0] == "run" && fields[3].StartsWith(RowPrefix))
                {
                    runs.Add(fields);
                }
            }

            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                for (int d = 0; d < catalog.Difficulties.Count; d++)
                {
                    WaveDefinition[] waves = node.Level.GetWaves(catalog.Difficulties[d]);
                    if (waves == null)
                    {
                        continue;
                    }

                    foreach (EnemyTraits trait in CounterTraits)
                    {
                        int first = FirstWave(rules, waves, trait);
                        if (first == 0)
                        {
                            continue;
                        }

                        AddRows(rows, rules, runs, node.Level.name, d, trait, first, waves.Length);
                    }
                }
            }

            return rows;
        }

        private static void AddRows(List<Row> rows, CombatRules rules, List<string[]> runs, string level, int difficulty, EnemyTraits trait, int first, int waveCount)
        {
            var byTower = new Dictionary<string, List<string[]>>();
            foreach (string[] run in runs)
            {
                if (run[1] != level || run[2] != difficulty.ToString())
                {
                    continue;
                }

                string id = run[3].Substring(RowPrefix.Length);
                if (!byTower.TryGetValue(id, out List<string[]> list))
                {
                    byTower[id] = list = new List<string[]>();
                }

                list.Add(run);
            }

            foreach (KeyValuePair<string, List<string[]>> entry in byTower)
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{TowerFolder}/{entry.Key}.asset");
                if (tower == null || tower.Answers(trait))
                {
                    continue;
                }

                int wins = 0;
                int earliest = 0;
                foreach (string[] run in entry.Value)
                {
                    if (run[6] == "won")
                    {
                        wins++;
                    }
                    else if (int.TryParse(run[8], out int fell) && (earliest == 0 || fell < earliest))
                    {
                        earliest = fell;
                    }
                }

                Verdict verdict = Judge(first, waveCount, earliest, wins, entry.Value.Count, rules.GraceWaves(difficulty), rules.FallWithinWaves(difficulty));
                rows.Add(new Row(level, difficulty, trait, first, waveCount, tower.DisplayName, entry.Value.Count, wins, earliest, verdict));
            }
        }

        private static int FirstWave(CombatRules rules, WaveDefinition[] waves, EnemyTraits trait)
        {
            for (int w = 0; w < waves.Length; w++)
            {
                foreach (SpawnGroup group in waves[w].Groups)
                {
                    EnemyDefinition enemy = group.Enemy;
                    // A boss carries its own fight; the window is about the troops a counter tower is bought for.
                    if (enemy != null && !enemy.IsBoss && (RoleMatrix.TraitsOf(rules, enemy) & trait) != 0)
                    {
                        return w + 1;
                    }
                }
            }

            return 0;
        }

        /// <summary>The table as Markdown (Tools/PlayTest/grace_windows.txt), with the count per verdict.</summary>
        public static string Report(CombatRules rules)
        {
            List<Row> rows = Rows(rules);
            var text = new StringBuilder();
            text.Append("Grace windows (round 67): a row of one tower type that is not made for a trait, after the trait first appears, as played in the yardstick.\n");
            text.Append("Window per difficulty (may hold / must have lost within, in waves): ");
            for (int d = 0; d < CombatRules.DifficultyCount; d++)
            {
                text.Append(Invariant($"{Names[d]} {rules.GraceWaves(d)} / {rules.FallWithinWaves(d)}{(d + 1 < CombatRules.DifficultyCount ? ", " : ".\n\n")}"));
            }

            text.Append("| Cell | Trait | First wave | Row without the counter | Wins | Fell at | Held | Verdict |\n| --- | --- | --- | --- | --- | --- | --- | --- |\n");
            var counts = new int[4];
            foreach (Row row in rows)
            {
                counts[(int)row.Verdict]++;
                string fell = row.EarliestFall > 0 ? "w" + row.EarliestFall : "-";
                string held = row.EarliestFall >= row.FirstWave ? (row.EarliestFall - row.FirstWave).ToString() : row.EarliestFall == 0 ? "all" : "-";
                text.Append(Invariant($"| {row.Level.Replace("Level_", string.Empty)} {Names[row.Difficulty]} | {row.Trait} | w{row.FirstWave} of {row.Waves} | {row.Tower} | {row.Wins}/{row.Runs} | {fell} | {held} | {Words(row.Verdict)} |\n"));
            }

            text.Append(Invariant($"\n{rows.Count} rows: {counts[(int)Verdict.InTime]} in time, {counts[(int)Verdict.Early]} early, {counts[(int)Verdict.Late]} late, {counts[(int)Verdict.NotJudged]} not judged.\n"));
            return text.ToString();
        }

        private static readonly string[] Names = { "Easy", "Medium", "Hard", "Psycho" };

        private static string Words(Verdict verdict)
        {
            switch (verdict)
            {
                case Verdict.InTime:
                    return "in time";
                case Verdict.Early:
                    return "early (inside the grace window)";
                case Verdict.Late:
                    return "LATE: the counter was not needed";
                default:
                    return "not judged";
            }
        }
    }
}
