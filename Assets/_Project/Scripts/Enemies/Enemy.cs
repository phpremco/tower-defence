using System;
using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>Walks along a list of waypoints and can be targeted. Has no knowledge of how it looks.</summary>
    public sealed class Enemy : MonoBehaviour, ITargetable
    {
        private const float ArrivalDistance = 0.02f;

        private EnemyDefinition _definition;
        private Vector3[] _waypoints;
        private int _nextWaypoint;
        private float _travelled;
        private float _heightOffset;
        private TargetRegistry _registry;
        private bool _active;

        public EnemyDefinition Definition => _definition;

        public Health Health { get; private set; }

        public int Reward { get; private set; }

        public Vector3 Position => transform.position;

        public TargetLayers Layer => _definition.Layer;

        public float PathProgress => _travelled;

        public bool IsAlive => _active && Health != null && !Health.IsDead;

        /// <summary>Raised after Initialize, so a view can rebind to the new Health.</summary>
        public event Action<Enemy> Initialized;

        public event Action<Enemy> Killed;

        public event Action<Enemy> ReachedEnd;

        public void Initialize(
            EnemyDefinition definition,
            Vector3[] waypoints,
            float healthMultiplier,
            float rewardMultiplier,
            float heightOffset,
            TargetRegistry registry)
        {
            _definition = definition;
            _waypoints = waypoints;
            _nextWaypoint = 1;
            _travelled = 0f;
            _heightOffset = heightOffset;
            _registry = registry;
            Reward = Mathf.RoundToInt(definition.Reward * rewardMultiplier);
            Health = new Health(definition.MaxHealth * healthMultiplier);
            Health.Died += OnDied;
            transform.position = waypoints[0] + Vector3.up * heightOffset;
            _active = true;
            _registry.Add(this);
            Initialized?.Invoke(this);
        }

        public void TakeDamage(float amount) => Health?.TakeDamage(amount);

        /// <summary>Called by whoever pools this enemy when it is released.</summary>
        public void Deactivate()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _registry.Remove(this);
            Health.Died -= OnDied;
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            float remaining = _definition.Speed * Time.deltaTime;

            while (remaining > 0f && _nextWaypoint < _waypoints.Length)
            {
                Vector3 target = _waypoints[_nextWaypoint] + Vector3.up * _heightOffset;
                Vector3 toTarget = target - transform.position;
                float distance = toTarget.magnitude;

                if (distance <= remaining + ArrivalDistance)
                {
                    transform.position = target;
                    remaining -= distance;
                    _travelled += distance;
                    _nextWaypoint++;
                    continue;
                }

                transform.position += toTarget / distance * remaining;
                _travelled += remaining;
                remaining = 0f;
            }

            if (_nextWaypoint >= _waypoints.Length)
            {
                ReachedEnd?.Invoke(this);
            }
        }

        private void OnDied() => Killed?.Invoke(this);
    }
}
