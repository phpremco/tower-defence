namespace Game.Towers
{
    /// <summary>Which of a modelled tower's shot origins the next shot leaves from.</summary>
    public enum TowerMuzzleChoice
    {
        /// <summary>Each barrel in turn (twin and quad guns, rocket tubes).</summary>
        Alternate = 0,

        /// <summary>The one closest to the target (the Tesla's arc prongs).</summary>
        Nearest = 1,
    }
}
