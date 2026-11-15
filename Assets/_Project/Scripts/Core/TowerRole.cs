namespace Game.Core
{
    /// <summary>
    /// The one job a tower is bought for (round 67, Remco: "every tower is good at one specific job; off-role it is clearly
    /// worse per coin"). The role names the creature trait the tower answers (<see cref="CombatRules.Answers"/>); the role
    /// tests hold every tower to it per coin.
    /// </summary>
    public enum TowerRole
    {
        /// <summary>No fighting role: a gate's own gun, the base's turret. They help and never replace a tower.</summary>
        Helper = 0,

        /// <summary>Light fire: many small hits, guided or instant, for streams of small creatures, runners and one big soft body.</summary>
        LightFire = 1,

        /// <summary>A blast or a chain that takes a bunched group at once.</summary>
        Splash = 2,

        /// <summary>Strips and pierces armour.</summary>
        AntiArmour = 3,

        /// <summary>Made for the sky.</summary>
        AntiAir = 4,

        /// <summary>Slows what passes: more seconds in every other gun's reach.</summary>
        Control = 5,

        /// <summary>Boosts the guns around it.</summary>
        Booster = 6,
    }
}
