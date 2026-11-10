namespace Game.Levels
{
    /// <summary>The moment a gate tip waits for. Values are only appended.</summary>
    public enum GateTipKind
    {
        /// <summary>A gate that can still be upgraded is hurt and the player can pay for its next level.</summary>
        UpgradeGate = 0,

        /// <summary>Another gate is hurt while the one the hero guards stands untouched: he would do more there.</summary>
        MoveHero = 1,
    }
}
