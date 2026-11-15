namespace Game.Core
{
    /// <summary>
    /// What the hero's permanent gear upgrades add when a level starts: the companion drone that mends him faster, the
    /// armour that takes the edge off every hit, and whether the blade has reached the top tier open to the player.
    /// The default value means "no gear": nothing healed faster, nothing absorbed, no plates, a plain blade.
    /// </summary>
    public readonly struct HeroGear
    {
        /// <summary>The armour never takes more than this share off a hit, whatever the data adds up to.</summary>
        public const float MaxDamageReduction = 0.9f;

        public HeroGear(float regenBonus, int repairDroneTier, float damageReduction, int armourTier, int armourTiers, bool bladeAtTop)
        {
            RegenBonus = regenBonus < 0f ? 0f : regenBonus;
            RepairDroneTier = repairDroneTier;
            DamageReduction = damageReduction < 0f ? 0f : damageReduction > MaxDamageReduction ? MaxDamageReduction : damageReduction;
            ArmourTier = armourTier;
            ArmourTiers = armourTiers;
            BladeAtTop = bladeAtTop;
        }

        /// <summary>Extra healing as a fraction of the hero's own rate (0.3 heals 30% faster).</summary>
        public float RegenBonus { get; }

        /// <summary>Tiers bought of the companion repair drone; zero means no drone follows him.</summary>
        public int RepairDroneTier { get; }

        /// <summary>Share of every hit the armour takes off (0.2 leaves 80%).</summary>
        public float DamageReduction { get; }

        /// <summary>Tiers bought of the armour; the plates on the model follow it.</summary>
        public int ArmourTier { get; }

        /// <summary>How many armour tiers exist in all, so a view can tell how far the plates have grown.</summary>
        public int ArmourTiers { get; }

        /// <summary>True when the blade damage upgrade stands at the highest tier the player can buy right now.</summary>
        public bool BladeAtTop { get; }

        public float RegenMultiplier => 1f + RegenBonus;

        public float DamageTakenMultiplier => 1f - DamageReduction;

        /// <summary>The armour's growth from 0 (none) to 1 (every tier).</summary>
        public float ArmourGrowth => ArmourTiers > 0 ? (float)ArmourTier / ArmourTiers : 0f;
    }
}
