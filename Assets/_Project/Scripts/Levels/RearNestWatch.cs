namespace Game.Levels
{
    /// <summary>
    /// The once-per-level trigger of the rear-rift warning: the first time a nest that joins behind the defence gate starts to
    /// open (or is forced open by an early wave), it fires once; every later opening stays quiet. Whether the warning plays at
    /// all (once per save) is the caller's seen-dialogue check. Pure logic.
    /// </summary>
    public sealed class RearNestWatch
    {
        private readonly bool[] _behind;
        private bool _fired;

        /// <param name="behind">Per nest: true when it joins the road behind the defence gate (<see cref="RearNests"/>).</param>
        public RearNestWatch(bool[] behind)
        {
            _behind = behind ?? new bool[0];
        }

        /// <summary>True when any nest of the level lies behind the gate, so the watch is worth keeping.</summary>
        public bool HasAny
        {
            get
            {
                foreach (bool behind in _behind)
                {
                    if (behind)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>True exactly once: the first behind-the-gate nest entering <see cref="OriginState.Opening"/> or <see cref="OriginState.Open"/>.</summary>
        public bool TryFire(int nest, OriginState state)
        {
            if (_fired || nest < 0 || nest >= _behind.Length || !_behind[nest])
            {
                return false;
            }

            if (state != OriginState.Opening && state != OriginState.Open)
            {
                return false;
            }

            _fired = true;
            return true;
        }
    }
}
