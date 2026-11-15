namespace Game.Core
{
    /// <summary>
    /// What one weapon's damage is worth against the traits that change a hit (round 67): the share that works on an
    /// armour pool and the share that lands on a creature in the air. It is never typed on a weapon: every weapon gets it
    /// from the one rule table (<see cref="CombatRules.ProfileOf"/>) by its damage type, so a new weapon follows the rules
    /// without anyone remembering to.
    /// </summary>
    public readonly struct DamageProfile
    {
        /// <summary>A weapon without rules (a test fixture built in memory): every hit lands in full.</summary>
        public static readonly DamageProfile Full = new DamageProfile(1f, 1f);

        public DamageProfile(float armour, float air)
        {
            Armour = armour;
            Air = air;
        }

        /// <summary>Share of a hit that works on a creature's armour while it has any (Health.TakeDamage).</summary>
        public float Armour { get; }

        /// <summary>Share of a hit that lands on a creature only the sky guns are made for.</summary>
        public float Air { get; }

        /// <summary>The share that lands on a target of this layer: anything that touches the ground takes the hit in full.</summary>
        public float Against(TargetLayers layer) => (layer & TargetLayers.Ground) != 0 ? 1f : Air;
    }
}
