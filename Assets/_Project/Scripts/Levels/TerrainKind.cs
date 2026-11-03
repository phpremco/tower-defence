namespace Game.Levels
{
    /// <summary>
    /// What a cell of the landscape is made of. Raised ground is only a height difference; the liquids (water,
    /// lava, quicksand, a frozen lake, a bog and a rift) cannot hold a tower.
    /// </summary>
    public enum TerrainKind
    {
        Ground = 0,
        Raised = 1,
        Water = 2,
        Lava = 3,
        Quicksand = 4,

        /// <summary>A frozen lake: a glossy pale sheet with cracks, nests of the frost bats.</summary>
        Ice = 5,

        /// <summary>A bog: dark still water under scum and reeds, nests of the mire bats.</summary>
        Bog = 6,

        /// <summary>A rift: a sunken fissure in the rock with glowing veins at the bottom, nests of the stone creatures.</summary>
        Rift = 7,
    }
}
