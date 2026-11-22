using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The coins one level starts with on one difficulty, set by hand (round 88e). A level that carries an entry for a
    /// difficulty starts with exactly this sum; without one the shared rule counts it (<see cref="StartMoneyRule"/>).
    /// </summary>
    [Serializable]
    public struct LevelStartMoney
    {
        [Tooltip("The difficulty this sum is for.")]
        public DifficultyProfile Difficulty;

        [Tooltip("Everything the wallet holds at the start of the level on that difficulty. The difficulty's multiplier and the air-raid money are not added to it.")]
        [Min(0)] public int Money;
    }
}
