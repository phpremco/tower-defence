namespace Game.Levels
{
    /// <summary>What the intro shows behind a story beat. The scene is drawn by the intro screen; the data only names it.</summary>
    public enum StoryScene
    {
        /// <summary>Stars only.</summary>
        Space = 0,

        /// <summary>The planet grows as the ship closes in.</summary>
        Approach = 1,

        /// <summary>The planet fills the view and its regions light up one by one.</summary>
        Surface = 2,

        /// <summary>The view dives onto the first region and fades out.</summary>
        Landing = 3,
    }
}
