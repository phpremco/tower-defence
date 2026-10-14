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
        private EnemyDefinition _enemy;
        private WaveDefinition _wave;
        private DifficultyProfile _difficulty;

        [SetUp]
        public void SetUp()
        {
            _enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            _difficulty = ScriptableObject.CreateInstance<DifficultyProfile>();
            _wave = ScriptableObject.CreateInstance<WaveDefinition>();

            var so = new SerializedObject(_wave);
            SerializedProperty groups = so.FindProperty("_groups");
            groups.arraySize = 1;
            SerializedProperty group = groups.GetArrayElementAtIndex(0);
            group.FindPropertyRelative("Enemy").objectReferenceValue = _enemy;
            group.FindPropertyRelative("Count").intValue = 3;
            group.FindPropertyRelative("Interval").floatValue = 1f;
            group.FindPropertyRelative("Delay").floatValue = 2f;
            group.FindPropertyRelative("PathIndex").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemy);
            Object.DestroyImmediate(_wave);
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

