namespace Game.Levels
{
    /// <summary>
    /// What a decoration piece does when a continent boss walks into it off the road (Docs/BOSSES.md round 13b). Set per
    /// imported mesh by the environment generator, so the logic never asks what a piece is called.
    /// </summary>
    public enum DecorContact
    {
        /// <summary>Flat or tiny (pebbles, shells, kerbs, banks): the body passes over it.</summary>
        None = 0,

        /// <summary>Small and brittle (crystals, flora, small rocks): smashed when the body overlaps it, remains left.</summary>
        Breakable = 1,

        /// <summary>Large and solid (spires, columns, cliffs): a walk steers round it.</summary>
        Obstacle = 2,

        /// <summary>A low rise (boulders, drifts, terrace steps): the body climbs over it, its height following the top.</summary>
        Climbable = 3,
    }
}
