namespace Game.Enemies
{
    /// <summary>How a continent boss enters the map, on top of the spawn source its wave names.</summary>
    public enum BossArrival
    {
        /// <summary>Wherever the wave sends it: a gate, a pool or the level's own boss entrance.</summary>
        Wave = 0,

        /// <summary>A crack runs across the map to the colony drill rig; the boss breaks out under it and smashes it.</summary>
        ColonyRig = 1,

        /// <summary>It rises out of the largest pool of a liquid it dives in (a lava pool, a frozen lake that cracks open).</summary>
        Pool = 2,
    }
}
