namespace Game.Levels
{
    /// <summary>How the dialogue box frames a speaker. Values are only ever appended.</summary>
    public enum SpeakerFrame
    {
        /// <summary>A bust portrait beside the text: people on the ground.</summary>
        Portrait = 0,

        /// <summary>A projected hologram: the base AI.</summary>
        Hologram = 1,

        /// <summary>An incoming message without a portrait, under a header bar.</summary>
        Transmission = 2,

        /// <summary>A recorded log entry.</summary>
        Log = 3,
    }
}
