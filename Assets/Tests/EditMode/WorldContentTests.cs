using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.PathObjects;
using Game.Platform;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Checks the first world as a whole (five levels, rewards, recipes) and the rules of the later systems.</summary>
    public sealed class WorldContentTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

        private sealed class MemorySave : ISaveService
        {
            public ProgressData Stored;

            public void Save<T>(string key, T data) => Stored = data as ProgressData;

            public bool TryLoad<T>(string key, out T data)
            {
                data = (T)(object)Stored;
                return Stored != null;
            }
        }

        private sealed class FakeTarget : ITargetable
        {
            public Vector3 Position { get; set; }

            public TargetLayers Layer { get; set; } = TargetLayers.Ground;

            public float PathProgress => 0f;

            public bool IsAlive => true;

            public float Damage { get; private set; }

            public float SlowFactor { get; private set; } = 1f;

            public void TakeDamage(float amount) => Damage += amount;

            public void ApplySlow(float speedFactor, float seconds) => SlowFactor = speedFactor;
        }

        private sealed class FakeSupport : IStructure, ISupportSource
        {
            public Vector3 Position { get; set; }

            public bool IsAlive => true;

            public bool BlocksGround => false;
            public bool IsBase => false;

            public float HealthFraction => 1f;

            public float SupportRadius { get; set; }

            public float RangeBonus { get; set; }

            public float RateBonus { get; set; }

            public void TakeDamage(float amount)
            {
            }

            public void Repair(float amount)
            {
            }
        }

        private static CampaignCatalog LoadCampaign()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            return catalog;
        }

        private const int FirstRecipeLevel = 4;
        private const int FirstWorldLevels = 5;

        // Docs/LEVEL_GUIDE.md: a boss that comes up away from the other spawns is a front of its own, which the first
        // world never asks for.
        [Test]
        public void FirstWorld_BossesComeThroughTheMainGate()
        {
            CampaignCatalog catalog = LoadCampaign();
            for (int i = 0; i < FirstWorldLevels && i < catalog.Nodes.Count; i++)
            {
                LevelDefinition level = catalog.Nodes[i].Level;
                Assert.AreEqual(EntranceKind.Gate, level.BossEntrance.Kind, $"{level.name}: bosses must use the main gate in the first world.");
                foreach (DifficultyWaves set in level.DifficultyWaves)
                {
                    foreach (WaveDefinition wave in set.Waves)
                    {
                        foreach (SpawnGroup group in wave.Groups)
                        {
                            if (group.Enemy != null && group.Enemy.IsBoss)
                            {
                                Assert.AreEqual(SpawnSource.Gate, group.Source, $"{wave.name}: a first-world boss must come through the gate.");
                                Assert.AreEqual(0, group.PathIndex, $"{wave.name}: a first-world boss must take the main road.");
                            }
                        }
                    }
                }
            }
        }

        // GAME_DESIGN: harder difficulties bring extra boss waves, never fewer.
        [Test]
        public void HarderDifficulties_BringAtLeastAsManyBosses()
        {
            CampaignCatalog catalog = LoadCampaign();
            foreach (MapNode node in catalog.Nodes)
            {
                int previous = 0;
                foreach (DifficultyWaves set in node.Level.DifficultyWaves)
                {
                    int bosses = 0;
                    foreach (WaveDefinition wave in set.Waves)
                    {
                        foreach (SpawnGroup group in wave.Groups)
                        {
                            bosses += group.Enemy != null && group.Enemy.IsBoss ? 1 : 0;
                        }
                    }

                    Assert.GreaterOrEqual(bosses, previous, $"{node.Level.name} {set.Difficulty.name} has fewer bosses than the easier difficulty.");
                    previous = bosses;
                }
            }
        }

        [Test]
        public void AllLevels_ExistAndPassTheValidator()
        {
            CampaignCatalog catalog = LoadCampaign();

            Assert.AreEqual(7, catalog.Nodes.Count);
            for (int i = 0; i < catalog.Nodes.Count; i++)
            {
                LevelDefinition level = catalog.Nodes[i].Level;
                Assert.IsNotNull(level, $"Node {i} has no level.");

                var errors = new List<string>();
                Assert.IsTrue(LevelValidator.Validate(level, errors), $"{level.name}: {string.Join("\n", errors)}");
                Assert.IsNotNull(level.Hero, $"{level.name} has no hero.");
                Assert.Greater(level.Blockades.Length, 0, $"{level.name} has no blockades.");
            }
        }

        [Test]
        public void EveryStreetWeapon_ExplainsItself()
        {
            CampaignCatalog catalog = LoadCampaign();
            int seen = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                foreach (PathObjectDefinition definition in node.Level.PathObjects)
                {
                    seen++;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Description), $"{definition.name} has no description for the loadout screen.");
                }
            }

            Assert.Greater(seen, 0, "no level offers a street weapon");
        }

        [Test]
        public void EveryLevel_GivesANewWeaponAndHasWavesForAllDifficulties()
        {
            CampaignCatalog catalog = LoadCampaign();
            var rewards = new HashSet<TowerDefinition>();

            foreach (MapNode node in catalog.Nodes)
            {
                Assert.IsNotNull(node.Level.Reward.NewTower, $"{node.Level.name} gives no weapon.");
                Assert.IsTrue(rewards.Add(node.Level.Reward.NewTower), $"{node.Level.name} repeats a weapon reward.");
                foreach (DifficultyProfile difficulty in catalog.Difficulties)
                {
                    Assert.IsNotNull(node.Level.GetWaves(difficulty), $"{node.Level.name} lacks waves for {difficulty.name}.");
                }
            }
        }

        [Test]
        public void EveryLevel_GrowsEnemyHealthWaveByWave()
        {
            CampaignCatalog catalog = LoadCampaign();
            foreach (MapNode node in catalog.Nodes)
            {
                WaveDefinition[] waves = node.Level.GetWaves(catalog.Difficulties[0]);
                for (int i = 1; i < waves.Length; i++)
                {
                    Assert.GreaterOrEqual(waves[i].HealthMultiplier, waves[i - 1].HealthMultiplier, $"{node.Level.name} wave {i + 1} is weaker than the wave before it.");
                }

                Assert.Greater(waves[waves.Length - 1].HealthMultiplier, 1f, $"{node.Level.name} never grows its creatures.");
            }
        }

        [Test]
        public void CombineRecipes_StartAtTheNest()
        {
            CampaignCatalog catalog = LoadCampaign();

            for (int i = 0; i < catalog.Nodes.Count; i++)
            {
                int recipes = catalog.Nodes[i].Level.Recipes.Length;
                if (i >= FirstRecipeLevel)
                {
                    Assert.Greater(recipes, 0);
                }
                else
                {
                    Assert.AreEqual(0, recipes);
                }
            }
        }

        [Test]
        public void Recipes_MatchTheirIngredientsInEitherOrder()
        {
            CampaignCatalog catalog = LoadCampaign();
            CombineRecipe recipe = catalog.Nodes[catalog.Nodes.Count - 1].Level.Recipes[0];

            Assert.IsTrue(recipe.Matches(recipe.First, recipe.Second));
            Assert.IsTrue(recipe.Matches(recipe.Second, recipe.First));
            Assert.IsFalse(recipe.Matches(recipe.First, recipe.First));
            Assert.IsNotNull(recipe.Result);
        }

        [Test]
        public void SupportAt_AddsBoostsOnlyWithinTheRadius()
        {
            var registry = new StructureRegistry();
            registry.Add(new FakeSupport { Position = Vector3.zero, SupportRadius = 2f, RangeBonus = 0.15f, RateBonus = 0.2f });
            registry.Add(new FakeSupport { Position = new Vector3(1f, 0f, 0f), SupportRadius = 2f, RangeBonus = 0.1f, RateBonus = 0.1f });

            registry.SupportAt(new Vector3(0.5f, 0f, 0f), out float range, out float rate);
            Assert.AreEqual(0.25f, range, 0.001f);
            Assert.AreEqual(0.3f, rate, 0.001f);

            registry.SupportAt(new Vector3(5f, 0f, 0f), out range, out rate);
            Assert.AreEqual(0f, range, 0.001f);
            Assert.AreEqual(0f, rate, 0.001f);
        }

        [Test]
        public void Mine_DamagesEnemiesInItsBlastOnceAndFinishes()
        {
            PathObjectDefinition definition = NewPathObject(PathObjectKind.Mine, damage: 80f, radius: 1.5f, trigger: 0.6f);
            var registry = new TargetRegistry();
            var near = new FakeTarget { Position = new Vector3(0.4f, 0f, 0f) };
            var inBlast = new FakeTarget { Position = new Vector3(1.2f, 0f, 0f) };
            var far = new FakeTarget { Position = new Vector3(5f, 0f, 0f) };
            registry.Add(near);
            registry.Add(inBlast);
            registry.Add(far);

            var go = new GameObject("Mine");
            var mine = go.AddComponent<PathObject>();
            mine.Initialize(definition, registry, new Vector3[0], 0);
            bool finished = false;
            mine.Finished += _ => finished = true;
            Invoke(mine, "Update");

            Assert.AreEqual(80f, near.Damage, 0.001f);
            Assert.AreEqual(80f, inBlast.Damage, 0.001f);
            Assert.AreEqual(0f, far.Damage, 0.001f);
            Assert.IsTrue(finished);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void SlowField_SlowsEnemiesInsideOnly()
        {
            PathObjectDefinition definition = NewPathObject(PathObjectKind.SlowField, damage: 0f, radius: 1.5f, trigger: 0.5f);
            var registry = new TargetRegistry();
            var inside = new FakeTarget { Position = new Vector3(1f, 0f, 0f) };
            var outside = new FakeTarget { Position = new Vector3(4f, 0f, 0f) };
            registry.Add(inside);
            registry.Add(outside);

            var go = new GameObject("Field");
            var field = go.AddComponent<PathObject>();
            field.Initialize(definition, registry, new Vector3[0], 0);
            Invoke(field, "Update");

            Assert.Less(inside.SlowFactor, 1f);
            Assert.AreEqual(1f, outside.SlowFactor, 0.001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void PowerCurrencyUpgrade_IsPaidInPowerNotResearch()
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            var so = new SerializedObject(upgrade);
            so.FindProperty("_currency").enumValueIndex = (int)UpgradeCurrency.PowerCurrency;
            SerializedProperty costs = so.FindProperty("_costs");
            costs.arraySize = 1;
            costs.GetArrayElementAtIndex(0).intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();

            var progress = new ProgressService(new MemorySave(), new string[0]);
            progress.AddRewards(100, 3);
            Assert.IsFalse(progress.TryBuyUpgrade(upgrade));

            progress.AddRewards(0, 2);
            Assert.IsTrue(progress.TryBuyUpgrade(upgrade));
            Assert.AreEqual(1, progress.Data.PowerCurrency);
            Assert.AreEqual(100, progress.Data.ResearchPoints);

            Object.DestroyImmediate(upgrade);
        }

        private static PathObjectDefinition NewPathObject(PathObjectKind kind, float damage, float radius, float trigger)
        {
            var definition = ScriptableObject.CreateInstance<PathObjectDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_radius").floatValue = radius;
            so.FindProperty("_triggerRadius").floatValue = trigger;
            so.FindProperty("_slowFactor").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        // Edit mode does not run Update by itself, so the tests call it directly.
        private static void Invoke(object target, string method)
        {
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(target, null);
        }
    }
}
