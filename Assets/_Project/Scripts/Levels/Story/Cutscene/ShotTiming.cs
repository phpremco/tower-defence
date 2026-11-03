namespace Game.Levels
{
    /// <summary>How a cutscene shot decides it is over.</summary>
    public enum ShotTiming
    {
        /// <summary>After its seconds; a dialogue still open holds it until the player has read it.</summary>
        Fixed = 0,

        /// <summary>Its seconds after the dialogue of the shot has ended.</summary>
        AfterDialogue = 1,
    }
}
