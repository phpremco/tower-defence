using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The clock for short shot effects (beams, muzzle flashes): the slower of game time and real time. At x2 or x3 an
    /// effect timed in game time is gone before the eye catches it, so it keeps at least its real-time length; a pause or
    /// a slow-motion beat still holds it, as it holds the rest of the world. View code only, never gameplay timing.
    /// </summary>
    public static class FeedbackClock
    {
        public static float DeltaTime => Mathf.Min(Time.deltaTime, Time.unscaledDeltaTime);
    }
}
