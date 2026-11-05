namespace Game.Bosses
{
    /// <summary>
    /// Fires each health threshold of a boss once, in order (75, 50, 25 percent): a burst of damage that drops it past
    /// two at once still hands them out one at a time, so it never skips a burrow. Pure logic.
    /// </summary>
    public sealed class ThresholdTracker
    {
        private readonly float[] _thresholds;

        public ThresholdTracker(float[] thresholds)
        {
            _thresholds = thresholds ?? new float[0];
        }

        /// <summary>How many thresholds have fired.</summary>
        public int Passed { get; private set; }

        /// <summary>True once every threshold has fired.</summary>
        public bool Done => Passed >= _thresholds.Length;

        /// <summary>The share of health below which the last threshold lies (the rage), or zero without thresholds.</summary>
        public float Last => _thresholds.Length > 0 ? _thresholds[_thresholds.Length - 1] : 0f;

        public void Reset() => Passed = 0;

        /// <summary>True when the next threshold has been reached, without firing it.</summary>
        public bool IsDue(float healthFraction) => !Done && healthFraction <= _thresholds[Passed];

        /// <summary>True (once) when the health share has reached the next threshold; the caller acts on it.</summary>
        public bool TryCross(float healthFraction)
        {
            if (Done || healthFraction > _thresholds[Passed])
            {
                return false;
            }

            Passed++;
            return true;
        }
    }
}
