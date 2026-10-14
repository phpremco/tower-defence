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
            int repairDroneCount = 0,
            int collectorDroneCount = 0,
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
            RepairDroneCount = repairDroneCount;
            CollectorDroneCount = collectorDroneCount;
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
        public int RepairDroneCount { get; }
        public int CollectorDroneCount { get; }
        public float DroneSpeedMultiplier { get; }
    }
}
