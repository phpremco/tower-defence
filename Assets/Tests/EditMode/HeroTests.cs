using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using Game.Hero;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class HeroTests
    {
        private sealed class FakeGuarded : IGuardable
        {
            public Vector3 Position { get; set; }

            public IDamageGuard Guard { get; set; }

            public float Restored { get; private set; }

            public void Restore(float fraction) => Restored += fraction;
        }

        private HeroDefinition _definition;
        private HeroWeaponDefinition _weapon;
        private GameObject _go;
        private Commander _hero;
        private FakeGuarded _first;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<HeroDefinition>();
            _weapon = ScriptableObject.CreateInstance<HeroWeaponDefinition>();
            var definitionProps = new UnityEditor.SerializedObject(_definition);
            definitionProps.FindProperty("_weapon").objectReferenceValue = _weapon;
            definitionProps.ApplyModifiedPropertiesWithoutUndo();
            _go = new GameObject("Hero");
            _hero = _go.AddComponent<Commander>();
            _first = new FakeGuarded { Position = Vector3.zero };
            var second = new FakeGuarded { Position = new Vector3(5f, 0f, 0f) };
            var posts = new List<HeroPost>
            {
                new HeroPost(_first, new Vector3(-0.65f, 0f, 0f), Vector3.right),
                new HeroPost(second, new Vector3(4.35f, 0f, 0f), Vector3.right),
            };
            _hero.Initialize(_definition, new TargetRegistry(), posts, 0, PlayerModifiers.None);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_definition);
            Object.DestroyImmediate(_weapon);
        }

        [Test]
        public void Hero_StandsInFrontOfTheGuardedStructure()
        {
            Assert.AreEqual(-0.65f, _go.transform.position.x, 0.001f);
            Assert.IsTrue(_hero.IsAt(_first.Position));
        }

        [Test]
        public void StandingHero_GuardsItsPostAndTakesItsShare()
        {
            Assert.AreSame(_hero, _first.Guard);

            float passedThrough = _hero.Absorb(100f);

            Assert.AreEqual(100f * (1f - _definition.GuardShare), passedThrough, 0.001f);
            Assert.AreEqual(_definition.MaxHealth - 100f * _definition.GuardShare, _hero.Health.Current, 0.001f);
        }

        [Test]
        public void MovingAway_ReleasesTheGuardUntilArrival()
        {
            _hero.MoveTo(1);

            Assert.IsNull(_first.Guard);
            Assert.IsTrue(_hero.IsWalking);
            Assert.AreEqual(100f, _hero.Absorb(100f), 0.001f);
        }

        [Test]
        public void HeroWithoutHealth_IsDownAndAbsorbsNothing()
        {
            _hero.Health.TakeDamage(_definition.MaxHealth);

            Assert.IsTrue(_hero.IsDown);
            Assert.IsNull(_first.Guard);
            Assert.AreEqual(50f, _hero.Absorb(50f), 0.001f);
        }

        [Test]
        public void EmergencyRepair_RestoresThePostOnceAndThenCoolsDown()
        {
            Assert.IsTrue(_hero.CanEmergencyRepair);
            Assert.IsTrue(_hero.TryEmergencyRepair());
            Assert.AreEqual(_definition.RepairFraction, _first.Restored, 0.001f);
            Assert.IsFalse(_hero.CanEmergencyRepair);
            Assert.IsFalse(_hero.TryEmergencyRepair());
        }
        private sealed class FakeTarget : ITargetable
        {
            public Vector3 Position { get; set; }

            public TargetLayers Layer => TargetLayers.Ground;

            public float PathProgress => 0f;

            public bool IsAlive => true;

            public float Taken { get; private set; }

            public void TakeDamage(float amount) => Taken += amount;

            public void ApplySlow(float speedFactor, float seconds)
            {
            }
        }

        [Test]
        public void Hero_ShootsWhatTheBladeCannotReach_AndUsesTheGunsPace()
        {
            var gun = ScriptableObject.CreateInstance<HeroWeaponDefinition>();
            var gunProps = new UnityEditor.SerializedObject(gun);
            gunProps.FindProperty("_kind").intValue = (int)HeroWeaponKind.Ranged;
            gunProps.FindProperty("_damage").floatValue = 4f;
            gunProps.FindProperty("_reach").floatValue = 3.5f;
            gunProps.FindProperty("_attackInterval").floatValue = 0.35f;
            gunProps.ApplyModifiedPropertiesWithoutUndo();
            var definitionProps = new UnityEditor.SerializedObject(_definition);
            definitionProps.FindProperty("_rangedWeapon").objectReferenceValue = gun;
            definitionProps.ApplyModifiedPropertiesWithoutUndo();

            var targets = new TargetRegistry();
            var far = new FakeTarget { Position = new Vector3(-0.65f + 3f, 0f, 0f) };
            targets.Add(far);
            var go = new GameObject("Hero2");
            var hero = go.AddComponent<Commander>();
            HeroWeaponDefinition used = null;
            hero.Struck += (position, weapon) => used = weapon;
            hero.Initialize(_definition, targets, new List<HeroPost> { new HeroPost(_first, new Vector3(-0.65f, 0f, 0f), Vector3.right) }, 0, PlayerModifiers.None);

            // Beyond the blade (reach 1.5) but inside the gun's reach: the gun fires, for its own damage. Update is
            // driven by hand, because SendMessage asserts outside Play Mode.
            typeof(Commander).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(hero, null);

            Assert.AreSame(gun, used);
            Assert.AreEqual(4f, far.Taken, 0.001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(gun);
        }
    }
}
