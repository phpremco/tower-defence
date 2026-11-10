using Game.Levels;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// The shoulder buttons walk the whole build bar: the towers, then the street weapons, round again. Before round 32
    /// they only walked the towers, so a gamepad could never pick a street weapon.
    /// </summary>
    public sealed class SelectionCycleTests
    {
        [Test]
        public void Forward_FromTheLastTower_ReachesTheFirstStreetWeapon()
        {
            Assert.AreEqual(3, SelectionCycle.Next(2, 3, 2, 1));
        }

        [Test]
        public void Forward_FromTheLastStreetWeapon_WrapsToTheFirstTower()
        {
            Assert.AreEqual(0, SelectionCycle.Next(4, 3, 2, 1));
        }

        [Test]
        public void Back_FromTheFirstTower_WrapsToTheLastStreetWeapon()
        {
            Assert.AreEqual(4, SelectionCycle.Next(0, 3, 2, -1));
        }

        [Test]
        public void WithoutStreetWeapons_OnlyTheTowersCycle()
        {
            Assert.AreEqual(0, SelectionCycle.Next(2, 3, 0, 1));
            Assert.AreEqual(2, SelectionCycle.Next(0, 3, 0, -1));
        }

        [Test]
        public void AnEmptyBar_HasNothingToPick()
        {
            Assert.AreEqual(-1, SelectionCycle.Next(0, 0, 0, 1));
        }

        [Test]
        public void AnUnknownPosition_StartsFromTheFirstCard()
        {
            Assert.AreEqual(1, SelectionCycle.Next(-1, 3, 2, 1));
        }
    }
}
