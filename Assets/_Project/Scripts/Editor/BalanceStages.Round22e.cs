using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using static System.FormattableString;

namespace Game.Editor
{
    /// <summary>
    /// Round 22e: stage A over adaptive strategies (<see cref="StrategySearch"/>), the in-play ramp on the recorded runs, and the
    /// README written from the strategies that won in stage B with the build order their winning seed took.
    /// </summary>
    public static partial class BalanceStages
    {
        private const int StrategySamples = 1500;

        /// <summary>How many of the best paper strategies the replay gets when none holds on paper (round 22f).</summary>
        private const int NoHoldCandidates = 3;

        /// <summary>The number of paper plans a strategy search samples per cell when the caller gives none.</summary>
        public static int DefaultStrategySamples => StrategySamples;

        // ---- Stage A: strategies -------------------------------------------------------------------------------------------------

        /// <summary>
        /// Searches strategies for every level and difficulty the filter file allows: per cell the strategy space is sampled and executed
        /// on paper, the distinct winners are written as LEVEL_DIFFICULTY_strategyN.json (the replay plays them with the adaptive bot) and
        /// described in LEVEL_DIFFICULTY.txt with the build order the paper took; plans_summary.txt gets one line per cell. The creative
        /// reference is one point of the space and is reported beside the obvious family on the same paper.
        /// </summary>
        public static SearchReport SearchStrategies(int samples = StrategySamples)
        {
            Loaded loaded = Load(true);
            var report = new SearchReport { Stale = loaded.Stale };
            Directory.CreateDirectory(PlansFolder);
            var summary = new StringBuilder();
            summary.Append(Invariant($"Strategy search (stage A, round 22e) {DateTime.Now:yyyy-MM-dd HH:mm}, {samples} strategies per cell. {loaded.CalibrationNote}\n"));
            summary.Append("Per cell: the best strategy on paper (stars and base left, or the fall wave), its worst-wave margin, how many of the top strategies hold,\n");
            summary.Append("the distinct winners (different spots, mix or timing), the creative reference on the same paper, and the obvious family.\n");
            summary.Append("The paper is a pre-screen; stage B (the replay with the adaptive bot) is the judge. Details: Tools/PlayTest/Plans/<level>_<difficulty>.txt.\n\n");
            summary.Append("level        diff    best strategy    margin  holding/top  distinct  creative          obvious\n");
            BalanceRules rules = loaded.Model.Rules;
            BalanceRules judge = rules.HumanWithHero();
            int index = 0;
            int node = -1;
            foreach (MapNode map in loaded.Catalog.Nodes)
            {
                node++;
                if (map.Level == null)
                {
                    continue;
                }

                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    if (!WantedCell(node, d))
                    {
                        continue;
                    }

                    StrategySearchResult result = StrategySearch.Run(new PlanSearchInput
                    {
                        LevelKey = map.Level.name,
                        Difficulty = d,
                        Level = loaded.Model.InputFor(index, d),
                        Waves = Waves(loaded.Model, map.Level, loaded.Catalog.Difficulties[d]),
                        Stats = loaded.Model.Difficulties[d],
                        Judge = judge,
                        Cap = rules.HeroCapFor(d) * (1f + rules.BeatableTolerance),
                        AllowSell = d == AcceptanceRules.Psycho,
                        Samples = samples,
                    });
                    report.Cells++;
                    WriteCellStrategies(map.Level, d, result, loaded);
                    string obvious = string.Join(", ", result.Obvious.ConvertAll(o => o.Plan.Name + " " + Outcome(o)));
                    PlanOutcome top = result.Top.Count > 0 ? result.Top[0].Outcome : null;
                    summary.Append(Invariant($"{map.Level.name,-12} {DifficultyName(d),-7} {(top != null ? Outcome(top) : "-"),-16} {(top != null ? top.Margin : 0f),6:0.00}  {result.Top.FindAll(t => t.Outcome.Holds).Count,4}/{result.Top.Count,-7} {result.DistinctWinners.Count,8}  {Outcome(result.Creative),-16}  {obvious}\n"));
                }

                index++;
            }

            File.WriteAllText(PlansSummaryPath, summary.ToString());
            report.Summary = summary.ToString();
            return report;
        }

        private static void WriteCellStrategies(LevelDefinition level, int difficulty, StrategySearchResult result, Loaded loaded)
        {
            Func<string, string> name = NameOf(level);
            var groundIds = new List<string>();
            foreach (TowerStats tower in result.Input.Level.Towers)
            {
                if (tower.IsGun && Layer.Has(tower.Targets, Layer.Ground))
                {
                    groundIds.Add(tower.Id);
                }
            }

            var text = new StringBuilder();
            text.Append(Invariant($"{level.DisplayName} ({level.name}) on {DifficultyName(difficulty)}: strategy search, {result.Sampled} strategies sampled, {result.Holding} hold on paper, base {result.Input.Level.BaseHealth:0}, cap {result.Input.Cap:0.00}. {loaded.CalibrationNote}\n"));
            text.Append("A strategy is a way of playing, not a timeline: which spots each tower takes first, which tower answers which threat, when an upgrade beats a\n");
            text.Append("new gun, when mines, the slow field and the drones come in. The adaptive bot plays it wave by wave; on paper the same rules give the build\n");
            text.Append("order shown. The paper verdict is a pre-screen (the siege model); the replay over the seeds is the judge. Cells are (x,y) on the level grid.\n\n");
            text.Append("Best spots per tower (first is best):\n");
            foreach (KeyValuePair<string, List<(Cell Cell, SpotCover Cover)>> pair in result.BestSpots)
            {
                text.Append(Invariant($"  {name(pair.Key),-14}"));
                foreach ((Cell cell, SpotCover cover) in pair.Value)
                {
                    text.Append(Invariant($" ({cell.X},{cell.Y}) cover {cover.GroundPass:0.0}{(cover.AirPass > 0f ? "/" + cover.AirPass.ToString("0.0", CultureInfo.InvariantCulture) + " air" : string.Empty)}{(cover.Gate ? " gate" : string.Empty)}{(cover.Base ? " door" : string.Empty)};"));
                }

                text.Append('\n');
            }

            text.Append("\nObvious plans on paper:\n");
            foreach (PlanOutcome outcome in result.Obvious)
            {
                text.Append(Invariant($"  {outcome.Plan.Name,-18} {Outcome(outcome)}, margin {outcome.Margin:0.00}{(outcome.FirstBreak > 0 ? " (leaks from wave " + outcome.FirstBreak + ")" : string.Empty)}\n"));
            }

            text.Append(Invariant($"\nThe creative reference on paper: {Outcome(result.Creative)}, margin {result.Creative.Margin:0.00}\n"));
            text.Append(result.Creative.Plan.Describe(name));
            text.Append(Invariant($"\nDistinct winning strategies ({result.DistinctWinners.Count}; different spots, mixes or timing):\n"));
            int shown = 0;
            foreach ((StrategySpec strategy, PlanOutcome outcome) in result.DistinctWinners)
            {
                if (shown++ >= result.Input.ShowPlans)
                {
                    break;
                }

                text.Append(Invariant($"\n{strategy.Name}: {Outcome(outcome)}, margin {outcome.Margin:0.00}, mean cover {outcome.MeanCover:0.00}, coins left {outcome.CoinsLeft}, gate repairs {outcome.Repairs} ({outcome.RepairSpent} coins)\n"));
                text.Append(strategy.Describe(groundIds, name));
                text.Append("  The build order the paper took:\n");
                text.Append(outcome.Plan.Describe(name));
                text.Append("  waves: ");
                foreach (PlanWaveLine line in outcome.Lines)
                {
                    text.Append(Invariant($"w{line.Wave}{(line.Boss ? "B" : string.Empty)} {line.Pressure:0.00}{(line.BaseDamage > 0.5f ? " (-" + line.BaseDamage.ToString("0", CultureInfo.InvariantCulture) + " base)" : string.Empty)}  "));
                }

                text.Append('\n');
            }

            if (result.DistinctWinners.Count == 0 && result.Top.Count > 0)
            {
                (StrategySpec best, PlanOutcome outcome) = result.Top[0];
                text.Append(Invariant($"\nNo strategy holds on paper. Best found: {Outcome(outcome)}, margin {outcome.Margin:0.00}{(outcome.FirstBreak > 0 ? ", leaks from wave " + outcome.FirstBreak : string.Empty)}\n"));
                text.Append(best.Describe(groundIds, name));
                text.Append(outcome.Plan.Describe(name));
            }

            File.WriteAllText(Path.Combine(PlansFolder, $"{level.name}_{difficulty}.txt"), text.ToString());
            foreach (string stale in Directory.GetFiles(PlansFolder, $"{level.name}_{difficulty}_*.json"))
            {
                File.Delete(stale);
            }

            // The strategies the replay plays: the distinct winners, or the best few found when none holds (the replay is the judge, and
            // the paper is pessimistic on the hard cells: round 22f replays more than one candidate there).
            var toWrite = new List<StrategySpec>();
            for (int i = 0; i < result.DistinctWinners.Count && i < result.Input.ShowPlans; i++)
            {
                toWrite.Add(result.DistinctWinners[i].Strategy);
            }

            if (toWrite.Count == 0)
            {
                for (int i = 0; i < Math.Min(NoHoldCandidates, result.Top.Count); i++)
                {
                    result.Top[i].Strategy.Name = $"{level.name}_{difficulty}_strategy{i + 1}";
                    toWrite.Add(result.Top[i].Strategy);
                }
            }

            foreach (StrategySpec strategy in toWrite)
            {
                File.WriteAllText(Path.Combine(PlansFolder, strategy.Name + ".json"), JsonUtility.ToJson(strategy, true));
            }
        }

        /// <summary>The searched strategies of a cell written by the last search, by name.</summary>
        public static Dictionary<string, StrategySpec> LoadStrategies(string levelName, int difficulty)
        {
            var strategies = new Dictionary<string, StrategySpec>();
            if (!Directory.Exists(PlansFolder))
            {
                return strategies;
            }

            foreach (string file in Directory.GetFiles(PlansFolder, $"{levelName}_{difficulty}_strategy*.json"))
            {
                StrategySpec strategy = JsonUtility.FromJson<StrategySpec>(File.ReadAllText(file));
                if (strategy != null && strategy.Spots != null)
                {
                    strategies[strategy.Name] = strategy;
                }
            }

            return strategies;
        }

        private static Func<string, string> NameOf(LevelDefinition level) => id =>
        {
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                if (tower.Id == id)
                {
                    return tower.DisplayName;
                }
            }

            return id;
        };

        // ---- The in-play ramp --------------------------------------------------------------------------------------------------

        /// <summary>
        /// The in-play ramp per continent (the curator, round 22e: play judges the ramp): per level and difficulty the cell margin
        /// (<see cref="AcceptanceRules.CellMargin"/>, the best replayed plan and the creative reference), and the dips where a level is
        /// easier in play than the one before by more than <see cref="BalanceRules.InPlayRampTolerance"/>.
        /// </summary>
        public static string InPlayRamp(Loaded loaded, List<RunRecord> obvious, List<RunRecord> plans, out List<(string Level, int Difficulty, string Reason)> dips)
        {
            var text = new StringBuilder();
            dips = new List<(string, int, string)>();
            int difficulties = loaded.Catalog.Difficulties.Count;
            text.Append(Invariant($"In-play ramp (tolerance {loaded.Model.Rules.InPlayRampTolerance:0.00}): per level the cell margin per difficulty (Easy Medium Hard Psycho; a win is 0.5 plus half its stars share, a loss half of how far it got; the mean of the best replayed plan and the creative reference).\n"));
            for (int region = 0; region < loaded.Catalog.Regions.Count; region++)
            {
                var levels = new List<string>();
                var margins = new List<float[]>();
                foreach (MapNode map in loaded.Catalog.Nodes)
                {
                    if (map.Level == null || map.Region != region)
                    {
                        continue;
                    }

                    var row = new float[difficulties];
                    var names = new string[difficulties];
                    for (int d = 0; d < difficulties; d++)
                    {
                        string cell = map.Level.name + " " + d.ToString(CultureInfo.InvariantCulture);
                        row[d] = AcceptanceRules.CellMargin(obvious.FindAll(r => r.Cell == cell), plans.FindAll(r => r.Cell == cell), out names[d]);
                    }

                    levels.Add(map.Level.name);
                    margins.Add(row);
                    text.Append(Invariant($"  {map.Level.name,-12}"));
                    for (int d = 0; d < difficulties; d++)
                    {
                        text.Append(Invariant($" {row[d],5:0.00}"));
                    }

                    text.Append("  (best: ").Append(string.Join(", ", Array.ConvertAll(names, n => string.IsNullOrEmpty(n) ? "-" : RunRecord.PlanNameOf(n)))).Append(")\n");
                }

                dips.AddRange(AcceptanceRules.InPlayRamp(levels, margins, loaded.Model.Rules.InPlayRampTolerance));
            }

            text.Append(dips.Count == 0 ? "  monotone within tolerance on every difficulty.\n" : "  dips: " + string.Join("; ", dips.ConvertAll(d => d.Level + " " + DifficultyName(d.Difficulty) + " " + d.Reason)) + "\n");
            return text.ToString();
        }
    }
}
