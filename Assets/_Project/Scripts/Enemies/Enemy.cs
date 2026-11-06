using System;
using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Walks or flies along a list of waypoints and can be targeted. Rollers alternate walking with faster rolling,
    /// flyers may stop to strike the gates they pass, trail layers drop a patch every so often, and anything can be
    /// slowed or hurried for a while. Has no knowledge of how it looks.
    /// </summary>
    public sealed class Enemy : MonoBehaviour, ITargetable, ISourcedTarget
    {
        private const float ArrivalDistance = 0.02f;
        private const float MinSpeedFactor = 0.1f;
        private const float MaxSpeedFactor = 3f;

        // One cell is kept free in front of a blockade for the hero, so enemies halt just before that cell.
        private const float BlockadeStopDistance = 1.4f;

        // Distance between two patches of a trail, so a slow crawler still leaves an unbroken smear.
        private const float TrailSpacing = 0.55f;

        private EnemyDefinition _definition;
        private Vector3[] _waypoints;
        private int _nextWaypoint;
        private float _travelled;
        private float _heightOffset;
        private TargetRegistry _registry;
        private StructureRegistry _structures;
        private float _attackCooldown;
        private bool _active;
        private float _slowFactor = 1f;
        private float _slowRemaining;
        private float _hasteFactor = 1f;
        private float _hasteRemaining;
        private float _speedMultiplier = 1f;
        private float _structureDamageMultiplier = 1f;
        private bool _rolling;
        private float _rollTimer;
        private int _gateStrikesLeft;
        private float _sinceTrailDrop;
        private bool _directed;
        private float _concealedShare = -1f;
        private float _pace = 1f;
        private float _damageScale = 1f;
        private float _bridgeLift;

        // A walker passing a gate a boss stands on climbs over it within this distance of the gate's middle.
        private const float BridgeReach = 1.6f;

        public EnemyDefinition Definition => _definition;

        /// <summary>How fast this individual walks compared to its kind, so a group does not march in step.</summary>
        public float SpeedMultiplier => _speedMultiplier;

        public Health Health { get; private set; }

        public int Reward { get; private set; }

        public Vector3 Position => transform.position;

        /// <summary>Direction of travel on the ground plane, for views that turn towards it.</summary>
        public Vector3 Heading { get; private set; } = Vector3.forward;

        /// <summary>
        /// How much of its own width a body that lies on the ground keeps across its heading (1: all of it), so a road
        /// narrower than it fits it between the edge stones. Set by the spawner from the road; views read it.
        /// </summary>
        public float BodyFit { get; private set; } = 1f;

        /// <summary>Narrows a ground body to a share of its width (clamped to 0.1..1); the next Initialize resets it.</summary>
        public void FitBody(float share) => BodyFit = Mathf.Clamp(share, 0.1f, 1f);

        public TargetLayers Layer => _definition.Layer;

        /// <summary>Distance covered along the road, counted from the road's start even for enemies that joined it halfway.</summary>
        public float PathProgress => _travelled;

        public bool IsAlive => _active && Health != null && !Health.IsDead;

        /// <summary>Where this enemy came up, so the view can play the right entrance.</summary>
        public EnemyOrigin Origin { get; private set; }

        /// <summary>True while a roller is curled up and rolling.</summary>
        public bool IsRolling => _rolling;

        /// <summary>True while a haste (a trail or a pack's rage) is pushing this creature on.</summary>
        public bool IsHasted => _hasteRemaining > 0f;

        /// <summary>True while a chilling hit still holds the creature back.</summary>
        public bool IsSlowed => _slowRemaining > 0f;

        /// <summary>Raised after Initialize, so a view can rebind to the new Health.</summary>
        public event Action<Enemy> Initialized;

        public event Action<Enemy> Killed;

        public event Action<Enemy> ReachedEnd;

        /// <summary>Raised when the enemy is taken off the field, killed or not; every view clears what it hung beside it.</summary>
        public event Action<Enemy> Deactivated;

        /// <summary>Raised each time this enemy hits a structure, with the place it hit, so a view can play the attack.</summary>
        public event Action<Vector3> Attacked;

        /// <summary>Raised when a roller curls up (true) or unrolls (false).</summary>
        public event Action<bool> RollChanged;

        /// <summary>Raised when a slow takes hold on a creature that was moving freely, so audio can play the chill once.</summary>
        public event Action Slowed;

        /// <summary>Raised with the ground position where a trail layer left a fresh patch.</summary>
        public event Action<Enemy, Vector3> TrailDropped;

        /// <summary>Raised with the creature, what hit it (a tower, or null) and the damage it took.</summary>
        public event Action<Enemy, object, float> DamagedBy;

        /// <summary>Raised with the damage of anything that reached it while it was hidden (a mine going off over a burrow).</summary>
        public event Action<Enemy, float> StruckWhileHidden;

        /// <summary>Raised every step a living gate holds a walker up, with that gate.</summary>
        public event Action<Enemy, IStructure> HeldByGate;

        /// <summary>True while a boss mechanic moves the creature itself: its own walk, siege and attacks wait.</summary>
        public bool IsDirected => _directed;

        /// <summary>True while it is hidden under the ground or a liquid: towers cannot aim at it.</summary>
        public bool IsConcealed => _concealedShare >= 0f;

        /// <summary>World units per second it walks right now, before slows and hastes.</summary>
        public float GroundSpeed => _definition.Speed * _speedMultiplier * _pace;

        public void Initialize(
            EnemyDefinition definition,
            Vector3[] waypoints,
            float healthMultiplier,
            float rewardMultiplier,
            float heightOffset,
            TargetRegistry registry,
            StructureRegistry structures,
            float speedMultiplier = 1f,
            EnemyOrigin origin = EnemyOrigin.Gate,
            float progressOffset = 0f,
            float structureDamageMultiplier = 1f)
        {
            _definition = definition;
            _speedMultiplier = speedMultiplier;
            _structureDamageMultiplier = structureDamageMultiplier;
            _waypoints = waypoints;
            // Faces along its road from the first frame, so a creature that stands still (the menu guardian) does not
            // keep the default heading and show the player its back.
            if (waypoints.Length > 1)
            {
                Vector3 first = waypoints[1] - waypoints[0];
                first.y = 0f;
                if (first.sqrMagnitude > 0.000001f)
                {
                    Heading = first.normalized;
                }
            }

            _nextWaypoint = 1;
            _travelled = progressOffset;
            _heightOffset = heightOffset;
            _registry = registry;
            _structures = structures;
            _attackCooldown = 0f;
            _slowFactor = 1f;
            _slowRemaining = 0f;
            _hasteFactor = 1f;
            _hasteRemaining = 0f;
            _rolling = false;
            _rollTimer = definition.WalkSeconds;
            _gateStrikesLeft = definition.Flies ? definition.GateStrikes : 0;
            _sinceTrailDrop = 0f;
            _directed = false;
            _concealedShare = -1f;
            _pace = 1f;
            _damageScale = 1f;
            _bridgeLift = 0f;
            SourceScale = null;
            BodyFit = 1f;
            Origin = origin;
            Reward = Mathf.RoundToInt(definition.Reward * rewardMultiplier);
            Health = new Health(definition.MaxHealth * healthMultiplier, definition.Armor * healthMultiplier, definition.ArmorReduction, definition.Plating, definition.PlatingFloor);
            Health.Died += OnDied;
            transform.position = waypoints[0] + Vector3.up * heightOffset;
            Heading = waypoints.Length > 1 ? Flat(waypoints[1] - waypoints[0]) : Vector3.forward;
            _active = true;
            _registry.Add(this);
            Initialized?.Invoke(this);
        }

        // Anything without a source (a mine, the hero, a pulse) counts as a blast: it still reaches a hidden boss in part.
        public void TakeDamage(float amount) => TakeDamageFrom(amount, null, true);

        public void TakeDamageFrom(float amount, object source, bool splash, bool continuous = false)
        {
            if (Health == null || !_active)
            {
                return;
            }

            if (IsConcealed)
            {
                // An aimed shot cannot find it; a blast that lands on the spot reaches it at a share.
                if (!splash)
                {
                    return;
                }

                StruckWhileHidden?.Invoke(this, amount);
                amount *= _concealedShare;
            }

            amount *= _damageScale;
            if (SourceScale != null)
            {
                amount *= SourceScale(source);
            }

            if (amount <= 0f)
            {
                return;
            }

            Health.TakeDamage(amount, continuous);
            DamagedBy?.Invoke(this, source, amount);
        }

        /// <summary>Hands the creature to a boss mechanic (true) or back to its own walk (false).</summary>
        public void SetDirected(bool directed) => _directed = directed;

        /// <summary>
        /// Hides it from aiming (under the ground or a liquid), with the share of a blast that still reaches it; zero means
        /// nothing gets through. Shows it again when <paramref name="concealed"/> is false.
        /// </summary>
        public void SetConcealed(bool concealed, float blastShare = 0f)
        {
            _concealedShare = concealed ? Mathf.Clamp01(blastShare) : -1f;
            _registry?.SetConcealed(this, concealed);
        }

        /// <summary>A speed factor of its own on top of its kind's (an enraged boss).</summary>
        public void SetPace(float pace) => _pace = Mathf.Max(MinSpeedFactor, pace);

        /// <summary>The share of every hit that lands (ice armour soaking it, a cracked armour letting double through).</summary>
        public float DamageScale => _damageScale;

        public void SetDamageScale(float scale) => _damageScale = Mathf.Max(0f, scale);

        /// <summary>
        /// A share of the hits from one source on top of <see cref="DamageScale"/> (a boss that has learned a weapon takes
        /// half from it), or null for every source alike. Cleared whenever the creature is sent out again.
        /// </summary>
        public Func<object, float> SourceScale { get; set; }

        /// <summary>Puts a directed creature at a point (its height included) facing a direction on the ground.</summary>
        public void Place(Vector3 position, Vector3 heading)
        {
            transform.position = position;
            Heading = Flat(heading);
        }

        /// <summary>Gives the creature a new way to walk from where it is, counted from a road progress, and lets it walk.</summary>
        public void Reroute(Vector3[] route, float progress)
        {
            _waypoints = route;
            _nextWaypoint = route.Length > 1 ? 1 : route.Length;
            _travelled = progress;
            _directed = false;
            if (route.Length > 1)
            {
                Heading = Flat(route[1] - route[0]);
            }
        }


        public void ApplySlow(float speedFactor, float seconds)
        {
            if (!_active || seconds <= 0f)
            {
                return;
            }

            bool fresh = _slowRemaining <= 0f;
            // The strongest slow wins, and a weaker one never shortens a stronger one that is still running.
            if (_slowRemaining <= 0f || speedFactor <= _slowFactor)
            {
                _slowFactor = Mathf.Clamp(speedFactor, MinSpeedFactor, 1f);
            }

            _slowRemaining = Mathf.Max(_slowRemaining, seconds);
            if (fresh)
            {
                Slowed?.Invoke();
            }
        }

        /// <summary>Speeds the creature up for a while; the strongest haste wins, like slows. Slows still apply on top.</summary>
        public void ApplyHaste(float speedFactor, float seconds)
        {
            if (!_active || seconds <= 0f)
            {
                return;
            }

            if (_hasteRemaining <= 0f || speedFactor >= _hasteFactor)
            {
                _hasteFactor = Mathf.Clamp(speedFactor, 1f, MaxSpeedFactor);
            }

            _hasteRemaining = Mathf.Max(_hasteRemaining, seconds);
        }

        /// <summary>Called by whoever pools this enemy when it is released.</summary>
        public void Deactivate()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _concealedShare = -1f;
            _directed = false;
            _registry.Remove(this);
            Health.Died -= OnDied;
            Deactivated?.Invoke(this);
        }

        /// <summary>The way still ahead: from where the enemy stands now to the end of its route.</summary>
        public Vector3[] RemainingRoute()
        {
            int left = Mathf.Max(0, _waypoints.Length - _nextWaypoint);
            var route = new Vector3[left + 1];
            route[0] = new Vector3(transform.position.x, 0f, transform.position.z);
            for (int i = 0; i < left; i++)
            {
                route[i + 1] = _waypoints[_nextWaypoint + i];
            }

            return route;
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>Advances the enemy by a time step. Update calls it with the frame time; tests call it directly.</summary>
        public void Step(float deltaTime)
        {
            if (!_active || _directed)
            {
                return;
            }

            if (IsHeldByBlockade(deltaTime))
            {
                // Nothing rolls through a gate: a roller unrolls to bite and starts a fresh walk afterwards.
                if (_rolling)
                {
                    SetRolling(false);
                }

                return;
            }

            if (IsStrikingGate(deltaTime))
            {
                return;
            }

            TryAttackStructure(deltaTime);
            UpdateRoll(deltaTime);
            // The climb over a bridged gate is laid on top of the walk, so the walk itself stays on the flat route.
            transform.position -= Vector3.up * _bridgeLift;

            float remaining = GroundSpeed * CurrentSpeedFactor(deltaTime) * deltaTime;
            float stepped = 0f;

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
                        Heading = Flat(toTarget);
                    }

                    remaining -= distance;
                    stepped += distance;
                    _travelled += distance;
                    _nextWaypoint++;
                    continue;
                }

                Heading = Flat(toTarget);
                transform.position += toTarget / distance * remaining;
                _travelled += remaining;
                stepped += remaining;
                remaining = 0f;
            }

            DropTrail(stepped);
            _bridgeLift = _definition.Flies ? 0f : _structures.BridgeLiftAt(transform.position, BridgeReach);
            transform.position += Vector3.up * _bridgeLift;

            if (_nextWaypoint >= _waypoints.Length && !IsBesiegingEnd(deltaTime))
            {
                ReachedEnd?.Invoke(this);
            }
        }

        private float CurrentSpeedFactor(float deltaTime)
        {
            float roll = _rolling ? _definition.RollSpeedMultiplier : 1f;
            float slow = 1f;
            if (_slowRemaining > 0f)
            {
                _slowRemaining -= deltaTime;
                slow = _slowFactor;
            }

            float haste = 1f;
            if (_hasteRemaining > 0f)
            {
                _hasteRemaining -= deltaTime;
                haste = _hasteFactor;
            }

            return roll * slow * haste;
        }

        // A trail layer leaves a patch every so many units of road behind it.
        private void DropTrail(float stepped)
        {
            if (!_definition.LeavesTrail || stepped <= 0f)
            {
                return;
            }

            _sinceTrailDrop += stepped;
            if (_sinceTrailDrop >= TrailSpacing)
            {
                _sinceTrailDrop = 0f;
                TrailDropped?.Invoke(this, transform.position);
            }
        }

        private void UpdateRoll(float deltaTime)
        {
            if (!_definition.Rolls)
            {
                return;
            }

            _rollTimer -= deltaTime;
            if (_rollTimer <= 0f)
            {
                SetRolling(!_rolling);
            }
        }

        private void SetRolling(bool rolling)
        {
            _rolling = rolling;
            _rollTimer = rolling ? _definition.RollSeconds : _definition.WalkSeconds;
            RollChanged?.Invoke(rolling);
        }

        // Walkers cannot pass a living blockade: they stop in front of it and hit it until it falls.
        private bool IsHeldByBlockade(float deltaTime)
        {
            if (_definition.Flies)
            {
                return false;
            }

            IStructure blocker = _structures.FindBlocker(transform.position, BlockadeStopDistance);
            if (blocker == null)
            {
                return false;
            }

            HeldByGate?.Invoke(this, blocker);
            if (_directed)
            {
                return true;
            }

            _attackCooldown -= deltaTime;
            if (_attackCooldown <= 0f)
            {
                blocker.TakeDamage(_definition.BlockadeDamage * _structureDamageMultiplier);
                Attacked?.Invoke(blocker.Position);
                _attackCooldown = Cadence.Idle(Cadence.Rearm(_attackCooldown, _definition.AttackInterval));
            }

            return true;
        }

        // A flyer with strikes left hovers over a gate it reaches and hits it a few times before flying on.
        private bool IsStrikingGate(float deltaTime)
        {
            if (_gateStrikesLeft <= 0)
            {
                return false;
            }

            IStructure gate = _structures.FindBlocker(transform.position, _definition.AttackRange);
            if (gate == null)
            {
                return false;
            }

            Heading = Flat(gate.Position - transform.position);
            _attackCooldown -= deltaTime;
            if (_attackCooldown <= 0f)
            {
                float damage = _definition.AttackDamage > 0f ? _definition.AttackDamage : _definition.BlockadeDamage;
                gate.TakeDamage(damage * _structureDamageMultiplier);
                Attacked?.Invoke(gate.Position);
                _attackCooldown = Cadence.Idle(Cadence.Rearm(_attackCooldown, _definition.AttackInterval));
                _gateStrikesLeft--;
            }

            return true;
        }

        // At the end of the road a living base (or a gate standing there) is besieged: the creature stays and hits it
        // until one of them falls. Only with nothing left to attack does it slip through.
        private bool IsBesiegingEnd(float deltaTime)
        {
            IStructure target = _structures.FindBlocker(transform.position, _definition.AttackRange);
            if (target == null)
            {
                return false;
            }

            Heading = Flat(target.Position - transform.position);
            _attackCooldown -= deltaTime;
            if (_attackCooldown <= 0f)
            {
                target.TakeDamage(_definition.BlockadeDamage * _structureDamageMultiplier);
                Attacked?.Invoke(target.Position);
                _attackCooldown = Cadence.Idle(Cadence.Rearm(_attackCooldown, _definition.AttackInterval));
            }

            return true;
        }

        private void TryAttackStructure(float deltaTime)
        {
            if (_definition.AttackDamage <= 0f)
            {
                return;
            }

            _attackCooldown -= deltaTime;
            if (_attackCooldown > 0f)
            {
                return;
            }

            IStructure target = _structures.FindNearest(transform.position, _definition.AttackRange);
            if (target != null)
            {
                target.TakeDamage(_definition.AttackDamage * _structureDamageMultiplier);
                Attacked?.Invoke(target.Position);
                _attackCooldown = Cadence.Idle(Cadence.Rearm(_attackCooldown, _definition.AttackInterval));
            }
        }

        private Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Heading;
        }

        private void OnDied() => Killed?.Invoke(this);
    }
}
