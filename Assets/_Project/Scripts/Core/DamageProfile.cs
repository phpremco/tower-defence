namespace Game.Core
{
    /// <summary>
    /// What one weapon's damage is worth against what changes a hit (round 67): the share that works on an armour pool,
    /// the share that lands on a creature in the air and, since round 87, the factor on a creature family. It is never
    /// typed on a weapon: every weapon gets it from the one rule table (<see cref="CombatRules.ProfileOf"/>) by its
    /// damage type, so a new weapon follows the rules without anyone remembering to.
    /// </summary>
    public readonly struct DamageProfile
    {
        /// <summary>A weapon without rules (a test fixture built in memory): every hit lands in full.</summary>
        public static readonly DamageProfile Full = new DamageProfile(1f, 1f);

        // Indexed by CreatureFamily; null while the damage type has no family row, so most weapons carry nothing.
        private readonly float[] _families;

        public DamageProfile(float armour, float air)
            : this(armour, air, null)
        {
        }

        /// <param name="families">The factor per <see cref="CreatureFamily"/> by its number, or null for one on every family.</param>
        public DamageProfile(float armour, float air, float[] families)
        {
            Armour = armour;
            Air = air;
            _families = families;
        }

        /// <summary>Share of a hit that works on a creature's armour while it has any (Health.TakeDamage).</summary>
        public float Armour { get; }

        /// <summary>Share of a hit that lands on a creature only the sky guns are made for.</summary>
        public float Air { get; }

        /// <summary>The share that lands on a target of this layer: anything that touches the ground takes the hit in full.</summary>
        public float Against(TargetLayers layer) => (layer & TargetLayers.Ground) != 0 ? 1f : Air;

        /// <summary>What a hit is multiplied by on a creature of this family; one for a family the damage type has no row for.</summary>
        public float Against(CreatureFamily family)
        {
            int index = (int)family;
            return _families != null && index >= 0 && index < _families.Length ? _families[index] : 1f;
        }

        /// <summary>
        /// The whole hit on a creature of this layer and family, before its armour: the family factor first, then the air
        /// share. Every place that lands or prices a hit reads this one line, so none can forget a factor.
        /// </summary>
        public float Against(TargetLayers layer, CreatureFamily family) => Against(family) * Against(layer);
    }
}
