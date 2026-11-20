namespace Game.Core
{
    /// <summary>
    /// What a creature is made of, where that changes what a kind of damage is worth on it (round 87, Remco: "a bonus
    /// against certain enemies, e.g. energy enemies"). Unlike the traits (<see cref="EnemyTraits"/>), which follow from a
    /// creature's numbers, a family is typed on the creature: nothing in its health or speed says a Wisp is energy.
    /// What a damage type is worth on a family is one table in the combat rules (<see cref="CombatRules.FamilyFactor"/>),
    /// so a new pairing is a row of data and never a line of code.
    /// </summary>
    public enum CreatureFamily
    {
        /// <summary>Flesh, shell or stone: every damage type lands as its row in the rule table says.</summary>
        None = 0,

        /// <summary>A body of raw energy without mass (the Wisp, the Core Wisp).</summary>
        Energy = 1,
    }
}
