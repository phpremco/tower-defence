namespace Game.Levels
{
    /// <summary>
    /// The moment in a level that starts a dialogue. Values are only ever appended (dialogues between levels come later).
    /// </summary>
    public enum StoryTriggerKind
    {
        /// <summary>The first moment the field is playable: briefing deployed and recon done.</summary>
        LevelStart = 0,

        /// <summary>Wave number <see cref="StoryTrigger.Wave"/> (1-based, as the HUD shows it) begins.</summary>
        WaveStart = 1,

        /// <summary>The first enemy of <see cref="StoryTrigger.Enemy"/> enters the field in this run.</summary>
        FirstSighting = 2,

        /// <summary>A boss enters the field; <see cref="StoryTrigger.Enemy"/> optionally narrows it to one boss.</summary>
        BossSpawn = 3,

        /// <summary>A boss dies; <see cref="StoryTrigger.Enemy"/> optionally narrows it to one boss.</summary>
        BossDefeated = 4,

        /// <summary>The level is won, before the result screen.</summary>
        LevelWon = 5,

        /// <summary>The player places the first <see cref="StoryTrigger.PathObject"/> of this run (a gadget on the road).</summary>
        FirstUse = 6,
    }
}
