namespace Game.Levels
{
    /// <summary>Where a spawn origin (a burrow or a vent in a pool) stands in its cycle.</summary>
    public enum OriginState
    {
        /// <summary>No wave of the level uses it: plain ground, nothing to see.</summary>
        Closed = 0,

        /// <summary>Shut, but marked on the ground so the player sees where creatures will come from.</summary>
        Marked = 1,

        /// <summary>Breaking open: tremor, bulge, the burst, the maw rising.</summary>
        Opening = 2,

        /// <summary>Open, breathing, creatures coming out; once open it stays open for the rest of the level.</summary>
        Open = 3,
    }
}
