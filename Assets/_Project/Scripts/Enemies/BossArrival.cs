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

        /// <summary>A sealed gate in the old ruins (the level's boss entrance) opens for the first and only time.</summary>
        Ruins = 3,

        /// <summary>The map comes to it: the ground near the start of the main road cracks open and a new road of light runs from there to the base.</summary>
        Rift = 4,
    }
}
