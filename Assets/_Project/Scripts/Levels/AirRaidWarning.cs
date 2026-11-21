namespace Game.Levels
{
    /// <summary>The warning of a level's first air raid (round 88c, <see cref="FirstRaidRule"/>): how far off it is and what came with it.</summary>
    public readonly struct AirRaidWarning
    {
        public AirRaidWarning(int wavesAhead, int purse)
        {
            WavesAhead = wavesAhead;
            Purse = purse;
        }

        /// <summary>How many waves after the one that starts now the raid comes; zero when the raid is this wave.</summary>
        public int WavesAhead { get; }

        /// <summary>Coins paid with the warning for the air defence; zero on the difficulties that pay none.</summary>
        public int Purse { get; }
    }
}
