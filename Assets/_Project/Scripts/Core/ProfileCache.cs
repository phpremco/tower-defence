namespace Game.Core
{
    /// <summary>
    /// A weapon definition's cached <see cref="DamageProfile"/>: read from the rule table once and again whenever a rules
    /// asset loads or changes (<see cref="CombatRules.Version"/>), so a shot never looks the table up or allocates.
    /// </summary>
    public struct ProfileCache
    {
        // The rules version the profile was read at, plus one: zero is "not read yet".
        private int _stamp;
        private DamageProfile _profile;

        /// <summary>The weapon's profile from the rules; a weapon without rules (a fixture built in memory) lands in full.</summary>
        public DamageProfile Get(CombatRules rules, DamageType type, UnityEngine.Object weapon)
        {
            if (_stamp != CombatRules.Version + 1)
            {
                _profile = rules != null ? rules.ProfileOf(type, weapon.name) : DamageProfile.Full;
                _stamp = CombatRules.Version + 1;
            }

            return _profile;
        }

        /// <summary>Forgets the profile (the weapon's own data changed).</summary>
        public void Clear() => _stamp = 0;
    }
}
