using System;
using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.PathObjects
{
    /// <summary>
    /// Runs one placed path object: waits and explodes (mine), slows (slow field) or rolls along the road (roller).
    /// Has no knowledge of how it looks.
    /// </summary>
    public sealed class PathObject : MonoBehaviour
    {
        private const float ArrivalDistance = 0.02f;
        private const float SlowRefreshSeconds = 0.3f;

        private readonly List<ITargetable> _buffer = new List<ITargetable>(32);

        private PathObjectDefinition _definition;
        private TargetRegistry _targets;
        private Vector3[] _road;
        private int _nextWaypoint;
        private float _age;
        private bool _finished;

        public PathObjectDefinition Definition => _definition;

        /// <summary>Raised with position and radius when a mine goes off.</summary>
        public event Action<Vector3, float> Triggered;

        /// <summary>Raised once when the object is used up, expires or reaches the end of the road.</summary>
        public event Action<PathObject> Finished;

        /// <param name="road">Waypoints of the road a roller follows. Ignored by other kinds.</param>
        /// <param name="nextWaypoint">Index of the waypoint a roller heads for first.</param>
        public void Initialize(PathObjectDefinition definition, TargetRegistry targets, Vector3[] road, int nextWaypoint)
        {
            _definition = definition;
            _targets = targets;
            _road = road;
            _nextWaypoint = nextWaypoint;
        }

        private void Update()
        {
            if (_finished)
            {
                return;
            }

            _age += Time.deltaTime;
            if (_definition.Lifetime > 0f && _age >= _definition.Lifetime)
            {
                Finish();
                return;
            }

            switch (_definition.Kind)
            {
                case PathObjectKind.Mine:
                    UpdateMine();
                    break;
                case PathObjectKind.SlowField:
                    UpdateSlowField();
                    break;
                case PathObjectKind.Roller:
                    UpdateRoller();
                    break;
            }
        }

        private void UpdateMine()
        {
            _targets.FindInRadius(transform.position, _definition.TriggerRadius, _definition.Targets, _buffer);
            if (_buffer.Count == 0)
            {
                return;
            }

            _targets.FindInRadius(transform.position, _definition.Radius, _definition.Targets, _buffer);
            for (int i = 0; i < _buffer.Count; i++)
            {
                Hit(_buffer[i], _definition.Damage);
            }

            Triggered?.Invoke(transform.position, _definition.Radius);
            Finish();
        }

        private void UpdateSlowField()
        {
            _targets.FindInRadius(transform.position, _definition.Radius, _definition.Targets, _buffer);
            for (int i = 0; i < _buffer.Count; i++)
            {
                _buffer[i].ApplySlow(_definition.SlowFactor, SlowRefreshSeconds);
            }
        }

        private void UpdateRoller()
        {
            Move();
            if (_finished)
            {
                return;
            }

            _targets.FindInRadius(transform.position, _definition.Radius, _definition.Targets, _buffer);
            float damage = _definition.Damage * Time.deltaTime;
            for (int i = 0; i < _buffer.Count; i++)
            {
                Hit(_buffer[i], damage);
            }
        }

        private void Move()
        {
            float remaining = _definition.Speed * Time.deltaTime;
            while (remaining > 0f && _nextWaypoint < _road.Length)
            {
                Vector3 toTarget = _road[_nextWaypoint] - transform.position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;
                if (distance <= remaining + ArrivalDistance)
                {
                    transform.position = new Vector3(_road[_nextWaypoint].x, transform.position.y, _road[_nextWaypoint].z);
                    remaining -= distance;
                    _nextWaypoint++;
                    continue;
                }

                transform.position += toTarget / distance * remaining;
                remaining = 0f;
            }

            if (_nextWaypoint >= _road.Length)
            {
                Finish();
            }
        }

        // The blast or the roll is passed with its definition as the source, so a boss knows a lava ball burned it.
        private void Hit(ITargetable target, float damage)
        {
            if (target is ISourcedTarget sourced)
            {
                sourced.TakeDamageFrom(damage, _definition, true);
            }
            else
            {
                target.TakeDamage(damage);
            }
        }

        private void Finish()
        {
            _finished = true;
            Finished?.Invoke(this);
        }
    }
}
