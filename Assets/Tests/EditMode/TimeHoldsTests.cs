using Game.Levels;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class TimeHoldsTests
    {
        [Test]
        public void OverlappingHolds_TimeResumesOnlyAfterTheLastRelease()
        {
            var holds = new TimeHolds();
            var briefing = new object();
            var codex = new object();

            holds.Acquire(briefing);
            holds.Acquire(codex);
            holds.Release(briefing);

            Assert.IsTrue(holds.IsHeld);

            holds.Release(codex);

            Assert.IsFalse(holds.IsHeld);
        }

        [Test]
        public void SameOwnerTwice_CountsAsOneHold()
        {
            var holds = new TimeHolds();
            var owner = new object();

            Assert.IsTrue(holds.Acquire(owner));
            Assert.IsFalse(holds.Acquire(owner));
            holds.Release(owner);

            Assert.IsFalse(holds.IsHeld);
        }

        [Test]
        public void ReleaseWithoutHold_ChangesNothing()
        {
            var holds = new TimeHolds();
            var held = new object();
            holds.Acquire(held);

            Assert.IsFalse(holds.Release(new object()));
            Assert.AreEqual(1, holds.Count);
        }
    }
}
