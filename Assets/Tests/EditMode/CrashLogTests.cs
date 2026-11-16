using System;
using System.IO;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 72: the local crash log (only exceptions, a header with version, scene, level and time, rotation past the
    /// size limit, a rate limit that counts what it leaves out), the version text, the smoke-test gate and the frame-time
    /// summary of the smoke test.
    /// </summary>
    public sealed class CrashLogTests
    {
        private string _folder;
        private double _now;

        [SetUp]
        public void CreateFolder()
        {
            _folder = Path.Combine(Path.GetTempPath(), "td_crashlog_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            _now = 0.0;
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        private CrashLog NewLog(long maxBytes = CrashLog.DefaultMaxBytes) =>
            new CrashLog(_folder, () => _now, () => new CrashContext("1.2.3 dev", "Level", "C1_01 / Easy"), maxBytes);

        private string LogText() => File.ReadAllText(Path.Combine(_folder, CrashLog.FileName));

        [TestCase(LogType.Log)]
        [TestCase(LogType.Warning)]
        [TestCase(LogType.Error)]
        [TestCase(LogType.Assert)]
        public void OtherLogTypes_WriteNothing(LogType type)
        {
            NewLog().Handle("something", "stack", type);

            Assert.IsFalse(File.Exists(Path.Combine(_folder, CrashLog.FileName)));
        }

        [Test]
        public void AnException_IsWrittenWithVersionSceneLevelTimeAndStack()
        {
            NewLog().Handle("NullReferenceException: boom", "Game.Towers.Tower.Update ()", LogType.Exception);

            string text = LogText();
            StringAssert.Contains("version 1.2.3 dev", text);
            StringAssert.Contains("scene Level", text);
            StringAssert.Contains("level C1_01 / Easy", text);
            StringAssert.Contains("NullReferenceException: boom", text);
            StringAssert.Contains("Game.Towers.Tower.Update ()", text);
            StringAssert.IsMatch(@"=== \d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}Z", text);
        }

        [Test]
        public void Format_ShowsADashForAMissingSceneOrLevel()
        {
            string entry = CrashLog.Format(new CrashContext("0.1.0", null, ""), new DateTime(2026, 11, 16, 8, 0, 0, DateTimeKind.Utc), "E", null, 0);

            StringAssert.StartsWith("=== 2026-11-16 08:00:00Z | version 0.1.0 | scene - | level -\n", entry);
        }

        [Test]
        public void ABurst_IsCutAtTheLimit_AndTheNextEntryCountsWhatWasLeftOut()
        {
            CrashLog log = NewLog();
            for (int i = 0; i < CrashLog.BurstLimit + 5; i++)
            {
                log.Handle("Exception " + i, "s", LogType.Exception);
            }

            Assert.AreEqual(5, log.Suppressed);
            StringAssert.DoesNotContain("Exception " + CrashLog.BurstLimit, LogText());

            _now = CrashLog.WindowSeconds + 1.0;
            log.Handle("Exception later", "s", LogType.Exception);

            StringAssert.Contains("(5 earlier exceptions not written: rate limit)", LogText());
            StringAssert.Contains("Exception later", LogText());
            Assert.AreEqual(0, log.Suppressed);
        }

        [Test]
        public void AFullFile_IsRotated_KeepingOnePreviousFile()
        {
            CrashLog log = NewLog(maxBytes: 200);
            string big = new string('x', 300);
            log.Handle("first " + big, "s", LogType.Exception);
            log.Handle("second", "s", LogType.Exception);
            log.Handle("third " + big, "s", LogType.Exception);
            log.Handle("fourth", "s", LogType.Exception);

            string current = LogText();
            string previous = File.ReadAllText(Path.Combine(_folder, CrashLog.PreviousFileName));
            StringAssert.Contains("fourth", current);
            StringAssert.DoesNotContain("third", current);
            StringAssert.Contains("third", previous);
            StringAssert.DoesNotContain("first", previous);
        }

        [Test]
        public void AFolderThatCannotBeWritten_IsSwallowed()
        {
            // A file where the folder should be: the write fails, the handler must not throw.
            string blocked = Path.Combine(_folder, "blocked");
            File.WriteAllText(blocked, "x");
            var log = new CrashLog(blocked, () => 0.0, () => new CrashContext("v", "s", "l"));

            Assert.DoesNotThrow(() => log.Handle("Exception", "s", LogType.Exception));
        }

        [TestCase("0.1.0", false, false, false, "0.1.0")]
        [TestCase("0.1.0", true, false, false, "0.1.0 dev")]
        [TestCase("0.1.0", true, true, false, "0.1.0 editor")]
        [TestCase("0.1.0", false, false, true, "0.1.0 il2cpp")]
        [TestCase("", false, false, false, "0")]
        public void BuildVersion_SaysWhatKindOfBuild(string version, bool development, bool editor, bool il2Cpp, string expected)
        {
            Assert.AreEqual(expected, BuildVersion.Describe(version, development, editor, il2Cpp));
        }

        [Test]
        public void MenuVersionText_HasAnEnglishTextWithTheNumber()
        {
            StringAssert.Contains("0.1.0", Loc.Format("menu.version", "0.1.0"));
        }

        [TestCase(new[] { "game.exe", "-autotest" }, false, false, false, false)]
        [TestCase(new[] { "game.exe", "-autotest" }, false, false, true, true)]
        [TestCase(new[] { "game.exe", "-AUTOTEST" }, false, true, false, true)]
        [TestCase(new[] { "game.exe", "-autotest-crashprobe" }, false, false, true, true)]
        [TestCase(new[] { "game.exe" }, true, true, true, false)]
        [TestCase(null, true, true, true, false)]
        public void SmokeTestGate_NeedsTheArgumentAndANonReleaseBuild(string[] arguments, bool editor, bool development, bool smokeBuild, bool expected)
        {
            Assert.AreEqual(expected, SmokeTestGate.Resolve(arguments, editor, development, smokeBuild));
        }

        [Test]
        public void SmokeTestGate_IsClosedInThisEditorCompile()
        {
            // The editor and every release build compile without TD_SMOKE_TEST; only the smoke build method adds it.
            Assert.IsFalse(SmokeTestGate.SmokeBuild);
        }

        [Test]
        public void FrameTimeSampler_SummarisesAndNeverGrows()
        {
            var sampler = new FrameTimeSampler(4);
            sampler.Add(0.010f);
            sampler.Add(0.020f);
            sampler.Add(0.010f);
            sampler.Add(0.040f);
            sampler.Add(1.000f);

            Assert.AreEqual(4, sampler.Count);
            Assert.AreEqual(50f, sampler.AverageFps, 0.01f);
            Assert.AreEqual(10f, sampler.PercentileMs(0.5f), 0.01f);
            Assert.AreEqual(40f, sampler.PercentileMs(0.99f), 0.01f);
            Assert.AreEqual(40f, sampler.WorstMs, 0.01f);
            StringAssert.Contains("4 frames", sampler.Summary());
        }
    }
}
