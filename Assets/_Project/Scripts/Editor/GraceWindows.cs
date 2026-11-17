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

            /// <summary>The last wave the row may fall at: the trait's first wave, the grace waves and the waves it must fall within.</summary>
            public int Deadline(CombatRules rules) => FirstWave + rules.GraceWaves(Difficulty) + rules.FallWithinWaves(Difficulty) - 1;
        }

        /// <summary>A cell and trait whose rows may fall later than the window says, by how many waves at most, and why (round 68).</summary>
        public readonly struct Allowance
        {
            public Allowance(string level, int difficulty, EnemyTraits trait, int wavesLate, string reason)
            {
                Level = level;
                Difficulty = difficulty;
                Trait = trait;
                WavesLate = wavesLate;
                Reason = reason;
            }

            public string Level { get; }
            public int Difficulty { get; }
            public EnemyTraits Trait { get; }
            public int WavesLate { get; }
            public string Reason { get; }
        }

        private const string RaidEndsIt = "on Hard and Psycho the plated pack walks in from wave 3 (Remco, round 34b) and the troops stay few until the raids: growth strong enough to end a row by the window left no searched plan winning (round 68, growth 1.5: best plan 0/3; growth 1.2 moved no row and cost the creative bot Hard and Psycho), so the row falls to the first raid instead";
        private const string LateCragbacks = "the Cragbacks walk in from wave 8 on Easy and Medium (round 68, was wave 4: five waves late); the row falls to the first raid, one wave after the window";
        private const string MediumScale = "at the old scale 1.15 with growth no searched plan won (best 0/3); scale 1.07 with growth 1.3 ends the row in time but leaves the best plan 2/3 and the creative bot 0/3, so the cell keeps scale 1.0 and growth 1.2 (best plan and creative bot 3/3)";
        private const string EdgeOfGrowth = "growth is at the measured edge of what a mixed plan still wins; the rush and boss waves inside the window carry no plated troops, so the row lasts to the next plated wave";
        private const string OneRaidLate = "the row falls to the raid one wave after the window closes; a bigger raid there costs the mixed plan its margin (Quartz Flats and Fault Line are the cells the creative bot already loses seeds on)";

        // Round 77 (Remco): creatures bite every structure softer on Easy and Medium (DifficultyProfile.StructureDamageFactor), so a
        // row without the counter keeps its guns and its base a wave or two longer. Measured on base and gates alone the rows were
        // as late, so the towers are not the cause. Every such row still loses all three seeds.
        private const string SofterBites = "the structure damage factor (round 77) softens every bite on Easy and Medium: the row keeps its base a wave or two longer than the window, and still loses every seed";
        private const string SofterBitesLaterRaids = "the structure damage factor (round 77) softens every bite, and the first-flyer rule moved the raids that used to end the row (first raid wave 6, was 3 and 4); the row still loses every seed";

        /// <summary>
        /// The cells that do not meet the window yet, each with the most waves its rows may be late and the reason. A row later
        /// than its allowance fails the gate (<c>CombatRulesTests</c>), and so does a row without the counter that wins a level:
        /// that has no allowance anywhere. Measured in round 68; shorten an allowance when a cell is tuned further.
        /// </summary>
        public static readonly Allowance[] Allowances =
        {
            new Allowance("Level_C1_01", 1, EnemyTraits.Armoured, 1, EdgeOfGrowth),
            new Allowance("Level_C1_01", 2, EnemyTraits.Armoured, 2, EdgeOfGrowth),
            new Allowance("Level_C1_01", 3, EnemyTraits.Armoured, 3, EdgeOfGrowth),
            new Allowance("Level_C1_02", 0, EnemyTraits.Armoured, 1, EdgeOfGrowth),
            new Allowance("Level_C1_02", 1, EnemyTraits.Armoured, 2, MediumScale + "; round 77: " + SofterBites),
            new Allowance("Level_C1_02", 2, EnemyTraits.Armoured, 1, EdgeOfGrowth),
            new Allowance("Level_C1_02", 3, EnemyTraits.Armoured, 3, EdgeOfGrowth),
            new Allowance("Level_C1_03", 0, EnemyTraits.Armoured, 1, LateCragbacks),
            new Allowance("Level_C1_03", 1, EnemyTraits.Armoured, 2, LateCragbacks + "; round 77: " + SofterBites),
            new Allowance("Level_C1_03", 2, EnemyTraits.Armoured, 6, RaidEndsIt),
            new Allowance("Level_C1_03", 3, EnemyTraits.Armoured, 7, RaidEndsIt),
            new Allowance("Level_C1_04", 0, EnemyTraits.Armoured, 1, SofterBites),
            new Allowance("Level_C1_04", 0, EnemyTraits.Airborne, 2, OneRaidLate + "; round 77: " + SofterBites),
            new Allowance("Level_C1_04", 1, EnemyTraits.Airborne, 2, SofterBitesLaterRaids),
            new Allowance("Level_C1_05", 0, EnemyTraits.Armoured, 2, "growth 1.5 here left no searched plan winning (round 68: best plan 0/3); the row falls to the raid of wave 8 and the waves after it"),
            new Allowance("Level_C1_05", 1, EnemyTraits.Armoured, 3, SofterBitesLaterRaids),
            new Allowance("Level_C1_05", 1, EnemyTraits.Airborne, 1, OneRaidLate),
            new Allowance("Level_C1_05", 3, EnemyTraits.Airborne, 1, OneRaidLate),
            new Allowance("Level_C1_06", 0, EnemyTraits.Airborne, 1, SofterBites),
            new Allowance("Level_C1_06", 1, EnemyTraits.Airborne, 1, OneRaidLate),
        };

        /// <summary>The allowance of a cell and trait, or null.</summary>
        public static Allowance? AllowanceFor(string level, int difficulty, EnemyTraits trait)
        {
            foreach (Allowance allowance in Allowances)
            {
                if (allowance.Level == level && allowance.Difficulty == difficulty && allowance.Trait == trait)
                {
                    return allowance;
                }
            }

            return null;
        }

        /// <summary>
        /// What breaks the gate: a row without the counter that wins a level whose window closes before the last wave, and a row
        /// that falls later than the window plus its cell's allowance. Empty when the growth layer holds.
        /// </summary>
        public static List<string> Breaks(CombatRules rules)
        {
            var breaks = new List<string>();
            foreach (Row row in Rows(rules))
            {
                if (row.Verdict != Verdict.Late)
                {
                    continue;
                }

                string cell = Invariant($"{row.Level} {Names[row.Difficulty]}, {row.Trait}, {row.Tower} row");
                if (row.Wins * 2 > row.Runs || row.EarliestFall == 0)
                {
                    breaks.Add(cell + Invariant($": wins {row.Wins}/{row.Runs} without the counter tower"));
                    continue;
                }

                int late = row.EarliestFall - row.Deadline(rules);
                Allowance? allowance = AllowanceFor(row.Level, row.Difficulty, row.Trait);
                if (allowance == null || late > allowance.Value.WavesLate)
                {
                    breaks.Add(cell + Invariant($": fell at wave {row.EarliestFall}, {late} after the window (allowed: {(allowance == null ? 0 : allowance.Value.WavesLate)})"));
                }
            }

            return breaks;
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
                text.Append(Invariant($"| {row.Level.Replace("Level_", string.Empty)} {Names[row.Difficulty]} | {row.Trait} | w{row.FirstWave} of {row.Waves} | {row.Tower} | {row.Wins}/{row.Runs} | {fell} | {held} | {Words(row.Verdict)}{Allowed(row, rules)} |\n"));
            }

            List<string> breaks = Breaks(rules);
            text.Append(Invariant($"\nGate (CombatRulesTests): {(breaks.Count == 0 ? "holds" : breaks.Count + " breaks")}. No row without the counter may win; a late row needs an allowance with a reason (GraceWindows.Allowances).\n"));
            foreach (string line in breaks)
            {
                text.Append("- BREAK: ").Append(line).Append('\n');
            }

            text.Append(Invariant($"\n{rows.Count} rows: {counts[(int)Verdict.InTime]} in time, {counts[(int)Verdict.Early]} early, {counts[(int)Verdict.Late]} late, {counts[(int)Verdict.NotJudged]} not judged.\n"));
            return text.ToString();
        }

        private static readonly string[] Names = { "Easy", "Medium", "Hard", "Psycho" };

        // A late row inside its cell's allowance says so, with the reason.
        private static string Allowed(Row row, CombatRules rules)
        {
            Allowance? allowance = row.Verdict == Verdict.Late ? AllowanceFor(row.Level, row.Difficulty, row.Trait) : null;
            int late = row.EarliestFall - row.Deadline(rules);
            return allowance != null && row.EarliestFall > 0 && late <= allowance.Value.WavesLate
                ? Invariant($" ({late} late, allowed {allowance.Value.WavesLate}: {allowance.Value.Reason})")
                : string.Empty;
        }

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
