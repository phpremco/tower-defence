using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The enemy logic that does not need a scene: rolling, gate strikes, armour, haste, trails, boss powers and the remaining route.</summary>
    public sealed class EnemyBehaviourTests
    {
        private sealed class FakeStructure : IStructure
        {
            public FakeStructure(Vector3 position, bool blocks)
            {
                Position = position;
                BlocksGround = blocks;
            }

            public Vector3 Position { get; }
            public bool IsAlive => Damage < 1000f;
            public bool BlocksGround { get; set; }
            public float Damage { get; private set; }
            public float HealthFraction => 1f;

            public void TakeDamage(float amount) => Damage += amount;

            public void Repair(float amount)
            {
            }
        }

        private GameObject _go;
        private Enemy _enemy;
        private EnemyDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Enemy");
            _enemy = _go.AddComponent<Enemy>();
            _definition = ScriptableObject.CreateInstance<EnemyDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_definition);
        }

        [Test]
        public void Roller_WalksThenRollsFasterThenWalksAgain()
        {
            Configure(speed: 1f, walkSeconds: 1.2f, rollSeconds: 0.9f, rollSpeed: 3f);
            Launch(new TargetRegistry(), new StructureRegistry());

            _enemy.Step(0.5f);
            _enemy.Step(0.5f);
            Assert.IsFalse(_enemy.IsRolling, "still walking after one second");
            Assert.AreEqual(1f, _enemy.PathProgress, 0.001f);

            // The roll starts on the next step and triples the pace.
            _enemy.Step(0.5f);
            Assert.IsTrue(_enemy.IsRolling);
            Assert.AreEqual(2.5f, _enemy.PathProgress, 0.001f);

            _enemy.Step(0.5f);
            _enemy.Step(0.5f);
            Assert.IsFalse(_enemy.IsRolling, "unrolled after the roll time");
        }

        [Test]
        public void Roller_UnrollsAtABlockade()
        {
            Configure(speed: 1f, walkSeconds: 0.1f, rollSeconds: 10f, rollSpeed: 3f);
            var structures = new StructureRegistry();
            var gate = new FakeStructure(new Vector3(0f, 0f, 2f), true);
            Launch(new TargetRegistry(), structures);

            _enemy.Step(0.2f);
            Assert.IsTrue(_enemy.IsRolling);

            structures.Add(gate);
            _enemy.Step(0.2f);
            Assert.IsFalse(_enemy.IsRolling, "a gate stops the roll");
            Assert.Greater(gate.Damage, 0f, "and gets bitten");
        }

        [Test]
        public void Flyer_StopsToStrikeAGateTheGivenNumberOfTimesThenFliesOn()
        {
            Configure(speed: 1f, flies: true, gateStrikes: 2, attackDamage: 5f, attackRange: 2f, attackInterval: 0.5f);
            var structures = new StructureRegistry();
            var gate = new FakeStructure(new Vector3(0f, 0f, 1f), true);
            structures.Add(gate);
            Launch(new TargetRegistry(), structures);

            _enemy.Step(0.1f);
            Assert.AreEqual(5f, gate.Damage, 0.001f, "first strike lands at once");
            Assert.AreEqual(0f, _enemy.Position.z, 0.001f, "and the flyer hovers");

            for (int i = 0; i < 6; i++)
            {
                _enemy.Step(0.1f);
            }

            Assert.AreEqual(10f, gate.Damage, 0.001f, "second strike after the interval");

            _enemy.Step(1f);
            Assert.Greater(_enemy.Position.z, 0.5f, "out of strikes it flies on");
        }

        [Test]
        public void Walker_IsHeldByAGate_ButAFlyerAndAHoveringWispDifferOnlyInWhoCanShootThem()
        {
            Configure(speed: 1f, layer: TargetLayers.Ground | TargetLayers.Air);
            var structures = new StructureRegistry();
            structures.Add(new FakeStructure(new Vector3(0f, 0f, 1f), true));
            Launch(new TargetRegistry(), structures);

            _enemy.Step(1f);
            Assert.AreEqual(0f, _enemy.Position.z, 0.001f, "a wisp walks the road, so the gate holds it");
            Assert.AreNotEqual(0, (int)(_enemy.Layer & TargetLayers.Air), "but air defence may shoot it");
        }

        [Test]
        public void StructureDamage_ScalesWithTheDifficultyMultiplier()
        {
            Configure(speed: 1f, attackDamage: 4f, attackRange: 3f, attackInterval: 10f);
            var structures = new StructureRegistry();
            var tower = new FakeStructure(new Vector3(1f, 0f, 0f), false);
            structures.Add(tower);
            Launch(new TargetRegistry(), structures, structureDamage: 1.5f);

            _enemy.Step(0.1f);
            Assert.AreEqual(6f, tower.Damage, 0.001f);
        }

        [Test]
        public void Armor_SoaksItsShareUntilTheplatesAreGone()
        {
            var health = new Health(100f, 30f, 0.6f);
            float armorLeft = -1f;
            health.ArmorChanged += a => armorLeft = a;

            health.TakeDamage(20f);
            Assert.AreEqual(18f, health.Armor, 0.001f, "the plates soak sixty percent");
            Assert.AreEqual(92f, health.Current, 0.001f, "the rest goes through");
            Assert.AreEqual(18f, armorLeft, 0.001f);

            health.TakeDamage(50f);
            Assert.AreEqual(0f, health.Armor, 0.001f, "the last plates go");
            Assert.AreEqual(60f, health.Current, 0.001f, "what the plates could not take lands on the body");

            health.TakeDamage(10f);
            Assert.AreEqual(50f, health.Current, 0.001f, "without plates every hit lands in full");

            health.RestoreArmor(100f);
            Assert.AreEqual(30f, health.Armor, 0.001f, "regrown armour never exceeds the original");
        }

        [Test]
        public void Haste_SpeedsUpAndTheStrongestWins_WhileASlowStillCounts()
        {
            Configure(speed: 1f);
            Launch(new TargetRegistry(), new StructureRegistry());

            _enemy.ApplyHaste(1.5f, 1f);
            _enemy.ApplyHaste(1.2f, 1f);
            Assert.IsTrue(_enemy.IsHasted);
            _enemy.Step(0.5f);
            Assert.AreEqual(0.75f, _enemy.PathProgress, 0.001f, "the stronger haste wins");

            _enemy.ApplySlow(0.5f, 1f);
            _enemy.Step(0.5f);
            Assert.AreEqual(0.75f + 0.375f, _enemy.PathProgress, 0.001f, "a slow halves the hurried pace");

            _enemy.Step(1f);
            Assert.IsFalse(_enemy.IsHasted, "the haste runs out");
        }

        [Test]
        public void TrailLayer_DropsAPatchEveryHalfCellOrSo()
        {
            Configure(speed: 1f, trailSeconds: 5f);
            Launch(new TargetRegistry(), new StructureRegistry());
            var drops = new List<Vector3>();
            _enemy.TrailDropped += (_, at) => drops.Add(at);

            for (int i = 0; i < 20; i++)
            {
                _enemy.Step(0.1f);
            }

            Assert.AreEqual(3, drops.Count, "two units of road give three patches");
            Assert.AreEqual(0f, drops[0].y, 0.001f, "patches lie on the ground");
        }

        [Test]
        public void SlimeField_HurriesOtherSpeciesButNotTheLayer_AndBurnsGatesAndTheHero()
        {
            Configure(speed: 1f, trailSeconds: 2f, trailHaste: 1.5f, trailDamage: 10f);
            var living = new List<Enemy>();
            var structures = new StructureRegistry();
            var gate = new FakeStructure(new Vector3(0f, 0f, 0f), true);
            structures.Add(gate);
            var field = new SlimeField(living, structures, 0.6f, 2f);
            Launch(new TargetRegistry(), structures);
            living.Add(_enemy);

            var otherGo = new GameObject("Other");
            var other = otherGo.AddComponent<Enemy>();
            var otherDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();
            other.Initialize(otherDefinition, new[] { Vector3.zero, new Vector3(0f, 0f, 10f) }, 1f, 1f, 0f, new TargetRegistry(), new StructureRegistry());
            living.Add(other);

            field.Add(_enemy, Vector3.zero);
            field.Step(0.5f);
            Assert.IsTrue(other.IsHasted, "the other species is hurried");
            Assert.IsFalse(_enemy.IsHasted, "the layer gains nothing from its own slime");
            Assert.AreEqual(10f, gate.Damage, 0.001f, "the gate in the slime burns at the difficulty rate");

            field.Step(2f);
            Assert.AreEqual(0, field.Patches.Count, "the patch dries up");

            Object.DestroyImmediate(otherGo);
            Object.DestroyImmediate(otherDefinition);
        }

        [Test]
        public void Boss_VolleyHitsTheNearestStructures_RallyHurriesOthers_RegrowRestoresPlates_SplitShedsMinions()
        {
            var minion = ScriptableObject.CreateInstance<EnemyDefinition>();
            BossAbilityDefinition volley = Ability(BossAbilityKind.Volley, damage: 7f, range: 5f, targets: 2);
            BossAbilityDefinition rally = Ability(BossAbilityKind.Rally, speedFactor: 1.8f, sweep: 3f);
            BossAbilityDefinition regrow = Ability(BossAbilityKind.Regrow, damage: 25f);
            BossAbilityDefinition split = Ability(BossAbilityKind.Split, minion: minion, minions: 2, trigger: 0.5f);
            Configure(speed: 0f, armor: 50f, abilities: new[] { volley, rally, regrow, split });

            var structures = new StructureRegistry();
            var near = new FakeStructure(new Vector3(1f, 0f, 0f), false);
            var mid = new FakeStructure(new Vector3(2f, 0f, 0f), false);
            var far = new FakeStructure(new Vector3(4f, 0f, 0f), false);
            structures.Add(far);
            structures.Add(near);
            structures.Add(mid);

            int spawned = 0;
            float rallied = 0f;
            var abilities = _go.AddComponent<BossAbilities>();
            abilities.Initialize(structures, (d, boss) => spawned++, (origin, range) => null, (caller, factor, seconds) => rallied = factor);
            abilities.SetDamageMultiplier(2f);
            Launch(new TargetRegistry(), structures);

            _enemy.Health.TakeDamage(60f);
            Assert.Less(_enemy.Health.Armor, 50f);
            abilities.Step(0.1f);
            Assert.AreEqual(14f, near.Damage, 0.001f, "the two nearest take a bolt at twice the damage");
            Assert.AreEqual(14f, mid.Damage, 0.001f);
            Assert.AreEqual(0f, far.Damage, 0.001f);
            Assert.AreEqual(1.8f, rallied, 0.001f, "the rally reaches the spawner");
            Assert.AreEqual(45f, _enemy.Health.Armor, 0.001f, "the plates grew back by the regrow amount");
            Assert.AreEqual(0, spawned, "the split waits for half the health to go");

            _enemy.Health.TakeDamage(60f);
            abilities.Step(0.1f);
            Assert.AreEqual(2, spawned, "half the health gone, two minions break off");
            abilities.Step(0.1f);
            Assert.AreEqual(2, spawned, "and only once per share");

            Object.DestroyImmediate(minion);
            Object.DestroyImmediate(volley);
            Object.DestroyImmediate(rally);
            Object.DestroyImmediate(regrow);
            Object.DestroyImmediate(split);
        }

        [Test]
        public void RemainingRoute_StartsWhereTheEnemyStandsAndEndsAtTheRouteEnd()
        {
            Configure(speed: 1f);
            var end = new Vector3(0f, 0f, 10f);
            _enemy.Initialize(_definition, new[] { Vector3.zero, new Vector3(0f, 0f, 5f), end }, 1f, 1f, 0f, new TargetRegistry(), new StructureRegistry());
            _enemy.Step(2f);

            Vector3[] route = _enemy.RemainingRoute();

            Assert.AreEqual(3, route.Length);
            Assert.AreEqual(2f, route[0].z, 0.001f);
            Assert.AreEqual(end, route[route.Length - 1]);
        }

        [Test]
        public void StructureRegistry_DamagesInsideARadiusAndFindsTheNearestInRange()
        {
            var structures = new StructureRegistry();
            var near = new FakeStructure(new Vector3(1f, 0f, 0f), false);
            var far = new FakeStructure(new Vector3(6f, 0f, 0f), false);
            var beside = new FakeStructure(new Vector3(3f, 0f, 2f), false);
            structures.Add(near);
            structures.Add(far);
            structures.Add(beside);

            Assert.AreEqual(1, structures.DamageInRadius(Vector3.zero, 2f, 10f));
            Assert.AreEqual(10f, near.Damage, 0.001f);
            Assert.AreEqual(0f, far.Damage, 0.001f);

            Assert.AreSame(near, structures.FindNearest(Vector3.zero, 4f), "the laser locks onto the closest structure");
            Assert.AreSame(beside, structures.FindNearest(new Vector3(3f, 0f, 4f), 3f));
            Assert.IsNull(structures.FindNearest(new Vector3(20f, 0f, 0f), 2f), "nothing in reach, no target");

            var several = new List<IStructure>();
            structures.FindNearest(Vector3.zero, 10f, 2, several);
            Assert.AreEqual(2, several.Count);
            Assert.AreSame(near, several[0], "closest first");
            Assert.AreSame(beside, several[1]);
        }

        [Test]
        public void CreatureModels_BuildForEveryShape()
        {
            foreach (EnemyShape shape in System.Enum.GetValues(typeof(EnemyShape)))
            {
                if (shape == EnemyShape.None)
                {
                    continue;
                }

                EnemyModelFactory.Parts parts = EnemyModelFactory.Create(shape);
                Assert.Greater(parts.Body.vertexCount, 0, $"{shape} has no body");
                Assert.Greater(parts.Moving.Count, 0, $"{shape} has nothing that moves");
                Assert.Greater(parts.Height, 0f, $"{shape} has no height");
                Assert.AreEqual(EnemyModelFactory.SlotCount, parts.Body.subMeshCount, $"{shape} does not use the creature palette");
            }

            Assert.Greater(EnemyModelFactory.Create(EnemyShape.Roller).BallRadius, 0f, "the roller curls into a ball");
            Assert.Greater(EnemyModelFactory.Create(EnemyShape.Wisp).HoverHeight, 0f, "the wisp floats");
            Assert.IsTrue(EnemyModelFactory.Create(EnemyShape.Bulwark).Moving.Exists(p => p.Role == CreatureRole.Plate), "the bulwark wears plates");
            Assert.IsTrue(EnemyModelFactory.Create(EnemyShape.Stinger).Moving.Exists(p => p.Role == CreatureRole.Stinger), "the scorpion has a tail");
        }

        [Test]
        public void Flyer_BesiegesTheBaseAtTheEndOfItsRoute_UntilItFalls()
        {
            Configure(speed: 50f, flies: true, attackRange: 2f, attackInterval: 0.5f);
            var structures = new StructureRegistry();
            var outpost = new FakeStructure(new Vector3(0f, 0f, 101f), true);
            structures.Add(outpost);
            Launch(new TargetRegistry(), structures);
            bool reached = false;
            _enemy.ReachedEnd += _ => reached = true;

            _enemy.Step(3f);
            Assert.IsFalse(reached, "a living base holds the flyer at the end of its route");
            Assert.AreEqual(4f, outpost.Damage, 0.001f, "and takes the first blow");

            _enemy.Step(0.6f);
            Assert.AreEqual(8f, outpost.Damage, 0.001f, "another after the interval");

            outpost.BlocksGround = false;
            _enemy.Step(0.1f);
            Assert.IsTrue(reached, "with the base down it slips through");
        }

        [Test]
        public void Deactivate_TellsTheViewsOnce()
        {
            Configure(speed: 1f);
            Launch(new TargetRegistry(), new StructureRegistry());
            int calls = 0;
            _enemy.Deactivated += _ => calls++;

            _enemy.Deactivate();
            _enemy.Deactivate();
            Assert.AreEqual(1, calls, "a second call does nothing");
        }

        private void Launch(TargetRegistry targets, StructureRegistry structures, float structureDamage = 1f)
        {
            _enemy.Initialize(_definition, new[] { Vector3.zero, new Vector3(0f, 0f, 100f) }, 1f, 1f, 0f, targets, structures, 1f, EnemyOrigin.Gate, 0f, structureDamage);
        }

        private static BossAbilityDefinition Ability(BossAbilityKind kind, float damage = 0f, float range = 5f, int targets = 3, float speedFactor = 1.5f, float sweep = 3f, EnemyDefinition minion = null, int minions = 1, float trigger = 0.25f)
        {
            var ability = ScriptableObject.CreateInstance<BossAbilityDefinition>();
            var so = new SerializedObject(ability);
            so.FindProperty("_kind").enumValueIndex = (int)kind;
            so.FindProperty("_interval").floatValue = 100f;
            so.FindProperty("_firstDelay").floatValue = 0f;
            so.FindProperty("_damage").floatValue = damage;
            so.FindProperty("_range").floatValue = range;
            so.FindProperty("_targetCount").intValue = targets;
            so.FindProperty("_speedFactor").floatValue = speedFactor;
            so.FindProperty("_sweepSeconds").floatValue = sweep;
            so.FindProperty("_minion").objectReferenceValue = minion;
            so.FindProperty("_minionCount").intValue = minions;
            so.FindProperty("_triggerFraction").floatValue = trigger;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ability;
        }

        private void Configure(
            float speed,
            TargetLayers layer = TargetLayers.Ground,
            bool flies = false,
            float walkSeconds = 0f,
            float rollSeconds = 0f,
            float rollSpeed = 2.5f,
            int gateStrikes = 0,
            float attackDamage = 0f,
            float attackRange = 2.5f,
            float attackInterval = 1f,
            float armor = 0f,
            float trailSeconds = 0f,
            float trailHaste = 1.4f,
            float trailDamage = 0f,
            BossAbilityDefinition[] abilities = null)
        {
            var so = new SerializedObject(_definition);
            so.FindProperty("_layer").intValue = (int)(flies ? TargetLayers.Air : layer);
            so.FindProperty("_flies").boolValue = flies;
            so.FindProperty("_maxHealth").floatValue = 100f;
            so.FindProperty("_armor").floatValue = armor;
            so.FindProperty("_armorReduction").floatValue = 0.5f;
            so.FindProperty("_speed").floatValue = Mathf.Max(0.1f, speed);
            so.FindProperty("_walkSeconds").floatValue = walkSeconds;
            so.FindProperty("_rollSeconds").floatValue = rollSeconds;
            so.FindProperty("_rollSpeedMultiplier").floatValue = rollSpeed;
            so.FindProperty("_gateStrikes").intValue = gateStrikes;
            so.FindProperty("_attackDamage").floatValue = attackDamage;
            so.FindProperty("_attackRange").floatValue = attackRange;
            so.FindProperty("_attackInterval").floatValue = attackInterval;
            so.FindProperty("_blockadeDamage").floatValue = 4f;
            so.FindProperty("_trailSeconds").floatValue = trailSeconds;
            so.FindProperty("_trailHaste").floatValue = trailHaste;
            so.FindProperty("_trailDamagePerSecond").floatValue = trailDamage;
            SerializedProperty list = so.FindProperty("_abilities");
            list.arraySize = abilities?.Length ?? 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = abilities[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
