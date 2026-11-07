using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Economy;
using Game.Levels;
using Game.Towers;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The EditMode stages of the balance pipeline (round 22; Docs/BALANCE.md "The balance pipeline"), each one entry point that
    /// reads the previous stage's files and the game's definitions, never a typed number: fit the calibration from the recorded
    /// runs, apply it to the asset, solve the waves against it, search plans per cell (stage A), judge the recorded runs by the
    /// acceptance rules. The tests call these; the Tools/Generate/Balance menu and Tools/PlayTest/Scripts call the same code.
    /// Every report starts with the calibration's state against the current inputs, so a stale calibration is never reused silently.
    /// </summary>
    public static partial class BalanceStages
    {
        public const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        public const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        public const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        public const string RecordsPath = "Tools/PlayTest/calibration_records.txt";
        public const string PlanRecordsPath = "Tools/PlayTest/plan_records.txt";
        public const string StepRecordsPath = "Tools/PlayTest/calibration_records_step.txt";
        public const string CalibrationTablePath = "Tools/PlayTest/calibration.txt";
        public const string WaveTablePath = "Tools/PlayTest/calibration_waves.txt";
        public const string PlansFolder = "Tools/PlayTest/Plans";
        public const string PlansSummaryPath = "Tools/PlayTest/plans_summary.txt";
        public const string AcceptancePath = "Tools/PlayTest/acceptance.txt";
        private const string FilterPath = "Tools/PlayTest/probe_arg.txt";

        // The calibration's acceptance (the critic of round 21): verdicts agree on 90% of the runs, mono rows too; plan rankings
        // correlate at 0.8; the fine step moves no efficiency by more than 10%.
        public const float MinAgreement = 0.9f;
        public const float MinSpearman = 0.8f;
        public const float MaxStepDrift = 0.1f;

        public sealed class Loaded
        {
            public CampaignCatalog Catalog;
            public UpgradeCatalog Upgrades;
            public UnlockTable Unlocks;
            public BalanceCalibration Calibration;
            public CampaignModel Model;
            public string Hash;

            public string CalibrationNote => Calibration != null ? Calibration.Describe(Hash) : "no calibration asset yet (inputs " + Hash + "); run the measure, fit and apply stages";

            public bool Stale => Calibration == null || Calibration.IsStaleFor(Hash);
        }

        /// <summary>The catalog and the model with the calibration applied (or at efficiency one when <paramref name="calibrated"/> is false).</summary>
        public static Loaded Load(bool calibrated)
        {
            var loaded = new Loaded
            {
                Catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath),
                Upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath),
                Unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath),
                Calibration = AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath),
            };
            if (loaded.Catalog == null)
            {
                return loaded;
            }

            CampaignModel plain = BalanceLedger.Campaign(loaded.Catalog, loaded.Upgrades, loaded.Unlocks.StartingTowerIds, BalanceRules.Default);
            loaded.Hash = InputHash.Of(plain);
            loaded.Model = calibrated ? BalanceLedger.Campaign(loaded.Catalog, loaded.Upgrades, loaded.Unlocks.StartingTowerIds, BalanceRules.Default, loaded.Calibration) : plain;
            return loaded;
        }

        // ---- Stage: fit ---------------------------------------------------------------------------------------------------

        public sealed class FitReport
        {
            public string Text = string.Empty;
            public bool Accepted;
            public string Verdict = string.Empty;
        }

        /// <summary>Fits the calibration on the recorded runs and writes calibration.txt; null when there are no records yet.</summary>
        public static FitReport FitCalibration()
        {
            if (!File.Exists(RecordsPath))
            {
                return null;
            }

            Loaded loaded = Load(false);
            BalanceRules rules = loaded.Model.Rules;
            List<RunRecord> runs = CalibrationFit.Read(File.ReadAllLines(RecordsPath));
            int planRuns = 0;
            if (File.Exists(PlanRecordsPath))
            {
                List<RunRecord> plans = CalibrationFit.Read(File.ReadAllLines(PlanRecordsPath));
                planRuns = plans.Count;
                runs.AddRange(plans);
            }

            var cells = new Dictionary<string, RecordedCell>();
            RecordedCell CellOf(RunRecord run) => CellFor(run, loaded, cells);
            CalibrationFit.Result all = CalibrationFit.Fit(runs, CellOf);
            CalibrationFit.Agreement self = CalibrationFit.Check(runs, CellOf, all.Efficiency, all.SiegeFactor, all.QueueGuardShare);

            // Hold-out over the levels: fit on the odd ones, judge the even ones, and the other way round.
            var holdOut = new CalibrationFit.Agreement();
            for (int half = 0; half < 2; half++)
            {
                List<RunRecord> train = runs.FindAll(r => LevelIndex(loaded.Catalog, r.Level) % 2 == half);
                List<RunRecord> test = runs.FindAll(r => LevelIndex(loaded.Catalog, r.Level) % 2 != half);
                if (train.Count == 0 || test.Count == 0)
                {
                    continue;
                }

                CalibrationFit.Result trained = CalibrationFit.Fit(train, CellOf);
                CalibrationFit.Agreement check = CalibrationFit.Check(test, CellOf, trained.Efficiency, trained.SiegeFactor, trained.QueueGuardShare);
                holdOut.Runs += check.Runs;
                holdOut.Agreed += check.Agreed;
                holdOut.MonoRuns += check.MonoRuns;
                holdOut.MonoAgreed += check.MonoAgreed;
                holdOut.Disagreements.AddRange(check.Disagreements);
                holdOut.Rankings.AddRange(check.Rankings);
            }

            var seeds = new SortedSet<int>();
            var steps = new SortedSet<float>();
            foreach (RunRecord run in runs)
            {
                seeds.Add(run.Seed);
                steps.Add(run.Step);
            }

            var text = new StringBuilder();
            text.Append(Invariant($"Calibration fit {DateTime.Now:yyyy-MM-dd HH:mm}: {runs.Count} runs ({planRuns} replayed plans), seeds {string.Join(",", seeds)}, step {string.Join(",", steps)}, inputs {loaded.Hash}.\n"));
            text.Append("Per tower type: efficiency = realised damage / paper capacity at one (from the towers' own spots, waves saturated at the health\n");
            text.Append("there was to take), absolute in the human lens with the hero (Docs/BALANCE.md, \"The balance pipeline\"). The siege factor is the gate and base\n");
            text.Append("health the runs really lost over the siege model's paper damage (round 22c). The cells are judged against the player who played each run (the\n");
            text.Append("campaign's expected player, or a plain save for the fresh-save sanity runs; round 22d). ApplyCalibration writes the factor, siege and utilisation lines into the asset.\n\n");
            text.Append("tower          efficiency   dealt   capacity  waves\n");
            foreach (KeyValuePair<string, float> pair in all.Efficiency)
            {
                (float dealt, float capacity, int waves) = all.Sums.TryGetValue(pair.Key, out var sums) ? sums : (0f, 0f, 0);
                text.Append(Invariant($"{pair.Key,-14} {pair.Value,10:0.000} {dealt,7:0} {capacity,10:0} {waves,6}\n"));
            }

            text.Append(Invariant($"\nQueue guard share {all.QueueGuardShare:0.00}: the share of their damage per second the queue and doorstep guards land on a siege, the one of 0.3..1.0 with the best verdict agreement (round 22d).\n"));
            text.Append(Invariant($"\nSiege factor {all.SiegeFactor:0.000}: gate and base health lost {all.StructureLost:0} over the siege model's paper damage {all.PaperStructureDamage:0} at factor one, on {all.SiegeWaves} waves with a siege.\n"));
            text.Append(all.RepairDroneBusyShare >= 0f
                ? Invariant($"Repair drone busy share {all.RepairDroneBusyShare:0.000} over {all.RepairDroneSeconds:0} drone-seconds on the field.\n")
                : "Repair drone busy share: no run had a repair drone (the rules' default stays).\n");
            text.Append(Invariant($"\nSelf check (fitted on all): verdict agreement {self.Agreed}/{self.Runs} ({self.Share:P0}), mono rows {self.MonoAgreed}/{self.MonoRuns} ({self.MonoShare:P0}), ranking Spearman {self.MeanSpearman:0.00} over cells with 4+ runs.\n"));
            text.Append(Invariant($"Hold-out (odd levels judge even and back): agreement {holdOut.Agreed}/{holdOut.Runs} ({holdOut.Share:P0}), mono {holdOut.MonoAgreed}/{holdOut.MonoRuns} ({holdOut.MonoShare:P0}), Spearman {holdOut.MeanSpearman:0.00}.\n"));
            text.Append("Rankings per cell (hold-out): ").Append(string.Join("; ", holdOut.Rankings.ConvertAll(r => Invariant($"{r.Cell} {r.Spearman:0.00} ({r.Plans})")))).Append('\n');
            text.Append("Disagreements (self check):\n");
            foreach (string line in self.Disagreements)
            {
                text.Append("  ").Append(line).Append('\n');
            }

            text.Append('\n').Append(StepCheck(loaded, cells)).Append("\n\n");
            foreach (KeyValuePair<string, float> pair in all.Efficiency)
            {
                text.Append(Invariant($"factor {pair.Key} {pair.Value:0.000}\n"));
            }

            if (all.RepairDroneBusyShare >= 0f)
            {
                text.Append(Invariant($"repairbusy {all.RepairDroneBusyShare:0.000}\n"));
            }

            text.Append(Invariant($"queueguard {all.QueueGuardShare:0.00}\n"));
            text.Append(Invariant($"siege {all.SiegeFactor:0.000}\nutilisation human {rules.HumanUtilisation:0.000}\nutilisation bot {rules.Utilisation:0.000}\ninputs {loaded.Hash}\nseeds {string.Join(",", seeds)}\nstep {(steps.Count > 0 ? Min(steps) : 0f):0.###}\n"));
            Directory.CreateDirectory(Path.GetDirectoryName(CalibrationTablePath));
            File.WriteAllText(CalibrationTablePath, text.ToString());
            File.WriteAllText(WaveTablePath, CalibrationFit.WaveTable(runs, CellOf, all.Efficiency, all.SiegeFactor));

            var report = new FitReport { Text = text.ToString() };
            report.Accepted = holdOut.Share >= MinAgreement && holdOut.MonoShare >= MinAgreement && holdOut.MeanSpearman >= MinSpearman;
            report.Verdict = Invariant($"hold-out agreement {holdOut.Share:P0} (mono {holdOut.MonoShare:P0}, need {MinAgreement:P0}), Spearman {holdOut.MeanSpearman:0.00} (need {MinSpearman}); self {self.Share:P0}");
            return report;
        }

        private static string StepCheck(Loaded loaded, Dictionary<string, RecordedCell> cells)
        {
            if (!File.Exists(StepRecordsPath))
            {
                return "Step check: no fine-step records (run the PlayMode Measure_StepCheck_FineAgainstUsual).";
            }

            var bySteps = new SortedDictionary<float, List<RunRecord>>();
            foreach (RunRecord run in CalibrationFit.Read(File.ReadAllLines(StepRecordsPath)))
            {
                if (!bySteps.TryGetValue(run.Step, out List<RunRecord> list))
                {
                    bySteps[run.Step] = list = new List<RunRecord>();
                }

                list.Add(run);
            }

            if (bySteps.Count < 2)
            {
                return "Step check: the records hold one step only.";
            }

            float fine = Min(bySteps.Keys);
            float coarse = Max(bySteps.Keys);
            CalibrationFit.Result fineFit = CalibrationFit.Fit(bySteps[fine], r => CellFor(r, loaded, cells));
            CalibrationFit.Result coarseFit = CalibrationFit.Fit(bySteps[coarse], r => CellFor(r, loaded, cells));
            var text = new StringBuilder("Step check (the same runs at each step, efficiency per type):\n");
            bool valid = true;
            foreach (KeyValuePair<string, float> tower in coarseFit.Efficiency)
            {
                float fineValue = fineFit.Efficiency.TryGetValue(tower.Key, out float f) ? f : tower.Value;
                float drift = Mathf.Abs(fineValue - tower.Value) / Mathf.Max(0.01f, tower.Value);
                valid &= drift <= MaxStepDrift;
                text.Append(Invariant($"  {tower.Key,-14} step {coarse:0.###}: {tower.Value:0.000}   step {fine:0.###}: {fineValue:0.000}   drift {drift:P0}{(drift > MaxStepDrift ? "  OVER" : string.Empty)}\n"));
            }

            text.Append(valid ? Invariant($"  the {coarse:0.###} s step is a valid calibration step (every type within {MaxStepDrift:P0}).") : Invariant($"  the {coarse:0.###} s step is NOT a valid calibration step: calibrate at {fine:0.###} s."));
            return text.ToString();
        }

        /// <summary>The ledger's view of a run's cell at efficiency one: input, loads and cap in the human lens with the hero.</summary>
        public static RecordedCell CellFor(RunRecord run, Loaded loaded, Dictionary<string, RecordedCell> cache)
        {
            if (cache.TryGetValue(run.Cell, out RecordedCell cell))
            {
                return cell;
            }

            BalanceRules rules = loaded.Model.Rules;
            int index = 0;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                if (node.Level.name == run.Level && run.Difficulty >= 0 && run.Difficulty < loaded.Catalog.Difficulties.Count)
                {
                    // The paper judges the player who really played: the campaign's expected player (the probe writes that state into
                    // its in-memory save, round 22d) or, for the fresh-save sanity runs, a plain save.
                    cell = new RecordedCell { Input = run.FreshPlayer ? loaded.Model.InputForFreshPlayer(index, run.Difficulty) : loaded.Model.InputFor(index, run.Difficulty), Judge = rules.HumanWithHero(), Cap = rules.HeroCapFor(run.Difficulty) * (1f + rules.BeatableTolerance) };
                    Ledger.Evaluate(cell.Input, Waves(loaded.Model, node.Level, loaded.Catalog.Difficulties[run.Difficulty]), loaded.Model.Difficulties[run.Difficulty], cell.Judge, cell.Loads);
                    cache[run.Cell] = cell;
                    return cell;
                }

                index++;
            }

            cache[run.Cell] = null;
            return null;
        }

        // ---- Stage: apply -------------------------------------------------------------------------------------------------

        /// <summary>Writes calibration.txt's fit into the BalanceCalibration asset, creating it when absent.</summary>
        public static string ApplyCalibration()
        {
            if (!File.Exists(CalibrationTablePath))
            {
                return "No " + CalibrationTablePath + ": run the fit stage first.";
            }

            var calibration = AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath);
            if (calibration == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BalanceCalibration.AssetPath));
                calibration = ScriptableObject.CreateInstance<BalanceCalibration>();
                AssetDatabase.CreateAsset(calibration, BalanceCalibration.AssetPath);
            }

            var so = new SerializedObject(calibration);
            SerializedProperty towers = so.FindProperty("_towers");
            towers.arraySize = 0;
            var report = new StringBuilder();
            foreach (string line in File.ReadAllLines(CalibrationTablePath))
            {
                string[] parts = line.Split(' ');
                if (parts.Length == 3 && parts[0] == "factor")
                {
                    towers.arraySize++;
                    SerializedProperty element = towers.GetArrayElementAtIndex(towers.arraySize - 1);
                    element.FindPropertyRelative("TowerId").stringValue = parts[1];
                    element.FindPropertyRelative("Efficiency").floatValue = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    report.Append(parts[1]).Append(' ').Append(parts[2]).Append("; ");
                }
                else if (parts.Length == 2 && parts[0] == "repairbusy")
                {
                    so.FindProperty("_repairDroneBusyShare").floatValue = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    report.Append("repair drone busy ").Append(parts[1]).Append("; ");
                }
                else if (parts.Length == 2 && parts[0] == "queueguard")
                {
                    so.FindProperty("_queueGuardShare").floatValue = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    report.Append("queue guard ").Append(parts[1]).Append("; ");
                }
                else if (parts.Length == 2 && parts[0] == "siege")
                {
                    so.FindProperty("_siegeFactor").floatValue = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    report.Append("siege ").Append(parts[1]).Append("; ");
                }
                else if (parts.Length == 3 && parts[0] == "utilisation")
                {
                    so.FindProperty(parts[1] == "human" ? "_humanUtilisation" : "_botUtilisation").floatValue = float.Parse(parts[2], CultureInfo.InvariantCulture);
                }
                else if (parts.Length == 2 && parts[0] == "inputs")
                {
                    so.FindProperty("_inputHash").stringValue = parts[1];
                }
                else if (parts.Length == 2 && parts[0] == "seeds")
                {
                    so.FindProperty("_seeds").stringValue = parts[1];
                }
                else if (parts.Length == 2 && parts[0] == "step")
                {
                    so.FindProperty("_step").floatValue = float.Parse(parts[1], CultureInfo.InvariantCulture);
                }
            }

            so.FindProperty("_measuredOn").stringValue = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(calibration);
            AssetDatabase.SaveAssets();
            return "Calibration written to " + BalanceCalibration.AssetPath + ": " + report;
        }

        // ---- Stage: plan search (the tester's stage A) ---------------------------------------------------------------------

        public sealed class SearchReport
        {
            public string Summary = string.Empty;
            public int Cells;
            public bool Stale;
        }

        /// <summary>Stage A (round 22e): the strategy search; the fixed-timeline plan search of round 22 stays as <see cref="SearchFixedPlans"/>.</summary>
        public static SearchReport SearchPlans(int samples = StrategySamples) => SearchStrategies(samples);

        /// <summary>
        /// Searches fixed plans for every level and difficulty the filter file allows, writes one readable file per cell and the
        /// replayable plans (JSON) under Tools/PlayTest/Plans, and a summary. Reads the calibrated model.
        /// </summary>
        public static SearchReport SearchFixedPlans(int samples = 3000)
        {
            Loaded loaded = Load(true);
            var report = new SearchReport { Stale = loaded.Stale };
            Directory.CreateDirectory(PlansFolder);
            var summary = new StringBuilder();
            var readme = new StringBuilder();
            summary.Append(Invariant($"Plan search (stage A) {DateTime.Now:yyyy-MM-dd HH:mm}, {samples} samples per cell. {loaded.CalibrationNote}\n"));
            summary.Append("Per cell: the best plan's stars and base left on paper (the leak model: a plan holds when the base survives), its worst-wave margin,\n");
            summary.Append("how many of the top plans hold, the distinct winners, and the obvious family on the same paper (stars and base left, or the fall wave).\n");
            summary.Append("Details and the plans a player can follow: Tools/PlayTest/Plans/<level>_<difficulty>.txt.\n\n");
            summary.Append("Repairs: coins the best plan spends on gate repairs by hand; 'drone' winners are distinct winning plans that buy a repair drone.\n");
            summary.Append("level        diff    best plan        margin  holding/top  distinct  repairs  drone  obvious\n");
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
                    if (!Wanted("levels", node) || !Wanted("difficulties", d))
                    {
                        continue;
                    }

                    PlanSearchResult result = PlanSearch.Run(new PlanSearchInput
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
                    WriteCell(map.Level, d, result, loaded);
                    readme.Append(ReadmeCell(map.Level, d, result));
                    string obvious = string.Join(", ", result.Obvious.ConvertAll(o => o.Plan.Name + " " + Outcome(o)));
                    PlanOutcome top = result.Top.Count > 0 ? result.Top[0] : null;
                    int droneWinners = result.DistinctWinners.FindAll(w => UsesRepairDrone(w.Plan, result.Input.Level)).Count;
                    summary.Append(Invariant($"{map.Level.name,-12} {DifficultyName(d),-7} {(top != null ? Outcome(top) : "-"),-16} {(top != null ? top.Margin : 0f),6:0.00}  {result.Top.FindAll(t => t.Holds).Count,4}/{result.Top.Count,-7} {result.DistinctWinners.Count,8} {(top != null ? top.RepairSpent : 0),8} {droneWinners,6}  {obvious}\n"));
                }

                index++;
            }

            File.WriteAllText(PlansSummaryPath, summary.ToString());
            File.WriteAllText(Path.Combine(PlansFolder, "README.md"), ReadmeHeader(loaded) + readme);
            report.Summary = summary.ToString();
            return report;
        }

        public const int ReadmePlans = 2;
        public const int ReadmeSpots = 3;

        // The plans for Remco: per level and difficulty the one or two best winning plans on paper (which tower where, from which wave,
        // what to upgrade) and the best spots per tower type. Written by the search stage, never typed; the replay table says which won in play.
        private static string ReadmeHeader(Loaded loaded) =>
            Invariant($"# Winning plans per level and difficulty\n\nWritten by the plan search (stage A, `Tools/Generate/Balance/5` or `PlanSearchTests`) on {DateTime.Now:yyyy-MM-dd HH:mm}; {loaded.CalibrationNote}.\n\n")
            + "Each plan is a priority list: a step is taken from its wave on, as soon as the money is there (an earlier step blocks the later ones).\n"
            + "Cells are (x,y) on the level grid. The paper verdict comes from the leak model (the base survives, stars by the base left); what the\n"
            + "same plans did in play is in `Tools/PlayTest/plan_replay.txt`, the obvious family in `Tools/PlayTest/yardstick.txt`. A cell without a\n"
            + "winning plan shows the best plan found and where it falls: a design question, not a plan to follow.\n";

        private static string ReadmeCell(LevelDefinition level, int difficulty, PlanSearchResult result)
        {
            string Name(string id)
            {
                foreach (TowerDefinition tower in level.AvailableTowers)
                {
                    if (tower.Id == id)
                    {
                        return tower.DisplayName;
                    }
                }

                return id;
            }

            var text = new StringBuilder();
            text.Append(Invariant($"\n## {level.DisplayName} ({level.name}) on {DifficultyName(difficulty)}\n\n"));
            text.Append("Best spots per tower: ");
            var spots = new List<string>();
            foreach (KeyValuePair<string, List<(Cell Cell, SpotCover Cover)>> pair in result.BestSpots)
            {
                var cells = new List<string>();
                for (int i = 0; i < pair.Value.Count && i < ReadmeSpots; i++)
                {
                    cells.Add(Invariant($"({pair.Value[i].Cell.X},{pair.Value[i].Cell.Y})"));
                }

                spots.Add(Name(pair.Key) + " " + string.Join(" ", cells));
            }

            text.Append(string.Join("; ", spots)).Append(".\n\n");
            var plans = new List<PlanOutcome>();
            foreach (PlanOutcome outcome in result.DistinctWinners)
            {
                if (plans.Count < ReadmePlans)
                {
                    plans.Add(outcome);
                }
            }

            if (plans.Count == 0 && result.Top.Count > 0)
            {
                text.Append(Invariant($"No plan holds on paper; the best found ({Outcome(result.Top[0])}, margin {result.Top[0].Margin:0.00}):\n\n"));
                plans.Add(result.Top[0]);
            }

            foreach (PlanOutcome outcome in plans)
            {
                text.Append(Invariant($"**{outcome.Plan.Name}** ({Outcome(outcome)}, margin {outcome.Margin:0.00}, {outcome.CoinsLeft} coins left, {outcome.RepairSpent} coins on gate repairs{(UsesRepairDrone(outcome.Plan, result.Input.Level) ? ", repair drone" : string.Empty)})\n\n```\n"));
                text.Append(outcome.Plan.Describe(Name));
                text.Append("```\n\n");
            }

            return text.ToString();
        }

        /// <summary>Whether a plan buys a repair drone (by the level's drone data, never by name).</summary>
        public static bool UsesRepairDrone(PlanSpec plan, LevelInput level)
        {
            foreach (PlanStep step in plan.Steps)
            {
                if (step.Kind == PlanStep.Drone && level.Drones.Exists(d => d.Id == step.Item && d.Role == DroneRole.Repair))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>A plan's paper outcome in a few characters: "3* base 540" or "falls w9".</summary>
        public static string Outcome(PlanOutcome outcome) =>
            outcome.Holds ? Invariant($"{outcome.Stars}* base {outcome.BaseLeft:0}") : Invariant($"falls w{outcome.FallWave}");

        private static void WriteCell(LevelDefinition level, int difficulty, PlanSearchResult result, Loaded loaded)
        {
            string Name(string id)
            {
                foreach (TowerDefinition tower in level.AvailableTowers)
                {
                    if (tower.Id == id)
                    {
                        return tower.DisplayName;
                    }
                }

                return id;
            }

            var text = new StringBuilder();
            text.Append(Invariant($"{level.DisplayName} ({level.name}) on {DifficultyName(difficulty)}: plan search, {result.Sampled} plans sampled, {result.Holding} hold on paper, base {result.Input.Level.BaseHealth:0}, cap {result.Input.Cap:0.00}. {loaded.CalibrationNote}\n"));
            text.Append("Cells are (x,y) on the level grid; a tower's cover is the route cells in its reach (weighted by the share of creatures that walk them),\n");
            text.Append("'gate' when the defence gate's queue is in reach, 'door' when the base's doorstep is, 'bunch' the share of that route where walkers bunch.\n");
            text.Append("A step is taken from its wave on, as soon as the purse has the price. A wave over pressure one leaks: what gets through hammers the base\n");
            text.Append("until the guns at the doorstep and the hero kill it; a plan holds when the base survives the level, its stars follow the base left.\n");
            text.Append("The margin is the worst wave's cap over pressure (below 1 something leaks there).\n\n");
            text.Append("Best spots per tower (first is best):\n");
            foreach (KeyValuePair<string, List<(Cell Cell, SpotCover Cover)>> pair in result.BestSpots)
            {
                text.Append(Invariant($"  {Name(pair.Key),-14}"));
                foreach ((Cell cell, SpotCover cover) in pair.Value)
                {
                    text.Append(Invariant($" ({cell.X},{cell.Y}) cover {cover.GroundPass:0.0}{(cover.AirPass > 0f ? "/" + cover.AirPass.ToString("0.0", CultureInfo.InvariantCulture) + " air" : string.Empty)}{(cover.Gate ? " gate" : string.Empty)}{(cover.Base ? " door" : string.Empty)}{(cover.Bunch > 0.2f ? " bunch " + cover.Bunch.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty)};"));
                }

                text.Append('\n');
            }

            text.Append("\nObvious plans on paper:\n");
            foreach (PlanOutcome outcome in result.Obvious)
            {
                text.Append(Invariant($"  {outcome.Plan.Name,-18} {Outcome(outcome)}, margin {outcome.Margin:0.00}{(outcome.FirstBreak > 0 ? " (leaks from wave " + outcome.FirstBreak + ")" : string.Empty)}, coins left {outcome.CoinsLeft}, steps taken {outcome.StepsTaken}{(outcome.StepsBlocked > 0 ? ", " + outcome.StepsBlocked + " never affordable" : string.Empty)}\n"));
            }

            text.Append(Invariant($"\nDistinct winning plans ({result.DistinctWinners.Count}; different spots, mixes or timing):\n"));
            int shown = 0;
            foreach (PlanOutcome outcome in result.DistinctWinners)
            {
                if (shown++ >= result.Input.ShowPlans)
                {
                    break;
                }

                text.Append(Invariant($"\n{outcome.Plan.Name}: {Outcome(outcome)}, margin {outcome.Margin:0.00}, mean cover {outcome.MeanCover:0.00}, coins left {outcome.CoinsLeft}, gate repairs {outcome.Repairs} ({outcome.RepairSpent} coins), gates left {outcome.GateLeft:0}, {outcome.Plan.Steps.Length} steps\n"));
                text.Append(outcome.Plan.Describe(Name));
                text.Append("  waves: ");
                foreach (PlanWaveLine line in outcome.Lines)
                {
                    text.Append(Invariant($"w{line.Wave}{(line.Boss ? "B" : string.Empty)} {line.Pressure:0.00}{(line.BaseDamage > 0.5f ? " (-" + line.BaseDamage.ToString("0", CultureInfo.InvariantCulture) + " base)" : string.Empty)}  "));
                }

                text.Append('\n');
            }

            if (result.DistinctWinners.Count == 0 && result.Top.Count > 0)
            {
                PlanOutcome best = result.Top[0];
                text.Append(Invariant($"\nNo plan holds on paper. Best found: {Outcome(best)}, margin {best.Margin:0.00}{(best.FirstBreak > 0 ? ", leaks from wave " + best.FirstBreak : string.Empty)}\n"));
                text.Append(best.Plan.Describe(Name));
            }

            File.WriteAllText(Path.Combine(PlansFolder, $"{level.name}_{difficulty}.txt"), text.ToString());

            // The plans the replay (stage B) plays: the distinct winners (or the best found) and the obvious family, as JSON.
            foreach (string stale in Directory.GetFiles(PlansFolder, $"{level.name}_{difficulty}_*.json"))
            {
                File.Delete(stale);
            }

            var toWrite = new List<PlanSpec>();
            for (int i = 0; i < result.DistinctWinners.Count && i < result.Input.ShowPlans; i++)
            {
                toWrite.Add(result.DistinctWinners[i].Plan);
            }

            if (toWrite.Count == 0 && result.Top.Count > 0)
            {
                result.Top[0].Plan.Name = $"{level.name}_{difficulty}_best";
                toWrite.Add(result.Top[0].Plan);
            }

            foreach (PlanSpec plan in toWrite)
            {
                File.WriteAllText(Path.Combine(PlansFolder, plan.Name + ".json"), JsonUtility.ToJson(plan, true));
            }
        }

        /// <summary>The part of a plan file name that marks a strategy (played adaptively) rather than a fixed plan.</summary>
        public const string StrategyFileMark = "_strategy";

        /// <summary>True when the level sells at least one drone: the Psycho drone rule applies there only (C1_01 introduces none).</summary>
        private static bool SellsDrones(Loaded loaded, string levelName)
        {
            foreach (MapNode map in loaded.Catalog.Nodes)
            {
                if (map.Level != null && map.Level.name == levelName)
                {
                    return map.Level.Drones != null && Array.Exists(map.Level.Drones, d => d != null);
                }
            }

            return true;
        }

        /// <summary>The searched plans of a cell written by the last search, in file order.</summary>
        public static List<PlanSpec> LoadPlans(string levelName, int difficulty)
        {
            var plans = new List<PlanSpec>();
            if (!Directory.Exists(PlansFolder))
            {
                return plans;
            }

            var files = new List<string>(Directory.GetFiles(PlansFolder, $"{levelName}_{difficulty}_*.json"));
            files.Sort(StringComparer.Ordinal);
            foreach (string file in files)
            {
                // A strategy file (round 22e) parses as a plan with no steps and would hide the moves its runs really took from the
                // feature checks (round 22f: every Hard and Psycho cell read "uses 0 of the game's features" for that reason).
                if (Path.GetFileName(file).Contains(StrategyFileMark))
                {
                    continue;
                }

                PlanSpec plan = JsonUtility.FromJson<PlanSpec>(File.ReadAllText(file));
                if (plan != null && plan.Steps != null)
                {
                    plans.Add(plan);
                }
            }

            return plans;
        }

        // ---- Stage: solve ---------------------------------------------------------------------------------------------------

        /// <summary>Re-solves the difficulty profiles and every level's waves against the calibrated model (the generator's own steps).</summary>
        public static string SolveWaves()
        {
            Loaded loaded = Load(true);
            string warning = loaded.Stale ? "WARNING: the calibration is stale (" + loaded.CalibrationNote + "); the waves are solved against it anyway. " : string.Empty;
            ProjectGenerator.SolveWavesOnly();
            return warning + "Waves solved for every campaign level; " + loaded.CalibrationNote;
        }

        // ---- Stage: acceptance ----------------------------------------------------------------------------------------------

        public sealed class AcceptanceReport
        {
            public string Text = string.Empty;
            public readonly List<string> FailingCells = new List<string>();
            public int Cells;
        }

        /// <summary>Judges every cell with recorded obvious runs and replayed plans by the acceptance rules; writes acceptance.txt.</summary>
        public static AcceptanceReport Acceptance()
        {
            Loaded loaded = Load(true);
            var report = new AcceptanceReport();
            var obvious = File.Exists(RecordsPath) ? CalibrationFit.Read(File.ReadAllLines(RecordsPath)) : new List<RunRecord>();
            var plans = File.Exists(PlanRecordsPath) ? CalibrationFit.Read(File.ReadAllLines(PlanRecordsPath)) : new List<RunRecord>();
            var cells = new SortedSet<string>(StringComparer.Ordinal);
            foreach (RunRecord run in obvious)
            {
                cells.Add(run.Cell);
            }

            foreach (RunRecord run in plans)
            {
                cells.Add(run.Cell);
            }

            var text = new StringBuilder();
            text.Append(Invariant($"Acceptance per difficulty {DateTime.Now:yyyy-MM-dd HH:mm} (Docs/BALANCE.md \"Acceptance per difficulty\"). {loaded.CalibrationNote}\n"));
            text.Append("Obvious family from calibration_records.txt (the yardstick), searched plans from plan_records.txt (the replay). Tutorial cells: ")
                .Append(string.Join(", ", AcceptanceRules.TutorialCells)).Append(" (their obvious wins are forgiven and noted).\n\n");
            foreach (string cell in cells)
            {
                string[] parts = cell.Split(' ');
                int difficulty = int.Parse(parts[1], CultureInfo.InvariantCulture);
                if (parts.Length > 2)
                {
                    // The fresh-save sanity runs (round 22d): a new player can start; not judged by the rule.
                    List<RunRecord> fresh = obvious.FindAll(r => r.Cell == cell);
                    int freshWins = fresh.FindAll(r => r.Won).Count;
                    string freshRuns = string.Join(", ", fresh.ConvertAll(r => Invariant($"{r.Strategy} seed {r.Seed} {(r.Won ? "won" : "lost")}")));
                    text.Append(Invariant($"{parts[0]} {DifficultyName(difficulty)} on a fresh save: {freshWins}/{fresh.Count} runs won ({freshRuns}).\n"));
                    continue;
                }

                var specs = new Dictionary<string, PlanSpec>();
                foreach (PlanSpec plan in LoadPlans(parts[0], difficulty))
                {
                    specs[plan.Name] = plan;
                }

                List<RunRecord> mediumObvious = obvious.FindAll(r => r.Level == parts[0] && r.Difficulty == AcceptanceRules.Medium && !r.FreshPlayer);
                AcceptanceRules.Verdict verdict = AcceptanceRules.Judge(parts[0], difficulty, obvious.FindAll(r => r.Cell == cell), plans.FindAll(r => r.Cell == cell), specs, mediumObvious, SellsDrones(loaded, parts[0]));
                report.Cells++;
                text.Append(Invariant($"{parts[0]} {DifficultyName(difficulty)}: {(verdict.Passed ? "PASS" : "FAIL")}{(verdict.Tutorial ? " (tutorial cell)" : string.Empty)}\n"));
                foreach (string note in verdict.Notes)
                {
                    text.Append("  note: ").Append(note).Append('\n');
                }

                foreach (string failure in verdict.Failures)
                {
                    text.Append("  fail: ").Append(failure).Append('\n');
                }

                if (!verdict.Passed)
                {
                    report.FailingCells.Add(cell);
                }
            }

            text.Append('\n').Append(InPlayRamp(loaded, obvious.FindAll(r => !r.FreshPlayer), plans.FindAll(r => !r.FreshPlayer), out _));
            File.WriteAllText(AcceptancePath, text.ToString());
            report.Text = text.ToString();
            return report;
        }


        // ---- Stage: build (the builder bot) -----------------------------------------------------------------------------------

        // The builder's one lever and its step: a failing cell's target pressure moves by this factor per iteration, at most twice.
        private const float BuilderStep = 1.15f;
        public const int BuilderIterations = 2;
        public const string BuilderReportPath = "Tools/PlayTest/builder.txt";

        /// <summary>
        /// The builder bot: solves the waves against the frozen calibration, judges every cell on paper with the plan search (the
        /// obvious family must fall from Medium on, a searched plan must hold), and for a failing cell moves only its target scale
        /// (<see cref="WaveTuning"/>), then solves and judges again, at most <see cref="BuilderIterations"/> times. What still fails
        /// is written as a design question for Remco, not as a new rule. The PlayMode replay stage confirms the final table.
        /// </summary>
        public static string Build(int iterations = BuilderIterations, int samples = 1500)
        {
            var text = new StringBuilder();
            Loaded loaded = Load(true);
            text.Append(Invariant($"Builder {DateTime.Now:yyyy-MM-dd HH:mm}: at most {iterations} iterations, step {BuilderStep}. {loaded.CalibrationNote}\n"));
            if (loaded.Stale)
            {
                text.Append("STOP: the calibration is stale; measure, fit and apply first. Nothing was changed.\n");
                File.WriteAllText(BuilderReportPath, text.ToString());
                return text.ToString();
            }

            Dictionary<(string Key, int Difficulty), float> scales = WaveTuning.Read();
            var questions = new List<string>();
            for (int iteration = 0; iteration <= iterations; iteration++)
            {
                SolveWaves();
                loaded = Load(true);
                text.Append(Invariant($"\nIteration {iteration}: waves solved, judging every cell on paper.\n"));
                bool changed = false;
                questions.Clear();
                int index = 0;
                foreach (MapNode map in loaded.Catalog.Nodes)
                {
                    if (map.Level == null)
                    {
                        continue;
                    }

                    string key = map.Level.name.StartsWith("Level_", StringComparison.Ordinal) ? map.Level.name.Substring(6) : map.Level.name;
                    for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                    {
                        PlanSearchResult result = PlanSearch.Run(new PlanSearchInput
                        {
                            LevelKey = map.Level.name,
                            Difficulty = d,
                            Level = loaded.Model.InputFor(index, d),
                            Waves = Waves(loaded.Model, map.Level, loaded.Catalog.Difficulties[d]),
                            Stats = loaded.Model.Difficulties[d],
                            Judge = loaded.Model.Rules.HumanWithHero(),
                            Cap = loaded.Model.Rules.HeroCapFor(d) * (1f + loaded.Model.Rules.BeatableTolerance),
                            AllowSell = d == AcceptanceRules.Psycho,
                            Samples = samples,
                        });
                        bool tutorial = AcceptanceRules.TutorialCells.Contains(map.Level.name + " " + d.ToString(CultureInfo.InvariantCulture));
                        bool obviousHolds = d >= AcceptanceRules.Medium && !tutorial && result.Obvious.Exists(o => o.Holds);
                        bool nobodyHolds = result.DistinctWinners.Count == 0;
                        float scale = scales.TryGetValue((key, d), out float s) ? s : 1f;
                        string state = obviousHolds ? "too easy (an obvious plan holds)" : nobodyHolds ? "too hard (no searched plan holds)" : "ok";
                        text.Append(Invariant($"  {map.Level.name} {DifficultyName(d),-7} scale {scale:0.###}: {state}; distinct winners {result.DistinctWinners.Count}, obvious {string.Join(", ", result.Obvious.ConvertAll(o => o.Plan.Name + " " + Outcome(o)))}\n"));
                        if (iteration < iterations && (obviousHolds || nobodyHolds) && !(obviousHolds && nobodyHolds))
                        {
                            scales[(key, d)] = (float)Math.Round(obviousHolds ? scale * BuilderStep : scale / BuilderStep, 3);
                            changed = true;
                        }
                        else if (obviousHolds || nobodyHolds)
                        {
                            questions.Add(Invariant($"{map.Level.name} {DifficultyName(d)}: {state} at scale {scale:0.###} after {iterations} iterations; a script-level lever (roster, packs, raids, bosses) or a design choice is needed."));
                        }
                    }

                    index++;
                }

                if (!changed)
                {
                    break;
                }

                WaveTuning.Write(scales);
                text.Append("  tuning written to ").Append(WaveTuning.Path).Append('\n');
            }

            text.Append("\nDesign questions for Remco (cells the builder could not settle with its lever):\n");
            text.Append(questions.Count == 0 ? "  none\n" : "  " + string.Join("\n  ", questions) + "\n");
            File.WriteAllText(BuilderReportPath, text.ToString());
            return text.ToString();
        }

        [MenuItem("Tools/Generate/Balance/6 Build (solve, judge, tune at most twice)")]
        private static void MenuBuild() => Debug.Log("[Balance] " + Build());

        // ---- Helpers ----------------------------------------------------------------------------------------------------------

        public static List<WaveSpec> Waves(CampaignModel model, LevelDefinition level, DifficultyProfile difficulty)
        {
            var waves = new List<WaveSpec>();
            foreach (WaveDefinition wave in level.GetWaves(difficulty))
            {
                waves.Add(BalanceLedger.Spec(wave, model.Enemies));
            }

            return waves;
        }

        public static string DifficultyName(int difficulty)
        {
            string[] names = { "Easy", "Medium", "Hard", "Psycho" };
            return difficulty >= 0 && difficulty < names.Length ? names[difficulty] : difficulty.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The optional filter file narrows a stage: "levels=0,1" (node indices) and "difficulties=1,2" lines; no file means everything.</summary>
        public static bool Wanted(string key, int value)
        {
            if (!File.Exists(FilterPath))
            {
                return true;
            }

            foreach (string line in File.ReadAllLines(FilterPath))
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    foreach (string item in parts[1].Split(','))
                    {
                        if (int.TryParse(item.Trim(), out int wanted) && wanted == value)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        private static int LevelIndex(CampaignCatalog catalog, string level)
        {
            int index = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                if (node.Level.name == level)
                {
                    return index;
                }

                index++;
            }

            return 0;
        }

        private static float Min(IEnumerable<float> values)
        {
            float min = float.MaxValue;
            foreach (float value in values)
            {
                min = Math.Min(min, value);
            }

            return min;
        }

        private static float Max(IEnumerable<float> values)
        {
            float max = float.MinValue;
            foreach (float value in values)
            {
                max = Math.Max(max, value);
            }

            return max;
        }

        private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

        // ---- Menu --------------------------------------------------------------------------------------------------------------

        [MenuItem("Tools/Generate/Balance/2 Fit calibration (from records)")]
        private static void MenuFit()
        {
            FitReport report = FitCalibration();
            Debug.Log(report == null ? "[Balance] no records: run the PlayMode measure stage first" : "[Balance] " + report.Verdict + "; see " + CalibrationTablePath);
        }

        [MenuItem("Tools/Generate/Balance/3 Apply calibration")]
        private static void MenuApply() => Debug.Log("[Balance] " + ApplyCalibration());

        [MenuItem("Tools/Generate/Balance/4 Solve waves")]
        private static void MenuSolve() => Debug.Log("[Balance] " + SolveWaves());

        [MenuItem("Tools/Generate/Balance/5 Plan search (stage A)")]
        private static void MenuSearch()
        {
            SearchReport report = SearchPlans();
            Debug.Log($"[Balance] plan search over {report.Cells} cells{(report.Stale ? " with a STALE calibration" : string.Empty)}; see {PlansSummaryPath}");
        }

        [MenuItem("Tools/Generate/Balance/7 Acceptance report")]
        private static void MenuAcceptance()
        {
            AcceptanceReport report = Acceptance();
            Debug.Log($"[Balance] acceptance: {report.Cells - report.FailingCells.Count}/{report.Cells} cells pass; see {AcceptancePath}");
        }
    }
}
