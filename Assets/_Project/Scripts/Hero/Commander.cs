using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// The hero. Stands at one post in front of a blockade (or at the base), hits the enemies that come within reach
    /// and takes a share of the hits on a guarded blockade. When its health runs out it is down for a while and then
    /// stands up again. Its ability buttons come from data (<see cref="HeroAbilityDefinition"/>), its gun reloads after a
    /// magazine, and unlocked perks shorten that. Has no knowledge of how it looks.
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
        private HeroPerkModifiers _perks = HeroPerkModifiers.None;
        private HeroAbilityState[] _abilities = new HeroAbilityState[0];
        private int _shotsFired;
        private float _reloadRemaining;

        public HeroDefinition Definition => _definition;

        public Health Health { get; private set; }

        public bool IsDown { get; private set; }

        public bool IsWalking { get; private set; }

        /// <summary>Direction on the ground plane the hero currently looks in.</summary>
        public Vector3 Facing { get; private set; } = Vector3.forward;

        /// <summary>What the last strike went for, or null; the view keeps the weapon on it between strikes.</summary>
        public ITargetable Target { get; private set; }

        /// <summary>Index of the post the hero stands at, or is walking to.</summary>
        public int PostIndex => _postIndex;

        public int PostCount => _posts.Count;

        public float ReviveRemaining => _reviveRemaining;

        /// <summary>The ability slots, by button; a slot is null while the perk it needs is not unlocked.</summary>
        public IReadOnlyList<HeroAbilityState> Abilities => _abilities;

        /// <summary>The perks in force for this level.</summary>
        public HeroPerkModifiers Perks => _perks;

        /// <summary>Seconds left on the ranged weapon's reload; zero while it has shots.</summary>
        public float ReloadRemaining => _reloadRemaining;

        /// <summary>Seconds between two shots of the ranged weapon with the perks applied.</summary>
        public float RangedInterval => _definition.RangedWeapon != null ? _definition.RangedWeapon.AttackInterval * _perks.RangedIntervalMultiplier : 0f;

        /// <summary>Seconds a full reload of the ranged weapon takes with the perks applied.</summary>
        public float ReloadSeconds => _definition.RangedWeapon != null ? _definition.RangedWeapon.ReloadSeconds * _perks.ReloadMultiplier : 0f;

        /// <summary>Where the structure the hero guards right now stands.</summary>
        public Vector3 GuardedPosition => _posts[_postIndex].Guarded.Position;

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

        /// <summary>Raised with the slot, the ability and the hero's position whenever an ability is used.</summary>
        public event Action<int, HeroAbilityDefinition, Vector3> AbilityUsed;

        /// <summary>Raised with the reload time when the ranged weapon runs dry and starts to reload.</summary>
        public event Action<float> Reloading;

        /// <summary>Raised with where it is when something huge appears that the hero recoils from (a boss's first entrance).</summary>
        public event Action<Vector3> Startled;

        /// <summary>Raised when the hero calls out (a bark): the view points and shouts.</summary>
        public event Action Called;

        /// <summary>The hero recoils from something huge at a point; only the views react, the fight goes on.</summary>
        public void Startle(Vector3 at)
        {
            if (!IsDown)
            {
                Startled?.Invoke(at);
            }
        }

        /// <summary>The hero calls out a short line; only the views react.</summary>
        public void CallOut()
        {
            if (!IsDown)
            {
                Called?.Invoke();
            }
        }

        public void Initialize(HeroDefinition definition, TargetRegistry targets, IReadOnlyList<HeroPost> posts, int startPost, PlayerModifiers modifiers)
        {
            Initialize(definition, targets, posts, startPost, modifiers, null);
        }

        /// <param name="unlockedPerks">The perks the player has earned; they set the modifiers and open the abilities that need them.</param>
        public void Initialize(HeroDefinition definition, TargetRegistry targets, IReadOnlyList<HeroPost> posts, int startPost, PlayerModifiers modifiers, IReadOnlyCollection<HeroPerkDefinition> unlockedPerks)
        {
            _definition = definition;
            _targets = targets;
            _posts = posts;
            _damageMultiplier = modifiers.HeroDamageMultiplier;
            _perks = HeroPerkModifiers.From(unlockedPerks);
            _abilities = BuildAbilities(definition.Abilities, unlockedPerks);
            Health = new Health(definition.MaxHealth * modifiers.HeroHealthMultiplier);
            Health.Died += OnDied;
            Health.Changed += OnHealthChanged;
            _postIndex = Mathf.Clamp(startPost, 0, posts.Count - 1);
            transform.position = _posts[_postIndex].Stand;
            Facing = _posts[_postIndex].Facing;
            Arrive();
        }

        private static HeroAbilityState[] BuildAbilities(HeroAbilityDefinition[] definitions, IReadOnlyCollection<HeroPerkDefinition> perks)
        {
            var states = new HeroAbilityState[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                HeroAbilityDefinition ability = definitions[i];
                bool open = ability != null && (ability.RequiresPerk == null || Contains(perks, ability.RequiresPerk));
                states[i] = open ? new HeroAbilityState(ability) : null;
            }

            return states;
        }

        private static bool Contains(IReadOnlyCollection<HeroPerkDefinition> perks, HeroPerkDefinition perk)
        {
            if (perks == null)
            {
                return false;
            }

            foreach (HeroPerkDefinition owned in perks)
            {
                if (owned == perk)
                {
                    return true;
                }
            }

            return false;
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

        /// <summary>The ability in a slot, or null for an empty or locked slot.</summary>
        public HeroAbilityState AbilityAt(int slot) => slot >= 0 && slot < _abilities.Length ? _abilities[slot] : null;

        /// <summary>True when the ability in the slot could be used right now, apart from what it costs.</summary>
        public bool CanUse(int slot)
        {
            HeroAbilityState state = AbilityAt(slot);
            return state != null && !IsDown && state.Ready && HeroAbilityEffects.CanApply(state.Definition, _posts[_postIndex].Guarded, !IsWalking);
        }

        /// <summary>Uses the ability in a slot when it can; the caller has paid its price already.</summary>
        public bool TryUse(int slot)
        {
            if (!CanUse(slot))
            {
                return false;
            }

            HeroAbilityState state = _abilities[slot];
            HeroAbilityEffects.Apply(state.Definition, transform.position, _targets, _buffer, _posts[_postIndex].Guarded);
            state.Begin();
            AbilityUsed?.Invoke(slot, state.Definition, transform.position);
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
            TickCooldowns(Time.deltaTime);

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

        private void TickCooldowns(float seconds)
        {
            bool shownChanged = false;
            for (int i = 0; i < _abilities.Length; i++)
            {
                if (_abilities[i] != null && _abilities[i].Tick(seconds))
                {
                    shownChanged = true;
                }
            }

            if (_reloadRemaining > 0f)
            {
                _reloadRemaining = Mathf.Max(0f, _reloadRemaining - seconds);
            }

            if (shownChanged)
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
        // anything within its own reach, unless it is reloading. The target takes the damage straight away (a bolt is
        // too fast to dodge) and the view plays the swing or the shot of the weapon used.
        private void Strike()
        {
            _strikeCooldown -= Time.deltaTime;
            // A long frame may owe more than one attack; see Cadence.
            int strikes = 0;
            while (Cadence.Due(_strikeCooldown, strikes) && StrikeOnce())
            {
                strikes++;
            }

            _strikeCooldown = Cadence.Idle(_strikeCooldown);
        }

        private bool StrikeOnce()
        {
            HeroWeaponDefinition weapon = _definition.Weapon;
            ITargetable target = _targets.FindBest(transform.position, weapon.Reach, weapon.Targets);
            bool ranged = false;
            if (target == null && _definition.RangedWeapon != null)
            {
                weapon = _definition.RangedWeapon;
                target = _targets.FindBest(transform.position, weapon.Reach, weapon.Targets);
                ranged = true;
            }

            Target = target;
            if (target == null || (ranged && _reloadRemaining > 0f))
            {
                return false;
            }

            Vector3 toTarget = target.Position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Facing = toTarget.normalized;
            }

            target.TakeDamage(weapon.Damage * _damageMultiplier);
            Struck?.Invoke(target.Position, weapon);
            _strikeCooldown = Cadence.Rearm(_strikeCooldown, ranged ? RangedInterval : weapon.AttackInterval);
            if (ranged)
            {
                CountShot(weapon);
            }

            return true;
        }

        private void CountShot(HeroWeaponDefinition weapon)
        {
            if (weapon.Magazine <= 0)
            {
                return;
            }

            _shotsFired++;
            if (_shotsFired >= weapon.Magazine)
            {
                _shotsFired = 0;
                _reloadRemaining = ReloadSeconds;
                Reloading?.Invoke(_reloadRemaining);
            }
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
