namespace Game.PathObjects
{
    /// <summary>
    /// What a street weapon does to the enemy, as the player reads it: the badge on its card and the words in the
    /// loadout screen follow this, independent of the <see cref="PathObjectKind"/> that drives the behaviour.
    /// </summary>
    public enum PathObjectEffect
    {
        /// <summary>Slows what passes through it.</summary>
        Slow,

        /// <summary>Hidden on the road, goes off once under the first enemy.</summary>
        Trap,

        /// <summary>Bursts and hits a wide area when set off.</summary>
        Blast,

        /// <summary>Rolls down the road and hurts everything it touches.</summary>
        Roll,

        /// <summary>Drives the road and fights what it meets.</summary>
        Patrol,

        /// <summary>Catches and slows flyers overhead.</summary>
        AirNet,
    }
}
