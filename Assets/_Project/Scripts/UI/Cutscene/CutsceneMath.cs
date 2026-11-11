using UnityEngine;

namespace Game.UI
{
    /// <summary>Small timing helpers for set choreography, so every set reads the same way: share of a window, eases.</summary>
    public static class CutsceneMath
    {
        private const float BackOvershoot = 1.70158f;

        /// <summary>How far <paramref name="value"/> has come through the window from <paramref name="from"/> to <paramref name="to"/>, 0..1.</summary>
        public static float Span(float value, float from, float to) => to > from ? Mathf.Clamp01((value - from) / (to - from)) : (value >= to ? 1f : 0f);

        public static float Smooth(float t) => t * t * (3f - 2f * t);

        /// <summary>Overshoots a little past one and settles back: a part snapping into place.</summary>
        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * ((BackOvershoot + 1f) * t + BackOvershoot);
        }

        /// <summary>
        /// The heading of someone turning from <paramref name="fromYaw"/> to <paramref name="toYaw"/> (degrees), <paramref name="since"/>
        /// seconds after the turn began, the short way round at <paramref name="degreesPerSecond"/> on average, eased in and out,
        /// so a turn of any size takes its natural time and never snaps (round 39).
        /// </summary>
        public static float EasedTurn(float fromYaw, float toYaw, float since, float degreesPerSecond)
        {
            float delta = Mathf.DeltaAngle(fromYaw, toYaw);
            float seconds = TurnSeconds(delta, degreesPerSecond);
            return fromYaw + delta * Smooth(seconds > 0f ? Mathf.Clamp01(since / seconds) : 1f);
        }

        /// <summary>How long a turn of <paramref name="degrees"/> takes at <paramref name="degreesPerSecond"/> on average.</summary>
        public static float TurnSeconds(float degrees, float degreesPerSecond) => degreesPerSecond > 0f ? Mathf.Abs(degrees) / degreesPerSecond : 0f;

        /// <summary>A stable pseudo-random number in 0..1 for an index, so seeded scatter never changes between runs.</summary>
        public static float Hash(int index, int salt)
        {
            uint h = (uint)(index * 374761393 + salt * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
