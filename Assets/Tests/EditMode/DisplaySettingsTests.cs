using System.Collections.Generic;
using System.IO;
using Game.Economy;
using Game.Platform;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Round 70: the display, frame-rate, quality, shake and binding settings and their file round trip.</summary>
    public sealed class DisplaySettingsTests
    {
        private string _folder;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "r70_settings_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [Test]
        public void NewSettings_StartBorderlessAtDesktopSize_VSyncOn_FullShake_NoOverrides()
        {
            var settings = new SettingsService(new FileSaveService(_folder));
            SettingsData data = settings.Data;
            Assert.AreEqual((int)DisplayMode.Borderless, data.DisplayMode);
            Assert.AreEqual(0, data.ResolutionWidth);
            Assert.AreEqual(0, data.ResolutionHeight);
            Assert.IsTrue(data.VSync);
            Assert.AreEqual(0, data.FrameRateCap);
            Assert.IsFalse(data.LowQuality);
            Assert.AreEqual(100, data.ScreenShakePercent);
            Assert.AreEqual(1f, settings.ScreenShakeScale);
            Assert.AreEqual(string.Empty, data.BindingOverrides);
        }

        [Test]
        public void EveryNewField_SurvivesAFlushAndAReload()
        {
            var save = new FileSaveService(_folder);
            var settings = new SettingsService(save);
            settings.SetDisplay(DisplayMode.Windowed, 1600, 900);
            settings.SetFrameRate(false, 60);
            settings.SetLowQuality(true);
            settings.SetScreenShake(30);
            settings.SetBindingOverrides("{\"bindings\":[{\"action\":\"Gameplay/Confirm\",\"path\":\"<Keyboard>/k\"}]}");
            Assert.IsTrue(settings.IsDirty);
            settings.Flush();
            Assert.IsFalse(settings.IsDirty);

            SettingsData loaded = new SettingsService(new FileSaveService(_folder)).Data;
            Assert.AreEqual((int)DisplayMode.Windowed, loaded.DisplayMode);
            Assert.AreEqual(1600, loaded.ResolutionWidth);
            Assert.AreEqual(900, loaded.ResolutionHeight);
            Assert.IsFalse(loaded.VSync);
            Assert.AreEqual(60, loaded.FrameRateCap);
            Assert.IsTrue(loaded.LowQuality);
            Assert.AreEqual(30, loaded.ScreenShakePercent);
            StringAssert.Contains("<Keyboard>/k", loaded.BindingOverrides);
            Assert.AreEqual(SettingsData.CurrentVersion, loaded.Version);
        }

        [Test]
        public void HandEditedValues_AreBroughtBackIntoRange()
        {
            File.WriteAllText(Path.Combine(_folder, SettingsService.SaveKey + ".json"),
                "{\"Version\":2,\"DisplayMode\":9,\"ScreenShakePercent\":250,\"FrameRateCap\":-5}");
            var settings = new SettingsService(new FileSaveService(_folder));
            Assert.AreEqual((int)DisplayMode.Borderless, settings.Data.DisplayMode);
            Assert.AreEqual(100, settings.Data.ScreenShakePercent);
            settings.SetFrameRate(true, -5);
            Assert.AreEqual(0, settings.Data.FrameRateCap, "a negative cap means no cap");
            settings.SetScreenShake(-20);
            Assert.AreEqual(0f, settings.ScreenShakeScale);
        }

        [Test]
        public void AnOlderSettingsFile_KeepsItsValues_AndGetsTheNewDefaults()
        {
            File.WriteAllText(Path.Combine(_folder, SettingsService.SaveKey + ".json"),
                "{\"Version\":1,\"MasterVolumePercent\":40,\"LanguageCode\":\"en\",\"MuteInBackground\":false}");
            SettingsData data = new SettingsService(new FileSaveService(_folder)).Data;
            Assert.AreEqual(40, data.MasterVolumePercent);
            Assert.IsFalse(data.MuteInBackground);
            Assert.IsTrue(data.VSync, "missing fields take their defaults");
            Assert.AreEqual(100, data.ScreenShakePercent);
            Assert.AreEqual((int)DisplayMode.Borderless, data.DisplayMode);
        }

        [Test]
        public void Resolutions_AreListedOncePerSize_SmallestFirst()
        {
            var offered = new List<Resolution>
            {
                Size(1920, 1080), Size(1280, 720), Size(1920, 1080), Size(2560, 1440), Size(1280, 720), Size(1280, 1024),
            };
            List<Vector2Int> sizes = DisplayOptions.Resolutions(offered);
            CollectionAssert.AreEqual(new[] { new Vector2Int(1280, 720), new Vector2Int(1280, 1024), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) }, sizes);
            Assert.AreEqual(2, DisplayOptions.IndexOf(sizes, new Vector2Int(1920, 1080)));
            Assert.AreEqual(2, DisplayOptions.IndexOf(sizes, new Vector2Int(1900, 1100)), "a missing size picks the closest pixel count");
            Assert.AreEqual(-1, DisplayOptions.IndexOf(new List<Vector2Int>(), new Vector2Int(1, 1)));
            Assert.AreEqual(0, DisplayOptions.Resolutions(null).Count);
        }

        [Test]
        public void FrameCaps_AndModes_MapBothWays()
        {
            Assert.AreEqual(1, DisplayOptions.CapIndex(60));
            Assert.AreEqual(DisplayOptions.FrameRateCaps.Length - 1, DisplayOptions.CapIndex(0));
            Assert.AreEqual(DisplayOptions.FrameRateCaps.Length - 1, DisplayOptions.CapIndex(77), "an unknown cap reads as no cap");
            foreach (DisplayMode mode in new[] { DisplayMode.Fullscreen, DisplayMode.Borderless, DisplayMode.Windowed })
            {
                Assert.AreEqual(mode, DisplayOptions.FromFullScreenMode(DisplayOptions.ToFullScreenMode(mode)));
            }

            Assert.AreEqual(FullScreenMode.FullScreenWindow, DisplayOptions.ToFullScreenMode(DisplayMode.Borderless));
        }

        [Test]
        public void PcLowProfile_TakesTheLowQualitySwitches_AndKeepsThePcBehaviour()
        {
            var low = new PcPlatformSettings(true);
            var high = new PcPlatformSettings();
            var console = new SwitchPlatformSettings();
            Assert.AreEqual(console.FogEnabled, low.FogEnabled);
            Assert.AreEqual(console.DepthOfFieldEnabled, low.DepthOfFieldEnabled);
            Assert.AreEqual(console.ParticleScale, low.ParticleScale);
            Assert.AreEqual(console.LowDetailModels, low.LowDetailModels);
            Assert.AreEqual(console.LodBias, low.LodBias);
            Assert.AreEqual(console.BloomScale, low.BloomScale);
            Assert.IsTrue(low.CanQuit);
            Assert.IsTrue(low.HasDisplayOptions);
            Assert.AreEqual(0f, low.SafeMarginPercent);
            Assert.IsTrue(high.FogEnabled);
            Assert.IsFalse(high.LowDetailModels);
            Assert.IsFalse(console.HasDisplayOptions, "a console offers no window or quality rows");
        }

        [Test]
        public void QualitySwitch_AppliesOnlyRealChanges()
        {
            var applied = new List<bool>();
            var quality = new QualityProfileSwitch(false, applied.Add);
            quality.Set(false);
            quality.Set(true);
            quality.Set(true);
            quality.Set(false);
            CollectionAssert.AreEqual(new[] { true, false }, applied);
        }

        private static Resolution Size(int width, int height) => new Resolution { width = width, height = height };
    }
}
