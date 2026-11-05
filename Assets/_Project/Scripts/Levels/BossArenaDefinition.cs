using System.Collections.Generic;
using Game.Enemies;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The developer boss arena (dev story viewer): the bosses to pick from, each with the level it is fought in (the
    /// arena's own, or one with the liquid its mechanic needs), the money to build a defence with first, and a small escort.
    /// Fighting there pays nothing and saves nothing; it is for trying a boss before the campaign level that brings it exists.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss Arena", fileName = "BossArena")]
    public sealed class BossArenaDefinition : ScriptableObject
    {
        [SerializeField] private LevelDefinition _level;
        [SerializeField] private DifficultyProfile _difficulty;
        [SerializeField] private BossArenaEntry[] _entries = new BossArenaEntry[0];
        [SerializeField] [Min(0)] private int _startingMoney = 3000;
        [Tooltip("Seconds to build before the boss comes.")]
        [SerializeField] [Min(0f)] private float _buildSeconds = 40f;
        [SerializeField] private EnemyDefinition _escort;
        [SerializeField] [Min(0)] private int _escortCount = 6;
        [SerializeField] [Min(0.1f)] private float _escortInterval = 1.5f;
        [Tooltip("Health scale of the arena's wave: a campaign finale's late wave makes its boss this much tougher.")]
        [SerializeField] [Min(0.1f)] private float _healthMultiplier = 2.5f;

        /// <summary>The arena's own level, for the bosses that name none.</summary>
        public LevelDefinition Level => _level;
        public DifficultyProfile Difficulty => _difficulty;
        public IReadOnlyList<BossArenaEntry> Entries => _entries;
        public int StartingMoney => _startingMoney;

        /// <summary>Every boss the arena lists, in order.</summary>
        public IReadOnlyList<EnemyDefinition> Bosses
        {
            get
            {
                var bosses = new List<EnemyDefinition>(_entries.Length);
                foreach (BossArenaEntry entry in _entries)
                {
                    if (entry != null && entry.Boss != null)
                    {
                        bosses.Add(entry.Boss);
                    }
                }

                return bosses;
            }
        }

        /// <summary>The level a boss is fought in: its own entry's, else the arena's.</summary>
        public LevelDefinition LevelFor(EnemyDefinition boss)
        {
            foreach (BossArenaEntry entry in _entries)
            {
                if (entry != null && entry.Boss == boss && entry.Level != null)
                {
                    return entry.Level;
                }
            }

            return _level;
        }

        /// <summary>The arena's one wave: a few escorts down the road, then the boss through its own entrance.</summary>
        public WaveDefinition[] WavesFor(EnemyDefinition boss)
        {
            var groups = new List<SpawnGroup>();
            if (_escort != null && _escortCount > 0)
            {
                groups.Add(new SpawnGroup { Enemy = _escort, Count = _escortCount, Interval = _escortInterval, Source = SpawnSource.Gate });
            }

            // The boss comes halfway through the escort, as a continent boss does halfway through its wave (Docs/BOSSES.md).
            groups.Add(new SpawnGroup { Enemy = boss, Count = 1, Interval = 1f, Delay = _escortCount * _escortInterval * 0.5f, Source = SpawnSource.Entrance });
            return new[] { new WaveDefinition(groups, WaveDefinition.DefaultSpawnJitter, _buildSeconds, _healthMultiplier) };
        }

        private void OnValidate()
        {
            if (_level == null || _difficulty == null)
            {
                Debug.LogWarning($"BossArenaDefinition '{name}' needs a level and a difficulty.", this);
            }
        }
    }
}
