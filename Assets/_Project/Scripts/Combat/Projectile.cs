using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Homing projectile. Deals direct damage, or splash damage when the radius is above zero.</summary>
    public sealed class Projectile : MonoBehaviour
    {
        private const float HitDistance = 0.15f;

        private readonly List<ITargetable> _splashTargets = new List<ITargetable>();

        private TargetRegistry _registry;
        private Action<Projectile> _release;
        private ITargetable _target;
        private Vector3 _lastTargetPosition;
        private float _damage;
        private float _speed;
        private float _splashRadius;
        private TargetLayers _mask;

        /// <summary>Raised when the projectile lands: world position and splash radius (zero for single target).</summary>
        public event Action<Vector3, float> Impacted;

        public void Launch(
            Vector3 from,
            ITargetable target,
            float damage,
            float speed,
            float splashRadius,
            TargetLayers mask,
            TargetRegistry registry,
            Action<Projectile> release)
        {
            transform.position = from;
            var trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }

            _target = target;
            _lastTargetPosition = target.Position;
            _damage = damage;
            _speed = speed;
            _splashRadius = splashRadius;
            _mask = mask;
            _registry = registry;
            _release = release;
        }

        private void Update()
        {
            if (_target != null && _target.IsAlive)
            {
                _lastTargetPosition = _target.Position;
            }

            Vector3 toTarget = _lastTargetPosition - transform.position;
            float step = _speed * Time.deltaTime;

            if (toTarget.magnitude <= Mathf.Max(step, HitDistance))
            {
                Impact();
                return;
            }

            transform.position += toTarget.normalized * step;
        }

        private void Impact()
        {
            if (_splashRadius > 0f)
            {
                _registry.FindInRadius(_lastTargetPosition, _splashRadius, _mask, _splashTargets);
                for (int i = 0; i < _splashTargets.Count; i++)
                {
                    _splashTargets[i].TakeDamage(_damage);
                }
            }
            else if (_target != null && _target.IsAlive)
            {
                _target.TakeDamage(_damage);
            }

            Impacted?.Invoke(_lastTargetPosition, _splashRadius);
            _target = null;
            _release(this);
        }
    }
}


