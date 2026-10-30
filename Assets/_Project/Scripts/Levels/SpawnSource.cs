namespace Game.Levels
{
    /// <summary>
    /// Where a spawn group's enemies enter: through the gate of their path, up out of a kind of pool, or out of
    /// the element nest that matches the creature's element.
    /// </summary>
    public enum SpawnSource
    {
        Gate = 0,
        Water = 1,
        Lava = 2,
        Quicksand = 3,
        Nest = 4,
    }
}
