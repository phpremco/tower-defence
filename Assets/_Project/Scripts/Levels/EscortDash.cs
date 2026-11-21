using System;

namespace Game.Levels
{
    /// <summary>
    /// How long a creature of a boss's brood runs after it comes out of the boss (round 88c): until it is its lead ahead
    /// of the boss, which keeps walking behind it. Pure arithmetic for the spawner and the tests.
    /// </summary>
    public static class EscortDash
    {
        /// <summary>The longest a dash lasts: a creature that barely outruns its boss does not sprint down the whole road.</summary>
        public const float MostSeconds = 6f;

        /// <param name="lead">World units the creature must gain on the boss.</param>
        /// <param name="ownSpeed">Its own pace at a walk, world units a second.</param>
        /// <param name="bossSpeed">The boss's pace.</param>
        /// <param name="dashFactor">How many times its own pace it runs at.</param>
        public static float Seconds(float lead, float ownSpeed, float bossSpeed, float dashFactor)
        {
            float gain = ownSpeed * dashFactor - bossSpeed;
            if (lead <= 0f || gain <= 0f)
            {
                return 0f;
            }

            return Math.Min(MostSeconds, lead / gain);
        }

        /// <summary>
        /// Whether the creature can take a place ahead of its boss at all: only when its dash gains the lead in time. One
        /// that cannot (a plated walker behind a fast boss) is sent out behind the boss instead, and the boss leaves it.
        /// </summary>
        public static bool Reaches(float lead, float ownSpeed, float bossSpeed, float dashFactor)
        {
            float gain = ownSpeed * dashFactor - bossSpeed;
            return lead > 0f && gain > 0f && lead / gain < MostSeconds;
        }
    }
}
