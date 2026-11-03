using Game.Economy;
using Game.Platform;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class ProgressMigrationTests
    {
        // Keeps the save as JSON text, the way the file save does, so tests cover the real serializer.
        private sealed class JsonMemorySave : ISaveService
        {
            public string Json;

            public void Save<T>(string key, T data) => Json = JsonUtility.ToJson(data);

            public bool TryLoad<T>(string key, out T data)
            {
                data = Json == null ? default : JsonUtility.FromJson<T>(Json);
                return Json != null;
            }
        }

        // A save as written before the format had a version or story flags.
        private const string VersionZeroJson =
            "{\"ResearchPoints\":12,\"PowerCurrency\":4,\"IntroSeen\":true,\"UnlockedTowerIds\":[\"PulseRifle\",\"Cannon\"]," +
            "\"CompletedLevels\":[\"Level_Landing/Easy\"]}";

        // A version 1 save from the seven-level campaign: completions, stars, weapons, story flags and the intro seen.
        private const string VersionOneJson =
            "{\"Version\":1,\"ResearchPoints\":140,\"PowerCurrency\":22,\"MasterVolumePercent\":60,\"IntroSeen\":true," +
            "\"SeenStoryIds\":[\"Dlg_L01_Start.01\"],\"UnlockedTowerIds\":[\"PulseRifle\",\"Cannon\",\"Railgun\"]," +
            "\"CompletedLevels\":[\"Level_Landing/Easy\",\"Level_Forest/Hard\"],\"Upgrades\":[{\"Id\":\"PulseRifle_Damage\",\"Level\":2},{\"Id\":\"Cryo_Range\",\"Level\":1}]," +
            "\"Stars\":[{\"Key\":\"Level_Landing/Easy\",\"Stars\":3}],\"LoadoutIds\":[\"Mine\"]}";

        [Test]
        public void VersionZeroSave_IsMigratedToCurrent_KeepingItsCurrencies()
        {
            var progress = new ProgressService(new JsonMemorySave { Json = VersionZeroJson }, new[] { "PulseRifle" });

            Assert.AreEqual(ProgressMigrator.CurrentVersion, progress.Data.Version);
            Assert.AreEqual(12, progress.Data.ResearchPoints);
            Assert.AreEqual(4, progress.Data.PowerCurrency);
            Assert.IsFalse(progress.Data.IntroSeen, "the campaign restart plays the intro again");
            Assert.IsFalse(progress.IsTowerUnlocked("Cannon"), "weapons come back with the new campaign");
            Assert.IsTrue(progress.IsTowerUnlocked("PulseRifle"), "the starting set is unlocked on load");
            Assert.IsFalse(progress.IsCompleted("Level_Landing", "Easy"));
            Assert.IsNotNull(progress.Data.SeenStoryIds);
            Assert.IsFalse(progress.HasSeenStory("intro"));
        }

        // Step 2 restarts the campaign for the continent levels: progress goes, currencies, upgrades and settings stay.
        [Test]
        public void VersionOneSave_RestartsTheCampaign_AndKeepsCurrenciesUpgradesAndSettings()
        {
            var progress = new ProgressService(new JsonMemorySave { Json = VersionOneJson }, new[] { "PulseRifle", "Cannon" });

            Assert.AreEqual(ProgressMigrator.CurrentVersion, progress.Data.Version);
            Assert.AreEqual(140, progress.Data.ResearchPoints);
            Assert.AreEqual(22, progress.Data.PowerCurrency);
            Assert.AreEqual(60, progress.Data.MasterVolumePercent);
            Assert.AreEqual(2, progress.GetUpgradeLevel("PulseRifle_Damage"));
            Assert.AreEqual(1, progress.GetUpgradeLevel("Cryo_Range"), "dormant upgrades stay saved, unrefunded");
            CollectionAssert.AreEqual(new[] { "Mine" }, progress.Loadout);

            Assert.IsEmpty(progress.Data.CompletedLevels);
            Assert.IsEmpty(progress.Data.Stars);
            Assert.IsEmpty(progress.Data.SeenStoryIds);
            Assert.IsFalse(progress.Data.IntroSeen);
            CollectionAssert.AreEquivalent(new[] { "PulseRifle", "Cannon" }, progress.Data.UnlockedTowerIds);
            Assert.IsNotNull(progress.Data.UnlockedHeroPerkIds);
            Assert.IsNotNull(progress.Data.UnlockedPathObjectIds);
        }

        [Test]
        public void CurrentSave_IsNotMigratedAgain()
        {
            var save = new JsonMemorySave();
            var first = new ProgressService(save, new string[0]);
            first.MarkCompleted("Level_C1_01", "Easy");
            first.UnlockPathObject("Mine");
            first.Save();

            var second = new ProgressService(save, new string[0]);

            Assert.IsTrue(second.IsCompleted("Level_C1_01", "Easy"));
            Assert.IsTrue(second.IsPathObjectUnlocked("Mine"));
        }

        [Test]
        public void NewProgress_StartsAtCurrentVersion()
        {
            var progress = new ProgressService(new JsonMemorySave(), new string[0]);

            Assert.AreEqual(ProgressMigrator.CurrentVersion, progress.Data.Version);
        }

        [Test]
        public void NegativeVersion_IsTreatedAsTheOldestFormat()
        {
            var data = new ProgressData { Version = -3, SeenStoryIds = null };

            ProgressMigrator.Migrate(data);

            Assert.AreEqual(ProgressMigrator.CurrentVersion, data.Version, "a damaged version walks every step instead of throwing");
            Assert.IsNotNull(data.SeenStoryIds);
        }

        [Test]
        public void MissingStoryList_IsCreatedByTheMigration()
        {
            var data = new ProgressData { SeenStoryIds = null };

            ProgressMigrator.Migrate(data);

            Assert.IsNotNull(data.SeenStoryIds);
            Assert.AreEqual(ProgressMigrator.CurrentVersion, data.Version);
        }

        [Test]
        public void StoryFlags_SurviveSaveAndLoad()
        {
            var save = new JsonMemorySave();
            var first = new ProgressService(save, new string[0]);
            first.MarkStorySeen("landing_briefing");
            first.MarkStorySeen("landing_briefing");

            var second = new ProgressService(save, new string[0]);

            Assert.IsTrue(second.HasSeenStory("landing_briefing"));
            Assert.IsFalse(second.HasSeenStory("first_boss"));
            Assert.AreEqual(1, second.Data.SeenStoryIds.Count);
            Assert.AreEqual(ProgressMigrator.CurrentVersion, second.Data.Version);
        }
    }
}
