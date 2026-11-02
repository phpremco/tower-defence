namespace Game.Levels
{
    /// <summary>
    /// Where a spawn group's enemies enter: through the gate of their path, up out of a kind of pool, out of the element
    /// nest that matches the creature's element, or through the level's boss entrance.
    /// </summary>
    public enum SpawnSource
    {
        Gate = 0,
        Water = 1,
        Lava = 2,
        Quicksand = 3,
        Nest = 4,
        Ice = 5,
        Bog = 6,

        /// <summary>
        /// The level's own boss entrance (<see cref="LevelDefinition.BossEntrance"/>): a pool or the open ground. Wave data
        /// chooses it per boss and per difficulty, so a boss may take the gate on one difficulty and the entrance on another.
        /// </summary>
        Entrance = 7,
    }
}
