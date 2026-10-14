using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One wave: groups of enemies and how long to wait before it begins.</summary>
    [CreateAssetMenu(menuName = "Game/Wave Definition", fileName = "WaveDefinition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [SerializeField] [Min(0f)] private float _startDelay = 8f;
        [SerializeField] private List<SpawnGroup> _groups = new List<SpawnGroup>();

        /// <summary>Seconds between the previous wave finishing its spawns and this wave starting.</summary>
        public float StartDelay => _startDelay;

        public IReadOnlyList<SpawnGroup> Groups => _groups;
    }
}
