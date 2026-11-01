namespace Game.Core
{
    /// <summary>The background sound bed of a level. Picked per visual theme, so a level sounds like it looks.</summary>
    public enum AmbienceKind
    {
        None = 0,
        Wind = 1,
        Forest = 2,
        Lava = 3,
        Ruins = 4,
        Hive = 5,

        /// <summary>A snowstorm: gusting wind and a thin high whistle.</summary>
        Blizzard = 6,

        /// <summary>A marsh at night: frogs, insects and bubbles breaking the surface.</summary>
        Marsh = 7,
    }
}
