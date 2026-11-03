namespace Game.Towers
{
    /// <summary>Which procedural model a tower wears. None keeps whatever imported model or placeholder it has. GateGun is the small turret a gate grows with its gun upgrades.</summary>
    public enum TowerShape
    {
        None = 0,
        Cannon = 1,
        Laser = 2,
        Cryo = 3,
        Rocket = 4,
        Tesla = 5,
        Railgun = 6,
        Support = 7,
        Artillery = 8,
        StormRifle = 9,
        CryoBeam = 10,
        PulseRifle = 11,
        GateGun = 12,
        Flak = 13,
    }
}
