namespace Game.Levels
{
    /// <summary>How one piece of a dialogue line is drawn. Values are only ever appended.</summary>
    public enum SegmentStyle
    {
        Normal = 0,

        /// <summary>Blacked out: the text sets the width of the bar and is never shown.</summary>
        Redacted = 1,

        /// <summary>Struck through, still readable.</summary>
        Struck = 2,
    }
}
