namespace Game.Bosses
{
    /// <summary>What stands on a cell of the field, as far as a boss's weight and a crack line care.</summary>
    public enum SurfaceKind
    {
        Open = 0,
        Road = 1,
        /// <summary>A plateau: a low step, walked with a tilt.</summary>
        Raised = 2,
        /// <summary>A solid formation (rock, boulder, column): climbed over.</summary>
        Rock = 3,
        /// <summary>A formation that cannot hold the weight (crystals, plants): it breaks.</summary>
        Fragile = 4,
        Quicksand = 5,
        /// <summary>Any other liquid: water, lava, ice, bog, a rift.</summary>
        Liquid = 6,
        Tower = 7,
        Gate = 8,
        Base = 9,

        /// <summary>A large solid decoration piece (spire, column): nothing walks or cracks through it; walks steer round.</summary>
        Obstacle = 10,
    }
}
