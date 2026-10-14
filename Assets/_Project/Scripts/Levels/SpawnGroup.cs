using System;
using Game.Enemies;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>A run of identical enemies inside a wave.</summary>
    [Serializable]
    public struct SpawnGroup
    {
        public EnemyDefinition Enemy;
        [Min(1)] public int Count;

        /// <summary>Index of the level path this group follows.</summary>
        [Min(0)] public int PathIndex;

        /// <summary>Seconds between two enemies of this group.</summary>
        [Min(0.05f)] public float Interval;

        /// <summary>Seconds after the wave starts before the first enemy appears.</summary>
        [Min(0f)] public float Delay;
    }
}

