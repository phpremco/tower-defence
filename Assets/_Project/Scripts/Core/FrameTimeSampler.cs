using System;
using System.Globalization;

namespace Game.Core
{
    /// <summary>
    /// Collects frame times into a buffer allocated once and summarises them: average FPS, median, 95th and 99th
    /// percentile and the worst frame in milliseconds. Used by the smoke test of a player build; frames past the
    /// capacity are dropped, never reallocated.
    /// </summary>
    public sealed class FrameTimeSampler
    {
        private const float MillisecondsPerSecond = 1000f;

        private readonly float[] _seconds;
        private int _count;
        private double _total;

        public FrameTimeSampler(int capacity)
        {
            _seconds = new float[Math.Max(1, capacity)];
        }

        public int Count => _count;

        public double TotalSeconds => _total;

        public void Clear()
        {
            _count = 0;
            _total = 0.0;
        }

        public void Add(float seconds)
        {
            if (_count >= _seconds.Length || seconds <= 0f)
            {
                return;
            }

            _seconds[_count++] = seconds;
            _total += seconds;
        }

        public float AverageFps => _total > 0.0 ? (float)(_count / _total) : 0f;

        /// <summary>The frame time in milliseconds below which the given share of frames lies (0.5 = median).</summary>
        public float PercentileMs(float share)
        {
            if (_count == 0)
            {
                return 0f;
            }

            var sorted = new float[_count];
            Array.Copy(_seconds, sorted, _count);
            Array.Sort(sorted);
            int index = Math.Min(_count - 1, Math.Max(0, (int)Math.Ceiling(share * _count) - 1));
            return sorted[index] * MillisecondsPerSecond;
        }

        public float WorstMs
        {
            get
            {
                float worst = 0f;
                for (int i = 0; i < _count; i++)
                {
                    worst = Math.Max(worst, _seconds[i]);
                }

                return worst * MillisecondsPerSecond;
            }
        }

        /// <summary>One line for the log: frames, seconds, average FPS and the frame-time spread.</summary>
        public string Summary()
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            return string.Format(inv, "{0} frames in {1:0.0} s, avg {2:0.0} fps, median {3:0.0} ms, p95 {4:0.0} ms, p99 {5:0.0} ms, worst {6:0.0} ms",
                _count, _total, AverageFps, PercentileMs(0.5f), PercentileMs(0.95f), PercentileMs(0.99f), WorstMs);
        }
    }
}
