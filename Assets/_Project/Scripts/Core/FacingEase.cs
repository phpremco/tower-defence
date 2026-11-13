using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Eases a facing towards a new heading instead of snapping to it: a creature reaching a corner of its road, or turning
    /// to a gate it attacks, comes round over a few frames. Exponential, so the same turn takes the same time at any frame
    /// rate, and always the short way round.
    /// </summary>
    public static class FacingEase
    {
        /// <param name="current">The facing now, degrees of yaw.</param>
        /// <param name="target">The heading to come round to, degrees of yaw.</param>
        /// <param name="deltaTime">Seconds since the last step (game time, so a faster game turns faster with the walk).</param>
        /// <param name="seconds">Time constant: after this long about two thirds of the turn is done; zero snaps.</param>
        public static float Step(float current, float target, float deltaTime, float seconds)
        {
            if (seconds <= 0f)
            {
                return target;
            }

            return current + Mathf.DeltaAngle(current, target) * (1f - Mathf.Exp(-deltaTime / seconds));
        }

        /// <summary>
        /// The same ease with its turn rate capped: a large turn (a boss coming round to the target of its beam) starts at a
        /// steady rate instead of the exponential's fast first frames, and eases in once it is close.
        /// </summary>
        /// <param name="maxDegreesPerSecond">The fastest it may turn, in degrees per second of game time.</param>
        public static float Step(float current, float target, float deltaTime, float seconds, float maxDegreesPerSecond)
        {
            float limit = maxDegreesPerSecond * deltaTime;
            float turn = Mathf.DeltaAngle(current, Step(current, target, deltaTime, seconds));
            return current + Mathf.Clamp(turn, -limit, limit);
        }
    }
}
