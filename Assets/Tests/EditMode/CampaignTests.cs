using Game.Core;
using Game.Economy;
using System.Collections.Generic;
using Game.Levels;
using Game.Platform;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Uses the generated level 1 data: campaign unlock rules and the first-clear reward.</summary>
    public sealed class CampaignTests
    {
        private sealed class MemorySave : ISaveService
        {
            public void Save<T>(string key, T data) { }

            public bool TryLoad<T>(string key, out T data)
            {
                data = default;
                return false;
            }
        }

        // Keeps what is saved, as JSON like the real file save, so a reload sees it.
        private sealed class JsonMemorySave : ISaveService
        {
            private readonly Dictionary<string, string> _store = new Dictionary<string, string>();

            public void Save<T>(string key, T data) => _store[key] = JsonUtility.ToJson(data);

            public bool TryLoad<T>(string key, out T data)
            {
                if (_store.TryGetValue(key, out string json))
                {
                    data = JsonUtility.FromJson<T>(json);
                    return true;
                }

                data = default;
                return false;
            }
        }

        private CampaignCatalog _catalog;
        private LevelDefinition _level;
        private DifficultyProfile _medium;

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
            _level = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/_Project/Data/Levels/Level_Landing.asset");
            _medium = AssetDatabase.LoadAssetAtPath<DifficultyProfile>("Assets/_Project/Data/Difficulty/Medium.asset");
            Assert.IsNotNull(_catalog, "Run Tools/Generate/Scenes and Starter Data first.");
        }

        [Test]
        public void OnlyFirstLevelIsOpenAtStart()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);

            Assert.IsTrue(CampaignRules.IsOpen(_catalog, 0, progress));
            for (int i = 1; i < _catalog.Nodes.Count; i++)
            {
                Assert.IsFalse(CampaignRules.IsOpen(_catalog, i, progress), $"node {i}");
            }
        }

        [Test]
        public void EveryNodeLiesInsideItsRegion_AndRegionsFillInOrder()
        {
            Assert.Greater(_catalog.Regions.Count, 1, "the planet needs more than one region");
            for (int i = 0; i < _catalog.Nodes.Count; i++)
            {
                MapNode node = _catalog.Nodes[i];
                Assert.That(node.Region, Is.InRange(0, _catalog.Regions.Count - 1), $"node {i} region");
                MapRegion region = _catalog.Regions[node.Region];
                Assert.LessOrEqual(PlanetCoordinates.AngleBetween(node.Direction, region.Direction), region.RadiusDegrees, $"node {i} outside {region.DisplayName}");
                if (i > 0)
                {
                    Assert.GreaterOrEqual(node.Region, _catalog.Nodes[i - 1].Region, $"node {i} jumps back to an earlier region");
                }

                for (int k = 0; k < i; k++)
                {
                    Assert.GreaterOrEqual(PlanetCoordinates.AngleBetween(node.Direction, _catalog.Nodes[k].Direction), PlanetCoordinates.MinimumSpacingDegrees, $"nodes {k} and {i} too close");
                }
            }
        }

        [Test]
        public void OnlyTheLandingRegionIsOpenAtStart_AndItCountsItsLevels()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);

            Assert.IsTrue(CampaignRules.IsRegionOpen(_catalog, 0, progress));
            for (int r = 1; r < _catalog.Regions.Count; r++)
            {
                Assert.IsFalse(CampaignRules.IsRegionOpen(_catalog, r, progress), $"region {r}");
            }

            Assert.AreEqual(_catalog.Nodes.Count, _catalog.NodeCount(0), "all levels live in the landing region for now");
            Assert.AreEqual(0, CampaignRules.CompletedInRegion(_catalog, 0, progress));
            progress.MarkCompleted(_level.Id, _medium.name);
            Assert.AreEqual(1, CampaignRules.CompletedInRegion(_catalog, 0, progress));
        }

        [Test]
        public void SecondLevelOpensAfterTheFirstIsCompleted()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);
            Assert.IsFalse(CampaignRules.IsOpen(_catalog, 1, progress));

            progress.MarkCompleted(_level.Id, _medium.name);

            Assert.IsTrue(CampaignRules.IsOpen(_catalog, 1, progress));
        }

        [Test]
        public void NodeWithoutLevelStaysClosedEvenAfterPreviousIsCompleted()
        {
            var catalog = ScriptableObject.CreateInstance<CampaignCatalog>();
            var so = new SerializedObject(catalog);
            SerializedProperty nodes = so.FindProperty("_nodes");
            nodes.arraySize = 2;
            nodes.GetArrayElementAtIndex(0).FindPropertyRelative("Level").objectReferenceValue = _level;
            so.ApplyModifiedPropertiesWithoutUndo();

            var progress = new ProgressService(new MemorySave(), new string[0]);
            progress.MarkCompleted(_level.Id, _medium.name);

            Assert.IsFalse(CampaignRules.IsOpen(catalog, 1, progress));
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void FirstClear_GivesWeaponOnlyOnce()
        {
            var progress = new ProgressService(new MemorySave(), new[] { "PulseRifle" });

            LevelResult first = RewardCalculator.Compute(_level, _medium, progress, true, 1f);
            RewardCalculator.Apply(first, _level, _medium, progress);
            LevelResult second = RewardCalculator.Compute(_level, _medium, progress, true, 1f);

            Assert.IsNotNull(first.NewTower);
            Assert.IsTrue(progress.IsTowerUnlocked(first.NewTower.Id));
            Assert.IsNull(second.NewTower);
            Assert.AreEqual(0, second.ResearchPoints, "points come once per difficulty");
        }

        [Test]
        public void Rewards_ArePaidOncePerLevelPerDifficulty()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);

            LevelResult first = RewardCalculator.Compute(_level, _medium, progress, true, 1f);
            Assert.IsFalse(first.RewardsAlreadyEarned);
            Assert.Greater(first.ResearchPoints, 0);
            RewardCalculator.Apply(first, _level, _medium, progress);
            int held = progress.Data.ResearchPoints;

            LevelResult again = RewardCalculator.Compute(_level, _medium, progress, true, 1f);
            Assert.IsTrue(again.RewardsAlreadyEarned);
            Assert.AreEqual(0, again.ResearchPoints);
            Assert.AreEqual(0, again.PowerCurrency);
            Assert.AreEqual(3, again.Stars, "stars are still rated on a replay");
            RewardCalculator.Apply(again, _level, _medium, progress);
            Assert.AreEqual(held, progress.Data.ResearchPoints);

            var hard = AssetDatabase.LoadAssetAtPath<DifficultyProfile>("Assets/_Project/Data/Difficulty/Hard.asset");
            LevelResult harder = RewardCalculator.Compute(_level, hard, progress, true, 1f);
            Assert.IsFalse(harder.RewardsAlreadyEarned, "another difficulty pays again");
            Assert.Greater(harder.ResearchPoints, first.ResearchPoints);
        }

        [Test]
        public void EveryLevel_HasABaseTurretThatIsNeverSold()
        {
            foreach (MapNode node in _catalog.Nodes)
            {
                Assert.IsNotNull(node.Level.BaseTurret, $"{node.Level.name} has a base turret");
                Assert.IsTrue(node.Level.BaseTurret.IsBaseDefence);
                Assert.AreEqual(0, node.Level.BaseTurret.BuildCost, "the base gun is bought at the base, not in the level");
            }

            var progress = new ProgressService(new MemorySave(), new string[0]);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
            Assert.AreEqual(0, UpgradeEffects.ComputeGlobal(progress, upgrades).BaseWeaponLevel, "an unarmed base to begin with");
            progress.AddRewards(1000, 0);
            foreach (UpgradeDefinition upgrade in upgrades.Upgrades)
            {
                if (upgrade.Target == UpgradeTarget.BaseWeapon)
                {
                    Assert.IsTrue(progress.TryBuyUpgrade(upgrade));
                }
            }

            Assert.AreEqual(1, UpgradeEffects.ComputeGlobal(progress, upgrades).BaseWeaponLevel);
        }

        // The expected upgrade is reported per level and only grows; it no longer scales enemy health, because the wave
        // solver already sized the waves against the upgrades the player can have bought by then.
        [Test]
        public void LaterLevels_CountOnUpgrades()
        {
            float previous = -1f;
            foreach (MapNode node in _catalog.Nodes)
            {
                Assert.GreaterOrEqual(node.Level.ExpectedUpgradeBonus, previous, $"{node.Level.name} expects at least as much upgrade as the level before");
                Assert.AreEqual(node.Level.EnemyHealthMultiplier, node.Level.EnemyHealthScale, 0.0001f);
                previous = node.Level.ExpectedUpgradeBonus;
            }

            Assert.Greater(previous, 0f, "the last level counts on some upgrades");
        }

        [Test]
        public void Stars_FollowRemainingBaseHealth()
        {
            Assert.AreEqual(1, RewardCalculator.StarsFor(_level, 0.2f));
            Assert.AreEqual(2, RewardCalculator.StarsFor(_level, 0.6f));
            Assert.AreEqual(3, RewardCalculator.StarsFor(_level, 1f));
        }

        [Test]
        public void Stars_KeepTheBestRatingPerDifficulty()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);

            progress.RecordStars(_level.Id, _medium.name, 2);
            progress.RecordStars(_level.Id, _medium.name, 1);

            Assert.AreEqual(2, progress.GetStars(_level.Id, _medium.name));
        }

        [Test]
        public void Loss_GivesNothing()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);

            LevelResult result = RewardCalculator.Compute(_level, _medium, progress, false, 0f);
            RewardCalculator.Apply(result, _level, _medium, progress);

            Assert.AreEqual(0, progress.Data.ResearchPoints);
            Assert.IsFalse(progress.IsLevelCompleted(_level.Id));
        }

        [Test]
        public void UpgradeEffects_AreSeparatePerTower()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
            progress.AddRewards(100, 0);

            UpgradeDefinition pulseDamage = null;
            foreach (UpgradeDefinition upgrade in upgrades.Upgrades)
            {
                if (upgrade.TowerId == "PulseRifle" && upgrade.Target == UpgradeTarget.TowerDamage)
                {
                    pulseDamage = upgrade;
                }
            }

            Assert.IsNotNull(pulseDamage);
            Assert.IsTrue(progress.TryBuyUpgrade(pulseDamage));

            var pulse = UpgradeEffects.ComputeForTower(progress, upgrades, "PulseRifle");
            var cannon = UpgradeEffects.ComputeForTower(progress, upgrades, "Cannon");

            Assert.Greater(pulse.TowerDamageMultiplier, 1f);
            Assert.AreEqual(1f, cannon.TowerDamageMultiplier);
        }

        [Test]
        public void UpgradeEffects_EveryDroneHasItsOwnPathPaidInPower()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
            progress.AddRewards(0, 1000);

            var perDrone = new Dictionary<string, int>();
            foreach (UpgradeDefinition upgrade in upgrades.Upgrades)
            {
                if (string.IsNullOrEmpty(upgrade.DroneId))
                {
                    continue;
                }

                Assert.AreEqual(UpgradeCurrency.PowerCurrency, upgrade.Currency, $"{upgrade.Id} should cost power");
                Assert.IsTrue(string.IsNullOrEmpty(upgrade.TowerId), $"{upgrade.Id} belongs to a drone, not a tower");
                perDrone.TryGetValue(upgrade.DroneId, out int count);
                perDrone[upgrade.DroneId] = count + 1;
            }

            Assert.AreEqual(3, perDrone.Count, "repair, harvest and attack each have a path");
            foreach (KeyValuePair<string, int> pair in perDrone)
            {
                Assert.GreaterOrEqual(pair.Value, 3, $"{pair.Key} has at least three upgrades");
                Assert.AreEqual(1f, UpgradeEffects.ComputeForDrone(progress, upgrades, pair.Key).SpeedMultiplier);
            }

            foreach (UpgradeDefinition upgrade in upgrades.Upgrades)
            {
                if (upgrade.DroneId == "HarvestDrone")
                {
                    Assert.IsTrue(progress.TryBuyUpgrade(upgrade), $"{upgrade.Id} is buyable with power");
                }
            }

            DroneModifiers harvest = UpgradeEffects.ComputeForDrone(progress, upgrades, "HarvestDrone");
            DroneModifiers repair = UpgradeEffects.ComputeForDrone(progress, upgrades, "RepairDrone");
            Assert.Greater(harvest.SpeedMultiplier, 1f);
            Assert.Greater(harvest.OutputMultiplier, 1f);
            Assert.AreEqual(1, harvest.ExtraCount, "one extra harvester per bought count level");
            Assert.AreEqual(1f, repair.SpeedMultiplier, "buying for one drone leaves the others alone");
            Assert.AreEqual(1f, UpgradeEffects.ComputeGlobal(progress, upgrades).HeroDamageMultiplier, "drone upgrades are not global");
        }

        [Test]
        public void Loadout_IsSavedAndOfferedAgain()
        {
            var save = new JsonMemorySave();
            var progress = new ProgressService(save, new string[0]);
            progress.SetLoadout(new[] { "Mine", "LavaBall" });

            var reloaded = new ProgressService(save, new string[0]);
            CollectionAssert.AreEqual(new[] { "Mine", "LavaBall" }, reloaded.Loadout);
        }

        [Test]
        public void UpgradeEffects_BaseHealthIsGlobal()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
            progress.AddRewards(100, 0);
            foreach (UpgradeDefinition upgrade in upgrades.Upgrades)
            {
                if (string.IsNullOrEmpty(upgrade.TowerId))
                {
                    progress.TryBuyUpgrade(upgrade);
                }
            }

            Assert.Greater(UpgradeEffects.ComputeGlobal(progress, upgrades).BaseHealthBonus, 0);
            Assert.AreEqual(0, UpgradeEffects.ComputeForTower(progress, upgrades, "Cannon").BaseHealthBonus);
        }
    }
}

