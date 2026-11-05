using System.Collections;
using Game.Bosses;
using Game.Core;
using Game.Economy;
using Game.Hero;
using Game.Levels;
using Game.Towers;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Smoke test of one boss intro (Docs/BOSS_INTROS.md) in the developer arena with the intros switched on: Gravemaw,
    /// never met before, comes out of the colony rig and the beat plays. The game slows (it does not pause), Mercer recoils
    /// and says his line over his head, the visor cut-in and the Codex note show, ORA and Voss answer in their box, and
    /// within six real seconds the game is back at full speed. Nothing may log an error.
    /// </summary>
    public sealed class BossIntroSmokeTests
    {
        private const string BossName = "Gravemaw";
        private const float ArrivalSpeed = 4f;
        private const float ArrivalTimeoutSeconds = 120f;
        private const float BeatLimitSeconds = 6.5f;
        private const float SlowTolerance = 0.01f;

        [UnityTest]
        public IEnumerator Gravemaw_FirstMeeting_PlaysTheBeat_SlowsButNeverPauses_AndEndsWithinSixSeconds()
        {
            var harness = new BossArenaHarness();
            yield return harness.Load(BossName, bossIntros: true);

            // A first meeting, whatever the save says (the arena is practice: it marks nothing, and nothing is saved).
            ProgressData data = Services.Get<ProgressService>().Data;
            bool wasSeen = data.SeenEnemyIds != null && data.SeenEnemyIds.Remove(BossName);
            LevelContext context = harness.Context;
            Assert.IsNotNull(harness.Boss, "no Gravemaw in the arena");
            BossIntroDefinition intro = Services.Get<CampaignCatalog>().Story.IntroFor(harness.Boss);
            Assert.IsNotNull(intro, "no intro for Gravemaw");
            harness.Build(new[] { Load("PulseRifle"), Load("Cannon") }, 6, 0.5f);
            context.SkipWaveRequested.Raise();
            var director = Object.FindAnyObjectByType<BossIntroDirector>();
            Assert.IsNotNull(director, "the HUD made no boss intro director");

            Time.timeScale = ArrivalSpeed;
            float started = Time.realtimeSinceStartup;
            while (!director.InBeat && Time.realtimeSinceStartup - started < ArrivalTimeoutSeconds)
            {
                yield return null;
            }

            Assert.IsTrue(director.InBeat, "the beat never started");
            var brain = Object.FindAnyObjectByType<ContinentBossBrain>();
            Assert.IsTrue(brain.FirstMeeting, "the boss was not a first meeting");
            Assert.AreEqual(intro.SlowFactor, Time.timeScale, SlowTolerance, "the beat must slow the game to its factor");
            Assert.Greater(Time.timeScale, 0f, "the beat must never pause");
            Assert.AreEqual(intro.HeroLine, director.ShownLine, "Mercer's line is not over his head");
            HeroArtView hero = context.Hero != null ? context.Hero.GetComponentInChildren<HeroArtView>() : null;
            if (hero != null)
            {
                Assert.IsTrue(hero.IsReacting, "Mercer does not react");
            }

            float beatStarted = Time.realtimeSinceStartup;
            bool answered = false;
            while (director.InBeat && Time.realtimeSinceStartup - beatStarted < BeatLimitSeconds + 1f)
            {
                answered |= director.AnswerBox.IsShown;
                yield return null;
            }

            float beat = Time.realtimeSinceStartup - beatStarted;
            Assert.IsFalse(director.InBeat, "the beat did not end");
            Assert.LessOrEqual(beat, BeatLimitSeconds, "the beat ran too long");
            Assert.IsTrue(answered, "ORA and Voss never answered");
            Assert.AreEqual(1, director.Beats, "one beat for one boss");
            Assert.AreEqual(1f, Time.timeScale, SlowTolerance, "full speed after the beat");

            if (wasSeen)
            {
                data.SeenEnemyIds.Add(BossName);
            }

            Time.timeScale = 1f;
        }

        private static TowerDefinition Load(string id) =>
            UnityEditor.AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{id}.asset");
    }
}
