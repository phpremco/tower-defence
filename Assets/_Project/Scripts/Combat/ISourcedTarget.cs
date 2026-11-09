namespace Game.Combat
{
    /// <summary>
    /// A target that wants to know who hit it and how: a boss keeps a tally of the damage per tower (the one that hurt it
    /// most is the one it goes for), and a hidden boss only feels blasts. Projectiles call this instead of
    /// <see cref="ITargetable.TakeDamage"/> when the target offers it.
    /// </summary>
    public interface ISourcedTarget
    {
        /// <param name="source">What fired the shot (a tower), or null.</param>
        /// <param name="splash">True for a blast that caught the target, false for a shot aimed at it.</param>
        /// <param name="armourFactor">Share of the hit that works on the target's armour while it has any (Health.TakeDamage).</param>
        void TakeDamageFrom(float amount, object source, bool splash, float armourFactor = 1f);
    }
}
