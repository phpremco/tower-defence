using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Decides when each enemy of one wave appears. Pure timing logic, no scene objects.</summary>
    public sealed class WaveScheduler
    {
        private readonly SpawnGroup[] _groups;
        private readonly int[] _totals;
        private readonly int[] _spawned;
        private readonly float _intervalMultiplier;
        private float _elapsed;

        public WaveScheduler(WaveDefinition wave, DifficultyProfile difficulty)
        {
            _groups = new SpawnGroup[wave.Groups.Count];
            _totals = new int[_groups.Length];
            _spawned = new int[_groups.Length];
            _intervalMultiplier = difficulty.SpawnIntervalMultiplier;

            for (int i = 0; i < _groups.Length; i++)
            {
                _groups[i] = wave.Groups[i];
                _totals[i] = Mathf.Max(1, Mathf.CeilToInt(_groups[i].Count * difficulty.CountMultiplier));
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
                while (_spawned[i] < _totals[i] && SpawnTime(group, _spawned[i]) <= _elapsed)
                {
                    output.Add(new SpawnRequest(group.Enemy, group.PathIndex));
                    _spawned[i]++;
                }
            }
        }

        private float SpawnTime(SpawnGroup group, int index) => group.Delay + index * group.Interval * _intervalMultiplier;
    }
}

