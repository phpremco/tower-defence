using Game.Hero;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 57: the hero's repeated fire holds the aim and lays a kick per shot on top of it. The kick is nothing on the shot's
    /// own frame and stays small while the shot's bolt is lit (the bolt leaves the barrel in line), peaks once, dies away, and
    /// shots in a row add up without a jump from one frame to the next.
    /// </summary>
    public sealed class RecoilKickTests
    {
        private const float Peak = 0.06f;
        private const float Bolt = 0.09f;
        private const float Frame = 1f / 60f;

        [Test]
        public void Kick_IsNothingOnTheShotFrame_SmallUnderTheBolt_AndDiesAway()
        {
            Assert.AreEqual(0f, RecoilKick.At(0f, Peak, Bolt), "the shot frame itself must not kick the barrel off the target");
            Assert.AreEqual(0f, RecoilKick.At(-0.1f, Peak, Bolt));
            Assert.Less(RecoilKick.At(Bolt * 0.5f, Peak, Bolt), 0.25f, "half way through the bolt the barrel should still be near its line");
            Assert.Greater(RecoilKick.At(Bolt * 1.2f, Peak, Bolt), 0.6f, "once the bolt is out the kick shows");
            Assert.AreEqual(1f, RecoilKick.At(Peak, Peak, 0f), 1e-5f, "without a hold it peaks at the peak time");
            Assert.Less(RecoilKick.At(Peak * 6f, Peak, Bolt), 0.05f, "spent after a few tenths of a second");
            Assert.AreEqual(0f, RecoilKick.At(float.PositiveInfinity, Peak, Bolt), "an unfired slot adds nothing");
            Assert.AreEqual(0f, RecoilKick.Sum(RecoilKick.NoShots(3), 5f, Peak, Bolt));
        }

        [Test]
        public void ShotsInARow_AddUpWithoutAJump()
        {
            float[] shots = RecoilKick.NoShots(3);
            float[] fireAt = { 0.1f, 0.3f, 0.5f };
            int next = 0;
            float previous = 0f;
            float largest = 0f;
            for (float now = 0f; now < 1f; now += Frame)
            {
                if (next < fireAt.Length && now >= fireAt[next])
                {
                    shots[next] = now;
                    next++;
                }

                float kick = RecoilKick.Sum(shots, now, Peak, Bolt);
                largest = Mathf.Max(largest, Mathf.Abs(kick - previous));
                previous = kick;
            }

            // One frame never moves the kick by much more than a third of its peak (0.36 at 60 fps): it eases out of the shot and rises over several frames.
            Assert.Less(largest, 0.4f, "a shot snaps the pose");
            Assert.Greater(largest, 0.05f, "no kick at all");
        }
    }
}
