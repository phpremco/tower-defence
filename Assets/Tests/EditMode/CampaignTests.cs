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
        public void NodeWithoutLevelStaysClosedEvenAfterPreviousIsCompleted()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);
            progress.MarkCompleted(_level.Id, _medium.name);

            Assert.IsFalse(CampaignRules.IsOpen(_catalog, 1, progress));
        }

        [Test]
        public void FirstClear_GivesWeaponOnlyOnce()
        {
            var progress = new ProgressService(new MemorySave(), new[] { "PulseRifle" });

            LevelResult first = RewardCalculator.Compute(_level, _medium, progress, true);
            RewardCalculator.Apply(first, _level, _medium, progress);
            LevelResult second = RewardCalculator.Compute(_level, _medium, progress, true);

            Assert.IsNotNull(first.NewTower);
            Assert.IsTrue(progress.IsTowerUnlocked(first.NewTower.Id));
            Assert.IsNull(second.NewTower);
            Assert.Greater(second.ResearchPoints, 0);
        }

        [Test]
        public void Loss_GivesNothing()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);

            LevelResult result = RewardCalculator.Compute(_level, _medium, progress, false);
            RewardCalculator.Apply(result, _level, _medium, progress);

            Assert.AreEqual(0, progress.Data.ResearchPoints);
            Assert.IsFalse(progress.IsLevelCompleted(_level.Id));
        }

        [Test]
        public void UpgradeEffects_AppliesBoughtLevels()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);
            var upgrades = AssetDatabase.LoadAssetAtPath<UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
            progress.AddRewards(100, 0);
            foreach (UpgradeDefinition upgrade in upgrades.Upgrades)
            {
                progress.TryBuyUpgrade(upgrade);
            }

            var modifiers = UpgradeEffects.Compute(progress, upgrades);

            Assert.Greater(modifiers.TowerDamageMultiplier, 1f);
            Assert.Greater(modifiers.TowerRangeMultiplier, 1f);
            Assert.Greater(modifiers.BaseHealthBonus, 0);
        }
    }
}
