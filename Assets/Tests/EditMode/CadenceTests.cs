using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>A repeated action keeps its rhythm whatever the frame length (high game speed, low frame rate).</summary>
    public sealed class CadenceTests
    {
        // Counts the actions of a gun with the given interval over a stretch of time cut into equal frames.
        private static int Shots(float interval, float frame, float seconds)
        {
            float cooldown = 0f;
            int shots = 0;
            for (float t = 0f; t < seconds - 1e-4f; t += frame)
            {
                cooldown -= frame;
                for (int done = 0; Cadence.Due(cooldown, done); done++)
                {
                    shots++;
                    cooldown = Cadence.Rearm(cooldown, interval);
                }

                cooldown = Cadence.Idle(cooldown);
            }

            return shots;
        }

        [Test]
        public void ShotsPerSecond_DoNotDependOnTheFrameLength()
        {
            int smooth = Shots(0.35f, 1f / 60f, 35f);
            Assert.AreEqual(100, smooth, 1);
            Assert.AreEqual(smooth, Shots(0.35f, 0.25f, 35f), 1, "a quarter-second frame keeps the rhythm");
            Assert.AreEqual(smooth, Shots(0.35f, 0.5f, 35f), 1, "a frame longer than the interval fires twice in some frames");
        }

        [Test]
        public void LongHitch_FiresAtMostTheCap_AndBanksNothing()
        {
            Assert.AreEqual(Cadence.MaxPerFrame, Shots(0.1f, 5f, 5f));
            Assert.AreEqual(0f, Cadence.Idle(-3f), "idle time is not credit for later");
            Assert.AreEqual(0.2f, Cadence.Idle(0.2f));
        }
    }
}
