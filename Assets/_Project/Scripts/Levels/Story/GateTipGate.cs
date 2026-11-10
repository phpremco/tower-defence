namespace Game.Levels
{
    /// <summary>How one gate stands at the moment a gate tip is weighed.</summary>
    public readonly struct GateTipGate
    {
        public GateTipGate(float health, bool standing, bool upgradeAffordable)
        {
            Health = health;
            Standing = standing;
            UpgradeAffordable = upgradeAffordable;
        }

        /// <summary>Share of its health left, 0..1.</summary>
        public float Health { get; }

        /// <summary>False while it lies wrecked.</summary>
        public bool Standing { get; }

        /// <summary>It has a next level and the player can pay for it now.</summary>
        public bool UpgradeAffordable { get; }
    }
}
