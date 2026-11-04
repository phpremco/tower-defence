using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Decides when each enemy of one wave appears. Pure timing logic, no scene objects. Every spawn's drift is drawn when
    /// the wave starts, so the moment of any spawn is known ahead (a spawn origin opens a few seconds before its first
    /// creature comes out).
    /// </summary>
    public sealed class WaveScheduler
    {
        private const float BossSpacingSeconds = 18f;

        private readonly SpawnGroup[] _groups;
        private readonly int[] _totals;
        private readonly int[] _spawned;
        private readonly float[][] _jitter;
        private readonly float _intervalMultiplier;
        private float _elapsed;

        /// <param name="random">Source of the spawn jitter. Without one every spawn lands exactly on its schedule.</param>
        public WaveScheduler(WaveDefinition wave, DifficultyProfile difficulty, System.Random random = null)
        {
            _groups = new SpawnGroup[wave.Groups.Count];
            _totals = new int[_groups.Length];
            _spawned = new int[_groups.Length];
            _jitter = new float[_groups.Length][];
            _intervalMultiplier = difficulty.SpawnIntervalMultiplier;
            float jitterShare = random != null ? wave.SpawnJitter : 0f;

            for (int i = 0; i < _groups.Length; i++)
            {
                _groups[i] = wave.Groups[i];
                _totals[i] = CountOf(_groups[i], difficulty);
                if (IsBoss(_groups[i]))
                {
                    _groups[i].Interval = Mathf.Max(_groups[i].Interval, BossSpacingSeconds);
                }

                _jitter[i] = new float[_totals[i]];
                for (int k = 0; k < _totals[i]; k++)
                {
                    _jitter[i][k] = Jitter(_groups[i], random, jitterShare);
                }
            }
        }

        /// <summary>How many enemies a group sends on a difficulty: a boss is one creature however hard the level is.</summary>
        public static int CountOf(SpawnGroup group, DifficultyProfile difficulty) =>
            IsBoss(group) ? group.Count : Mathf.Max(1, Mathf.CeilToInt(group.Count * difficulty.CountMultiplier));

        private static bool IsBoss(SpawnGroup group) => group.Enemy != null && group.Enemy.IsBoss;

        public int GroupCount => _groups.Length;

        /// <summary>The group as this wave runs it (a boss group spaced out).</summary>
        public SpawnGroup Group(int index) => _groups[index];

        /// <summary>How many enemies the group sends in this wave.</summary>
        public int Total(int group) => _totals[group];

        /// <summary>Seconds after the wave start at which spawn <paramref name="index"/> of a group is due, drift included.</summary>
        public float PlannedTime(int group, int index) => SpawnTime(_groups[group], index) + _jitter[group][index];

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
                while (_spawned[i] < _totals[i] && PlannedTime(i, _spawned[i]) <= _elapsed)
                {
                    output.Add(new SpawnRequest(group.Enemy, group.PathIndex, group.Source, i, _spawned[i]));
                    _spawned[i]++;
                }
            }
        }

        private float SpawnTime(SpawnGroup group, int index) => group.Delay + (group.Stagger + index * group.Interval) * _intervalMultiplier;

        // A drift for one spawn of the group, early or late by at most the jitter share of one interval.
        private float Jitter(SpawnGroup group, System.Random random, float share)
        {
            if (random == null || share <= 0f)
            {
                return 0f;
            }

            float unit = (float)random.NextDouble() * 2f - 1f;
            return unit * share * group.Interval * _intervalMultiplier;
        }
    }
}
