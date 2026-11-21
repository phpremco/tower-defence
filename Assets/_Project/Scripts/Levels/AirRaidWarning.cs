namespace Game.Levels
{
    /// <summary>The warning of a level's first air raid (round 88c, <see cref="FirstRaidRule"/>): how far off it is.</summary>
    public readonly struct AirRaidWarning
    {
        public AirRaidWarning(int wavesAhead)
        {
            WavesAhead = wavesAhead;
        }

        /// <summary>How many waves after the one that starts now the raid comes; zero when the raid is this wave.</summary>
        public int WavesAhead { get; }
    }
}
