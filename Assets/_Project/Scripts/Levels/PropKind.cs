namespace Game.Levels
{
    /// <summary>The kinds of procedural decoration a level theme can scatter over its landscape.</summary>
    public enum PropKind
    {
        Rock = 0,
        Boulder = 1,
        Crystal = 2,
        Mushroom = 3,
        GlowTree = 4,
        Tendril = 5,
        Column = 6,
        Spike = 7,
        Egg = 8,
        Frond = 9,
        Spire = 10,

        /// <summary>A cluster of pale ice prisms, lit from inside.</summary>
        IceShard = 11,

        /// <summary>Soft mounds of snow with a crust of frost.</summary>
        Snowdrift = 12,

        /// <summary>A bare, twisted dead tree of the marsh.</summary>
        DeadTree = 13,

        /// <summary>A clump of tall reeds with dark cattail heads.</summary>
        Reed = 14,
    }
}
