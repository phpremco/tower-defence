using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class StoryTriggerSelectorTests
    {
        private StoryTestData _data;
        private DialogueDefinition _a;
        private DialogueDefinition _b;
        private readonly List<DialogueDefinition> _results = new List<DialogueDefinition>();
        private readonly HashSet<string> _seen = new HashSet<string>();

        [SetUp]
        public void SetUp()
        {
            _data = new StoryTestData();
            _a = _data.Dialogue("Dlg_A", "a");
            _b = _data.Dialogue("Dlg_B", "b");
            _results.Clear();
            _seen.Clear();
        }

        [TearDown]
        public void TearDown() => _data.Dispose();

        private int Select(StoryTrigger[] triggers, StoryEvent evt, bool[] fired = null)
        {
            return StoryTriggerSelector.Select(triggers, evt, _seen.Contains, fired ?? new bool[triggers.Length], _results);
        }

        private static StoryTrigger Trigger(StoryTriggerKind kind, DialogueDefinition dialogue, int wave = 0, EnemyDefinition enemy = null, StoryRepeat repeat = StoryRepeat.OncePerSave)
        {
            return new StoryTrigger { Kind = kind, Dialogue = dialogue, Wave = wave, Enemy = enemy, Repeat = repeat };
        }

        [Test]
        public void EachKind_MatchesOnlyItsOwnEvent()
        {
            EnemyDefinition enemy = _data.Enemy("Scout");
            var events = new[]
            {
                StoryEvent.LevelStart(), StoryEvent.WaveStart(1), StoryEvent.FirstSighting(enemy),
                StoryEvent.BossSpawn(enemy), StoryEvent.BossDefeated(enemy), StoryEvent.LevelWon(),
            };

            foreach (StoryEvent trigger in events)
            {
                foreach (StoryEvent evt in events)
                {
                    var t = Trigger(trigger.Kind, _a, 1, enemy);
                    Assert.AreEqual(trigger.Kind == evt.Kind, StoryTriggerSelector.Matches(t, evt), $"{trigger.Kind} against {evt.Kind}");
                }
            }
        }

        [Test]
        public void FirstUse_MatchesOnlyItsPathObject()
        {
            var mine = UnityEngine.ScriptableObject.CreateInstance<Game.PathObjects.PathObjectDefinition>();
            var field = UnityEngine.ScriptableObject.CreateInstance<Game.PathObjects.PathObjectDefinition>();
            try
            {
                var trigger = new StoryTrigger { Kind = StoryTriggerKind.FirstUse, Dialogue = _a, PathObject = mine };
                Assert.IsTrue(StoryTriggerSelector.Matches(trigger, StoryEvent.FirstUse(mine)));
                Assert.IsFalse(StoryTriggerSelector.Matches(trigger, StoryEvent.FirstUse(field)));
                Assert.IsFalse(StoryTriggerSelector.Matches(trigger, StoryEvent.LevelStart()));
                Assert.IsNotNull(new StoryTrigger { Kind = StoryTriggerKind.FirstUse, Dialogue = _a }.Problem(), "a first use needs its path object");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mine);
                UnityEngine.Object.DestroyImmediate(field);
            }
        }

        [Test]
        public void WaveStart_MatchesExactlyItsWave()
        {
            var triggers = new[] { Trigger(StoryTriggerKind.WaveStart, _a, 2) };

            Assert.AreEqual(0, Select(triggers, StoryEvent.WaveStart(1)));
            Assert.AreEqual(0, Select(triggers, StoryEvent.WaveStart(3)));
            Assert.AreEqual(1, Select(triggers, StoryEvent.WaveStart(2)));
        }

        [Test]
        public void TwoTriggersOnOneEvent_KeepTheirOrder()
        {
            var triggers = new[] { Trigger(StoryTriggerKind.LevelStart, _b), Trigger(StoryTriggerKind.LevelStart, _a) };

            Assert.AreEqual(2, Select(triggers, StoryEvent.LevelStart()));
            CollectionAssert.AreEqual(new[] { _b, _a }, _results);
        }

        [Test]
        public void OncePerSave_IsSkippedWhenSeen()
        {
            _seen.Add(_a.Id);
            var triggers = new[] { Trigger(StoryTriggerKind.LevelStart, _a) };

            Assert.AreEqual(0, Select(triggers, StoryEvent.LevelStart()));
        }

        [Test]
        public void Always_IgnoresSeen()
        {
            _seen.Add(_a.Id);
            var triggers = new[] { Trigger(StoryTriggerKind.LevelStart, _a, repeat: StoryRepeat.Always) };

            Assert.AreEqual(1, Select(triggers, StoryEvent.LevelStart()));
        }

        [Test]
        public void ATrigger_FiresAtMostOncePerRun()
        {
            var triggers = new[] { Trigger(StoryTriggerKind.LevelStart, _a, repeat: StoryRepeat.Always) };
            var fired = new bool[1];

            Assert.AreEqual(1, Select(triggers, StoryEvent.LevelStart(), fired));
            Assert.AreEqual(0, Select(triggers, StoryEvent.LevelStart(), fired));
            Assert.IsTrue(fired[0]);
        }

        [Test]
        public void FirstSighting_OfAnotherEnemy_DoesNotMatch()
        {
            var triggers = new[] { Trigger(StoryTriggerKind.FirstSighting, _a, enemy: _data.Enemy("Scout")) };

            Assert.AreEqual(0, Select(triggers, StoryEvent.FirstSighting(_data.Enemy("Grunt"))));
        }

        [Test]
        public void BossTrigger_WithoutEnemy_MatchesAnyBoss_AndWithEnemyOnlyThatOne()
        {
            EnemyDefinition queen = _data.Enemy("Queen");
            var any = Trigger(StoryTriggerKind.BossSpawn, _a);
            var only = Trigger(StoryTriggerKind.BossSpawn, _a, enemy: queen);

            Assert.IsTrue(StoryTriggerSelector.Matches(any, StoryEvent.BossSpawn(_data.Enemy("Other"))));
            Assert.IsTrue(StoryTriggerSelector.Matches(only, StoryEvent.BossSpawn(queen)));
            Assert.IsFalse(StoryTriggerSelector.Matches(only, StoryEvent.BossSpawn(_data.Enemy("Other"))));
        }

        [Test]
        public void TriggerProblems_NameTheMissingField()
        {
            Assert.IsNull(Trigger(StoryTriggerKind.LevelStart, _a).Problem());
            Assert.IsNotNull(Trigger(StoryTriggerKind.LevelStart, null).Problem(), "no dialogue");
            Assert.IsNotNull(Trigger(StoryTriggerKind.WaveStart, _a, 0).Problem(), "no wave number");
            Assert.IsNotNull(Trigger(StoryTriggerKind.FirstSighting, _a).Problem(), "no creature");
        }
    }
}
