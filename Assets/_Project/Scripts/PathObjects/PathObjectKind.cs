namespace Game.PathObjects
{
    /// <summary>How a path object behaves. Everything else about it is data on its definition.</summary>
    public enum PathObjectKind
    {
        /// <summary>Waits for an enemy to come close, then deals area damage once and is gone.</summary>
        Mine,

        /// <summary>Slows every enemy of its layers inside its radius for as long as it lasts.</summary>
        SlowField,

        /// <summary>Moves along the road and damages enemies it touches until it reaches the end or its time is up.</summary>
        Roller,
    }

    /// <summary>Where a path object may be placed.</summary>
    public enum PathObjectPlacement
    {
        /// <summary>On a road cell.</summary>
        Road,

        /// <summary>On a free cell next to the road, where towers would go.</summary>
        Beside,
    }
}
