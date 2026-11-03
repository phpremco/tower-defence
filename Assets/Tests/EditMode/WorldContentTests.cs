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
    /// <summary>Checks the campaign as a whole (continents, levels, rewards, recipes) and the rules of the later systems.</summary>
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

        private const int LevelsPerContinent = 20;
        private const string RecipePath = "Assets/_Project/Data/Recipes/Artillery.asset";
        private const string UnlocksPath = "Assets/_Project/Data/Unlocks/UnlockTable.asset";

        private static IEnumerable<LevelDefinition> Levels(CampaignCatalog catalog, int region = -1)
        {
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null && (region < 0 || node.Region == region))
                {
                    yield return node.Level;
                }
            }
        }

        // Docs/LEVEL_GUIDE.md: a boss that comes up away from the other spawns is a front of its own, which the first
        // continent never asks for.
        [Test]
        public void Continent1_BossesComeThroughTheMainGate()
        {
            CampaignCatalog catalog = LoadCampaign();
            foreach (LevelDefinition level in Levels(catalog, 0))
            {
                Assert.AreEqual(EntranceKind.Gate, level.BossEntrance.Kind, $"{level.name}: bosses must use the main gate in the first world.");
                foreach (DifficultyWaves set in level.DifficultyWaves)
                {
                    foreach (WaveDefinition wave in set.Waves)
                    {
                        foreach (SpawnGroup group in wave.Groups)
                        {
                            if (group.Enemy != null && group.Enemy.IsBoss)
                            {
                                Assert.AreEqual(SpawnSource.Gate, group.Source, $"{level.name} {set.Difficulty.name}: a first-world boss must come through the gate.");
                                Assert.AreEqual(0, group.PathIndex, $"{level.name} {set.Difficulty.name}: a first-world boss must take the main road.");
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
            foreach (LevelDefinition level in Levels(catalog))
            {
                int previous = 0;
                foreach (DifficultyWaves set in level.DifficultyWaves)
                {
                    int bosses = 0;
                    foreach (WaveDefinition wave in set.Waves)
                    {
                        foreach (SpawnGroup group in wave.Groups)
                        {
                            bosses += group.Enemy != null && group.Enemy.IsBoss ? 1 : 0;
                        }
                    }

                    Assert.GreaterOrEqual(bosses, previous, $"{level.name} {set.Difficulty.name} has fewer bosses than the easier difficulty.");
                    previous = bosses;
                }
            }
        }

        // A continent holds no sites ("coming soon") or all twenty; every level that exists passes the validator.
        [Test]
        public void AllLevels_ExistAndPassTheValidator()
        {
            CampaignCatalog catalog = LoadCampaign();
            for (int r = 0; r < catalog.Regions.Count; r++)
            {
                int count = catalog.NodeCount(r);
                Assert.IsTrue(count == 0 || count == LevelsPerContinent, $"{catalog.Regions[r].DisplayName} holds {count} sites");
            }

            int levels = 0;
            foreach (LevelDefinition level in Levels(catalog))
            {
                levels++;
                var errors = new List<string>();
                Assert.IsTrue(LevelValidator.Validate(level, errors), $"{level.name}: {string.Join("\n", errors)}");
                Assert.IsNotNull(level.Hero, $"{level.name} has no hero.");
                Assert.That(level.Blockades.Length, Is.InRange(3, 5), $"{level.name} has 3 to 5 gates.");
            }

            Assert.GreaterOrEqual(levels, 6, "Crystal Plains levels 1 to 6 exist");
        }

        [Test]
        public void EveryStreetWeapon_ExplainsItself()
        {
            CampaignCatalog catalog = LoadCampaign();
            int seen = 0;
            foreach (LevelDefinition level in Levels(catalog))
            {
                foreach (PathObjectDefinition definition in level.PathObjects)
                {
                    seen++;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(definition.Description), $"{definition.name} has no description for the loadout screen.");
                }
            }

            Assert.Greater(seen, 0, "no level offers a street weapon");
        }

        // STORY.md: a weapon or gadget is granted by the level before its debut, once; a level sells its continent's
        // roster and every gadget granted before it; every difficulty has waves.
        [Test]
        public void Rewards_FollowTheContinentScript()
        {
            CampaignCatalog catalog = LoadCampaign();
            var unlocks = AssetDatabase.LoadAssetAtPath<UnlockTable>(UnlocksPath);
            var granted = new HashSet<string>(unlocks.StartingTowerIds);
            var gadgets = new HashSet<PathObjectDefinition>();
            foreach (LevelDefinition level in Levels(catalog))
            {
                foreach (PathObjectDefinition sold in level.PathObjects)
                {
                    Assert.IsTrue(gadgets.Contains(sold), $"{level.name} sells {sold.name} before a level granted it");
                }

                TowerDefinition tower = level.Reward.NewTower;
                if (tower != null)
                {
                    Assert.IsTrue(granted.Add(tower.Id), $"{level.name} grants {tower.Id} again, or a starting weapon");
                    Assert.IsTrue(System.Array.Exists(level.AvailableTowers, t => t == tower), $"{level.name} grants {tower.Id}, which its continent does not sell");
                }

                foreach (PathObjectDefinition gadget in level.Reward.NewPathObjects ?? new PathObjectDefinition[0])
                {
                    Assert.IsTrue(gadgets.Add(gadget), $"{level.name} grants {gadget.name} again");
                }

                foreach (DifficultyProfile difficulty in catalog.Difficulties)
                {
                    Assert.IsNotNull(level.GetWaves(difficulty), $"{level.name} lacks waves for {difficulty.name}.");
                }
            }

            CollectionAssert.AreEquivalent(new[] { "PulseRifle", "Cannon" }, unlocks.StartingTowerIds, "level 1 brings the Gun and the Cannon");
            Assert.AreEqual("Laser", catalog.Nodes[0].Level.Reward.NewTower.Id, "level 1 grants the Laser for level 2");
            Assert.AreEqual("Rocket", catalog.Nodes[1].Level.Reward.NewTower.Id, "level 2 grants the Flak for level 3");
        }

        [Test]
        public void EveryLevel_GrowsEnemyHealthWaveByWave()
        {
            CampaignCatalog catalog = LoadCampaign();
            foreach (LevelDefinition level in Levels(catalog))
            {
                // The wave solver may soften a wave that brings a new creature or an air raid (pressure, not health, rises
                // wave by wave; BalanceLedgerTests checks that), so only the overall growth is asserted here. A boss wave spends
                // most of its budget on the boss, and a late wave that arrives together with the slow creatures of earlier waves
                // is softened for them (KillPurse.Carried), so the toughest plain wave of the last third is compared.
                string problem = GrowthProblem(level.GetWaves(catalog.Difficulties[0]));
                Assert.IsNull(problem, $"{level.name} {problem}");
            }
        }

        // The loosened growth check must still catch a level whose creatures do not grow, or shrink.
        [Test]
        public void GrowthCheck_CatchesFlatAndShrinkingHealth()
        {
            Assert.IsNotNull(GrowthProblem(SyntheticWaves(12, i => 1f)), "flat at one");
            Assert.IsNotNull(GrowthProblem(SyntheticWaves(12, i => 1.4f)), "flat above one");
            Assert.IsNotNull(GrowthProblem(SyntheticWaves(12, i => 1.5f - i * 0.05f)), "shrinking");
            Assert.IsNotNull(GrowthProblem(SyntheticWaves(12, i => 0.6f + i * 0.02f)), "growing but never above one");
            Assert.IsNull(GrowthProblem(SyntheticWaves(12, i => 0.8f + i * 0.05f)), "growing");
        }

        private static WaveDefinition[] SyntheticWaves(int count, System.Func<int, float> health)
        {
            var waves = new WaveDefinition[count];
            for (int i = 0; i < count; i++)
            {
                waves[i] = new WaveDefinition();
                JsonUtility.FromJsonOverwrite($"{{\"_healthMultiplier\":{health(i).ToString(System.Globalization.CultureInfo.InvariantCulture)}}}", waves[i]);
            }

            return waves;
        }

        // Null when the toughest plain wave of the last third is tougher than the first wave and above one.
        private static string GrowthProblem(WaveDefinition[] waves)
        {
            float last = ToughestLatePlainWave(waves);
            if (last <= waves[0].HealthMultiplier)
            {
                return "ends no tougher than it starts.";
            }

            return last <= 1f ? "never grows its creatures." : null;
        }

        private const int LateShare = 3;

        private static float ToughestLatePlainWave(WaveDefinition[] waves)
        {
            float toughest = 0f;
            for (int i = waves.Length - waves.Length / LateShare; i < waves.Length; i++)
            {
                bool boss = false;
                foreach (SpawnGroup group in waves[i].Groups)
                {
                    boss |= group.Enemy != null && group.Enemy.IsBoss;
                }

                if (!boss)
                {
                    toughest = Mathf.Max(toughest, waves[i].HealthMultiplier);
                }
            }

            return toughest;
        }

        // The hybrids leave the first continent; the script's combos come as recipes in continent 3.
        [Test]
        public void Continent1_HasNoRecipes()
        {
            CampaignCatalog catalog = LoadCampaign();
            foreach (LevelDefinition level in Levels(catalog, 0))
            {
                Assert.AreEqual(0, level.Recipes.Length, $"{level.name} offers recipes");
            }
        }

        [Test]
        public void Recipes_MatchTheirIngredientsInEitherOrder()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<CombineRecipe>(RecipePath);
            Assert.IsNotNull(recipe, "the hybrid recipes stay as data");

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
