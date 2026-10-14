using Game.Combat;
using Game.Core;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class TowerTests
    {
        private TowerDefinition _definition;
        private GameObject _go;
        private Tower _tower;

        [SetUp]
        public void SetUp()
        {
            _definition = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Cannon.asset");
            Assert.IsNotNull(_definition, "Run Tools/Generate/Scenes and Starter Data first.");

            _go = new GameObject("Tower");
            _tower = _go.AddComponent<Tower>();
            _tower.enabled = false;
            var targets = new TargetRegistry();
            _tower.Initialize(_definition, targets, new ProjectilePool(targets, _go.transform), PlayerModifiers.None);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void SellValue_IsShareOfEverythingSpent()
        {
            _tower.Upgrade();
            int spent = _definition.GetLevel(0).Cost + _definition.GetLevel(1).Cost;

            Assert.AreEqual(spent, _tower.TotalSpent);
            Assert.AreEqual(Mathf.FloorToInt(spent * _definition.SellRefundFraction), _tower.SellValue);
        }

        [Test]
        public void Damage_ReducesPowerAndRepairRestoresIt()
        {
            Assert.AreEqual(1f, _tower.Power, 0.001f);
            Assert.AreEqual(0, _tower.RepairCost);

            _tower.TakeDamage(_tower.Health.Max * 0.5f);

            Assert.Less(_tower.Power, 1f);
            Assert.Greater(_tower.Power, _definition.DamagedPowerFloor);
            Assert.Greater(_tower.RepairCost, 0);

            _tower.RepairFully();
            Assert.AreEqual(1f, _tower.Power, 0.001f);
        }

        [Test]
        public void Wreck_StopsWorkingAndScrapsForLess()
        {
            int sellWhole = _tower.SellValue;

            _tower.TakeDamage(_tower.Health.Max * 2f);

            Assert.IsTrue(_tower.IsWrecked);
            Assert.IsFalse(_tower.IsAlive);
            Assert.IsFalse(_tower.CanUpgrade);
            Assert.Less(_tower.SellValue, sellWhole);
        }
    }
}
