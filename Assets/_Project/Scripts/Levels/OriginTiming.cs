namespace Game.Levels
{
    /// <summary>
    /// The timing of a spawn origin's cycle in seconds (from <see cref="SpawnOriginDefinition"/>), as plain numbers so the
    /// director stays pure and testable.
    /// </summary>
    public readonly struct OriginTiming
    {
        public OriginTiming(float openLead, float burst, float emerge, float open, float closeDelay, float close)
        {
            OpenLead = openLead;
            Burst = burst;
            Emerge = emerge;
            Open = open;
            CloseDelay = closeDelay;
            Close = close;
        }

        /// <summary>The ground starts to open this long before the origin's first creature of the wave is due.</summary>
        public float OpenLead { get; }

        /// <summary>From the start of the opening to the burst.</summary>
        public float Burst { get; }

        /// <summary>From the start of the opening until the maw lets the first creature out.</summary>
        public float Emerge { get; }

        /// <summary>Length of the opening; the origin is fully open after it.</summary>
        public float Open { get; }

        /// <summary>The origin stays open this long after its last creature of the wave.</summary>
        public float CloseDelay { get; }

        /// <summary>Length of the closing; it is marked again after it.</summary>
        public float Close { get; }
    }
}
