namespace Game.Core
{
    /// <summary>Bonuses from permanent upgrades, applied when a level starts. Defaults mean "no upgrades".</summary>
    public readonly struct PlayerModifiers
    {
        public PlayerModifiers(
            float towerDamageMultiplier,
            float towerRangeMultiplier,
            float towerFireRateMultiplier,
            float towerHealthMultiplier,
            int baseHealthBonus,
            float blockadeHealthMultiplier,
            float heroDamageMultiplier = 1f,
            float heroHealthMultiplier = 1f,
            float droneSpeedMultiplier = 1f)
        {
            TowerDamageMultiplier = towerDamageMultiplier;
            TowerRangeMultiplier = towerRangeMultiplier;
            TowerFireRateMultiplier = towerFireRateMultiplier;
            TowerHealthMultiplier = towerHealthMultiplier;
            BaseHealthBonus = baseHealthBonus;
            BlockadeHealthMultiplier = blockadeHealthMultiplier;
            HeroDamageMultiplier = heroDamageMultiplier;
            HeroHealthMultiplier = heroHealthMultiplier;
            DroneSpeedMultiplier = droneSpeedMultiplier;
        }

        public static PlayerModifiers None => new PlayerModifiers(1f, 1f, 1f, 1f, 0, 1f);

        public float TowerDamageMultiplier { get; }
        public float TowerRangeMultiplier { get; }
        public float TowerFireRateMultiplier { get; }
        public float TowerHealthMultiplier { get; }
        public int BaseHealthBonus { get; }
        public float BlockadeHealthMultiplier { get; }
        public float HeroDamageMultiplier { get; }
        public float HeroHealthMultiplier { get; }

        public float DroneSpeedMultiplier { get; }
    }
}
