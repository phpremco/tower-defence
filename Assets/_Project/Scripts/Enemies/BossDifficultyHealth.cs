using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// One boss's own health factor on one difficulty, on top of that difficulty's boss share: for a boss whose fight shifts
    /// one difficulty's margin without touching the others (round 58: the Shattered's new entrance made Easy easier).
    /// </summary>
    [Serializable]
    public struct BossDifficultyHealth
    {
        /// <summary>The difficulty profile's asset name (Easy, Medium, Hard, Psycho).</summary>
        public string Difficulty;

        [Min(0.1f)] public float Factor;

        public BossDifficultyHealth(string difficulty, float factor)
        {
            Difficulty = difficulty;
            Factor = factor;
        }
    }
}
