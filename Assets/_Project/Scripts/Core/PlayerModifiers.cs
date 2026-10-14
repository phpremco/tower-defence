namespace Game.Core
{
    /// <summary>Bonuses from permanent upgrades, applied when a level starts. Defaults mean "no upgrades".</summary>
    public readonly struct PlayerModifiers
    {
        public PlayerModifiers(float towerDamageMultiplier, float towerRangeMultiplier, int baseHealthBonus, float blockadeHealthMultiplier)
        {
            TowerDamageMultiplier = towerDamageMultiplier;
            TowerRangeMultiplier = towerRangeMultiplier;
            BaseHealthBonus = baseHealthBonus;
            BlockadeHealthMultiplier = blockadeHealthMultiplier;
        }

        public static PlayerModifiers None => new PlayerModifiers(1f, 1f, 0, 1f);

        public float TowerDamageMultiplier { get; }
        public float TowerRangeMultiplier { get; }
        public int BaseHealthBonus { get; }

        /// <summary>Stored now, used once blockades exist.</summary>
        public float BlockadeHealthMultiplier { get; }
    }
}
