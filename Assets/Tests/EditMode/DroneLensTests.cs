using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Drones;
using Game.Economy;
using Game.Editor;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static System.FormattableString;

namespace Game.Tests
{
    /// <summary>
    /// Drone lens (round 20, report only): per level and drone kind its price against three of the cheapest owned towers,
    /// what it earns or is worth per second, and its payback. The harvest drone is followed through the level's real wave
    /// timeline (WaveScheduler spans and start delays) and ore stock: net coins by the level's end when bought at wave 1,
    /// a third, half, two thirds in and in the last two waves, and the latest wave at which it still breaks even (Remco:
    /// early pays clearly, mid and late still pay, only the last two waves should not). The repair and attack drones are
    /// valued per busy second (repair at the towers' repair price, shots at the waves' bounty per hit point). Written to
    /// Tools/PlayTest/drone_lens.txt; the probe measures the same drones in play (Tools/PlayTest/monoculture.txt).
    /// </summary>
    public sealed class DroneLensTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private const string TablePath = "Tools/PlayTest/drone_lens.txt";

        /// <summary>Remco's price target for an income drone: about three early towers.</summary>
        private const int TowersPerDrone = 3;

        /// <summary>After the last spawn the creatures still walk the road; the level ends about this much later.</summary>

        private const int LastWaves = 2;

        /// <summary>
        /// Remco 2026-11-07: the repair drone is priced by the investment rule (BalanceSheet.RepairDronePrice) from the measured busy
        /// share, the repair cost per hit point and the levels' wave spacing; the asset must carry that price (Tools/Generate/Balance/9).
        /// Round 31: Remco chose about 100 (BalanceRules.RepairDronePrice), so the asset carries that number and the rule's figure is advice
        /// in the report; the test follows the choice.
        /// </summary>
        [Test]
        public void RepairDrone_PriceFollowsTheInvestmentRule()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            var drone = AssetDatabase.LoadAssetAtPath<DroneDefinition>("Assets/_Project/Data/Drones/RepairDrone.asset");
            Assume.That(loaded.Catalog, Is.Not.Null, "Run Tools/Generate/Scenes and Starter Data first.");
            Assume.That(drone, Is.Not.Null);
            string report = BalanceStages.DronePriceReport(loaded, drone, out int price);
            Debug.Log("[DroneLens] " + report);
            Assert.AreEqual(price, drone.Cost, "the repair drone's price is off the chosen number (or the rule when none is chosen): run Tools/Generate/Balance/9 (ApplyDronePrice.cs); " + report);
        }

        [Test]
        public void Drones_PaybackIsReported()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, BalanceRules.Default, AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath));

            var report = new StringBuilder();
            report.Append("Drone lens (report only). One drone of each kind, no permanent drone upgrades. Times are game seconds; a wave is the\n");
            report.Append("level's mean time from one wave start to the next. Harvest: one drone mining the nodes nearest the base first, full\n");
            report.Append("ore stock, income share of the difficulty. 'net @' = coins earned by the level's end minus the price, bought at the start\n");
            report.Append("of that wave; 'break-even until' = the latest wave a purchase still earns its price back.\n\n");
            int index = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                LevelDefinition level = node.Level;
                if (level == null)
                {
                    continue;
                }

                report.Append(level.name).Append(' ').Append(level.DisplayName).Append('\n');
                if (level.Drones.Length == 0)
                {
                    report.Append("  no drones offered\n");
                }

                foreach (DroneDefinition drone in level.Drones)
                {
                    for (int d = 0; d < catalog.Difficulties.Count; d++)
                    {
                        report.Append(Describe(model, index, level, catalog.Difficulties[d], d, drone));
                    }
                }

                report.Append('\n');
                index++;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(TablePath));
            File.WriteAllText(TablePath, report.ToString());
            Debug.Log("[DroneLens] written to " + TablePath);
        }

        private static string Describe(CampaignModel model, int index, LevelDefinition level, DifficultyProfile difficulty, int d, DroneDefinition drone)
        {
            LevelInput input = model.InputFor(index, d);
            int towerPrice = CheapestGun(input);
            List<float> starts = WaveTiming.Starts(level, difficulty, out float end);
            float waveSeconds = starts.Count > 1 ? (starts[starts.Count - 1] - starts[0]) / (starts.Count - 1) : end;
            string head = Invariant($"  {drone.name,-12} {difficulty.name,-7} price {drone.Cost,4} vs 3 x {towerPrice} = {TowersPerDrone * towerPrice,4} ({drone.Cost / (float)Mathf.Max(1, TowersPerDrone * towerPrice):0.00}x) | ");
            switch (drone.Kind)
            {
                case DroneKind.Harvest:
                    return head + Harvest(level, difficulty, drone, starts, end, waveSeconds) + "\n";
                case DroneKind.Repair:
                    float coins = drone.RepairPerSecond * RepairPrice(level);
                    return head + Invariant($"repairs {drone.RepairPerSecond:0.#} hp/s = {coins:0.00} coins/s of repairs while busy, pays back after {drone.Cost / Mathf.Max(0.01f, coins):0} busy s ({drone.Cost / Mathf.Max(0.01f, coins) / waveSeconds:0.0} waves busy)\n");
                default:
                    float dps = drone.FireInterval > 0f ? drone.Damage / drone.FireInterval : 0f;
                    float bounty = BountyPerHealth(model, index, level, difficulty, d);
                    float earn = dps * bounty;
                    return head + Invariant($"{dps:0.#} DPS ({dps * 100f / drone.Cost:0.0} per 100 coins; cheapest gun {GunDps(input) * 100f / Mathf.Max(1, towerPrice):0.0}), bounty {earn:0.00} coins/s while every shot lands, pays back after {drone.Cost / Mathf.Max(0.01f, earn):0} busy s ({drone.Cost / Mathf.Max(0.01f, earn) / waveSeconds:0.0} waves)\n");
            }
        }

        // The harvest timeline: income from a purchase at each wave start until the level's end.
        private static string Harvest(LevelDefinition level, DifficultyProfile difficulty, DroneDefinition drone, List<float> starts, float end, float waveSeconds)
        {
            if (level.MineCells.Length == 0)
            {
                return "no ore nodes";
            }

            var nets = new int[starts.Count];
            int lastEven = -1;
            for (int w = 0; w < starts.Count; w++)
            {
                nets[w] = Mined(level, difficulty, drone, end - starts[w]) - drone.Cost;
                lastEven = nets[w] >= 0 ? w : lastEven;
            }

            float rate = Mined(level, difficulty, drone, waveSeconds * 3f) / (waveSeconds * 3f);
            int stock = level.MineCells.Length * level.MineStock;
            var text = new StringBuilder();
            text.Append(Invariant($"{rate:0.00} coins/s, payback {drone.Cost / Mathf.Max(0.01f, rate):0} s ({drone.Cost / Mathf.Max(0.01f, rate) / waveSeconds:0.0} waves of {waveSeconds:0} s), stock {stock} x income {difficulty.IncomeMultiplier:0.##} | net @"));
            int count = starts.Count;
            foreach ((string name, int wave) in new[] { ("w1", 0), ("1/3", count / 3), ("1/2", count / 2), ("2/3", 2 * count / 3), ("last2", Mathf.Max(0, count - LastWaves)) })
            {
                text.Append(Invariant($" {name}(w{wave + 1}) {nets[wave],5}"));
            }

            text.Append(Invariant($" | break-even until wave {lastEven + 1} of {count}"));
            return text.ToString();
        }

        // Coins one drone brings home in the given seconds: nearest node to the base first, a trip there and back plus the dig.
        private static int Mined(LevelDefinition level, DifficultyProfile difficulty, DroneDefinition drone, float seconds)
        {
            Vector3 home = LevelVisualBuilder.BasePosition(level);
            var nodes = new List<(float Trip, int Stock)>();
            foreach (Vector2Int cell in level.MineCells)
            {
                float distance = Vector3.Distance(home, level.Grid.CellToWorld(cell));
                nodes.Add((2f * distance / Mathf.Max(0.01f, drone.Speed) + drone.HarvestSeconds, level.MineStock));
            }

            nodes.Sort((a, b) => a.Trip.CompareTo(b.Trip));
            float time = 0f;
            float earned = 0f;
            foreach ((float trip, int stock) in nodes)
            {
                int left = stock;
                while (left > 0 && time + trip <= seconds)
                {
                    int take = Mathf.Min(drone.HarvestValue, left);
                    left -= take;
                    time += trip;
                    earned += take * difficulty.IncomeMultiplier;
                }
            }

            return Mathf.FloorToInt(earned);
        }

        // When each wave starts: its start delay after the previous wave's last spawn (LevelController), and when it ends.
        private static int CheapestGun(LevelInput input)
        {
            int cheapest = 0;
            foreach (TowerStats tower in input.Towers)
            {
                if (tower.IsGun && (cheapest == 0 || tower.Levels[0].Cost < cheapest))
                {
                    cheapest = tower.Levels[0].Cost;
                }
            }

            return cheapest;
        }

        private static float GunDps(LevelInput input)
        {
            TowerStats cheapest = null;
            foreach (TowerStats tower in input.Towers)
            {
                if (tower.IsGun && (cheapest == null || tower.Levels[0].Cost < cheapest.Levels[0].Cost))
                {
                    cheapest = tower;
                }
            }

            return cheapest != null ? cheapest.Levels[0].Dps : 0f;
        }

        // What the towers' repairs cost per hit point (the cheapest gun's price: the guns a drone mostly mends).
        private static float RepairPrice(LevelDefinition level)
        {
            float price = 0f;
            foreach (Game.Towers.TowerDefinition tower in level.AvailableTowers)
            {
                if (!tower.BlocksGroundEnemies && (price == 0f || tower.RepairCostPerHealth < price))
                {
                    price = tower.RepairCostPerHealth;
                }
            }

            return price;
        }

        // The level's kill money per hit point of creature, over all its waves (the ledger's own lines).
        private static float BountyPerHealth(CampaignModel model, int index, LevelDefinition level, DifficultyProfile difficulty, int d)
        {
            float money = 0f;
            float health = 0f;
            foreach (WaveLine line in BalanceLedger.Evaluate(model, index, level, difficulty, d))
            {
                money += line.KillMoney;
                health += line.GroundHealth + line.AirHealth;
            }

            return health > 0f ? money / health : 0f;
        }
    }
}
