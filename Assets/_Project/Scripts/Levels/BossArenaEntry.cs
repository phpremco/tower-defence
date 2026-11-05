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

        public BossArenaEntry()
        {
        }

        public BossArenaEntry(EnemyDefinition boss, LevelDefinition level)
        {
            _boss = boss;
            _level = level;
        }

        public EnemyDefinition Boss => _boss;
        public LevelDefinition Level => _level;
    }
}
