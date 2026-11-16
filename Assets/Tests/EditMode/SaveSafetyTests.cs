using System;
using System.IO;
using Game.Economy;
using Game.Platform;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Round 69: the file save is crash-safe (temp file, atomic replace, .bak of the previous good save), a damaged file
    /// falls back to the backup and then to a fresh save instead of throwing, and the options live in their own file,
    /// seeded once from an older progress file and written only when an options screen closes.
    /// </summary>
    public sealed class SaveSafetyTests
    {
        private string _root;

        [SetUp]
        public void CreateFolder()
        {
            _root = Path.Combine(Path.GetTempPath(), "td_r69_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private string FileOf(string key) => Path.Combine(_root, key + ".json");

        private static ProgressData Progress(int research) => new ProgressData { Version = ProgressMigrator.CurrentVersion, ResearchPoints = research };

        [Test]
        public void MissingFile_LoadsAsMissing_WithoutThrowing()
        {
            var save = new FileSaveService(_root);

            Assert.IsFalse(save.TryLoad("progress", out ProgressData data));
            Assert.IsNull(data);
        }

        [Test]
        public void Save_ReplacesTheFile_AndKeepsThePreviousOneAsBackup_WithNoTempLeft()
        {
            var save = new FileSaveService(_root);
            save.Save("progress", Progress(1));
            save.Save("progress", Progress(2));

            Assert.IsTrue(save.TryLoad("progress", out ProgressData data));
            Assert.AreEqual(2, data.ResearchPoints);
            StringAssert.Contains("\"ResearchPoints\":1", File.ReadAllText(FileOf("progress") + ".bak"));
            Assert.IsFalse(File.Exists(FileOf("progress") + ".tmp"));
        }

        [TestCase("{\"Version\":2,\"ResearchPoints\":7,\"Unlo", TestName = "TruncatedFile_FallsBackToTheBackup")]
        [TestCase("this is not json", TestName = "CorruptFile_FallsBackToTheBackup")]
        [TestCase("", TestName = "EmptyFile_FallsBackToTheBackup")]
        [TestCase("   \n", TestName = "WhitespaceFile_FallsBackToTheBackup")]
        public void DamagedFile_FallsBackToTheBackup(string damaged)
        {
            var save = new FileSaveService(_root);
            save.Save("progress", Progress(5));
            save.Save("progress", Progress(6));
            File.WriteAllText(FileOf("progress"), damaged);

            Assert.IsTrue(save.TryLoad("progress", out ProgressData data));
            Assert.AreEqual(5, data.ResearchPoints, "the previous good save comes back");
            Assert.AreEqual(damaged, File.ReadAllText(FileOf("progress") + ".damaged"), "the damaged file is kept beside it");
        }

        [Test]
        public void DamagedFileWithoutBackup_LoadsAsMissing_AndTheGameStartsFresh()
        {
            File.WriteAllText(FileOf("progress"), "{\"Version\":2,\"Resea");
            var save = new FileSaveService(_root);

            Assert.DoesNotThrow(() => new ProgressService(save, new[] { "PulseRifle" }));
            var progress = new ProgressService(save, new[] { "PulseRifle" });
            Assert.AreEqual(0, progress.Data.ResearchPoints);
            Assert.AreEqual(ProgressMigrator.CurrentVersion, progress.Data.Version);
            Assert.IsTrue(progress.IsTowerUnlocked("PulseRifle"));
        }

        [Test]
        public void DamagedFileAndDamagedBackup_LoadAsMissing()
        {
            File.WriteAllText(FileOf("progress"), "{");
            File.WriteAllText(FileOf("progress") + ".bak", "nope");
            var save = new FileSaveService(_root);

            Assert.IsFalse(save.TryLoad("progress", out ProgressData _));
        }

        [Test]
        public void SaveAfterAFallback_WritesAGoodFileAgain()
        {
            var save = new FileSaveService(_root);
            save.Save("progress", Progress(3));
            save.Save("progress", Progress(4));
            File.WriteAllText(FileOf("progress"), "{broken");

            var progress = new ProgressService(save, new string[0]);
            progress.Save();

            Assert.IsTrue(save.TryLoad("progress", out ProgressData data));
            Assert.AreEqual(3, data.ResearchPoints);
        }

        // An older progress file carried the options; the first boot after the split copies them into settings.json.
        private const string LegacyProgress =
            "{\"Version\":2,\"ResearchPoints\":27,\"PowerCurrency\":11,\"MasterVolumePercent\":40,\"MusicVolumePercent\":55," +
            "\"EffectsVolumePercent\":90,\"AmbienceVolumePercent\":20,\"CameraSpeedPercent\":130,\"EdgeScrollingOff\":true," +
            "\"RadarOff\":true,\"RadarSizePercent\":80,\"HudMarginPercent\":4,\"IntroSeen\":true,\"SeenStoryIds\":[\"Dlg_L01_Start\"]," +
            "\"UnlockedTowerIds\":[\"PulseRifle\",\"Cannon\"],\"CompletedLevels\":[\"Level_C1_01/Easy\"]," +
            "\"Upgrades\":[{\"Id\":\"Base_Weapon\",\"Level\":1}],\"Stars\":[{\"Key\":\"Level_C1_01/Easy\",\"Stars\":3}]}";

        [Test]
        public void FirstBootAfterTheSplit_SeedsTheSettingsFromTheProgressFile_AndKeepsTheProgress()
        {
            File.WriteAllText(FileOf("progress"), LegacyProgress);
            var save = new FileSaveService(_root);

            var settings = new SettingsService(save);
            var progress = new ProgressService(save, new string[0]);

            SettingsData s = settings.Data;
            Assert.AreEqual(new[] { 40, 55, 90, 20 }, new[] { s.MasterVolumePercent, s.MusicVolumePercent, s.EffectsVolumePercent, s.AmbienceVolumePercent });
            Assert.AreEqual(130, s.CameraSpeedPercent);
            Assert.IsTrue(s.EdgeScrollingOff);
            Assert.IsTrue(s.RadarOff);
            Assert.AreEqual(80, s.RadarSizePercent);
            Assert.AreEqual(4, s.HudMarginPercent);
            Assert.AreEqual(SettingsData.DefaultLanguage, s.LanguageCode);
            Assert.IsTrue(s.MuteInBackground, "new options take their defaults");
            Assert.AreEqual(SettingsData.CurrentVersion, s.Version);
            Assert.IsTrue(File.Exists(FileOf("settings")), "the seed is written at once");

            Assert.AreEqual(27, progress.Data.ResearchPoints);
            Assert.AreEqual(11, progress.Data.PowerCurrency);
            Assert.IsTrue(progress.IsCompleted("Level_C1_01", "Easy"));
            Assert.AreEqual(3, progress.GetStars("Level_C1_01", "Easy"));
            Assert.AreEqual(1, progress.GetUpgradeLevel("Base_Weapon"));
            Assert.AreEqual(LegacyProgress, File.ReadAllText(FileOf("progress")), "loading never rewrites the progress file");
        }

        [Test]
        public void SettingsFile_WinsOverTheProgressFile_AfterTheProgressDropsTheOldFields()
        {
            File.WriteAllText(FileOf("progress"), LegacyProgress);
            var save = new FileSaveService(_root);
            new SettingsService(save);
            new ProgressService(save, new string[0]).Save();
            StringAssert.DoesNotContain("MasterVolumePercent", File.ReadAllText(FileOf("progress")), "the progress file no longer carries options");

            var reloaded = new SettingsService(save);

            Assert.AreEqual(40, reloaded.Data.MasterVolumePercent);
            Assert.AreEqual(130, reloaded.Data.CameraSpeedPercent);
        }

        [Test]
        public void NoProgressAndNoSettings_StartsWithDefaults()
        {
            var settings = new SettingsService(new FileSaveService(_root));

            Assert.AreEqual(100, settings.Data.MasterVolumePercent);
            Assert.AreEqual(70, settings.Data.MusicVolumePercent);
            Assert.AreEqual(-1, settings.Data.HudMarginPercent);
            Assert.IsTrue(settings.Data.MuteInBackground);
        }

        [Test]
        public void SliderSteps_StayInMemory_UntilTheScreenFlushes()
        {
            var save = new FileSaveService(_root);
            var settings = new SettingsService(save);
            string seeded = File.ReadAllText(FileOf("settings"));
            int changes = 0;
            settings.Changed += () => changes++;

            for (int percent = 0; percent <= 100; percent += 5)
            {
                settings.SetVolumes(percent, 70, 100, 80);
            }

            settings.SetRadar(false, 120);
            Assert.AreEqual(22, changes, "listeners hear every step");
            Assert.IsTrue(settings.IsDirty);
            Assert.AreEqual(seeded, File.ReadAllText(FileOf("settings")), "nothing is written while the sliders move");

            settings.Flush();

            Assert.IsFalse(settings.IsDirty);
            Assert.AreEqual(100, new SettingsService(save).Data.MasterVolumePercent);
            Assert.AreEqual(120, new SettingsService(save).Data.RadarSizePercent);
        }

        [Test]
        public void DamagedSettingsFile_FallsBackToItsBackup()
        {
            var save = new FileSaveService(_root);
            var settings = new SettingsService(save);
            settings.SetHudMargin(3);
            settings.Flush();
            settings.SetHudMargin(6);
            settings.Flush();
            File.WriteAllText(FileOf("settings"), "{\"MasterVol");

            Assert.AreEqual(3, new SettingsService(save).Data.HudMarginPercent);
        }
    }
}
