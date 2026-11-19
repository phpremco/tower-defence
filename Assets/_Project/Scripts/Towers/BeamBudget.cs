using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// What a beam pays for the pulse that kills (round 85's rule, charged when the pulse is fired since round 86). A beam
    /// holds one creature and swings to the next the moment it dies: it wastes nothing on a dead body and gains nothing
    /// either, so the killing pulse costs the share of it the creature still needed, never less than the sweep to the
    /// next creature. The price is settled as the pulse leaves, on what the creature needs then less what the pulses
    /// already on their way to it will take. Settled a frame later (round 85) it depended on the frame: a second pulse
    /// owed in the same frame was fired at a body that was as good as dead, at a stale price, and a kill cost a whole
    /// pulse more at a high game speed or a low frame rate than at sixty frames a second. Pure, so it is tested without
    /// a scene.
    /// </summary>
    public static class BeamBudget
    {
        /// <summary>What the creature still needs from this pulse: the one hit that would kill it now, less the pulses under way.</summary>
        public static float Needed(float hitToKill, float underWay) => hitToKill - underWay;

        /// <summary>True when a pulse of this size ends the creature (it is never asked of one that cannot say what it needs).</summary>
        public static bool Kills(float needed, float damage) => damage > 0f && needed <= damage;

        /// <summary>
        /// Seconds the beam owes for a pulse: the whole interval for one that does not kill, else the share the creature
        /// needed, at least the sweep to the next creature. A beam that does not sweep (no sweep time) pays every pulse in full.
        /// </summary>
        public static float Seconds(float interval, float needed, float damage, float sweepSeconds)
        {
            if (!Kills(needed, damage) || sweepSeconds <= 0f)
            {
                return interval;
            }

            return Mathf.Max(Mathf.Min(sweepSeconds, interval), interval * Mathf.Clamp01(needed / damage));
        }
    }
}
