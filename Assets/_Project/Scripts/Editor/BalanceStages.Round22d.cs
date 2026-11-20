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
    /// Round 22d stages: the README of the plans that won in the replay, and the repair drone's price by the investment rule.
    /// (The builder on stage-B verdicts, which moved a scale per cell, went in round 87: a level has one scalar, set by the closed loop.)
    /// </summary>
    public static partial class BalanceStages
    {
        public const string DronePricePath = "Tools/PlayTest/drone_price.txt";
        private const string RepairDronePath = "Assets/_Project/Data/Drones/RepairDrone.asset";

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

                    Dictionary<string, StrategySpec> strategies = LoadStrategies(map.Level.name, d);

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
                    text.Append(ReadmeFromReplay(map.Level, d, runs, specs, strategies, PlanSearch.BestSpots(input), input.Level, out bool won));
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

        private static string ReadmeFromReplay(LevelDefinition level, int difficulty, List<RunRecord> runs, Dictionary<string, PlanSpec> specs, Dictionary<string, StrategySpec> strategies, Dictionary<string, List<(Cell Cell, SpotCover Cover)>> bestSpots, LevelInput input, out bool won)
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

                string planName = RunRecord.PlanNameOf(strategy);
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

                    PlanSpec played = AcceptanceRules.SpecOf(planRuns, specs);
                    text.Append(Invariant($"**{planName}** (won {wins}/{planRuns.Count} seeds, {stars / wins:0.0} stars, base {baseLeft / wins:0} left{(played != null && UsesRepairDrone(played, input) ? ", repair drone" : string.Empty)})\n\n"));
                }

                text.Append("```\n");
                if (strategies.TryGetValue(planName, out StrategySpec strategySpec))
                {
                    // A strategy: its words, then the build order the best seed really took (round 22e).
                    var groundIds = new List<string>();
                    foreach (TowerStats tower in input.Towers)
                    {
                        if (tower.IsGun && Layer.Has(tower.Targets, Layer.Ground))
                        {
                            groundIds.Add(tower.Id);
                        }
                    }

                    text.Append(strategySpec.Describe(groundIds, Name));
                    RunRecord bestSeed = planRuns[0];
                    foreach (RunRecord run in planRuns)
                    {
                        bestSeed = run.RealisedScore > bestSeed.RealisedScore ? run : bestSeed;
                    }

                    text.Append(Invariant($"  The build order seed {bestSeed.Seed} took ({(bestSeed.Won ? bestSeed.Stars + " stars, base " + bestSeed.BaseHealth.ToString("0", CultureInfo.InvariantCulture) : "lost at wave " + bestSeed.WaveReached)}):\n"));
                    text.Append(bestSeed.Moves.Count > 0 ? bestSeed.MovesAsPlan().Describe(Name) : "  (no moves recorded)\n");
                }
                else
                {
                    text.Append(specs.TryGetValue(planName, out PlanSpec plan) ? plan.Describe(Name) : "  (plan file missing: the search has rewritten the plans since this replay)\n");
                }
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

            int rulePrice = BalanceSheet.RepairDronePrice(drone.RepairPerSecond, busy, costPerHealth, waveSeconds, rules);
            price = BalanceSheet.ChosenRepairDronePrice(rulePrice, rules);
            text.Append(Invariant($"Repair drone price by the investment rule {DateTime.Now:yyyy-MM-dd HH:mm} (Remco 2026-11-07). {loaded.CalibrationNote}\n"));
            text.Append(Invariant($"Inputs: {drone.RepairPerSecond:0.#} hp/s x busy share {busy:0.000} (measured) x {costPerHealth:0.00} coins/hp x mean wave spacing {waveSeconds:0.0} s = {BalanceSheet.RepairDroneSavingPerWave(drone.RepairPerSecond, busy, costPerHealth, waveSeconds):0.0} coins of repairs saved per wave;\n"));
            text.Append(Invariant($"the rule's price = {rules.RepairDroneBreakEvenWaves:0.#} waves of that, rounded: {rulePrice}.\n"));
            text.Append(price == rulePrice
                ? Invariant($"The asset carries the rule's price: {price} (asset before this run: {drone.Cost}).\n\n")
                : Invariant($"The asset carries the designer's price (BalanceRules.RepairDronePrice, Remco 2026-11-09): {price} (asset before this run: {drone.Cost}); the rule's {rulePrice} is advice.\n\n"));
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
