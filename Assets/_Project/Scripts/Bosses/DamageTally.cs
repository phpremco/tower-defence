using System;
using System.Collections.Generic;

namespace Game.Bosses
{
    /// <summary>
    /// How much damage each source (each tower) has done to one boss, so the boss can go for the one that hurt it most.
    /// Pure logic; one entry per source, no allocation once a source is known.
    /// </summary>
    public sealed class DamageTally<T> where T : class
    {
        private readonly Dictionary<T, float> _damage = new Dictionary<T, float>();

        public void Add(T source, float amount)
        {
            if (source == null || amount <= 0f)
            {
                return;
            }

            _damage.TryGetValue(source, out float known);
            _damage[source] = known + amount;
        }

        public float Of(T source) => source != null && _damage.TryGetValue(source, out float amount) ? amount : 0f;

        public void Clear() => _damage.Clear();

        /// <summary>The source with the most damage that passes the filter (a tower still standing), or null.</summary>
        public T Top(Func<T, bool> eligible)
        {
            T best = null;
            float most = 0f;
            foreach (KeyValuePair<T, float> entry in _damage)
            {
                if (entry.Value > most && (eligible == null || eligible(entry.Key)))
                {
                    best = entry.Key;
                    most = entry.Value;
                }
            }

            return best;
        }
    }
}
