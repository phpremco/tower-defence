using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Homing projectile. Deals direct damage, or splash damage when the radius is above zero. It knows its style
    /// and colour only to pass them on to its view and the impact effect; it never looks at them itself.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private const float HitDistance = 0.15f;

        // An aimed shot whose creature died in flight (a crowd of guns on the one furthest along) turns to the nearest live
        // creature of the same layer this close to where it was heading, instead of landing on nothing (round 21a: light guns
        // lost most of their shots that way against ant swarms, a shell's splash never did).
        private const float RetargetRadius = 1.5f;

        private readonly List<ITargetable> _splashTargets = new List<ITargetable>();

        private TargetRegistry _registry;
        private Action<Projectile> _release;
        private ITargetable _target;
        private Vector3 _lastTargetPosition;
        private float _damage;
        private float _speed;
        private float _splashRadius;
        private TargetLayers _mask;
        private float _slowFactor = 1f;
        private float _slowSeconds;
        private float _startDistance;
        private float _fuseRadius;
        private Vector3 _impactPoint;
        private object _source;

        /// <summary>Raised when the projectile leaves the barrel, so the view can dress it.</summary>
        public event Action<Projectile> Launched;

        /// <summary>Raised when the projectile lands: world position, splash radius (zero for single target) and colour.</summary>
        public event Action<Vector3, float, Color, ProjectileStyle> Impacted;

        public ProjectileStyle Style { get; private set; }

        public Color Color { get; private set; }

        /// <summary>Where the shot left from.</summary>
        public Vector3 Origin { get; private set; }

        /// <summary>Where the shot is heading right now.</summary>
        public Vector3 Destination => _lastTargetPosition;

        /// <summary>How far along the flight the shot is, from zero at the muzzle to one at the target.</summary>
        public float Progress
        {
            get
            {
                float remaining = (_lastTargetPosition - transform.position).magnitude;
                return _startDistance > 0.0001f ? Mathf.Clamp01(1f - remaining / _startDistance) : 1f;
            }
        }

        private TargetLayers _aimedLayer;

        public void Launch(
            Vector3 from,
            ITargetable target,
            float damage,
            float speed,
            float splashRadius,
            TargetLayers mask,
            TargetRegistry registry,
            Action<Projectile> release,
            float slowFactor,
            float slowSeconds,
            ProjectileStyle style,
            Color color,
            float fuseRadius = 0f,
            object source = null)
        {
            transform.position = from;
            _source = source;
            Origin = from;
            _target = target;
            _lastTargetPosition = target.Position;
            _aimedLayer = target.Layer;
            _startDistance = (_lastTargetPosition - from).magnitude;
            _damage = damage;
            _speed = speed;
            _splashRadius = splashRadius;
            _mask = mask;
            _registry = registry;
            _release = release;
            _slowFactor = slowFactor;
            _fuseRadius = fuseRadius;
            _slowSeconds = slowSeconds;
            Style = style;
            Color = color;
            Launched?.Invoke(this);
        }

        private void Update()
        {
            if (_target != null && !_target.IsAlive && _splashRadius <= 0f && _fuseRadius <= 0f)
            {
                Retarget();
            }

            if (_target != null && _target.IsAlive)
            {
                _lastTargetPosition = _target.Position;
            }

            Vector3 toTarget = _lastTargetPosition - transform.position;
            float step = _speed * Time.deltaTime;

            // A fused shell bursts where it is, near the flyer; any other shot lands on its target.
            if (_fuseRadius > 0f)
            {
                if (ProximityFuse.ShouldBurst(transform.position, _lastTargetPosition, _fuseRadius, Mathf.Max(step, HitDistance)))
                {
                    _impactPoint = ProximityFuse.BurstPoint(transform.position, _lastTargetPosition, _fuseRadius);
                    Impact();
                    return;
                }
            }
            else if (toTarget.magnitude <= Mathf.Max(step, HitDistance))
            {
                _impactPoint = _lastTargetPosition;
                Impact();
                return;
            }

            transform.position += toTarget.normalized * step;
        }

        private void Retarget()
        {
            _registry.FindInRadius(_lastTargetPosition, RetargetRadius, _mask, _splashTargets);
            ITargetable nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < _splashTargets.Count; i++)
            {
                ITargetable candidate = _splashTargets[i];
                float distance = (candidate.Position - _lastTargetPosition).sqrMagnitude;
                if (candidate.Layer == _aimedLayer && !_registry.IsConcealed(candidate) && distance < best)
                {
                    best = distance;
                    nearest = candidate;
                }
            }

            _target = nearest;
        }

        private void Impact()
        {
            if (_splashRadius > 0f)
            {
                _registry.FindInRadius(_impactPoint, _splashRadius, _mask, _splashTargets);
                for (int i = 0; i < _splashTargets.Count; i++)
                {
                    Deal(_splashTargets[i], true);
                    _splashTargets[i].ApplySlow(_slowFactor, _slowSeconds);
                }
            }
            else if (_target != null && _target.IsAlive)
            {
                Deal(_target, false);
                _target.ApplySlow(_slowFactor, _slowSeconds);
            }

            Impacted?.Invoke(_impactPoint, _splashRadius, Color, Style);
            _target = null;
            _source = null;
            _release(this);
        }

        private void Deal(ITargetable target, bool splash)
        {
            if (target is ISourcedTarget sourced)
            {
                sourced.TakeDamageFrom(_damage, _source, splash);
                return;
            }

            target.TakeDamage(_damage);
        }
    }
}
