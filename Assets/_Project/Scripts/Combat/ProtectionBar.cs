namespace Game.Combat
{
    /// <summary>
    /// What the blue line over a creature's health bar shows. Armour is a pool that hits chip away, so the line is the
    /// share of it still on. Plating never wears off (every hit loses the same points), so a plated creature carries a
    /// full line for as long as it lives; its view flashes the line when the plates stop part of a hit. A creature with
    /// both shows its armour while any is left, then its plates. No armour and no plating: no line.
    /// </summary>
    public static class ProtectionBar
    {
        /// <summary>True when the creature has armour or plating, so its bars show from the start, before any hit.</summary>
        public static bool Shows(Health health) => health != null && (health.ArmorMax > 0f || health.Plating > 0f);

        /// <summary>The blue line's length, 0 to 1.</summary>
        public static float Line(Health health)
        {
            if (health == null)
            {
                return 0f;
            }

            if (health.ArmorMax > 0f && health.Armor > 0f)
            {
                return health.ArmorFraction;
            }

            return health.Plating > 0f ? 1f : 0f;
        }
    }
}
