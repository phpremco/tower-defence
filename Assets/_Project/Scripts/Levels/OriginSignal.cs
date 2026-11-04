namespace Game.Levels
{
    /// <summary>What a spawn origin tells its view.</summary>
    public enum OriginSignal
    {
        /// <summary>It entered a new <see cref="OriginState"/>.</summary>
        StateChanged = 0,

        /// <summary>The ground bursts open (frame 54 of the opening).</summary>
        Burst = 1,

        /// <summary>A creature came out of it.</summary>
        Emerged = 2,
    }
}
