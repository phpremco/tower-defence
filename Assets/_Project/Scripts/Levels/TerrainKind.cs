namespace Game.Levels
{
    /// <summary>
    /// What a cell of the landscape is made of. Raised ground is only a height difference; the three liquids
    /// (water, lava, quicksand) cannot hold a tower.
    /// </summary>
    public enum TerrainKind
    {
        Ground = 0,
        Raised = 1,
        Water = 2,
        Lava = 3,
        Quicksand = 4,
    }
}
