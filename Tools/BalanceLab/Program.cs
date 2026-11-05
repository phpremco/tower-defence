using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Balance;
using Game.Editor;

namespace BalanceLab
{
    /// <summary>
    /// Runs the pure balance model outside Unity on the generated assets.
    ///   dotnet run --project Tools/BalanceLab -- report   ledger of the waves as they are on disk
    ///   dotnet run --project Tools/BalanceLab -- solve    solve the budgets from the wave scripts and report
    ///   dotnet run --project Tools/BalanceLab -- write    solve, then rewrite the generated balance numbers in the assets
    /// Reports go to Tools/PlayTest/balance_*.txt. See Docs/BALANCE.md.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            string root = FindRoot();
            string command = args.Length > 0 ? args[0] : "report";
            var data = new GameData(root);
            bool solve = command != "report";
            CampaignModel model = Build(data);
            if (solve)
            {
                ApplySheet(model);
            }

            if (command == "roundtrip")
            {
                // Writes the waves read from disk straight back: the diff must be empty, which proves the writer.
                var current = new Dictionary<int, List<List<WaveSpec>>>();
                for (int l = 0; l < data.Levels.Count; l++)
                {
                    current[l] = data.Levels[l].Waves;
                }

                new AssetWriter(data, model).WriteWavesOnly(current);
                return 0;
            }

            if (command == "dump" && args.Length >= 4)
            {
                Dump(model, args[1], int.Parse(args[2]), int.Parse(args[3]));
                return 0;
            }

            var report = new StringBuilder();
            Meta(model, report);
            Towers(model, report);
            var failures = new List<string>();
            var solved = new Dictionary<int, List<List<WaveSpec>>>();
            var budgetsByLevel = new Dictionary<int, float[]>();
            var rampLevels = new Dictionary<int, List<string>>();
            var rampSurplus = new Dictionary<int, List<float[]>>();
            for (int l = 0; l < model.Levels.Count; l++)
            {
                List<List<WaveSpec>> sets = data.Levels[l].Waves;
                if (solve)
                {
                    WaveScriptSpec script = WaveScripts.ByKey(model.Levels[l].Key);
                    sets = model.BuildWaves(l, script, out float[] budgets);
                    solved[l] = sets;
                    budgetsByLevel[l] = budgets;
                    report.Append($"{model.Levels[l].Key} health: {string.Join(" ", budgets.Select(b => b.ToString("0.#")))}\n");
                }

                var plainMeans = new List<float>();
                BalanceRules human = model.Rules.HumanTowersOnly();
                BalanceRules withHero = model.Rules.HumanWithHero();
                for (int d = 0; d < model.Difficulties.Count && d < sets.Count; d++)
                {
                    LevelInput input = model.InputFor(l, d);
                    List<WaveLine> lines = Ledger.Evaluate(input, sets[d], model.Difficulties[d], model.Rules);
                    report.Append($"{model.Levels[l].Key} {model.Difficulties[d].Id}  towers [{string.Join(",", input.Towers.Select(t => t.Id))}] upgrade x{MeanBoost(input):0.00}\n");
                    foreach (WaveLine line in lines)
                    {
                        report.Append("  ").Append(line).Append('\n');
                    }

                    failures.AddRange(BalanceChecks.Check(model.Levels[l].Key, model.Difficulties[d].Id, d, lines, model.Rules));
                    plainMeans.Add(BalanceChecks.PlainMean(Ledger.Evaluate(input, sets[d], model.Difficulties[d], human)));
                    failures.AddRange(BalanceChecks.CheckBeatable(model.Levels[l].Key, model.Difficulties[d].Id, d, Ledger.Evaluate(input, sets[d], model.Difficulties[d], withHero), model.Rules));
                }

                report.Append(BalanceChecks.DescribeHuman(model.Levels[l].Key, plainMeans)).Append('\n');
                failures.AddRange(BalanceChecks.CheckHuman(model.Levels[l].Key, plainMeans, model.Rules));
                failures.AddRange(BalanceChecks.CheckHumanTarget(model.Levels[l].Key, model.ContinentIndexOf(l), plainMeans, model.Rules));
                int continent = model.Levels[l].Continent;
                if (!rampLevels.ContainsKey(continent))
                {
                    rampLevels[continent] = new List<string>();
                    rampSurplus[continent] = new List<float[]>();
                }

                rampLevels[continent].Add(model.Levels[l].Key);
                rampSurplus[continent].Add(plainMeans.Select(m => m > 0f ? 1f / m : 0f).ToArray());
            }

            report.Append("\nChecks: ").Append(failures.Count == 0 ? "all inside their bands" : failures.Count + " outside").Append('\n');
            foreach (string failure in failures)
            {
                report.Append("  ").Append(failure).Append('\n');
            }

            string output = Path.Combine(root, "Tools/PlayTest", solve ? "balance_solved.txt" : "balance_ledger.txt");
            File.WriteAllText(output, report.ToString());
            Console.WriteLine(report.ToString());
            if (command == "write")
            {
                new AssetWriter(data, model).WriteAll(solved);
            }

            return 0;
        }

        // The spawn groups of one solved wave: dump <level key> <difficulty index> <wave number>.
        private static void Dump(CampaignModel model, string key, int difficulty, int number)
        {
            int l = model.IndexOf(key);
            List<List<WaveSpec>> sets = model.BuildWaves(l, WaveScripts.ByKey(key), out float[] healths);
            WaveSpec wave = sets[difficulty][number - 1];
            Console.WriteLine($"{key} d{difficulty} w{number} health x{wave.HealthMultiplier:0.000}");
            foreach (GroupSpec g in wave.Groups)
            {
                Console.WriteLine($"  {g.Enemy.Id,-14} x{g.Count,-3} path {g.Path} source {g.Source} every {g.Interval:0.00}s delay {g.Delay:0.0} stagger {g.Stagger:0.0}");
            }
        }

        private static float MeanBoost(LevelInput input)
        {
            if (input.Towers.Count == 0)
            {
                return 1f;
            }

            float sum = 0f;
            foreach (TowerStats tower in input.Towers)
            {
                TowerBoost boost = input.BoostOf(tower.Id);
                sum += boost.Damage * boost.Rate;
            }

            return sum / input.Towers.Count;
        }

        public static CampaignModel Build(GameData data)
        {
            var model = new CampaignModel { Hero = data.Hero, HarvesterCost = data.HarvesterCost };
            foreach (var pair in data.Towers)
            {
                model.Towers[pair.Key] = pair.Value;
            }

            foreach (var pair in data.Enemies)
            {
                model.Enemies[pair.Key] = pair.Value;
            }

            model.Upgrades.AddRange(data.Upgrades);
            model.Difficulties.AddRange(data.Difficulties);
            model.StartingTowers.AddRange(data.StartingTowers);
            model.Rules.ResearchMultipliers = data.Difficulties.Select(d => d.Research).ToArray();
            foreach (LevelData level in data.Levels)
            {
                model.Levels.Add(new CampaignLevel { Key = level.Key, TowerIds = level.TowerIds, RewardTowerId = level.RewardTowerId, RewardHeroPerkId = level.RewardHeroPerkId, Research = level.Research, Power = level.Power });
                model.Facts.Add(new LevelFacts
                {
                    Key = level.Key,
                    StartingMoney = level.StartingMoney,
                    HealthScale = level.HealthScale,
                    Shape = level.Shape,
                    MineCount = level.MineCount,
                    MineStock = level.MineStock,
                    Harvest = level.Harvest,
                    SecondRoad = level.SecondRoad,
                    GateHealth = data.TowerHealth.TryGetValue(level.GateTowerId, out float health) ? health : 0f,
                });
            }

            return model;
        }

        /// <summary>The new numbers of the balance sheet on top of what the assets hold: what "solve" and "write" judge.</summary>
        public static void ApplySheet(CampaignModel model)
        {
            model.Rules = BalanceSheet.Rules;
            BalanceSheet.Difficulty[] sheet = BalanceSheet.Difficulties;
            for (int d = 0; d < model.Difficulties.Count && d < sheet.Length; d++)
            {
                DifficultyStats stats = model.Difficulties[d];
                stats.Health = sheet[d].Health;
                stats.Count = sheet[d].Count;
                stats.Interval = sheet[d].Interval;
                stats.Research = sheet[d].Research;
                stats.Power = sheet[d].Power;
                stats.StructureDamage = sheet[d].StructureDamage;
                stats.Income = sheet[d].Income;
                stats.StartMoney = sheet[d].StartMoney;
                stats.BossHealth = sheet[d].BossHealth;
            }

            foreach (EnemyStats enemy in model.Enemies.Values)
            {
                enemy.Reward = Threat.Reward(enemy, model.Rules);
            }

            foreach (TowerStats tower in model.Towers.Values)
            {
                int[] prices = BalanceSheet.Prices(tower.Id);
                for (int i = 0; prices != null && i < prices.Length && i < tower.Levels.Length; i++)
                {
                    tower.Levels[i].Cost = prices[i];
                }
            }

            foreach (CampaignLevel level in model.Levels)
            {
                // Payouts follow the continent curve by the level's index in its continent (Docs/BALANCE.md).
                LevelSpecs.TryLocate(level.Key, out int continent, out int index);
                level.Continent = Math.Max(0, continent);
                level.Research = BalanceSheet.Research(continent, index);
                level.Power = BalanceSheet.Power(continent, WaveScripts.ByKey(level.Key));
            }
        }

        private static void Meta(CampaignModel model, StringBuilder report)
        {
            model.MetaTotals(0, out int researchCost, out int powerCost, out int[] research, out int[] power);
            report.Append($"Research upgrades cost {researchCost}; paid per difficulty: {string.Join(" / ", research)}");
            report.Append($" (Easy {100f * research[0] / Math.Max(1, researchCost):0}%, Psycho {100f * research[research.Length - 1] / Math.Max(1, researchCost):0}%)\n");
            report.Append($"Power upgrades cost {powerCost}; paid per difficulty: {string.Join(" / ", power)}\n");
            for (int l = 0; l < model.Levels.Count; l++)
            {
                var parts = new List<string>();
                for (int d = 0; d < model.Difficulties.Count; d++)
                {
                    PlayerState state = model.PlayerAt(l, d);
                    parts.Add($"{model.Difficulties[d].Id} {state.ResearchEarned}rp x{1f + state.MeanTowerBonus:0.00}");
                }

                LevelShape shape = model.Facts[l].Shape;
                report.Append($"  {model.Levels[l].Key,-8} road {shape.MainLength,4:0} fronts {shape.GroundFronts(model.Rules.TrunkReference)} gates {shape.Gates.Count} trunk {string.Join("/", shape.Lanes.Select(x => x.TrunkShare.ToString("0.00")))} | {string.Join(" | ", parts)}\n");
            }

            report.Append('\n');
        }

        // Damage per passing creature per coin, per tower level (cumulative cost): the value curve of the roster.
        private static void Towers(CampaignModel model, StringBuilder report)
        {
            report.Append("Tower value (dps x range per coin, cumulative; speed 1, no splash):\n");
            foreach (TowerStats tower in model.Towers.Values.Where(t => t.IsGun).OrderBy(t => t.Id))
            {
                int cost = 0;
                var parts = new List<string>();
                foreach (TowerLevelStats level in tower.Levels)
                {
                    cost += level.Cost;
                    parts.Add($"L{parts.Count + 1} {level.Dps * level.Range / Math.Max(1, cost) * 100f:0.0}");
                }

                report.Append($"  {tower.Id,-11} splash {tower.Splash:0.0} targets {tower.Targets}  {string.Join("  ", parts)}\n");
            }

            report.Append('\n');
        }

        private static string FindRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (dir != null && !Directory.Exists(Path.Combine(dir, "Assets/_Project")))
            {
                dir = Path.GetDirectoryName(dir);
            }

            return dir ?? Directory.GetCurrentDirectory();
        }
    }
}
