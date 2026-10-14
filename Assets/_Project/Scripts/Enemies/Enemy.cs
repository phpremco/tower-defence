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

        // One cell is kept free in front of a blockade for the hero, so enemies halt just before that cell.
        private const float BlockadeStopDistance = 1.4f;

        private EnemyDefinition _definition;
        private Vector3[] _waypoints;
        private int _nextWaypoint;
        private float _travelled;
        private float _heightOffset;
        private TargetRegistry _registry;
        private StructureRegistry _structures;
        private float _attackCooldown;
        private bool _active;

        public EnemyDefinition Definition => _definition;

        public Health Health { get; private set; }

        public int Reward { get; private set; }

        public Vector3 Position => transform.position;

        /// <summary>Direction of travel on the ground plane, for views that turn towards it.</summary>
        public Vector3 Heading { get; private set; } = Vector3.forward;

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
            TargetRegistry registry,
            StructureRegistry structures)
        {
            _definition = definition;
            _waypoints = waypoints;
            _nextWaypoint = 1;
            _travelled = 0f;
            _heightOffset = heightOffset;
            _registry = registry;
            _structures = structures;
            _attackCooldown = 0f;
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

            if (IsHeldByBlockade())
            {
                return;
            }

            TryAttackStructure();

            float remaining = _definition.Speed * Time.deltaTime;

            while (remaining > 0f && _nextWaypoint < _waypoints.Length)
            {
                Vector3 target = _waypoints[_nextWaypoint] + Vector3.up * _heightOffset;
                Vector3 toTarget = target - transform.position;
                float distance = toTarget.magnitude;

                if (distance <= remaining + ArrivalDistance)
                {
                    transform.position = target;
                    if (distance > ArrivalDistance)
                    {
                        Heading = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
                    }

                    remaining -= distance;
                    _travelled += distance;
                    _nextWaypoint++;
                    continue;
                }

                Heading = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
                transform.position += toTarget / distance * remaining;
                _travelled += remaining;
                remaining = 0f;
            }

            if (_nextWaypoint >= _waypoints.Length)
            {
                ReachedEnd?.Invoke(this);
            }
        }

        // Ground enemies cannot pass a living blockade: they stop in front of it and hit it until it falls.
        private bool IsHeldByBlockade()
        {
            if (_definition.Layer != TargetLayers.Ground)
            {
                return false;
            }

            IStructure blocker = _structures.FindBlocker(transform.position, BlockadeStopDistance);
            if (blocker == null)
            {
                return false;
            }

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown <= 0f)
            {
                blocker.TakeDamage(_definition.BlockadeDamage);
                _attackCooldown = _definition.AttackInterval;
            }

            return true;
        }

        private void TryAttackStructure()
        {
            if (_definition.AttackDamage <= 0f)
            {
                return;
            }

            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f)
            {
                return;
            }

            IStructure target = _structures.FindNearest(transform.position, _definition.AttackRange);
            if (target != null)
            {
                target.TakeDamage(_definition.AttackDamage);
                _attackCooldown = _definition.AttackInterval;
            }
        }

        private void OnDied() => Killed?.Invoke(this);
    }
}


