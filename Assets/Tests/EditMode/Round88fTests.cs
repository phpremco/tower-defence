using System.Collections.Generic;
using System.Reflection;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 88f (Remco's playtest of 2026-11-23). Aiming: every gun takes the creature with the least road left to its
    /// goal, whichever road it came by (before, the distance travelled: a creature at the last gate off a short side road
    /// lost to one halfway down the main road). Tower wear: a gun takes the difficulty's share of a blow, a gate all of it.
    /// </summary>
    public sealed class Round88fTests
    {
        private const string TowerFolder = "Assets/_Project/Data/Towers";
        private const string DifficultyFolder = "Assets/_Project/Data/Difficulty";

        // The follower has walked half the main road and has most of it ahead; the leader came by a short side road and
        // stands two cells from its end. Both are a cell and a half from the gun.
        private const float MainRoadWalked = 50f;
        private const float MainRoadLeft = 60f;
        private const float SideRoadLeft = 2f;
        private const float Aside = 1.5f;

        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _made.Count; i++)
            {
                Object.DestroyImmediate(_made[i]);
            }

            _made.Clear();
        }

        // ---- aiming --------------------------------------------------------------------------------------------------

        [Test]
        public void ACreatureNearTheEndOfAShortRoad_OutranksOneHalfwayDownTheLongRoad()
        {
            var registry = new TargetRegistry();
            Enemy follower = Follower(registry, flies: false);
            Enemy leader = Leader(registry, flies: false);

            Assert.Greater(follower.PathProgress, leader.PathProgress, "the follower has travelled further: the old rule picked it");
            Assert.Less(leader.DistanceToGoal, follower.DistanceToGoal);
            Assert.AreSame(leader, registry.FindBest(Vector3.zero, 5f, TargetLayers.Ground));
        }

        [Test]
        public void TheRoadLeft_ShrinksAsTheCreatureWalks_AndIsCountedAgainOnANewRoute()
        {
            var registry = new TargetRegistry();
            Enemy walker = Leader(registry, flies: false);
            float before = walker.DistanceToGoal;
            Assert.AreEqual(SideRoadLeft, before, 0.001f);

            walker.Step(0.5f);
            Assert.AreEqual(before - 0.5f, walker.DistanceToGoal, 0.001f, "one cell a second for half a second");

            walker.Reroute(new[] { walker.Position, walker.Position + Vector3.forward * 7f }, 12f);
            Assert.AreEqual(7f, walker.DistanceToGoal, 0.001f);
            Assert.AreEqual(12f, walker.PathProgress, 0.001f, "the road progress is still the caller's");
        }

        [Test]
        public void EveryGun_AimsAtTheCreatureNearestItsGoal()
        {
            MethodInfo update = typeof(Tower).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(update, "Tower.Update");
            int guns = 0;
            foreach (TowerDefinition definition in Towers())
            {
                int level = FirstArmedLevel(definition);
                if (definition.IsSupport || level < 0)
                {
                    continue;
                }

                guns++;
                var registry = new TargetRegistry();
                bool skyOnly = (definition.Targets & TargetLayers.Ground) == 0;
                Enemy follower = Follower(registry, skyOnly);
                Enemy leader = Leader(registry, skyOnly);

                var go = new GameObject(definition.name);
                _made.Add(go);
                var tower = go.AddComponent<Tower>();
                tower.enabled = false;
                tower.Initialize(definition, registry, new ProjectilePool(registry, go.transform), PlayerModifiers.None);
                tower.SetLevel(level);
                Assert.Greater(tower.Range, Aside, $"{definition.name}: both creatures stand in its reach");

                Vector3? aimedAt = null;
                tower.Fired += at => aimedAt ??= at;
                tower.Charging += (at, _) => aimedAt ??= at;
                update.Invoke(tower, null);

                Assert.IsTrue(aimedAt.HasValue, $"{definition.name} did not open fire");
                Assert.Less(Vector3.Distance(aimedAt.Value, leader.Position), 0.01f, $"{definition.name} aims at the creature two cells from its goal, not at the one halfway down the main road (it shot at {aimedAt.Value}; follower at {follower.Position})");
            }

            Assert.GreaterOrEqual(guns, 8, "the tower folder holds the guns of the game");
        }

        // ---- tower wear ----------------------------------------------------------------------------------------------

        [Test]
        public void AGun_TakesTheLevelsShareOfABlow_AndAGateAllOfIt()
        {
            var structures = new StructureRegistry { GunDamageShare = 0.5f };
            Tower gun = Placed("Cannon", structures);
            Tower gate = Placed("Blockade", structures);
            float gunHealth = gun.Health.Current;
            float gateHealth = gate.Health.Current;

            gun.TakeDamage(20f);
            gate.TakeDamage(20f);

            Assert.AreEqual(gunHealth - 10f, gun.Health.Current, 0.001f, "a gun beside the road takes the share");
            Assert.AreEqual(gateHealth - 20f, gate.Health.Current, 0.001f, "what bars the road takes the blow in full");
        }

        [Test]
        public void OnlyPsychoSoftensTheBiteOnGuns_ToHardsBiteOrLess()
        {
            DifficultyProfile hard = Difficulty("Hard");
            DifficultyProfile psycho = Difficulty("Psycho");
            Assert.AreEqual(1f, Difficulty("Easy").TowerDamageFactor, 0.0001f);
            Assert.AreEqual(1f, Difficulty("Medium").TowerDamageFactor, 0.0001f);
            Assert.AreEqual(1f, hard.TowerDamageFactor, 0.0001f);
            Assert.Less(psycho.TowerDamageFactor, 1f);

            float hardBite = hard.StructureDamageMultiplier * hard.TowerDamageFactor;
            float psychoBite = psycho.StructureDamageMultiplier * psycho.TowerDamageFactor;
            Assert.LessOrEqual(psychoBite, hardBite + 0.0001f, "a Psycho creature bites a gun no harder than a Hard one; there are more of them and they live longer");
            Assert.Greater(psycho.StructureDamageMultiplier, hard.StructureDamageMultiplier, "gates, the base and the hero keep Psycho's bite");
        }

        [Test]
        public void TheGeneratorsSheet_HoldsTheSameTowerFactors_AsTheAssets()
        {
            foreach (Game.Editor.BalanceSheet.Difficulty sheet in Game.Editor.BalanceSheet.Difficulties)
            {
                Assert.AreEqual(sheet.TowerFactor, Difficulty(sheet.Id).TowerDamageFactor, 0.0001f, sheet.Id);
            }
        }

        // ---- helpers -------------------------------------------------------------------------------------------------

        private Enemy Follower(TargetRegistry registry, bool flies)
        {
            var from = new Vector3(-Aside, 0f, 0f);
            return Creature(registry, flies, new[] { from, from + Vector3.forward * MainRoadLeft }, MainRoadWalked);
        }

        private Enemy Leader(TargetRegistry registry, bool flies)
        {
            var from = new Vector3(Aside, 0f, 0f);
            return Creature(registry, flies, new[] { from, from + Vector3.forward * SideRoadLeft }, 0f);
        }

        private Enemy Creature(TargetRegistry registry, bool flies, Vector3[] route, float walked)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _made.Add(definition);
            var so = new SerializedObject(definition);
            so.FindProperty("_layer").intValue = (int)(flies ? TargetLayers.Air : TargetLayers.Ground);
            so.FindProperty("_flies").boolValue = flies;
            so.FindProperty("_maxHealth").floatValue = 100000f;
            so.FindProperty("_speed").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var go = new GameObject(flies ? "Flyer" : "Walker");
            _made.Add(go);
            var enemy = go.AddComponent<Enemy>();
            enemy.enabled = false;
            enemy.Initialize(definition, route, 1f, 1f, 0f, registry, new StructureRegistry(), 1f, EnemyOrigin.Gate, walked);
            return enemy;
        }

        private Tower Placed(string id, StructureRegistry structures)
        {
            TowerDefinition definition = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{TowerFolder}/{id}.asset");
            Assert.IsNotNull(definition, id);
            var go = new GameObject(id);
            _made.Add(go);
            var tower = go.AddComponent<Tower>();
            tower.enabled = false;
            var targets = new TargetRegistry();
            tower.Initialize(definition, targets, new ProjectilePool(targets, go.transform), PlayerModifiers.None);
            tower.Register(structures);
            return tower;
        }

        private static int FirstArmedLevel(TowerDefinition definition)
        {
            for (int i = 0; i <= definition.MaxLevelIndex; i++)
            {
                if (definition.GetLevel(i).Damage > 0f)
                {
                    return i;
                }
            }

            return -1;
        }

        private static IEnumerable<TowerDefinition> Towers()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { TowerFolder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        private static DifficultyProfile Difficulty(string id)
        {
            DifficultyProfile profile = AssetDatabase.LoadAssetAtPath<DifficultyProfile>($"{DifficultyFolder}/{id}.asset");
            Assert.IsNotNull(profile, id);
            return profile;
        }
    }
}
