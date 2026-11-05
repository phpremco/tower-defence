using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The boss intro rules (Docs/BOSS_INTROS.md): the full beat only for a boss never seen before, never while the field
    /// is paused, held or over; barks under a per-boss cooldown and a gap between any two; and the intro data of every
    /// continent boss complete and short enough.
    /// </summary>
    public sealed class BossIntroRulesTests
    {
        private const string StoryPath = "Assets/_Project/Data/Story/Story.asset";
        private const string ArenaPath = "Assets/_Project/Data/Levels/BossArena.asset";

        [Test]
        public void MeetingFor_NeverSeen_IsTheFullBeat_SeenBefore_IsABark()
        {
            Assert.AreEqual(BossMeeting.First, BossIntroRules.MeetingFor(seenBefore: false));
            Assert.AreEqual(BossMeeting.Again, BossIntroRules.MeetingFor(seenBefore: true));
        }

        [Test]
        public void FieldIsLive_OnlyWhenRunningAndNeitherPausedHeldNorInACutscene()
        {
            Assert.IsTrue(BossIntroRules.FieldIsLive(false, false, false, LevelOutcome.Running));
            Assert.IsFalse(BossIntroRules.FieldIsLive(true, false, false, LevelOutcome.Running), "paused");
            Assert.IsFalse(BossIntroRules.FieldIsLive(false, true, false, LevelOutcome.Running), "held by a story box");
            Assert.IsFalse(BossIntroRules.FieldIsLive(false, false, true, LevelOutcome.Running), "in a cutscene");
            Assert.IsFalse(BossIntroRules.FieldIsLive(false, false, false, LevelOutcome.Won), "level over");
        }

        [Test]
        public void BarkCooldown_SameBoss_WaitsItsCooldown()
        {
            var cooldown = new BarkCooldown(gap: 5f);
            object boss = new object();
            Assert.IsTrue(cooldown.TryBark(boss, 12f, 0f));
            Assert.IsFalse(cooldown.TryBark(boss, 12f, 6f), "within the boss's cooldown");
            Assert.IsFalse(cooldown.TryBark(boss, 12f, 11.9f), "just before the cooldown ends");
            Assert.IsTrue(cooldown.TryBark(boss, 12f, 12f), "the cooldown is over");
        }

        [Test]
        public void BarkCooldown_AnyTwoBarks_KeepTheGap()
        {
            var cooldown = new BarkCooldown(gap: 5f);
            object first = new object();
            object second = new object();
            Assert.IsTrue(cooldown.TryBark(first, 12f, 0f));
            Assert.IsFalse(cooldown.TryBark(second, 12f, 4f), "another boss inside the gap");
            Assert.IsTrue(cooldown.Ready(second, 12f, 5f), "the gap is over");
            Assert.IsTrue(cooldown.TryBark(second, 12f, 5f));
        }

        [Test]
        public void BarkCooldown_Hold_DelaysTheFirstBarkAfterAnIntro()
        {
            var cooldown = new BarkCooldown(gap: 5f);
            object boss = new object();
            cooldown.Hold(boss, 10f);
            Assert.IsFalse(cooldown.TryBark(boss, 12f, 15f), "the intro just played");
            Assert.IsTrue(cooldown.TryBark(boss, 12f, 22f));
        }

        [Test]
        public void Pick_TakesALineByTheRoll_AndNothingFromAnEmptyPool()
        {
            var lines = new[] { "a", "b", "c" };
            Assert.AreEqual("a", BossIntroRules.Pick(lines, 0f));
            Assert.AreEqual("c", BossIntroRules.Pick(lines, 0.999f));
            Assert.IsNull(BossIntroRules.Pick(new string[0], 0.5f));
        }

        [Test]
        public void IntroDefinition_WithoutData_ReportsEveryGap()
        {
            var intro = ScriptableObject.CreateInstance<BossIntroDefinition>();
            var errors = new List<string>();
            Assert.IsFalse(intro.Validate(errors));
            Assert.GreaterOrEqual(errors.Count, 4, string.Join(" ", errors));
            Object.DestroyImmediate(intro);
        }

        [Test]
        public void EveryContinentBoss_HasACompleteIntro_UnderSixSeconds()
        {
            var story = AssetDatabase.LoadAssetAtPath<StoryDefinition>(StoryPath);
            var arena = AssetDatabase.LoadAssetAtPath<BossArenaDefinition>(ArenaPath);
            Assert.IsNotNull(story, "no story asset");
            Assert.IsNotNull(arena, "no boss arena asset");
            foreach (EnemyDefinition boss in arena.Bosses)
            {
                BossIntroDefinition intro = story.IntroFor(boss);
                Assert.IsNotNull(intro, $"{boss.name} has no intro");
                var errors = new List<string>();
                Assert.IsTrue(intro.Validate(errors), $"{boss.name}: {string.Join(" ", errors)}");
                Assert.LessOrEqual(intro.MaxBeatSeconds, BossIntroDefinition.MaxBeatLimit, $"{boss.name}: beat too long");
                Assert.GreaterOrEqual(intro.Barks.Count, 2, $"{boss.name}: fewer than two mid-fight barks");
                Assert.AreEqual(2, intro.Answer.Lines.Count, $"{boss.name}: the answer is ORA then Voss");
            }
        }
    }
}
