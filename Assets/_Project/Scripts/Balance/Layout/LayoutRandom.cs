namespace Game.Balance
{
    /// <summary>
    /// The random source of the level layout generator: SplitMix64, seeded with FNV-1a of the level key, its seed and the
    /// attempt. Never <c>System.Random</c>, whose sequence is not specified across runtimes, so the editor and a dotnet
    /// tool lay out the same map from the same spec.
    /// </summary>
    public sealed class LayoutRandom
    {
        private const ulong Golden = 0x9E3779B97F4A7C15UL;
        private const ulong MixA = 0xBF58476D1CE4E5B9UL;
        private const ulong MixB = 0x94D049BB133111EBUL;
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;
        private const double UnitScale = 1.0 / (1UL << 53);

        private ulong _state;

        public LayoutRandom(string key, int seed, int attempt)
        {
            _state = Fnv($"{key}:{seed}:{attempt}");
        }

        /// <summary>64-bit FNV-1a of a text.</summary>
        public static ulong Fnv(string text)
        {
            unchecked
            {
                ulong hash = FnvOffset;
                foreach (char c in text)
                {
                    hash = (hash ^ c) * FnvPrime;
                }

                return hash;
            }
        }

        public ulong NextULong()
        {
            unchecked
            {
                _state += Golden;
                ulong z = _state;
                z = (z ^ (z >> 30)) * MixA;
                z = (z ^ (z >> 27)) * MixB;
                return z ^ (z >> 31);
            }
        }

        /// <summary>A whole number from min to max, both included.</summary>
        public int Range(int min, int max)
        {
            if (max <= min)
            {
                return min;
            }

            return min + (int)(NextULong() % (ulong)(max - min + 1));
        }

        /// <summary>A number in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * UnitScale;

        public double Range(double min, double max) => min + (max - min) * NextDouble();

        public bool Chance(double probability) => NextDouble() < probability;
    }
}
