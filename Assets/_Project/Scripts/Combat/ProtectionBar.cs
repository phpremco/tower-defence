namespace Game.Combat
{
    /// <summary>
    /// What the blue line over a creature's health bar shows: the armour pool left, as a share of what it started with
    /// (Remco's design, round 24). Heavy weapons strip it fast, light fire chips it slowly, and the green line only moves
    /// once the blue one is gone. A creature with armour shows both lines from the start; one without shows no blue line.
    /// </summary>
    public static class ProtectionBar
    {
        /// <summary>True when the creature has armour, so its bars show from the start, before any hit.</summary>
        public static bool Shows(Health health) => health != null && health.ArmorMax > 0f;

        /// <summary>The blue line's length, 0 to 1.</summary>
        public static float Line(Health health) => health == null ? 0f : health.ArmorFraction;
    }
}
