namespace Game.Balance
{
    /// <summary>
    /// The armour of a wave's creatures beside their health: how much of a weapon's damage counts against the wave as a
    /// whole. Armour is a pool in front of the body (<c>Game.Combat.Health</c>, round 24): a weapon with armour factor f
    /// spends 1/f points of damage per point of armour and then hurts the body point for point, so a wave with health H
    /// and armour A costs it H + A/f, while the ledger counts the wave as H + A (<see cref="EnemyStats.EffectiveHealth"/>).
    /// The share is the ratio of the two: one for a heavy shell (f = 1) or a soft wave, well under one for light fire on
    /// armoured creatures, over one for a slug that over-penetrates.
    /// </summary>
    public sealed class ArmourMix
    {
        private float _health;
        private float _armour;

        /// <summary>True when none of the creatures added wear armour.</summary>
        public bool IsSoft => _armour <= 0f;

        public float Health => _health;

        public float Armour => _armour;

        /// <summary>Adds creatures with this much body health and armour in total.</summary>
        public void Add(float health, float armour)
        {
            _health += System.Math.Max(0f, health);
            _armour += System.Math.Max(0f, armour);
        }

        /// <summary>Share of a weapon's damage that counts against the wave's effective health, for its armour factor.</summary>
        public float Share(float armourFactor)
        {
            if (_armour <= 0f || _health + _armour <= 0f)
            {
                return 1f;
            }

            if (armourFactor <= 0f)
            {
                // A weapon that cannot work on armour never gets past it.
                return 0f;
            }

            return (_health + _armour) / (_health + _armour / armourFactor);
        }
    }
}
