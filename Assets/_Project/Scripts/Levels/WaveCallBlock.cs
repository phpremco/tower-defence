namespace Game.Levels
{
    /// <summary>Why the next wave cannot be called right now; <see cref="None"/> when it can.</summary>
    public enum WaveCallBlock
    {
        None = 0,

        /// <summary>The level has ended.</summary>
        LevelOver = 1,

        /// <summary>Paused, or a story box, the codex or the recon holds the clock.</summary>
        InputBlocked = 2,

        /// <summary>The briefing or the recon fly-over is still to come.</summary>
        NotDeployed = 3,

        /// <summary>A boss intro or another beat that must not be cut short holds the call.</summary>
        BeatPlaying = 4,

        /// <summary>The opening countdown is build time (<see cref="WaveCallDefinition.AllowBeforeFirstWave"/>).</summary>
        BeforeFirstWave = 5,

        /// <summary>A wave is still coming out; only the countdown between waves can be cut short.</summary>
        WaveSpawning = 6,

        /// <summary>Every wave has started.</summary>
        NoWaveLeft = 7,

        /// <summary>The last call was too recent.</summary>
        Cooldown = 8,
    }
}
