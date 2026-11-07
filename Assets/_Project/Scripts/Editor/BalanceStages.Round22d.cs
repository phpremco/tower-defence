using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Drones;
using Game.Levels;
using Game.Towers;
using UnityEditor;
using UnityEngine;
using static System.FormattableString;

namespace Game.Editor
{
    /// <summary>
    /// Round 22d stages: the builder on stage-B verdicts (the lever moves on recorded runs, not on paper), the README of the plans
    /// that won in the replay, and the repair drone's price by the investment rule.
    /// </summary>
    public static partial class BalanceStages
    {
        public const string BuilderIterationsPath = "Tools/PlayTest/builder_iterations.txt";
        public const string DronePricePath = "Tools/PlayTest/drone_price.txt";
        private const string RepairDronePath = "Assets/_Project/Data/Drones/RepairDrone.asset";

        // ---- Stage: build from verdicts ----------------------------------------------------------------------------------------

        /// <summary>
        /// The builder on stage-B verdicts: reads the yardstick and the replay records, asks <see cref="AcceptanceRules.LeverFor"/> per
        /// cell which way its target scale should move, moves it one step (at most <see cref="BuilderIterations"/> times per cell over
        /// the rounds, counted in builder_iterations.txt), writes the tuning and solves the waves. Cells that pull both ways or have used
        /// their moves are written as design questions. The search and replay stages then judge the new waves.
        /// </summary>
        public static string BuildFromVerdicts()
        {
            var text = new StringBuilder();
            Loaded loaded = Load(true);
            text.Append(Invariant($"Builder on stage-B verdicts {DateTime.Now:yyyy-MM-dd HH:mm}: step {BuilderStep}, at most {BuilderIterations} moves per cell. {loaded.CalibrationNote}\n"));
            if (loaded.Stale)
            {
                text.Append("STOP: the calibration is stale; measure, fit and apply first. Nothing was changed.\n");
                File.WriteAllText(BuilderReportPath, text.ToString());
                return text.ToString();
            }

            var obvious = File.Exists(RecordsPath) ? CalibrationFit.Read(File.ReadAllLines(RecordsPath)) : new List<RunRecord>();
            var plans = File.Exists(PlanRecordsPath) ? CalibrationFit.Read(File.ReadAllLines(PlanRecordsPath)) : new List<RunRecord>();
            obvious.RemoveAll(r => r.FreshPlayer);
            plans.RemoveAll(r => r.FreshPlayer);
            Dictionary<(string Key, int Difficulty), float> scales = WaveTuning.Read();
            Dictionary<string, int> moves = ReadIterations();
            var questions = new List<string>();
            bool changed = false;
            foreach (MapNode map in loaded.Catalog.Nodes)
            {
                if (map.Level == null)
                {
                    continue;
                }

                string key = map.Level.name.StartsWith("Level_", StringComparison.Ordinal) ? map.Level.name.Substring(6) : map.Level.name;
                List<RunRecord> medium = obvious.FindAll(r => r.Level == map.Level.name && r.Difficulty == AcceptanceRules.Medium);
                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    string cell = map.Level.name + " " + d.ToString(CultureInfo.InvariantCulture);
                    List<RunRecord> cellObvious = obvious.FindAll(r => r.Cell == cell);
                    List<RunRecord> cellPlans = plans.FindAll(r => r.Cell == cell);
                    if (cellObvious.Count == 0 && cellPlans.Count == 0)
                    {
                        text.Append(Invariant($"  {map.Level.name} {DifficultyName(d),-7}: no recorded runs\n"));
                        continue;
                    }

                    AcceptanceRules.Lever lever = AcceptanceRules.LeverFor(map.Level.name, d, cellObvious, cellPlans, medium, out string reason);
                    float scale = scales.TryGetValue((key, d), out float s) ? s : 1f;
                    int used = moves.TryGetValue(cell, out int m) ? m : 0;
                    string state = lever == AcceptanceRules.Lever.Harder ? "too easy" : lever == AcceptanceRules.Lever.Easier ? "too hard" : string.IsNullOrEmpty(reason) ? "ok" : "pulls both ways";
                    text.Append(Invariant($"  {map.Level.name} {DifficultyName(d),-7} scale {scale:0.###}, moves used {used}/{BuilderIterations}: {state}{(reason.Length > 0 ? " (" + reason + ")" : string.Empty)}\n"));
                    if (lever == AcceptanceRules.Lever.None)
                    {
                        if (reason.Length > 0)
                        {
                            questions.Add(Invariant($"{map.Level.name} {DifficultyName(d)}: pulls both ways ({reason}); a script-level lever or a design choice is needed."));
                        }

                        continue;
                    }

                    if (used >= BuilderIterations)
                    {
                        questions.Add(Invariant($"{map.Level.name} {DifficultyName(d)}: still {state} at scale {scale:0.###} after {used} moves ({reason}); a script-level lever (roster, packs, raids, bosses) or a design choice is needed."));
                        continue;
                    }

                    scales[(key, d)] = (float)Math.Round(lever == AcceptanceRules.Lever.Harder ? scale * BuilderStep : scale / BuilderStep, 3);
                    moves[cell] = used + 1;
                    changed = true;
                    text.Append(Invariant($"    -> scale {scales[(key, d)]:0.###}\n"));
                }
            }

            if (changed)
            {
                WaveTuning.Write(scales);
                WriteIterations(moves);
                text.Append("  tuning written to ").Append(WaveTuning.Path).Append("; solving the waves.\n");
                SolveWaves();
            }
            else
            {
                text.Append("  nothing moved; the waves stay as they are.\n");
            }

            text.Append("\nDesign questions for Remco (cells the builder could not settle with its lever):\n");
            text.Append(questions.Count == 0 ? "  none\n" : "  " + string.Join("\n  ", questions) + "\n");
            File.WriteAllText(BuilderReportPath, text.ToString());
            return text.ToString();
        }

        [MenuItem("Tools/Generate/Balance/6b Build from stage-B verdicts (lever once, solve)")]
        private static void MenuBuildFromVerdicts() => Debug.Log("[Balance] " + BuildFromVerdicts());

        private static Dictionary<string, int> ReadIterations()
        {
            var moves = new Dictionary<string, int>();
            if (!File.Exists(BuilderIterationsPath))
            {
                return moves;
            }

            foreach (string line in File.ReadAllLines(BuilderIterationsPath))
            {
                string[] parts = line.Split('|');
                if (parts.Length == 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
                {
                    moves[parts[0]] = count;
                }
            }

            return moves;
        }

        private static void WriteIterations(Dictionary<string, int> moves)
        {
            var text = new StringBuilder();
            foreach (KeyValuePair<string, int> pair in moves)
            {
                text.Append(pair.Key).Append('|').Append(pair.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            File.WriteAllText(BuilderIterationsPath, text.ToString());
        }

        // ---- Stage: readme from the replay -------------------------------------------------------------------------------------

        /// <summary>
        /// Writes Tools/PlayTest/Plans/README.md from the plans that WON in stage B: per cell the one or two replayed plans with the
        /// most wins over the seeds (then the best score), in words, with the best spots per tower; a cell without a winner says so and
        /// shows how far the best replayed plan got. Run it before a new search, which rewrites the plan files.
        /// </summary>
        public static string WriteWinningPlans()
        {
            Loaded loaded = Load(true);
            var plans = File.Exists(PlanRecordsPath) ? CalibrationFit.Read(File.ReadAllLines(PlanRecordsPath)) : new List<RunRecord>();
            plans.RemoveAll(r => r.FreshPlayer);
            var text = new StringBuilder();
            text.Append(Invariant($"# Winning plans per level and difficulty\n\nWritten from the stage-B replay (`Tools/PlayTest/plan_records.txt`, `bash Tools/PlayTest/balance_pipeline.sh readme`) on {DateTime.Now:yyyy-MM-dd HH:mm}; {loaded.CalibrationNote}.\n\n"));
            text.Append("A plan counts as a winner only when it won in play on most seeds (the paper verdict is a pre-screen). Each plan is a priority list: a step is\n");
            text.Append("taken from its wave on, as soon as the money is there (an earlier step blocks the later ones). Cells are (x,y) on the level grid. The obvious\n");
            text.Append("family's results are in `Tools/PlayTest/yardstick.txt`. A cell without a winner shows how far the best replayed plan got: a design question.\n");
            BalanceRules rules = loaded.Model.Rules;
            int index = 0;
            int cells = 0;
            int winners = 0;
            foreach (MapNode map in loaded.Catalog.Nodes)
            {
                if (map.Level == null)
                {
                    continue;
                }

                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    string cell = map.Level.name + " " + d.ToString(CultureInfo.InvariantCulture);
                    List<RunRecord> runs = plans.FindAll(r => r.Cell == cell);
                    if (runs.Count == 0)
                    {
                        continue;
                    }

                    cells++;
                    var specs = new Dictionary<string, PlanSpec>();
                    foreach (PlanSpec plan in LoadPlans(map.Level.name, d))
                    {
                        specs[plan.Name] = plan;
                    }

                    var input = new PlanSearchInput
                    {
                        LevelKey = map.Level.name,
                        Difficulty = d,
                        Level = loaded.Model.InputFor(index, d),
                        Waves = Waves(loaded.Model, map.Level, loaded.Catalog.Difficulties[d]),
                        Stats = loaded.Model.Difficulties[d],
                        Judge = rules.HumanWithHero(),
                        Cap = rules.HeroCapFor(d) * (1f + rules.BeatableTolerance),
                    };
                    text.Append(ReadmeFromReplay(map.Level, d, runs, specs, PlanSearch.BestSpots(input), input.Level, out bool won));
                    winners += won ? 1 : 0;
                }

                index++;
            }

            Directory.CreateDirectory(PlansFolder);
            File.WriteAllText(Path.Combine(PlansFolder, "README.md"), text.ToString());
            return Invariant($"README written: {winners} of {cells} replayed cells have a plan that won in play.");
        }

        [MenuItem("Tools/Generate/Balance/8 README from the stage-B winners")]
        private static void MenuWriteWinningPlans() => Debug.Log("[Balance] " + WriteWinningPlans());

        private static string ReadmeFromReplay(LevelDefinition level, int difficulty, List<RunRecord> runs, Dictionary<string, PlanSpec> specs, Dictionary<string, List<(Cell Cell, SpotCover Cover)>> bestSpots, LevelInput input, out bool won)
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
            var spots = new List<string>();
            foreach (KeyValuePair<string, List<(Cell Cell, SpotCover Cover)>> pair in bestSpots)
            {
                var cellsText = new List<string>();
                for (int i = 0; i < pair.Value.Count && i < ReadmeSpots; i++)
                {
                    cellsText.Add(Invariant($"({pair.Value[i].Cell.X},{pair.Value[i].Cell.Y})"));
                }

                spots.Add(Name(pair.Key) + " " + string.Join(" ", cellsText));
            }

            text.Append("Best spots per tower: ").Append(string.Join("; ", spots)).Append(".\n\n");

            // Rank the replayed plans: wins over the seeds, then the mean realised score.
            var byPlan = new Dictionary<string, List<RunRecord>>();
            foreach (RunRecord run in runs)
            {
                if (!byPlan.TryGetValue(run.Strategy, out List<RunRecord> list))
                {
                    byPlan[run.Strategy] = list = new List<RunRecord>();
                }

                list.Add(run);
            }

            var ranked = new List<(string Strategy, List<RunRecord> Runs, int Wins, float Score)>();
            foreach (KeyValuePair<string, List<RunRecord>> pair in byPlan)
            {
                int wins = pair.Value.FindAll(r => r.Won).Count;
                float score = 0f;
                foreach (RunRecord run in pair.Value)
                {
                    score += run.RealisedScore;
                }

                ranked.Add((pair.Key, pair.Value, wins, score / pair.Value.Count));
            }

            ranked.Sort((a, b) => a.Wins != b.Wins ? b.Wins.CompareTo(a.Wins) : b.Score.CompareTo(a.Score));
            won = ranked.Count > 0 && ranked[0].Wins * 2 > ranked[0].Runs.Count;
            int shown = 0;
            foreach ((string strategy, List<RunRecord> planRuns, int wins, float score) in ranked)
            {
                bool winner = wins * 2 > planRuns.Count;
                if (!winner && shown > 0)
                {
                    break;
                }

                string planName = strategy.Replace("plan ", string.Empty);
                if (!winner)
                {
                    int reached = 0;
                    foreach (RunRecord run in planRuns)
                    {
                        reached = Math.Max(reached, run.WaveReached);
                    }

                    text.Append(Invariant($"No replayed plan won in play. The best, **{planName}**, won {wins}/{planRuns.Count} and reached wave {reached}/{planRuns[0].WaveCount}:\n\n"));
                }
                else
                {
                    float stars = 0f;
                    float baseLeft = 0f;
                    foreach (RunRecord run in planRuns)
                    {
                        stars += run.Won ? run.Stars : 0;
                        baseLeft += run.Won ? run.BaseHealth : 0f;
                    }

                    text.Append(Invariant($"**{planName}** (won {wins}/{planRuns.Count} seeds, {stars / wins:0.0} stars, base {baseLeft / wins:0} left{(specs.TryGetValue(planName, out PlanSpec spec) && UsesRepairDrone(spec, input) ? ", repair drone" : string.Empty)})\n\n"));
                }

                text.Append("```\n");
                text.Append(specs.TryGetValue(planName, out PlanSpec plan) ? plan.Describe(Name) : "  (plan file missing: the search has rewritten the plans since this replay)\n");
                text.Append("```\n\n");
                shown++;
                if (shown >= ReadmePlans)
                {
                    break;
                }
            }

            return text.ToString();
        }

        // ---- Stage: drone price ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Prices the repair drone by the investment rule (<see cref="BalanceSheet.RepairDronePrice"/>) from the calibration's busy
        /// share and the levels' repair cost and wave spacing; writes the asset and drone_price.txt (old and new price, break-even
        /// wave per level and difficulty). Remco 2026-11-07: bought early it must clearly pay, in the last two waves never.
        /// </summary>
        public static string ApplyDronePrices()
        {
            Loaded loaded = Load(true);
            var drone = AssetDatabase.LoadAssetAtPath<DroneDefinition>(RepairDronePath);
            if (loaded.Catalog == null || drone == null)
            {
                return "no catalog or repair drone asset: run the generator first";
            }

            string report = DronePriceReport(loaded, drone, out int price);
            if (price != drone.Cost)
            {
                var so = new SerializedObject(drone);
                so.FindProperty("_cost").intValue = price;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(drone);
                AssetDatabase.SaveAssets();
            }

            File.WriteAllText(DronePricePath, report);
            return Invariant($"repair drone price {price} (was {drone.Cost}); see {DronePricePath}");
        }

        [MenuItem("Tools/Generate/Balance/9 Repair drone price (investment rule)")]
        private static void MenuApplyDronePrices() => Debug.Log("[Balance] " + ApplyDronePrices());

        /// <summary>The rule's price and the report text: inputs, old and new price, break-even wave per level and difficulty.</summary>
        public static string DronePriceReport(Loaded loaded, DroneDefinition drone, out int price)
        {
            var text = new StringBuilder();
            BalanceRules rules = loaded.Model.Rules;
            if (!BalanceSheet.RepairDroneInputs(loaded.Catalog, loaded.Calibration, out float busy, out float costPerHealth, out float waveSeconds))
            {
                price = drone.Cost;
                return "no level sells a repair drone yet\n";
            }

            price = BalanceSheet.RepairDronePrice(drone.RepairPerSecond, busy, costPerHealth, waveSeconds, rules);
            text.Append(Invariant($"Repair drone price by the investment rule {DateTime.Now:yyyy-MM-dd HH:mm} (Remco 2026-11-07). {loaded.CalibrationNote}\n"));
            text.Append(Invariant($"Inputs: {drone.RepairPerSecond:0.#} hp/s x busy share {busy:0.000} (measured) x {costPerHealth:0.00} coins/hp x mean wave spacing {waveSeconds:0.0} s = {BalanceSheet.RepairDroneSavingPerWave(drone.RepairPerSecond, busy, costPerHealth, waveSeconds):0.0} coins of repairs saved per wave;\n"));
            text.Append(Invariant($"price = {rules.RepairDroneBreakEvenWaves:0.#} waves of that, rounded: {price} (asset before this run: {drone.Cost}).\n\n"));
            text.Append("Per level and difficulty: coins saved per wave in that level's own clock, the last wave a purchase still pays back before the end (0: never), and the payback in waves when bought at wave 1.\n");
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                if (node.Level == null || !Array.Exists(node.Level.Drones, d => d != null && d.Kind == DroneKind.Repair))
                {
                    continue;
                }

                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    DifficultyProfile profile = loaded.Catalog.Difficulties[d];
                    float seconds = WaveTiming.MeanWaveSeconds(node.Level, profile);
                    float saving = BalanceSheet.RepairDroneSavingPerWave(drone.RepairPerSecond, busy, costPerHealth, seconds);
                    int waves = (node.Level.GetWaves(profile) ?? new WaveDefinition[0]).Length;
                    int breakEven = BalanceSheet.RepairDroneBreakEvenWave(price, saving, waves);
                    text.Append(Invariant($"  {node.Level.name} {profile.name,-7} waves {waves,2}, {seconds:0.0} s each: saves {saving:0.0} coins/wave; break-even at wave {breakEven} of {waves}; payback {price / Math.Max(0.01f, saving):0.0} waves\n"));
                }
            }

            return text.ToString();
        }
    }
}
