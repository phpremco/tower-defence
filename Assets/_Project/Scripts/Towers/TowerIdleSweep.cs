using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// A tower with nothing to shoot keeps watch: after a pause its head sweeps slowly around where it last aimed. Shared
    /// by the procedural and the imported tower looks so both keep the same rhythm.
    /// </summary>
    public static class TowerIdleSweep
    {
        private const float IdleAfterSeconds = 2.5f;
        private const float SweepDegrees = 22f;
        private const float SweepSpeed = 0.45f;
        private const float EaseSeconds = 1.5f;

        /// <summary>True once the tower has been quiet long enough to look around.</summary>
        public static bool IsIdle(float sinceFire) => sinceFire >= IdleAfterSeconds;

        /// <summary>How far into the sweep it is, from 0 (just gone quiet) to 1 (sweeping fully).</summary>
        public static float Ease(float sinceFire) => Mathf.Clamp01((sinceFire - IdleAfterSeconds) / EaseSeconds);

        /// <summary>Degrees the head turns away from its last aim at this moment of the watch.</summary>
        public static float Degrees(float sinceFire, float phase)
        {
            return Mathf.Sin((sinceFire - IdleAfterSeconds) * SweepSpeed + phase) * SweepDegrees * Ease(sinceFire);
        }
    }
}
