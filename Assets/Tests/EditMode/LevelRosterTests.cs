using System.Collections.Generic;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>The in-level codex lists only the creatures a level's waves send (round 23a).</summary>
    public sealed class LevelRosterTests
    {
        private const string LevelPath = "Assets/_Project/Data/Levels/Level_C1_01.asset";
        private const string CodexPath = "Assets/_Project/Data/Levels/CodexCatalog.asset";
        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        private EnemyDefinition Enemy(string id, bool boss = false, EnemyDefinition minion = null)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            enemy.name = id;
            _made.Add(enemy);
            var data = new SerializedObject(enemy);
            data.FindProperty("_isBoss").boolValue = boss;
            if (minion != null)
            {
                var ability = ScriptableObject.CreateInstance<BossAbilityDefinition>();
                _made.Add(ability);
                var abilityData = new SerializedObject(ability);
                abilityData.FindProperty("_minion").objectReferenceValue = minion;
                abilityData.ApplyModifiedPropertiesWithoutUndo();
                SerializedProperty abilities = data.FindProperty("_abilities");
                abilities.arraySize = 1;
                abilities.GetArrayElementAtIndex(0).objectReferenceValue = ability;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }

        private static WaveDefinition Wave(params EnemyDefinition[] enemies)
        {
            var groups = new List<SpawnGroup>();
            foreach (EnemyDefinition enemy in enemies)
            {
                groups.Add(new SpawnGroup { Enemy = enemy, Count = 1, Interval = 1f });
            }

            return new WaveDefinition(groups);
        }

        [Test]
        public void AddFrom_TakesEveryWaveEnemy_AndTheMinionsOfItsBosses()
        {
            EnemyDefinition runner = Enemy("Runner");
            EnemyDefinition hatchling = Enemy("Hatchling");
            EnemyDefinition queen = Enemy("Queen", true, hatchling);
            Enemy("NeverSent");

            var roster = new HashSet<EnemyDefinition>();
            LevelRoster.AddFrom(new[] { Wave(runner, runner), Wave(queen) }, roster);

            CollectionAssert.AreEquivalent(new[] { runner, queen, hatchling }, roster);
        }

        [Test]
        public void Filter_KeepsCatalogOrder_DropsAbsentCreatures_AndUnmetBosses()
        {
            EnemyDefinition a = Enemy("A");
            EnemyDefinition b = Enemy("B");
            EnemyDefinition c = Enemy("C");
            EnemyDefinition boss = Enemy("Boss", true);
            EnemyDefinition metBoss = Enemy("MetBoss", true);
            var catalog = new List<EnemyDefinition> { c, a, boss, b, metBoss };
            var present = new HashSet<EnemyDefinition> { a, c, boss, metBoss };
            var shown = new List<EnemyDefinition>();

            LevelRoster.Filter(catalog, present, enemy => enemy == metBoss, shown);

            CollectionAssert.AreEqual(new[] { c, a, metBoss }, shown);
        }

        [Test]
        public void Filter_WithoutALevel_ListsEveryCreature()
        {
            EnemyDefinition a = Enemy("A");
            EnemyDefinition b = Enemy("B");
            var shown = new List<EnemyDefinition>();

            LevelRoster.Filter(new List<EnemyDefinition> { a, b }, null, null, shown);

            CollectionAssert.AreEqual(new[] { a, b }, shown);
        }

        [Test]
        public void FirstLevel_ListsOnlyItsOwnCreatures_FromTheCodex()
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
            var codex = AssetDatabase.LoadAssetAtPath<CodexCatalog>(CodexPath);
            Assert.IsNotNull(level);
            Assert.IsNotNull(codex);

            var roster = new HashSet<EnemyDefinition>();
            foreach (DifficultyWaves set in level.DifficultyWaves)
            {
                LevelRoster.Collect(level, set.Difficulty, roster);
                var shown = new List<EnemyDefinition>();
                LevelRoster.Filter(codex.Enemies, roster, null, shown);
                Assert.IsNotEmpty(shown, set.Difficulty.name);
                Assert.Less(shown.Count, codex.Enemies.Count, $"{set.Difficulty.name}: the first level should not list every creature");
                foreach (EnemyDefinition enemy in shown)
                {
                    Assert.IsTrue(roster.Contains(enemy), enemy.name);
                }
            }
        }
    }
}
