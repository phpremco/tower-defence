using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// How fast the camera pans, in world units per second. A tap moves about one and a half screen heights
    /// per second, so it feels the same at any zoom. Held, the speed climbs within a second to a top speed that
    /// crosses the whole field in a few seconds however large the map is, or to a couple of screens per second on a
    /// small map. The player's camera-speed factor scales both.
    /// </summary>
    public static class PanSpeedCurve
    {
        /// <summary>Screen heights per second at the first moment of a pan.</summary>
        public const float ScreensPerSecond = 1.4f;

        /// <summary>The top speed is at least this many times the starting speed.</summary>
        public const float MinBoost = 2.2f;

        /// <summary>Seconds of held panning a full traverse of the field's long side takes at top speed.</summary>
        public const float FieldCrossSeconds = 3f;

        /// <summary>Seconds of holding before the top speed is reached.</summary>
        public const float RampSeconds = 0.9f;

        public static float StartSpeed(float viewHeight, float userScale)
        {
            return ScreensPerSecond * viewHeight * userScale;
        }

        public static float TopSpeed(float viewHeight, float fieldSpan, float userScale)
        {
            float start = StartSpeed(viewHeight, userScale);
            return Mathf.Max(start * MinBoost, fieldSpan / FieldCrossSeconds * userScale);
        }

        /// <summary>The speed after the pan has been held for <paramref name="heldSeconds"/>, eased in.</summary>
        public static float Speed(float viewHeight, float fieldSpan, float heldSeconds, float userScale)
        {
            float ramp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(heldSeconds / RampSeconds));
            return Mathf.Lerp(StartSpeed(viewHeight, userScale), TopSpeed(viewHeight, fieldSpan, userScale), ramp);
        }
    }
}
