using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Decides when each enemy of one wave appears. Pure timing logic, no scene objects.</summary>
    public sealed class WaveScheduler
    {
        private const float BossSpacingSeconds = 18f;

        private readonly SpawnGroup[] _groups;
        private readonly int[] _totals;
        private readonly int[] _spawned;
        private readonly float[] _jitter;
        private readonly float _intervalMultiplier;
        private readonly float _jitterShare;
        private readonly System.Random _random;
        private float _elapsed;

        /// <param name="random">Source of the spawn jitter. Without one every spawn lands exactly on its schedule.</param>
        public WaveScheduler(WaveDefinition wave, DifficultyProfile difficulty, System.Random random = null)
        {
            _groups = new SpawnGroup[wave.Groups.Count];
            _totals = new int[_groups.Length];
            _spawned = new int[_groups.Length];
            _jitter = new float[_groups.Length];
            _intervalMultiplier = difficulty.SpawnIntervalMultiplier;
            _random = random;
            _jitterShare = random != null ? wave.SpawnJitter : 0f;

            for (int i = 0; i < _groups.Length; i++)
            {
                _groups[i] = wave.Groups[i];
                // A boss is one creature however hard the level is, and several in one group keep room between them.
                bool boss = _groups[i].Enemy != null && _groups[i].Enemy.IsBoss;
                _totals[i] = boss ? _groups[i].Count : Mathf.Max(1, Mathf.CeilToInt(_groups[i].Count * difficulty.CountMultiplier));
                if (boss)
                {
                    _groups[i].Interval = Mathf.Max(_groups[i].Interval, BossSpacingSeconds);
                }
                _jitter[i] = NextJitter(_groups[i]);
            }
        }

        public bool IsFinished
        {
            get
            {
                for (int i = 0; i < _groups.Length; i++)
                {
                    if (_spawned[i] < _totals[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Advances time and appends every enemy that is due to the output list.</summary>
        public void Advance(float deltaTime, List<SpawnRequest> output)
        {
            _elapsed += deltaTime;

            for (int i = 0; i < _groups.Length; i++)
            {
                SpawnGroup group = _groups[i];
                while (_spawned[i] < _totals[i] && SpawnTime(group, _spawned[i]) + _jitter[i] <= _elapsed)
                {
                    output.Add(new SpawnRequest(group.Enemy, group.PathIndex, group.Source));
                    _spawned[i]++;
                    _jitter[i] = NextJitter(group);
                }
            }
        }

        private float SpawnTime(SpawnGroup group, int index) => group.Delay + (group.Stagger + index * group.Interval) * _intervalMultiplier;

        // A fresh drift for the next spawn of the group, early or late by at most the jitter share of one interval.
        private float NextJitter(SpawnGroup group)
        {
            if (_random == null || _jitterShare <= 0f)
            {
                return 0f;
            }

            float unit = (float)_random.NextDouble() * 2f - 1f;
            return unit * _jitterShare * group.Interval * _intervalMultiplier;
        }
    }
}
