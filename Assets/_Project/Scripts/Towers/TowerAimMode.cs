namespace Game.Towers
{
    /// <summary>How a modelled tower points at its target.</summary>
    public enum TowerAimMode
    {
        /// <summary>The head turns and the barrels tilt up or down to the target.</summary>
        YawAndPitch = 0,

        /// <summary>The head turns; the barrels keep their modelled elevation (Flak, Artillery, Rocket).</summary>
        YawOnly = 1,

        /// <summary>The head never aims; its idle clip turns it (Tesla, Support).</summary>
        None = 2,
    }
}
