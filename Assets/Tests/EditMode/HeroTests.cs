using System.Collections.Generic;
using Game.Combat;
using Game.Hero;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class HeroTests
    {
        private sealed class FakePost : IGuardable
        {
            public Vector3 Position { get; set; }

            public IDamageGuard Guard { get; set; }

            public float Restored { get; private set; }

            public void Restore(float fraction) => Restored += fraction;
        }

        private HeroDefinition _definition;
        private GameObject _go;
        private Commander _hero;
        private FakePost _first;
        private FakePost _second;

        [SetUp]
        public void SetUp()
        {
            _definition = ScriptableObject.CreateInstance<HeroDefinition>();
            _go = new GameObject("Hero");
            _hero = _go.AddComponent<Commander>();
            _first = new FakePost { Position = Vector3.zero };
            _second = new FakePost { Position = new Vector3(5f, 0f, 0f) };
            var targets = new TargetRegistry();
            var projectiles = new ProjectilePool(targets, _go.transform);
            _hero.Initialize(_definition, targets, projectiles, new List<IGuardable> { _first, _second }, 0);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_definition);
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
            Assert.IsTrue(_hero.TryEmergencyRepair());
            Assert.AreEqual(_definition.RepairFraction, _first.Restored, 0.001f);
            Assert.IsFalse(_hero.TryEmergencyRepair());
        }
    }
}
