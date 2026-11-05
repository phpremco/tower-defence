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
        void TakeDamageFrom(float amount, object source, bool splash);
    }
}
