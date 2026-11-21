using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Editor;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 88b, the tower threat (Remco: "my towers are never damaged because I place them between the roads"): an attack
    /// style reaches a set distance to the guns beside the road (the combat rules' reach table), every boss of a level hurts a
    /// tower from <see cref="CombatRules.BossReach"/> cells, the swarm kinds leave towers alone, and none of it reaches a
    /// gate, the base or the hero from further than before.
    /// </summary>
    public sealed class Round88bThreatTests
    {
        private const string EnemyFolder = "Assets/_Project/Data/Enemies";
        private const string LevelFolder = "Assets/_Project/Data/Levels";
        private const string DifficultyFolder = "Assets/_Project/Data/Difficulty";
        private static readonly string[] SwarmKinds = { "Skitter", "Prowler", "Roller" };
        private static readonly AttackStyle[] MeleeStyles = { AttackStyle.Bite, AttackStyle.Whip, AttackStyle.Slash };
        private static readonly AttackStyle[] RangedStyles = { AttackStyle.Sting, AttackStyle.Bolt };

        private sealed class Target : IStructure
        {
            public Target(Vector3 position, bool blocks)
            {
                Position = position;
                BlocksGround = blocks;
            }

            public Vector3 Position { get; }
            public bool IsAlive => true;
            public bool BlocksGround { get; }
            public bool IsBase => false;
            public float Damage { get; private set; }
            public float HealthFraction => 1f;

            public void TakeDamage(float amount) => Damage += amount;

            public void Repair(float amount)
            {
            }
        }

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

        private static CombatRules Rules() => AssetDatabase.LoadAssetAtPath<CombatRules>(CombatRules.AssetPath);

        private static EnemyDefinition Kind(string id) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyFolder}/{id}.asset");

        private static IEnumerable<EnemyDefinition> Kinds()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyFolder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        // Every boss the stored waves of any level send out, on any difficulty.
        private static List<EnemyDefinition> LevelBosses()
        {
            var bosses = new List<EnemyDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                // The campaign's levels; the boss arena is a continent boss's own stage, with its own mechanics.
                if (!level.name.StartsWith("Level_"))
                {
                    continue;
                }

                foreach (DifficultyWaves set in level.DifficultyWaves)
                {
                    foreach (WaveDefinition wave in set.Waves)
                    {
                        foreach (SpawnGroup group in wave.Groups)
                        {
                            if (group.Enemy != null && group.Enemy.IsBoss && !bosses.Contains(group.Enemy))
                            {
                                bosses.Add(group.Enemy);
                            }
                        }
                    }
                }
            }

            return bosses;
        }

        private EnemyDefinition Creature(AttackStyle style, float damage, float range, float interval, CombatRules rules, BossAbilityDefinition[] abilities = null)
        {
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            _made.Add(definition);
            var so = new SerializedObject(definition);
            so.FindProperty("_speed").floatValue = 0.1f;
            so.FindProperty("_attackStyle").enumValueIndex = (int)style;
            so.FindProperty("_attackDamage").floatValue = damage;
            so.FindProperty("_attackRange").floatValue = range;
            so.FindProperty("_attackInterval").floatValue = interval;
            so.FindProperty("_rules").objectReferenceValue = rules;
            SerializedProperty list = so.FindProperty("_abilities");
            list.arraySize = abilities != null ? abilities.Length : 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = abilities[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private BossAbilityDefinition Volley(float damage, float range, int targets, bool sparesBlockers)
        {
            var ability = ScriptableObject.CreateInstance<BossAbilityDefinition>();
            _made.Add(ability);
            var so = new SerializedObject(ability);
            so.FindProperty("_kind").enumValueIndex = (int)BossAbilityKind.Volley;
            so.FindProperty("_interval").floatValue = 100f;
            so.FindProperty("_firstDelay").floatValue = 0f;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_targetCount").intValue = targets;
            so.FindProperty("_sparesBlockers").boolValue = sparesBlockers;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ability;
        }

        // Sends one creature out at the origin on a road that leads away along z, and steps it and its powers for a time.
        private void Play(EnemyDefinition definition, StructureRegistry structures, float seconds, float structureDamage = 1f)
        {
            var go = new GameObject("Creature");
            _made.Add(go);
            var enemy = go.AddComponent<Enemy>();
            BossAbilities abilities = null;
            if (definition.Abilities.Length > 0)
            {
                abilities = go.AddComponent<BossAbilities>();
                abilities.Initialize(structures, (minion, boss) => { }, (origin, range) => structures.FindNearest(origin, range));
                abilities.SetDamageMultiplier(structureDamage);
            }

            enemy.Initialize(definition, new[] { Vector3.zero, new Vector3(0f, 0f, 0.5f) }, 1f, 1f, 0f, new TargetRegistry(), structures, structureDamageMultiplier: structureDamage);
            const float Tick = 0.1f;
            for (float t = 0f; t < seconds; t += Tick)
            {
                enemy.Step(Tick);
                if (abilities != null)
                {
                    abilities.Step(Tick);
                }
            }
        }

        // ---- the reach rule per attack style ---------------------------------------------------------------------

        [Test]
        public void ReachTable_RangedStylesReachATowerBetweenTwoRoads_MeleeStylesHaveNoRow()
        {
            CombatRules rules = Rules();
            foreach (AttackStyle style in RangedStyles)
            {
                Assert.GreaterOrEqual(rules.StructureReach(style.ToString()), rules.BossReach, $"{style} is a ranged style: it reaches a tower {rules.BossReach} cells off the road");
            }

            foreach (AttackStyle style in MeleeStyles)
            {
                Assert.AreEqual(0f, rules.StructureReach(style.ToString()), $"{style} is a melee style: the creature's own range");
            }

            float slam = rules.StructureReach(AttackStyle.Slam.ToString());
            Assert.Greater(slam, 3f, "a slam carries to the second row");
            Assert.Less(slam, rules.StructureReach(AttackStyle.Sting.ToString()), "and less far than any ranged style");
            Assert.GreaterOrEqual(rules.BossReach, 4f, "a tower between two roads six cells apart is in a boss's reach");
        }

        [Test]
        public void ReachTable_IsWhatTheGeneratorWrites()
        {
            CombatRules rules = Rules();
            Assert.AreEqual(ProjectGenerator.ReachRules.Length, rules.Reach.Length);
            for (int i = 0; i < rules.Reach.Length; i++)
            {
                Assert.AreEqual(ProjectGenerator.ReachRules[i].Style, rules.Reach[i].Style);
                Assert.AreEqual(ProjectGenerator.ReachRules[i].Cells, rules.Reach[i].Cells, 0.0001f);
                Assert.IsTrue(System.Enum.TryParse(rules.Reach[i].Style, out AttackStyle _), $"'{rules.Reach[i].Style}' is an attack style");
            }

            Assert.AreEqual(ProjectGenerator.BossReach, rules.BossReach, 0.0001f);
        }

        [Test]
        public void EveryCreature_ReadsItsReachFromTheRules_AndNeverLessThanItsOwnRange()
        {
            CombatRules rules = Rules();
            foreach (EnemyDefinition kind in Kinds())
            {
                float styled = rules.StructureReach(kind.AttackStyle.ToString());
                Assert.AreEqual(Mathf.Max(kind.AttackRange, styled), kind.StructureReach, 0.0001f, $"{kind.name} ({kind.AttackStyle})");
            }
        }

        [Test]
        public void ARangedCreature_HurtsATowerFourCellsOff_AMeleeOneWithTheSameNumbersDoesNot()
        {
            CombatRules rules = Rules();
            foreach (AttackStyle style in RangedStyles)
            {
                var tower = new Target(new Vector3(4f, 0f, 0f), false);
                var structures = new StructureRegistry();
                structures.Add(tower);
                Play(Creature(style, 5f, 2.5f, 1f, rules), structures, 3f);
                Assert.Greater(tower.Damage, 0f, $"{style} reaches four cells");
            }

            foreach (AttackStyle style in MeleeStyles)
            {
                var tower = new Target(new Vector3(4f, 0f, 0f), false);
                var structures = new StructureRegistry();
                structures.Add(tower);
                Play(Creature(style, 5f, 2.5f, 1f, rules), structures, 3f);
                Assert.AreEqual(0f, tower.Damage, $"{style} does not");
            }
        }

        // ---- who threatens a tower -----------------------------------------------------------------------------------

        [Test]
        public void SwarmKinds_CannotHurtATower_HoweverNear()
        {
            foreach (string id in SwarmKinds)
            {
                EnemyDefinition kind = Kind(id);
                Assert.AreEqual(0f, kind.AttackDamage, $"{id} leaves towers alone");
                var tower = new Target(new Vector3(1f, 0f, 0f), false);
                var structures = new StructureRegistry();
                structures.Add(tower);
                Play(kind, structures, 5f);
                Assert.AreEqual(0f, tower.Damage, $"{id} walked past a tower one cell off");
            }
        }

        [Test]
        public void TheCragback_SlamsTheSecondRow_ButNotATowerFourCellsOff()
        {
            EnemyDefinition cragback = Kind("Cragback");
            Assert.AreEqual(ProjectGenerator.CragbackAttackDamage, cragback.AttackDamage, 0.0001f);
            var second = new Target(new Vector3(3f, 0f, 0f), false);
            var structures = new StructureRegistry();
            structures.Add(second);
            Play(cragback, structures, 4f);
            Assert.Greater(second.Damage, 0f, "three cells off");

            var far = new Target(new Vector3(4f, 0f, 0f), false);
            structures = new StructureRegistry();
            structures.Add(far);
            Play(cragback, structures, 4f);
            Assert.AreEqual(0f, far.Damage, "four cells off");
        }

        [Test]
        public void EveryLevelBoss_HurtsATowerAtTheBossReach_AndItsDataSaysSo()
        {
            CombatRules rules = Rules();
            List<EnemyDefinition> bosses = LevelBosses();
            Assert.GreaterOrEqual(bosses.Count, 4, "the stored waves send out bosses");
            foreach (EnemyDefinition boss in bosses)
            {
                float reach = boss.AttackDamage > 0f ? boss.StructureReach : 0f;
                foreach (BossAbilityDefinition ability in boss.Abilities)
                {
                    reach = Mathf.Max(reach, ability.StructureReach);
                }

                Assert.GreaterOrEqual(reach, rules.BossReach, $"{boss.name}: no attack or power of it hurts a structure {rules.BossReach} cells off");

                var tower = new Target(new Vector3(rules.BossReach, 0f, 0f), false);
                var structures = new StructureRegistry();
                structures.Add(tower);
                Play(boss, structures, 20f);
                Assert.Greater(tower.Damage, 0f, $"{boss.name} stood {rules.BossReach} cells from a tower for 20 s");
            }
        }

        [Test]
        public void TheBossVolleys_AreTheGeneratorsRule_AndLeaveTheRoadBlocksAlone()
        {
            foreach ((string boss, string id) in new[] { ("BroodMother", "VenomSpit"), ("CoreWisp", "ArcDischarge"), ("PackAlpha", "SpineVolley") })
            {
                var volley = AssetDatabase.LoadAssetAtPath<BossAbilityDefinition>($"{EnemyFolder}/Abilities/{id}.asset");
                Assert.IsNotNull(volley, id);
                Assert.AreEqual(BossAbilityKind.Volley, volley.Kind);
                Assert.IsTrue(volley.SparesBlockers, $"{id} flies at the guns only");
                Assert.AreEqual(ProjectGenerator.BossVolleyDamage, volley.Damage, 0.0001f);
                Assert.AreEqual(ProjectGenerator.BossVolleyInterval, volley.Interval, 0.0001f);
                Assert.AreEqual(ProjectGenerator.BossVolleyRange, volley.Range, 0.0001f);
                Assert.AreEqual(ProjectGenerator.BossVolleyTargets, volley.TargetCount);
                Assert.Contains(volley, Kind(boss).Abilities, $"{boss} carries {id}");
                StringAssert.Contains($"data.bossAbility.{id}.displayName = ", System.IO.File.ReadAllText(LocalisationTables.DataTablePath), $"{id} has a name in the English table");
            }
        }

        // ---- gates, the base and the hero take no more than before ---------------------------------------------------

        [Test]
        public void AGate_IsNotReachedFromFurtherThanTheCreaturesOwnRange()
        {
            CombatRules rules = Rules();
            // Inside the style's reach (4.5 cells), outside the creature's own range (2.5): a gate there was safe before and is now.
            var gate = new Target(new Vector3(3.5f, 0f, 0f), true);
            var structures = new StructureRegistry();
            structures.Add(gate);
            Play(Creature(AttackStyle.Bolt, 5f, 2.5f, 1f, rules), structures, 3f);
            Assert.AreEqual(0f, gate.Damage, "the reach is for the guns beside the road");

            // The same gate with a tower behind it: the bolts go to the tower, never to the gate.
            var tower = new Target(new Vector3(4f, 0f, 0f), false);
            structures.Add(tower);
            Play(Creature(AttackStyle.Bolt, 5f, 2.5f, 1f, rules), structures, 3f);
            Assert.AreEqual(0f, gate.Damage);
            Assert.Greater(tower.Damage, 0f);
        }

        [Test]
        public void AGateInsideTheOwnRange_TakesExactlyWhatItTookWithoutTheRule()
        {
            var withRule = new Target(new Vector3(2f, 0f, 0f), true);
            var structures = new StructureRegistry();
            structures.Add(withRule);
            Play(Creature(AttackStyle.Bolt, 5f, 2.5f, 1f, Rules()), structures, 3f);

            var without = new Target(new Vector3(2f, 0f, 0f), true);
            structures = new StructureRegistry();
            structures.Add(without);
            Play(Creature(AttackStyle.Bolt, 5f, 2.5f, 1f, null), structures, 3f);

            Assert.Greater(without.Damage, 0f);
            Assert.AreEqual(without.Damage, withRule.Damage, 0.0001f);
        }

        [Test]
        public void ABossVolleyAtTheGuns_SparesAGateAndTheNearestGunsShareIt()
        {
            BossAbilityDefinition volley = Volley(10f, 5f, 2, true);
            var gate = new Target(new Vector3(2f, 0f, 0f), true);
            var near = new Target(new Vector3(2.5f, 0f, 0f), false);
            var second = new Target(new Vector3(3f, 0f, 0f), false);
            var third = new Target(new Vector3(4f, 0f, 0f), false);
            var structures = new StructureRegistry();
            structures.Add(gate);
            structures.Add(third);
            structures.Add(second);
            structures.Add(near);
            Play(Creature(AttackStyle.Bite, 0f, 2.5f, 1f, null, new[] { volley }), structures, 0.2f);

            Assert.AreEqual(0f, gate.Damage, "the gate is the nearest structure and is left alone");
            Assert.AreEqual(10f, near.Damage, 0.0001f);
            Assert.AreEqual(10f, second.Damage, 0.0001f);
            Assert.AreEqual(0f, third.Damage, "two bolts a volley");
        }

        [Test]
        public void AVolleyWithoutTheFlag_StillHitsAGate_AsBefore()
        {
            BossAbilityDefinition volley = Volley(10f, 5f, 2, false);
            var gate = new Target(new Vector3(2f, 0f, 0f), true);
            var structures = new StructureRegistry();
            structures.Add(gate);
            Play(Creature(AttackStyle.Bite, 0f, 2.5f, 1f, null, new[] { volley }), structures, 0.2f);
            Assert.AreEqual(10f, gate.Damage, 0.0001f);
        }

        // ---- the difficulty's structure damage reaches both ----------------------------------------------------------

        [Test]
        public void TheDifficultysStructureDamage_ScalesTheBiteAndTheVolley()
        {
            var factors = new Dictionary<string, float>();
            foreach (string guid in AssetDatabase.FindAssets("t:DifficultyProfile", new[] { DifficultyFolder }))
            {
                var difficulty = AssetDatabase.LoadAssetAtPath<DifficultyProfile>(AssetDatabase.GUIDToAssetPath(guid));
                factors[difficulty.name] = difficulty.StructureDamageMultiplier;
            }

            Assert.LessOrEqual(factors["Easy"], factors["Medium"]);
            Assert.Less(factors["Medium"], factors["Hard"]);
            Assert.Less(factors["Hard"], factors["Psycho"]);

            foreach (KeyValuePair<string, float> pair in factors)
            {
                var bitten = new Target(new Vector3(4f, 0f, 0f), false);
                var structures = new StructureRegistry();
                structures.Add(bitten);
                // One bite: the first lands at once, the next only after the interval.
                Play(Creature(AttackStyle.Bolt, 5f, 2.5f, 100f, Rules()), structures, 0.2f, pair.Value);
                Assert.AreEqual(5f * pair.Value, bitten.Damage, 0.0001f, $"bite on {pair.Key}");

                var shot = new Target(new Vector3(4f, 0f, 0f), false);
                structures = new StructureRegistry();
                structures.Add(shot);
                Play(Creature(AttackStyle.Bite, 0f, 2.5f, 1f, null, new[] { Volley(10f, 5f, 3, true) }), structures, 0.2f, pair.Value);
                Assert.AreEqual(10f * pair.Value, shot.Damage, 0.0001f, $"volley on {pair.Key}");
            }
        }
    }
}
