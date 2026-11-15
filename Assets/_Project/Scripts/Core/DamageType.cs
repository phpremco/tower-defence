namespace Game.Core
{
    /// <summary>
    /// What a weapon's damage is made of. Armour reads it (round 64, Remco: "reduced damage from non-exploding things; the
    /// Cannon is explosive"): everything that does not explode or pierce is turned by a creature's armour pool to a fraction
    /// of itself (<see cref="DamageTypeRule"/>), an explosion or a slug works on it in full. The fraction per type is one
    /// table in the designer's sheet (<c>BalanceSheet.ArmourFactors</c>), written onto every weapon as its armour factor.
    /// </summary>
    public enum DamageType
    {
        /// <summary>Shells, rockets, mines, barrels and the hero's shockwave: armour does not reduce it.</summary>
        Explosive = 0,

        /// <summary>Gun Turret, base and gate guns, the hero's blaster, the attack drone's shots.</summary>
        Bullet = 1,

        /// <summary>Lasers and cryo beams.</summary>
        Beam = 2,

        /// <summary>The hero's energy sword.</summary>
        Blade = 3,

        /// <summary>Tesla arcs and the storm rifle.</summary>
        Arc = 4,

        /// <summary>A lava ball's burn: damage over time.</summary>
        Burn = 5,

        /// <summary>A ground drone ramming what it meets.</summary>
        Ram = 6,

        /// <summary>The Railgun's slug: made for armour, it works on it better than on the body.</summary>
        Piercing = 7,
    }
}
