using System;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>One shard of a splitting boss: the creature, the route it takes, where it leaves the giant, and its spire.</summary>
    [Serializable]
    public sealed class ShardSpec
    {
        [SerializeField] private EnemyDefinition _enemy;
        [SerializeField] private ShardRoute _route;
        [Tooltip("Where it bursts out of the giant (model units: x right, y up, z forward), README round 28 spawn_offsets.")]
        [SerializeField] private Vector3 _offset;
        [Tooltip("The giant's core spire this shard was, shrunk on the rejoined giant when the shard died.")]
        [SerializeField] private string _spire = string.Empty;

        public ShardSpec()
        {
        }

        public ShardSpec(EnemyDefinition enemy, ShardRoute route, Vector3 offset, string spire)
        {
            _enemy = enemy;
            _route = route;
            _offset = offset;
            _spire = spire;
        }

        public EnemyDefinition Enemy => _enemy;
        public ShardRoute Route => _route;
        public Vector3 Offset => _offset;
        public string Spire => _spire;
    }
}
