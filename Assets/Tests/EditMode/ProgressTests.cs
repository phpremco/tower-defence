using Game.Economy;
using Game.Platform;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class ProgressTests
    {
        private sealed class MemorySave : ISaveService
        {
            public ProgressData Stored;

            public void Save<T>(string key, T data) => Stored = data as ProgressData;

            public bool TryLoad<T>(string key, out T data)
            {
                data = (T)(object)Stored;
                return Stored != null;
            }
        }

        private UpgradeDefinition _upgrade;

        [SetUp]
        public void SetUp()
        {
            _upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            var so = new SerializedObject(_upgrade);
            SerializedProperty costs = so.FindProperty("_costs");
            costs.arraySize = 2;
            costs.GetArrayElementAtIndex(0).intValue = 5;
            costs.GetArrayElementAtIndex(1).intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_upgrade);

        [Test]
        public void New_ProgressHasStartingTowersUnlocked()
        {
            var progress = new ProgressService(new MemorySave(), new[] { "PulseRifle" });

            Assert.IsTrue(progress.IsTowerUnlocked("PulseRifle"));
            Assert.IsFalse(progress.IsTowerUnlocked("Cannon"));
        }

        [Test]
        public void BuyUpgrade_SpendsPointsAndStopsAtMaxLevel()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);
            progress.AddRewards(30, 0);

            Assert.IsTrue(progress.TryBuyUpgrade(_upgrade));
            Assert.IsTrue(progress.TryBuyUpgrade(_upgrade));
            Assert.IsFalse(progress.TryBuyUpgrade(_upgrade));
            Assert.AreEqual(2, progress.GetUpgradeLevel(_upgrade.Id));
            Assert.AreEqual(15, progress.Data.ResearchPoints);
        }

        [Test]
        public void BuyUpgrade_FailsWhenTooPoor()
        {
            var progress = new ProgressService(new MemorySave(), new string[0]);
            progress.AddRewards(4, 0);

            Assert.IsFalse(progress.TryBuyUpgrade(_upgrade));
            Assert.AreEqual(0, progress.GetUpgradeLevel(_upgrade.Id));
        }

        [Test]
        public void Progress_SurvivesSaveAndLoad()
        {
            var save = new MemorySave();
            var first = new ProgressService(save, new[] { "PulseRifle" });
            first.AddRewards(7, 3);
            first.MarkCompleted("Level_Landing", "Medium");
            first.UnlockTower("Cannon");
            first.Save();

            var second = new ProgressService(save, new[] { "PulseRifle" });

            Assert.AreEqual(7, second.Data.ResearchPoints);
            Assert.AreEqual(3, second.Data.PowerCurrency);
            Assert.IsTrue(second.IsCompleted("Level_Landing", "Medium"));
            Assert.IsTrue(second.IsLevelCompleted("Level_Landing"));
            Assert.IsTrue(second.IsTowerUnlocked("Cannon"));
        }
    }
}
