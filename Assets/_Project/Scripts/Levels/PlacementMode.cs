namespace Game.Levels
{
    /// <summary>What the player is doing on the field.</summary>
    public enum PlacementMode
    {
        /// <summary>Moving the cursor around.</summary>
        Browsing,

        /// <summary>A tower preview with its range follows the cursor; confirming places it.</summary>
        Placing,

        /// <summary>A placed tower is selected and its action icons are shown.</summary>
        TowerMenu,
    }
}
