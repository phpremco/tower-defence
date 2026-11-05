using System;
using Game.Enemies;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One boss of the developer arena and the level it is fought in: a boss that needs a liquid the arena's own level
    /// lacks (lava, ice) gets a level that has it.
    /// </summary>
    [Serializable]
    public sealed class BossArenaEntry
    {
        [SerializeField] private EnemyDefinition _boss;
        [Tooltip("The level to fight it in; empty uses the arena's own level.")]
        [SerializeField] private LevelDefinition _level;
        [Tooltip("Short escort waves before the boss wave, for a boss whose coming shows over the waves before it (roots).")]
        [SerializeField] [Min(0)] private int _leadWaves;

        public BossArenaEntry()
        {
        }

        public BossArenaEntry(EnemyDefinition boss, LevelDefinition level, int leadWaves = 0)
        {
            _boss = boss;
            _level = level;
            _leadWaves = leadWaves;
        }

        public EnemyDefinition Boss => _boss;
        public LevelDefinition Level => _level;

        /// <summary>Escort waves before the boss wave (compressed: a few seconds each).</summary>
        public int LeadWaves => _leadWaves;
    }
}
