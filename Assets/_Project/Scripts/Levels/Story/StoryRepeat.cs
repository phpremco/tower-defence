namespace Game.Levels
{
    /// <summary>Whether a story trigger plays once per save or on every run. Either way it fires at most once per run.</summary>
    public enum StoryRepeat
    {
        OncePerSave = 0,
        Always = 1,
    }
}
