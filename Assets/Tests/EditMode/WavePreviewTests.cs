using System.Collections.Generic;
using System.Linq;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>What the next-wave preview lists for a wave (round 78): types, numbers, marks, order, and what it leaves out.</summary>
    public sealed class WavePreviewTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const int HudPlaces = 6;
        private readonly List<Object> _made = new List<Object>();
        private readonly List<WavePreviewEntry> _entries = new List<WavePreviewEntry>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
            _entries.Clear();
        }

        private EnemyDefinition Enemy(string id, bool flies = false, float armour = 0f, bool boss = false)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            enemy.name = id;
            _made.Add(enemy);
            var data = new SerializedObject(enemy);
            data.FindProperty("_flies").boolValue = flies;
            data.FindProperty("_armor").floatValue = armour;
            data.FindProperty("_isBoss").boolValue = boss;
            data.ApplyModifiedPropertiesWithoutUndo();
            return enemy;
        }

        private DifficultyProfile Difficulty(float countMultiplier = 1f)
        {
            var difficulty = ScriptableObject.CreateInstance<DifficultyProfile>();
            _made.Add(difficulty);
            var data = new SerializedObject(difficulty);
            data.FindProperty("_countMultiplier").floatValue = countMultiplier;
            data.ApplyModifiedPropertiesWithoutUndo();
            return difficulty;
        }

        private static SpawnGroup Group(EnemyDefinition enemy, int count, SpawnSource source = default, int path = 0, int nest = 0) =>
            new SpawnGroup { Enemy = enemy, Count = count, Interval = 1f, Source = source, PathIndex = path, Nest = nest };

        [Test]
        public void Collect_AddsUpTheGroupsOfOneCreature_WhateverTheirEntrance()
        {
            EnemyDefinition runner = Enemy("Runner");
            var wave = new WaveDefinition(new[] { Group(runner, 5), Group(runner, 3, path: 1), Group(runner, 2, SpawnSource.Nest, nest: 2) });

            WavePreview.Collect(wave, Difficulty(), _entries);

            Assert.AreEqual(1, _entries.Count, "one creature type is one entry: the entrance (a road, a rift) is not part of the preview");
            Assert.AreSame(runner, _entries[0].Enemy);
            Assert.AreEqual(10, _entries[0].Count);
        }

        [Test]
        public void Collect_CountsAsTheWaveSpawns_OnTheDifficulty()
        {
            EnemyDefinition runner = Enemy("Runner");
            EnemyDefinition queen = Enemy("Queen", boss: true);
            DifficultyProfile hard = Difficulty(1.5f);
            var wave = new WaveDefinition(new[] { Group(runner, 5), Group(runner, 4), Group(queen, 1) });

            WavePreview.Collect(wave, hard, _entries);

            var scheduler = new WaveScheduler(wave, hard);
            int spawned = 0;
            for (int g = 0; g < scheduler.GroupCount; g++)
            {
                spawned += scheduler.Total(g);
            }

            int listed = 0;
            foreach (WavePreviewEntry entry in _entries)
            {
                listed += entry.Count;
            }

            Assert.AreEqual(spawned, listed, "the preview promises exactly what the wave sends");
            Assert.AreEqual(1, _entries[0].Count, "a boss is one creature on every difficulty");
            Assert.AreEqual(8 + 6, _entries[1].Count, "each group is rounded up as the scheduler does it (5 x 1.5 -> 8, 4 x 1.5 -> 6)");
        }

        [Test]
        public void Collect_RanksBossThenFlyersThenPlated_ThenByNumber()
        {
            EnemyDefinition crowd = Enemy("Crowd");
            EnemyDefinition few = Enemy("Few");
            EnemyDefinition plated = Enemy("Plated", armour: 40f);
            EnemyDefinition flyer = Enemy("Flyer", flies: true);
            EnemyDefinition boss = Enemy("Boss", boss: true);
            var wave = new WaveDefinition(new[] { Group(few, 2), Group(crowd, 20), Group(plated, 3), Group(flyer, 4), Group(boss, 1) });

            WavePreview.Collect(wave, Difficulty(), _entries);

            CollectionAssert.AreEqual(new[] { boss, flyer, plated, crowd, few }, _entries.ConvertAll(e => e.Enemy));
            Assert.IsTrue(_entries[0].Boss);
            Assert.IsTrue(_entries[1].Flies && !_entries[1].Armoured);
            Assert.IsTrue(_entries[2].Armoured && !_entries[2].Flies);
            Assert.IsFalse(_entries[3].Flies || _entries[3].Armoured || _entries[3].Boss);
        }

        [Test]
        public void Collect_KeepsTheWavesOrder_BetweenEquals()
        {
            EnemyDefinition first = Enemy("First");
            EnemyDefinition second = Enemy("Second");
            var wave = new WaveDefinition(new[] { Group(first, 6), Group(second, 6) });

            WavePreview.Collect(wave, Difficulty(), _entries);

            CollectionAssert.AreEqual(new[] { first, second }, _entries.ConvertAll(e => e.Enemy));
        }

        [Test]
        public void Collect_LeavesOutABossesMinions()
        {
            EnemyDefinition hatchling = Enemy("Hatchling");
            EnemyDefinition queen = Enemy("Queen", boss: true);
            var ability = ScriptableObject.CreateInstance<BossAbilityDefinition>();
            _made.Add(ability);
            var abilityData = new SerializedObject(ability);
            abilityData.FindProperty("_minion").objectReferenceValue = hatchling;
            abilityData.ApplyModifiedPropertiesWithoutUndo();
            var data = new SerializedObject(queen);
            SerializedProperty abilities = data.FindProperty("_abilities");
            abilities.arraySize = 1;
            abilities.GetArrayElementAtIndex(0).objectReferenceValue = ability;
            data.ApplyModifiedPropertiesWithoutUndo();

            WavePreview.Collect(new WaveDefinition(new[] { Group(queen, 1) }), Difficulty(), _entries);

            Assert.AreEqual(1, _entries.Count, "what a boss calls in during the fight stays a surprise");
        }

        [Test]
        public void Collect_NoWave_ListsNothing()
        {
            _entries.Add(new WavePreviewEntry(Enemy("Stale"), 1));

            WavePreview.Collect(null, Difficulty(), _entries);

            Assert.AreEqual(0, _entries.Count, "after the last wave the preview is empty, and an old list is cleared");
        }

        [Test]
        public void Collect_SkipsAGroupWithoutAnEnemy()
        {
            EnemyDefinition runner = Enemy("Runner");

            WavePreview.Collect(new WaveDefinition(new[] { Group(null, 4), Group(runner, 2) }), Difficulty(), _entries);

            Assert.AreEqual(1, _entries.Count);
        }

        [Test]
        public void FlyersAndPlated_AddUpTheMarkedCreatures()
        {
            EnemyDefinition flyer = Enemy("Flyer", flies: true);
            EnemyDefinition platedFlyer = Enemy("PlatedFlyer", flies: true, armour: 10f);
            EnemyDefinition plated = Enemy("Plated", armour: 40f);
            var wave = new WaveDefinition(new[] { Group(flyer, 4), Group(platedFlyer, 2), Group(plated, 3), Group(Enemy("Plain"), 9) });

            WavePreview.Collect(wave, Difficulty(), _entries);

            Assert.AreEqual(6, WavePreview.Flyers(_entries));
            Assert.AreEqual(5, WavePreview.Plated(_entries));
        }

        [TestCase(3, 6, 3, 0)]
        [TestCase(6, 6, 6, 0)]
        [TestCase(7, 6, 5, 2)]
        [TestCase(4, 2, 1, 3)]
        [TestCase(4, 0, 0, 4)]
        public void Shown_FoldsTheTailIntoOnePlace_WhenTheStripIsTooNarrow(int types, int places, int expectedShown, int expectedHidden)
        {
            for (int i = 0; i < types; i++)
            {
                _entries.Add(new WavePreviewEntry(Enemy("E" + i), 1));
            }

            int shown = WavePreview.Shown(_entries, places, out int hidden);

            Assert.AreEqual(expectedShown, shown);
            Assert.AreEqual(expectedHidden, hidden);
        }

        // The HUD has room for six chips beside the wave line at the default margin; a wave that needs more would fold
        // creatures into "+N". The data must not ask for that silently.
        [Test]
        public void FirstContinent_NoWaveListsMoreTypesThanTheHudHasPlaces_AndEveryMarkedTypeComesFirst()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, CatalogPath);
            int waves = 0;
            foreach (LevelDefinition level in AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/_Project/Data/Levels" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<LevelDefinition>))
            {
                foreach (DifficultyProfile difficulty in catalog.Difficulties)
                {
                    WaveDefinition[] set = level.GetWaves(difficulty);
                    for (int w = 0; set != null && w < set.Length; w++)
                    {
                        WavePreview.Collect(set[w], difficulty, _entries);
                        waves++;
                        Assert.Greater(_entries.Count, 0, $"{level.name} {difficulty.name} wave {w + 1} lists nothing");
                        Assert.LessOrEqual(_entries.Count, HudPlaces, $"{level.name} {difficulty.name} wave {w + 1}");
                        bool plainSeen = false;
                        foreach (WavePreviewEntry entry in _entries)
                        {
                            bool marked = entry.Boss || entry.Flies || entry.Armoured;
                            Assert.IsFalse(marked && plainSeen, $"{level.name} {difficulty.name} wave {w + 1}: a marked creature after a plain one");
                            plainSeen |= !marked;
                        }
                    }
                }
            }

            Assert.Greater(waves, 0);
        }
    }
}
