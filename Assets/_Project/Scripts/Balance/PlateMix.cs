using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// The plating of a wave's creatures weighted by their health: how much of a gun's hit lands on the wave as a whole. A
    /// wave of soft creatures gives every gun a share of one.
    /// </summary>
    public sealed class PlateMix
    {
        private readonly List<(float Plating, float Floor, float Health)> _parts = new List<(float, float, float)>();
        private float _health;

        public bool IsSoft => _parts.Count == 0;

        /// <summary>Adds creatures with this much health in total; soft ones only count towards the whole.</summary>
        public void Add(float plating, float floor, float health)
        {
            if (health <= 0f)
            {
                return;
            }

            _health += health;
            if (plating > 0f)
            {
                _parts.Add((plating, floor, health));
            }
        }

        /// <summary>Share of a hit of this size that lands, averaged over the wave's health.</summary>
        public float Share(float hit)
        {
            if (_parts.Count == 0 || _health <= 0f)
            {
                return 1f;
            }

            float lost = 0f;
            foreach ((float plating, float floor, float health) in _parts)
            {
                lost += (1f - Plating.Share(hit, plating, floor)) * health;
            }

            return 1f - lost / _health;
        }
    }
}
