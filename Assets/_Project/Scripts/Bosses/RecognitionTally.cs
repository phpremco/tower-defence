using System.Collections.Generic;

namespace Game.Bosses
{
    /// <summary>
    /// What a boss that learns has taken from each kind of weapon in its current phase (Docs/BOSSES.md, the Sealed One):
    /// at the phase change it learns the kind that hurt it most, and in the next phase that kind lands only a share of its
    /// damage. Resistance, never immunity. Pure logic, keyed by tower type.
    /// </summary>
    public sealed class RecognitionTally<T> where T : class
    {
        private readonly Dictionary<T, float> _phase = new Dictionary<T, float>();

        /// <summary>The kind it has learned (it takes a share from it), or null.</summary>
        public T Learned { get; private set; }

        public void Add(T kind, float amount)
        {
            if (kind == null || amount <= 0f)
            {
                return;
            }

            _phase.TryGetValue(kind, out float known);
            _phase[kind] = known + amount;
        }

        public float Of(T kind) => kind != null && _phase.TryGetValue(kind, out float amount) ? amount : 0f;

        /// <summary>The phase ends: it learns the kind with the most damage in it (none without any) and starts counting afresh.</summary>
        public T Learn()
        {
            T best = null;
            float most = 0f;
            foreach (KeyValuePair<T, float> entry in _phase)
            {
                if (entry.Value > most)
                {
                    best = entry.Key;
                    most = entry.Value;
                }
            }

            Learned = best;
            _phase.Clear();
            return best;
        }

        /// <summary>The share of a hit from that kind that lands: the dampened share for the learned kind, else all of it.</summary>
        public float ScaleFor(T kind, float dampenedShare) => kind != null && ReferenceEquals(kind, Learned) ? dampenedShare : 1f;

        public void Clear()
        {
            _phase.Clear();
            Learned = null;
        }
    }
}
