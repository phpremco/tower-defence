using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// The commander. Stands at one post in front of a blockade (or at the base), hits the enemies that come within
    /// reach and takes a share of the hits on a guarded blockade. When its health runs out it is down for a while
    /// and then stands up again. Has no knowledge of how it looks.
    /// </summary>
    public sealed class Commander : MonoBehaviour, IDamageGuard
    {
        private const float ArrivalDistance = 0.05f;

        private readonly List<ITargetable> _buffer = new List<ITargetable>(32);

        private HeroDefinition _definition;
        private TargetRegistry _targets;
        private IReadOnlyList<HeroPost> _posts;
        private int _postIndex;
        private float _damageMultiplier = 1f;
        private float _strikeCooldown;
        private float _reviveRemaining;
        private float _shockwaveRemaining;
        private float _repairRemaining;

        public HeroDefinition Definition => _definition;

        public Health Health { get; private set; }

        public bool IsDown { get; private set; }

        public bool IsWalking { get; private set; }

        /// <summary>Direction on the ground plane the hero currently looks in.</summary>
        public Vector3 Facing { get; private set; } = Vector3.forward;

        /// <summary>Index of the post the hero stands at, or is walking to.</summary>
        public int PostIndex => _postIndex;

        public int PostCount => _posts.Count;

        public float ReviveRemaining => _reviveRemaining;

        /// <summary>One when the ability can be used, growing from zero while it cools down.</summary>
        public float ShockwaveReady => 1f - Mathf.Clamp01(_shockwaveRemaining / _definition.ShockwaveCooldown);

        public float RepairReady => 1f - Mathf.Clamp01(_repairRemaining / _definition.RepairCooldown);

        /// <summary>True when the emergency repair could be used right now, apart from what it costs.</summary>
        public bool CanEmergencyRepair => !IsDown && !IsWalking && _repairRemaining <= 0f;

        /// <summary>Where the hero stands for a post, so the HUD can find the spot to click.</summary>
        public Vector3 StandAt(int postIndex) => _posts[postIndex].Stand;

        /// <summary>True when the hero stands at, or is walking to, the post that guards the structure at this position.</summary>
        public bool IsAt(Vector3 guardedPosition)
        {
            Vector3 delta = _posts[_postIndex].Guarded.Position - guardedPosition;
            delta.y = 0f;
            return delta.sqrMagnitude < 0.01f;
        }

        /// <summary>Raised when anything the HUD shows changes: post, down state, health or cooldowns.</summary>
        public event Action Changed;

        /// <summary>Raised with the enemy's position and the weapon used whenever the hero attacks.</summary>
        public event Action<Vector3, HeroWeaponDefinition> Struck;

        /// <summary>Raised when the hero's health runs out and it goes down.</summary>
        public event Action Downed;

        /// <summary>Raised when a downed hero stands up again.</summary>
        public event Action Revived;

        /// <summary>Raised with the centre and radius when the shockwave goes off.</summary>
        public event Action<Vector3, float> ShockwaveReleased;

        public void Initialize(HeroDefinition definition, TargetRegistry targets, IReadOnlyList<HeroPost> posts, int startPost, PlayerModifiers modifiers)
        {
            _definition = definition;
            _targets = targets;
            _posts = posts;
            _damageMultiplier = modifiers.HeroDamageMultiplier;
            Health = new Health(definition.MaxHealth * modifiers.HeroHealthMultiplier);
            Health.Died += OnDied;
            Health.Changed += OnHealthChanged;
            _postIndex = Mathf.Clamp(startPost, 0, posts.Count - 1);
            transform.position = _posts[_postIndex].Stand;
            Facing = _posts[_postIndex].Facing;
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
                transform.position = _posts[_postIndex].Stand;
                Facing = _posts[_postIndex].Facing;
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

            _targets.FindInRadius(transform.position, _definition.ShockwaveRadius, _definition.ShockwaveTargets, _buffer);
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
            if (!CanEmergencyRepair)
            {
                return false;
            }

            _posts[_postIndex].Guarded.Restore(_definition.RepairFraction);
            _repairRemaining = _definition.RepairCooldown;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Hurts the hero directly, as slime or acid on the ground does. A downed hero is past hurting.</summary>
        public void TakeHazardDamage(float damage)
        {
            if (IsDown || damage <= 0f)
            {
                return;
            }

            Health.TakeDamage(damage);
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
                    Revived?.Invoke();
                    Changed?.Invoke();
                }

                return;
            }

            if (IsWalking)
            {
                Walk();
            }
            else
            {
                Strike();
            }
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
            Vector3 destination = _posts[_postIndex].Stand;
            Vector3 toTarget = destination - transform.position;
            toTarget.y = 0f;
            float step = _definition.WalkSpeed * Time.deltaTime;
            if (toTarget.magnitude <= step + ArrivalDistance)
            {
                transform.position = destination;
                Facing = _posts[_postIndex].Facing;
                Arrive();
                Changed?.Invoke();
                return;
            }

            Facing = toTarget.normalized;
            transform.position += toTarget.normalized * step;
        }

        private void Arrive()
        {
            IsWalking = false;
            _posts[_postIndex].Guarded.Guard = this;
        }

        private void ReleaseGuard()
        {
            if (ReferenceEquals(_posts[_postIndex].Guarded.Guard, this))
            {
                _posts[_postIndex].Guarded.Guard = null;
            }
        }

        // An attack: whatever stands within reach of the melee weapon gets the blade; otherwise the gun takes
        // anything within its own reach. The target takes the damage straight away (a bolt is too fast to dodge)
        // and the view plays the swing or the shot of the weapon used.
        private void Strike()
        {
            _strikeCooldown -= Time.deltaTime;
            if (_strikeCooldown > 0f)
            {
                return;
            }

            HeroWeaponDefinition weapon = _definition.Weapon;
            ITargetable target = _targets.FindBest(transform.position, weapon.Reach, weapon.Targets);
            if (target == null && _definition.RangedWeapon != null)
            {
                weapon = _definition.RangedWeapon;
                target = _targets.FindBest(transform.position, weapon.Reach, weapon.Targets);
            }

            if (target == null)
            {
                return;
            }

            Vector3 toTarget = target.Position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Facing = toTarget.normalized;
            }

            target.TakeDamage(weapon.Damage * _damageMultiplier);
            Struck?.Invoke(target.Position, weapon);
            _strikeCooldown = weapon.AttackInterval;
        }

        private void OnDied()
        {
            IsDown = true;
            IsWalking = false;
            _reviveRemaining = _definition.ReviveSeconds;
            ReleaseGuard();
            Downed?.Invoke();
            Changed?.Invoke();
        }

        private void OnHealthChanged(float _) => Changed?.Invoke();
    }
}
