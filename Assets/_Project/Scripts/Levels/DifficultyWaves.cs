using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>The waves a level uses on one difficulty, stored inline in its <see cref="LevelWaves"/> asset.</summary>
    [Serializable]
    public struct DifficultyWaves
    {
        public DifficultyProfile Difficulty;
        public WaveDefinition[] Waves;
    }
}
