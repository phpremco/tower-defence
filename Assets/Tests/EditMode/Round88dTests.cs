using Game.Balance;
using Game.Combat;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 88d, Remco's playtest of round 88c: the coins for the air defence are start money, not a purse in the middle
    /// of the level (<see cref="StartMoneyRule"/>), and the balance model starts every level with the wallet's coins; a
    /// wrecked base turret is raised by the repair crew (<see cref="ICrewRaisable"/>, <see cref="RepairDispatcher"/>).
    /// </summary>
    public sealed class Round88dTests
    {
        private const string ShardRidgePath = "Assets/_Project/Data/Levels/Level_C1_03.asset";
        private const string CatalogPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string UpgradesPath = "Assets/_Project/Data/Upgrades/UpgradeCatalog.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";
        private const string BaseTurretPath = "Assets/_Project/Data/Towers/BaseTurret.asset";
        private const string CannonPath = "Assets/_Project/Data/Towers/Cannon.asset";
        private const string BlockadePath = "Assets/_Project/Data/Towers/Blockade.asset";
        private const float DroneSpeed = 4f;

        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        // ---- start money ---------------------------------------------------------------------------------------------

        [Test]
        public void ALevelWithAnAirRaid_StartsWithItsSkyGunsPrice_OnTheDifficultiesThatPayIt_AndNoOtherLevelDoes()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, CatalogPath);
            int paid = 0;
            int unpaid = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                foreach (DifficultyProfile difficulty in catalog.Difficulties)
                {
                    WaveDefinition[] waves = node.Level.GetWaves(difficulty);
                    bool raid = waves != null && FirstRaidRule.FirstRaid(waves) != FirstRaidRule.None;
                    int plain = Mathf.RoundToInt(node.Level.StartingMoney * difficulty.StartingMoneyMultiplier);
                    int extra = raid ? Mathf.RoundToInt(FirstRaidRule.SkyGunPrice(node.Level.AvailableTowers) * difficulty.RaidStartGuns) : 0;
                    // Round 88e: a level's own sum for the difficulty wins; every other cell starts on the shared rule.
                    int expected = node.Level.TryGetStartMoney(difficulty, out int own) ? own : plain + extra;
                    Assert.AreEqual(plain + extra, StartMoneyRule.Shared(node.Level, difficulty), $"{node.Level.name} {difficulty.name}");
                    Assert.AreEqual(expected, StartMoneyRule.For(node.Level, difficulty), $"{node.Level.name} {difficulty.name}");
                    paid += extra > 0 ? 1 : 0;
                    unpaid += extra == 0 ? 1 : 0;
                }
            }

            Assert.Greater(paid, 0, "Hard and Psycho of the first continent have air raids: the test read none");
            Assert.Greater(unpaid, 0, "Easy and Medium start on the plain sum");
        }

        [Test]
        public void TheBalanceModel_StartsEveryLevelWithTheWalletsCoins()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>(UpgradesPath);
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            Assert.IsNotNull(catalog, CatalogPath);
            Assert.IsNotNull(unlocks, UnlocksPath);
            CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, BalanceRules.Default);
            int cells = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
                int index = model.IndexOf(key);
                Assert.GreaterOrEqual(index, 0, key);
                for (int d = 0; d < catalog.Difficulties.Count; d++)
                {
                    // The bootstrapper's own sum (LevelBootstrapper.BuildGameplay) is StartMoneyRule.For.
                    Assert.AreEqual(StartMoneyRule.For(node.Level, catalog.Difficulties[d]), model.InputFor(index, d).StartingMoney, $"{key} {catalog.Difficulties[d].name}: the model and the game start with different money");
                    cells++;
                }
            }

            Assert.Greater(cells, 0);
        }

        [Test]
        public void ShardRidge_StartsHardOnFourHundred_AndPsychoAboveTheSharedRule()
        {
            // Round 88e, Remco: "make Hard 400 money, and Psycho needs more too", through the level's own start money
            // (LevelDefinition.TryGetStartMoney), not the shared ratios. Shard Ridge is the level he named: its sums are pinned.
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(ShardRidgePath);
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            Assert.IsNotNull(level, ShardRidgePath);
            DifficultyProfile hard = catalog.Difficulties[2];
            DifficultyProfile psycho = catalog.Difficulties[3];
            int gun = FirstRaidRule.SkyGunPrice(level.AvailableTowers);

            Assert.AreEqual(400, StartMoneyRule.For(level, hard));
            // Round 88g, Remco after winning it "quite easily": "a very little less start money", 1000 -> 920.
            Assert.AreEqual(920, StartMoneyRule.For(level, psycho));
            Assert.Greater(StartMoneyRule.For(level, psycho), StartMoneyRule.Shared(level, psycho), "Psycho still starts above the shared rule of round 88d");
            Assert.Greater(gun, 0, "the level sells a sky gun");
            Assert.IsTrue(level.TryGetStartMoney(hard, out int own) && own == 400, "the sum is the level's own entry");
            Assert.IsFalse(level.TryGetStartMoney(catalog.Difficulties[0], out _), "Easy and Medium stay on the shared rule");
            Assert.AreEqual(StartMoneyRule.Shared(level, catalog.Difficulties[1]), StartMoneyRule.For(level, catalog.Difficulties[1]));
            Assert.AreEqual(1f, BalanceRules.Default.RaidStartGunsFor(2), "the shared ratio is as round 88d left it");
        }

        // ---- a wrecked base turret and the repair crew ------------------------------------------------------------------

        private Tower Build(string definitionPath)
        {
            var definition = AssetDatabase.LoadAssetAtPath<TowerDefinition>(definitionPath);
            Assert.IsNotNull(definition, definitionPath);
            _go = new GameObject("Tower");
            Tower tower = _go.AddComponent<Tower>();
            tower.enabled = false;
            var targets = new TargetRegistry();
            tower.Initialize(definition, targets, new ProjectilePool(targets, _go.transform), PlayerModifiers.None);
            return tower;
        }

        [Test]
        public void AWreckedBaseTurret_IsAJobForTheRepairCrew_AndStandsAgainWholeOnceTheWorkIsDone()
        {
            Tower turret = Build(BaseTurretPath);
            Assert.IsTrue(turret.Definition.IsBaseDefence, "the base turret asset is a base defence");
            var registry = new StructureRegistry();
            turret.Register(registry);
            var dispatcher = new RepairDispatcher(registry);
            var drone = new object();
            Assert.IsNull(dispatcher.Assign(drone, Vector3.zero, DroneSpeed, null), "a whole turret is no job");

            turret.TakeDamage(turret.Health.Max * 2f);
            Assert.IsTrue(turret.IsWrecked);
            Assert.IsTrue(turret.AwaitsCrew);
            Assert.IsFalse(turret.CanBeSold, "no new button: it is not scrapped");
            Assert.AreEqual(0, turret.RepairCost, "and no price: the crew does it");
            Assert.AreSame(turret, dispatcher.Assign(drone, Vector3.zero, DroneSpeed, null), "the crew takes the wreck");

            turret.Repair(turret.Health.Max * 0.6f);
            Assert.IsTrue(turret.IsWrecked, "it does not come back with a sliver of health");
            Assert.AreSame(turret, dispatcher.Assign(drone, Vector3.zero, DroneSpeed, turret), "the crew stays on the job");

            turret.Repair(turret.Health.Max * 0.4f + 0.01f);
            Assert.IsFalse(turret.IsWrecked);
            Assert.AreEqual(turret.Health.Max, turret.Health.Current, 0.001f, "raised whole");
            Assert.IsFalse(turret.AwaitsCrew);
            Assert.IsNull(dispatcher.Assign(drone, Vector3.zero, DroneSpeed, turret), "done: the job is given back");

            // Wrecked a second time, the work starts from nothing again.
            turret.TakeDamage(turret.Health.Max * 2f);
            turret.Repair(turret.Health.Max * 0.9f);
            Assert.IsTrue(turret.IsWrecked);
        }

        [Test]
        public void AWreckedGun_IsStillThePlayersToScrap_AndNoJobForTheCrew()
        {
            Tower gun = Build(CannonPath);
            var registry = new StructureRegistry();
            gun.Register(registry);
            gun.TakeDamage(gun.Health.Max * 2f);
            Assert.IsFalse(gun.AwaitsCrew);
            gun.Repair(gun.Health.Max * 2f);
            Assert.IsTrue(gun.IsWrecked);
            Assert.IsNull(new RepairDispatcher(registry).Assign(new object(), Vector3.zero, DroneSpeed, null));
        }

        [Test]
        public void AWreckedGate_IsStillRebuiltAtItsPrice_AndNoJobForTheCrew()
        {
            Tower gate = Build(BlockadePath);
            var registry = new StructureRegistry();
            gate.Register(registry);
            gate.TakeDamage(gate.Health.Max * 2f);
            Assert.IsFalse(gate.AwaitsCrew);
            Assert.Greater(gate.RepairCost, 0);
            gate.Repair(gate.Health.Max * 2f);
            Assert.IsTrue(gate.IsWrecked);
            Assert.IsNull(new RepairDispatcher(registry).Assign(new object(), Vector3.zero, DroneSpeed, null));
        }
    }
}
