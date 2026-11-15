using System;

namespace Game.Core
{
    /// <summary>
    /// What a creature is, as the combat rules see it (round 67). Nothing here is typed on a creature: the traits follow
    /// from its numbers through <see cref="CombatRules.TraitsOf"/>, so a new creature gets them by being what it is.
    /// Every creature has one size (swarm, mob or tough) and any of the three modifiers.
    /// </summary>
    [Flags]
    public enum EnemyTraits
    {
        None = 0,

        /// <summary>Wears an armour pool: everything that neither explodes nor pierces only chips it.</summary>
        Armoured = 1,

        /// <summary>Only the sky can be aimed at: guns not made for the air land a fraction, or cannot aim up at all.</summary>
        Airborne = 2,

        /// <summary>Tiny and many, in a stream: a big hit is wasted on one, light fire kills them.</summary>
        Swarm = 4,

        /// <summary>Ordinary bodies that bunch up at a gate: one blast takes several.</summary>
        Mob = 8,

        /// <summary>One big soft body: light fire held on a single target.</summary>
        Tough = 16,

        /// <summary>Outruns an unguided shell: only guided or instant fire lands on it while it moves.</summary>
        Fast = 32,
    }
}
