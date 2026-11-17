namespace Game.Balance
{
    /// <summary>
    /// The vocabulary of the wave grammar (round 79, Docs/WAVE_GRAMMAR.md): how the creatures at the gate arrive in one wave.
    /// A wave has one front pattern; breathers cut it into clusters and a surprise (<see cref="SurpriseKind"/>) may ride on it.
    /// </summary>
    public enum WavePattern
    {
        /// <summary>One short stream, every troop of the level mixed by weight: the opening waves.</summary>
        Stream = 0,

        /// <summary>The same stream cut into clusters with breathers between them.</summary>
        SpreadClusters = 1,

        /// <summary>One long surge of the cheapest creatures, close together.</summary>
        Swarm = 2,

        /// <summary>The toughest troop walks in front; the light ones start late enough to run into its back at the first guns.</summary>
        TankEscort = 3,

        /// <summary>Every troop of the level at once, close together: all roles in one block.</summary>
        ComboSwarm = 4,

        /// <summary>A packed raid of flyers over a thin ground stream.</summary>
        AirRaid = 5,
    }
}
