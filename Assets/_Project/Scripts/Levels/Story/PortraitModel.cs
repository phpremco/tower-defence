namespace Game.Levels
{
    /// <summary>
    /// Which procedural bust the dialogue box shows for a speaker. Values are only ever appended, because the
    /// speaker assets store the number.
    /// </summary>
    public enum PortraitModel
    {
        /// <summary>The commander, built from the hero model.</summary>
        Hero = 0,

        /// <summary>The chief scientist.</summary>
        Scientist = 1,

        /// <summary>The base AI: an emissive core with orbit rings and no body.</summary>
        Hologram = 2,

        /// <summary>A flat emissive company badge.</summary>
        Emblem = 3,

        /// <summary>An old probe with dish and antenna.</summary>
        Probe = 4,
    }
}
