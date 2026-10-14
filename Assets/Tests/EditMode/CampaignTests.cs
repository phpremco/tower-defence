using Game.Economy;
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
            Assert.Greater(second.ResearchPoints, 0);
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

