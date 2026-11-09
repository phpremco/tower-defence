namespace Game.Combat
{
    /// <summary>
    /// Lands a hit on a target the way a projectile does: through <see cref="ISourcedTarget"/> when the target offers it,
    /// so the weapon's armour factor and its source reach the creature, else the plain <see cref="ITargetable"/> hit.
    /// For the hero's blows and abilities and anything else that hurts a creature without a projectile.
    /// </summary>
    public static class Hits
    {
        /// <param name="armourFactor">Share of the hit that works on the target's armour while it has any.</param>
        /// <param name="splash">True for a blast that caught the target (it reaches a hidden boss in part), false for an aimed blow.</param>
        public static void Deal(ITargetable target, float damage, float armourFactor, object source = null, bool splash = true)
        {
            if (target is ISourcedTarget sourced)
            {
                sourced.TakeDamageFrom(damage, source, splash, armourFactor);
                return;
            }

            target.TakeDamage(damage);
        }
    }
}
