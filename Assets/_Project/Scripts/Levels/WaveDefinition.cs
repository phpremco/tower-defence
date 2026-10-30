using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One wave: groups of enemies and how long to wait before it begins.</summary>
    [CreateAssetMenu(menuName = "Game/Wave Definition", fileName = "WaveDefinition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [SerializeField] [Min(0f)] private float _startDelay = 8f;
        [SerializeField] [Range(0f, 1f)] private float _spawnJitter = 0.45f;
        [SerializeField] private List<SpawnGroup> _groups = new List<SpawnGroup>();
        [SerializeField] [Min(0.1f)] private float _healthMultiplier = 1f;

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
