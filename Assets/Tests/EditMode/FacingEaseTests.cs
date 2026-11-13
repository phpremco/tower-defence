using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>Round 47: a creature comes round to a new heading over a few frames, the short way, at any frame rate.</summary>
    public sealed class FacingEaseTests
    {
        private const float Step = 1f / 60f;

        [Test]
        public void FacingEase_TurnsSmoothlyTheShortWayRound()
        {
            float yaw = 170f;
            float previous = yaw;
            for (int f = 0; f < 60; f++)
            {
                yaw = FacingEase.Step(yaw, -170f, Step, 0.08f);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(previous, yaw)), 5f);
                previous = yaw;
            }

            Assert.AreEqual(0f, Mathf.DeltaAngle(yaw, -170f), 0.01f);
            float once = FacingEase.Step(0f, 90f, 2f * Step, 0.08f);
            float twice = FacingEase.Step(FacingEase.Step(0f, 90f, Step, 0.08f), 90f, Step, 0.08f);
            Assert.AreEqual(once, twice, 1e-3f, "the same turn at any frame rate");
            Assert.AreEqual(90f, FacingEase.Step(0f, 90f, Step, 0f), 1e-5f, "zero seconds snaps");
        }

        // Round 58: the ray boss comes round to its beam's target at a capped rate, the short way, and still arrives.
        [Test]
        public void FacingEase_CappedTurn_NeverExceedsItsRateAndArrives()
        {
            const float cap = 120f;
            float yaw = 90f;
            for (int f = 0; f < 180; f++)
            {
                float next = FacingEase.Step(yaw, -60f, Step, 0.25f, cap);
                Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(yaw, next)), cap * Step + 1e-4f);
                Assert.LessOrEqual(Mathf.DeltaAngle(yaw, next), 0f, "the short way round");
                yaw = next;
            }

            Assert.AreEqual(0f, Mathf.DeltaAngle(yaw, -60f), 0.5f);
            Assert.AreEqual(FacingEase.Step(10f, 12f, Step, 0.25f), FacingEase.Step(10f, 12f, Step, 0.25f, cap), 1e-5f, "a small turn eases as before");
        }
    }
}
