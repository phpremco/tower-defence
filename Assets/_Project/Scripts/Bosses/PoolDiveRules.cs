using System.Collections.Generic;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// The limits of hiding in a pool (Docs/BOSSES.md): each pool once per phase, and never longer under than the boss's
    /// own maximum (6 s at most), so it swims faster rather than staying down longer. Pure logic.
    /// </summary>
    public sealed class PoolDiveRules
    {
        // The rules' own ceiling, whatever the data says.
        public const float AbsoluteMaxSeconds = 6f;

        private readonly HashSet<(int Pool, int Phase)> _used = new HashSet<(int, int)>();

        public PoolDiveRules(float maxSeconds)
        {
            MaxSeconds = Mathf.Clamp(maxSeconds, 0.5f, AbsoluteMaxSeconds);
        }

        public float MaxSeconds { get; }

        public bool CanUse(int pool, int phase) => !_used.Contains((pool, phase));

        public void MarkUsed(int pool, int phase) => _used.Add((pool, phase));

        /// <summary>The swimming speed that covers a distance within the time limit, never slower than its own.</summary>
        public float SpeedFor(float distance, float ownSpeed) => Mathf.Max(ownSpeed, distance / MaxSeconds);

        /// <summary>Seconds under for a distance at that speed: never more than the limit.</summary>
        public float SecondsFor(float distance, float ownSpeed) => Mathf.Min(MaxSeconds, distance / SpeedFor(distance, ownSpeed));
    }
}
