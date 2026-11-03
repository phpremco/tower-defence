namespace Game.Levels
{
    /// <summary>Where a cutscene sends the player when it ends or is skipped.</summary>
    public enum CutsceneHandoff
    {
        /// <summary>The first level of the campaign, with its briefing: the first launch.</summary>
        FirstCampaignLevel = 0,

        /// <summary>Back to the scene named in the request: a replay from the options.</summary>
        ReturnScene = 1,
    }
}
