using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One wave: groups of enemies and how long to wait before it begins. Stored inline in a <see cref="LevelWaves"/>
    /// asset rather than as an asset of its own, so a campaign of many levels and waves stays a handful of files.
    /// </summary>
    [Serializable]
    public sealed class WaveDefinition
    {
        public const float DefaultStartDelay = 8f;
        public const float DefaultSpawnJitter = 0.45f;

        [SerializeField] [Min(0f)] private float _startDelay = DefaultStartDelay;
        [SerializeField] [Range(0f, 1f)] private float _spawnJitter = DefaultSpawnJitter;
        [SerializeField] private List<SpawnGroup> _groups = new List<SpawnGroup>();
        [SerializeField] [Min(0.1f)] private float _healthMultiplier = 1f;

        /// <summary>Unity's serializer builds waves through this constructor, so the field defaults apply.</summary>
        public WaveDefinition()
        {
        }

        /// <summary>A wave built in code, for tests and tools; the generator writes waves through serialized properties.</summary>
        public WaveDefinition(IEnumerable<SpawnGroup> groups, float spawnJitter = DefaultSpawnJitter, float startDelay = DefaultStartDelay, float healthMultiplier = 1f)
        {
            _healthMultiplier = healthMultiplier;
            _groups.AddRange(groups);
            _spawnJitter = spawnJitter;
            _startDelay = startDelay;
        }

        /// <summary>Seconds between the previous wave finishing its spawns and this wave starting.</summary>
        public float StartDelay => _startDelay;

        /// <summary>
        /// Scales the health (and armour) of everything this wave sends, on top of the difficulty. Later waves carry
        /// more than one, so creatures keep growing tougher while the player's towers grow stronger.
        /// </summary>
        public float HealthMultiplier => _healthMultiplier;

        /// <summary>
        /// How far each spawn may drift from its scheduled moment, as a share of its group's interval. Zero spawns
        /// like clockwork; higher values make a stream feel like a crowd rather than a queue.
        /// </summary>
        public float SpawnJitter => _spawnJitter;

        public IReadOnlyList<SpawnGroup> Groups => _groups;
    }
}
