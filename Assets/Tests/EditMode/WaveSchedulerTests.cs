using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class WaveSchedulerTests
    {
        private const float TestJitter = 0.5f;

        private EnemyDefinition _enemy;
        private SpawnGroup _group;
        private WaveDefinition _wave;
        private DifficultyProfile _difficulty;

        [SetUp]
        public void SetUp()
        {
            _enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            _difficulty = ScriptableObject.CreateInstance<DifficultyProfile>();
            _group = new SpawnGroup { Enemy = _enemy, Count = 3, Interval = 1f, Delay = 2f, PathIndex = 3 };
            _wave = new WaveDefinition(new[] { _group });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemy);
            Object.DestroyImmediate(_difficulty);
        }

        [Test]
        public void Advance_SpawnsOnSchedule()
        {
            var scheduler = new WaveScheduler(_wave, _difficulty);
            var output = new List<SpawnRequest>();

            scheduler.Advance(1.9f, output);
            Assert.AreEqual(0, output.Count);

            scheduler.Advance(0.2f, output);
            Assert.AreEqual(1, output.Count);

            scheduler.Advance(2f, output);
            Assert.AreEqual(3, output.Count);
            Assert.IsTrue(scheduler.IsFinished);
            Assert.AreEqual(3, output[0].PathIndex);
        }

        [Test]
        public void Advance_WithJitter_SpawnsEveryoneNearItsSchedule()
        {
            var wave = new WaveDefinition(new[] { _group }, TestJitter);
            var scheduler = new WaveScheduler(wave, _difficulty, new System.Random(7));
            var output = new List<SpawnRequest>();

            // Nobody appears earlier than half an interval before the delay, and everyone within half an interval after the last slot.
            scheduler.Advance(1.4f, output);
            Assert.AreEqual(0, output.Count);

            scheduler.Advance(3.2f, output);
            Assert.AreEqual(3, output.Count);
            Assert.IsTrue(scheduler.IsFinished);
        }

        [Test]
        public void Advance_AppliesCountMultiplier()
        {
            var so = new SerializedObject(_difficulty);
            so.FindProperty("_countMultiplier").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            var scheduler = new WaveScheduler(_wave, _difficulty);
            var output = new List<SpawnRequest>();

            scheduler.Advance(1000f, output);

            Assert.AreEqual(6, output.Count);
        }
    }
}

