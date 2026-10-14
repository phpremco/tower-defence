using System;
using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// The commander. Stands at one post (a blockade or the base), shoots what comes near and takes a share of the
    /// hits on a guarded blockade. When its health runs out it is down for a while and then stands up again.
    /// Has no knowledge of how it looks.
    /// </summary>
    public sealed class Commander : MonoBehaviour, IDamageGuard
    {
        private const float ArrivalDistance = 0.05f;

        private readonly List<ITargetable> _buffer = new List<ITargetable>(32);

        private HeroDefinition _definition;
        private TargetRegistry _targets;
        private ProjectilePool _projectiles;
        private IReadOnlyList<IGuardable> _posts;
        private int _postIndex;
        private float _fireCooldown;
        private float _reviveRemaining;
        private float _shockwaveRemaining;
        private float _repairRemaining;

        public HeroDefinition Definition => _definition;

        public Health Health { get; private set; }

        public bool IsDown { get; private set; }

        public bool IsWalking { get; private set; }

        /// <summary>Index of the post the hero stands at, or is walking to.</summary>
        public int PostIndex => _postIndex;

        public int PostCount => _posts.Count;

        public float ReviveRemaining => _reviveRemaining;

        /// <summary>True when the hero stands at, or is walking to, the post at this position.</summary>
        public bool IsAt(Vector3 postPosition)
        {
            Vector3 delta = _posts[_postIndex].Position - postPosition;
            delta.y = 0f;
            return delta.sqrMagnitude < 0.01f;
        }

        /// <summary>One when the ability can be used, growing from zero while it cools down.</summary>
        public float ShockwaveReady => 1f - Mathf.Clamp01(_shockwaveRemaining / _definition.ShockwaveCooldown);

        public float RepairReady => 1f - Mathf.Clamp01(_repairRemaining / _definition.RepairCooldown);

        /// <summary>Raised when anything the HUD shows changes: post, down state, health or cooldowns.</summary>
        public event Action Changed;

        /// <summary>Raised with the target position whenever the hero shoots.</summary>
        public event Action<Vector3> Fired;

        /// <summary>Raised with the centre and radius when the shockwave goes off.</summary>
        public event Action<Vector3, float> ShockwaveReleased;

        public void Initialize(HeroDefinition definition, TargetRegistry targets, ProjectilePool projectiles, IReadOnlyList<IGuardable> posts, int startPost)
        {
            _definition = definition;
            _targets = targets;
            _projectiles = projectiles;
            _posts = posts;
            Health = new Health(definition.MaxHealth);
            Health.Died += OnDied;
            Health.Changed += OnHealthChanged;
            _postIndex = Mathf.Clamp(startPost, 0, posts.Count - 1);
            transform.position = _posts[_postIndex].Position;
            Arrive();
        }

        private void OnDestroy()
        {
            if (Health != null)
            {
                Health.Died -= OnDied;
                Health.Changed -= OnHealthChanged;
            }
        }

        /// <summary>Sends the hero to a post. A downed hero stands up at the new post instead of walking.</summary>
        public void MoveTo(int postIndex)
        {
            if (postIndex < 0 || postIndex >= _posts.Count || (postIndex == _postIndex && !IsWalking))
            {
                return;
            }

            ReleaseGuard();
            _postIndex = postIndex;
            if (IsDown)
            {
                transform.position = _posts[_postIndex].Position;
            }
            else
            {
                IsWalking = true;
            }

            Changed?.Invoke();
        }

        public bool TryShockwave()
        {
            if (IsDown || _shockwaveRemaining > 0f)
            {
                return false;
            }

            _targets.FindInRadius(transform.position, _definition.ShockwaveRadius, _definition.Targets, _buffer);
            for (int i = 0; i < _buffer.Count; i++)
            {
                _buffer[i].TakeDamage(_definition.ShockwaveDamage);
            }

            _shockwaveRemaining = _definition.ShockwaveCooldown;
            ShockwaveReleased?.Invoke(transform.position, _definition.ShockwaveRadius);
            Changed?.Invoke();
            return true;
        }

        public bool TryEmergencyRepair()
        {
            if (IsDown || IsWalking || _repairRemaining > 0f)
            {
                return false;
            }

            _posts[_postIndex].Restore(_definition.RepairFraction);
            _repairRemaining = _definition.RepairCooldown;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Takes the hero's share of a hit on the guarded structure and returns what goes through.</summary>
        public float Absorb(float damage)
        {
            if (IsDown || IsWalking)
            {
                return damage;
            }

            float taken = damage * _definition.GuardShare;
            Health.TakeDamage(taken);
            return damage - taken;
        }

        private void Update()
        {
            TickCooldowns();

            if (IsDown)
            {
                _reviveRemaining -= Time.deltaTime;
                if (_reviveRemaining <= 0f)
                {
                    Health.Heal(Health.Max);
                    IsDown = false;
                    Arrive();
                    Changed?.Invoke();
                }

                return;
            }

            if (IsWalking)
            {
                Walk();
            }

            Shoot();
        }

        private void TickCooldowns()
        {
            bool cooling = _shockwaveRemaining > 0f || _repairRemaining > 0f;
            _shockwaveRemaining = Mathf.Max(0f, _shockwaveRemaining - Time.deltaTime);
            _repairRemaining = Mathf.Max(0f, _repairRemaining - Time.deltaTime);
            if (cooling)
            {
                Changed?.Invoke();
            }
        }

        private void Walk()
        {
            Vector3 destination = _posts[_postIndex].Position;
            Vector3 toTarget = destination - transform.position;
            float step = _definition.WalkSpeed * Time.deltaTime;
            if (toTarget.magnitude <= step + ArrivalDistance)
            {
                transform.position = destination;
                Arrive();
                Changed?.Invoke();
                return;
            }

            transform.position += toTarget.normalized * step;
        }

        private void Arrive()
        {
            IsWalking = false;
            _posts[_postIndex].Guard = this;
        }

        private void ReleaseGuard()
        {
            if (ReferenceEquals(_posts[_postIndex].Guard, this))
            {
                _posts[_postIndex].Guard = null;
            }
        }

        private void Shoot()
        {
            _fireCooldown -= Time.deltaTime;
            if (_fireCooldown > 0f)
            {
                return;
            }

            ITargetable target = _targets.FindBest(transform.position, _definition.Range, _definition.Targets);
            if (target == null)
            {
                return;
            }

            _projectiles.Fire(transform.position + Vector3.up, target, _definition.Damage, _definition.ProjectileSpeed, 0f, _definition.Targets);
            Fired?.Invoke(target.Position);
            _fireCooldown = _definition.FireInterval;
        }

        private void OnDied()
        {
            IsDown = true;
            IsWalking = false;
            _reviveRemaining = _definition.ReviveSeconds;
            ReleaseGuard();
            Changed?.Invoke();
        }

        private void OnHealthChanged(float _) => Changed?.Invoke();
    }
}
