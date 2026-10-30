namespace Game.Enemies
{
    /// <summary>How a creature goes when it dies. Pure view data; every style ends with the body gone.</summary>
    public enum DeathStyle
    {
        /// <summary>Folds up, then the parts fly apart and sink away while ichor splashes.</summary>
        Shatter = 0,

        /// <summary>A flyer tumbles out of the air, hits the ground and dissolves into its element.</summary>
        Crash = 1,

        /// <summary>Topples over as one body, lies still and fades into the ground.</summary>
        Collapse = 2,

        /// <summary>Flares and bursts into sparks; nothing solid is left.</summary>
        Burst = 3,
    }
}
