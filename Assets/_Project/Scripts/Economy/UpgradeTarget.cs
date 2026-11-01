namespace Game.Economy
{
    /// <summary>
    /// What a permanent upgrade improves. Tower targets apply to the tower named by the upgrade, drone targets to
    /// the drone named by it.
    /// </summary>
    public enum UpgradeTarget
    {
        TowerDamage,
        TowerRange,
        TowerFireRate,
        TowerHealth,
        BaseHealth,

        /// <summary>Guns on the base itself: each level adds a turret or strengthens them.</summary>
        BaseWeapon,
        BlockadeHealth,
        HeroDamage,
        HeroHealth,
        DroneSpeed,

        /// <summary>Hit points of the drone, so flyers need longer to bring it down.</summary>
        DroneArmour,

        /// <summary>The job's yield: repair per second, money per trip or damage per shot.</summary>
        DroneOutput,

        /// <summary>How fast the job is done: shorter harvest stops or a quicker fire rate.</summary>
        DroneTempo,

        /// <summary>One more drone of this type may fly at once, per level.</summary>
        DroneCount,
    }
}
