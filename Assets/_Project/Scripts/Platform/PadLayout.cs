namespace Game.Platform
{
    /// <summary>
    /// Which face-button names and glyphs a prompt shows: Xbox-style letters by default, the PlayStation shapes for a
    /// DualShock or DualSense, the Nintendo layout for a Switch pad. The pad in the player's hands picks it (see
    /// <see cref="PadLayoutResolver"/>); the platform only gives the default. Values are only ever appended.
    /// </summary>
    public enum PadLayout
    {
        Xbox = 0,
        Nintendo = 1,
        PlayStation = 2,
    }
}
