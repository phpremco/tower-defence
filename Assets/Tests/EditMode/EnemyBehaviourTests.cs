using Game.Combat;
using Game.Core;
using Game.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The enemy logic that does not need a scene: rolling, gate strikes, boss powers and the remaining route.</summary>
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
            public bool BlocksGround { get; }
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
            Configure(TargetLayers.Ground, speed: 1f, walkSeconds: 1.2f, rollSeconds: 0.9f, rollSpeed: 3f);
            _enemy.Initialize(_definition, new[] { Vector3.zero, new Vector3(0f, 0f, 100f) }, 1f, 1f, 0f, new TargetRegistry(), new StructureRegistry());

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
            Configure(TargetLayers.Ground, speed: 1f, walkSeconds: 0.1f, rollSeconds: 10f, rollSpeed: 3f);
            var structures = new StructureRegistry();
            var gate = new FakeStructure(new Vector3(0f, 0f, 2f), true);
            _enemy.Initialize(_definition, new[] { Vector3.zero, new Vector3(0f, 0f, 100f) }, 1f, 1f, 0f, new TargetRegistry(), structures);

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
            Configure(TargetLayers.Air, speed: 1f, gateStrikes: 2, attackDamage: 5f, attackRange: 2f, attackInterval: 0.5f);
            var structures = new StructureRegistry();
            var gate = new FakeStructure(new Vector3(0f, 0f, 1f), true);
            structures.Add(gate);
            _enemy.Initialize(_definition, new[] { Vector3.zero, new Vector3(0f, 0f, 100f) }, 1f, 1f, 0f, new TargetRegistry(), structures);

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
        public void RemainingRoute_StartsWhereTheEnemyStandsAndEndsAtTheRouteEnd()
        {
            Configure(TargetLayers.Ground, speed: 1f);
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
            }

            Assert.Greater(EnemyModelFactory.Create(EnemyShape.Roller).BallRadius, 0f, "the roller curls into a ball");
        }

        private void Configure(TargetLayers layer, float speed, float walkSeconds = 0f, float rollSeconds = 0f, float rollSpeed = 2.5f, int gateStrikes = 0, float attackDamage = 0f, float attackRange = 2.5f, float attackInterval = 1f)
        {
            var so = new SerializedObject(_definition);
            so.FindProperty("_layer").intValue = (int)layer;
            so.FindProperty("_speed").floatValue = speed;
            so.FindProperty("_walkSeconds").floatValue = walkSeconds;
            so.FindProperty("_rollSeconds").floatValue = rollSeconds;
            so.FindProperty("_rollSpeedMultiplier").floatValue = rollSpeed;
            so.FindProperty("_gateStrikes").intValue = gateStrikes;
            so.FindProperty("_attackDamage").floatValue = attackDamage;
            so.FindProperty("_attackRange").floatValue = attackRange;
            so.FindProperty("_attackInterval").floatValue = attackInterval;
            so.FindProperty("_blockadeDamage").floatValue = 4f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
