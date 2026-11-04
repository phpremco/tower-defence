using System;
using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// Runs the special powers of a boss: pulses that damage every structure around it, a laser that locks onto the
    /// nearest tower, gate or hero and burns it, bursts of minions, volleys of bolts at several structures, rallying
    /// calls that hurry the whole field, regrowing armour, and splitting off minions as its health falls. Timing and
    /// strength come from the ability definitions. Pure logic; the view reads the laser state and listens for the rest.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class BossAbilities : MonoBehaviour
    {
        private const float BeamHeight = 0.9f;
        private const float TargetHeight = 0.5f;
        private const float RetrySeconds = 2f;
        private const float HoldRangeFactor = 1.3f;

        private readonly List<IStructure> _volleyTargets = new List<IStructure>(8);
        private readonly List<Vector3> _volleyPoints = new List<Vector3>(8);

        private Enemy _enemy;
        private StructureRegistry _structures;
        private Action<EnemyDefinition, Enemy> _spawnMinion;
        private Func<Vector3, float, IStructure> _findTarget;
        private Action<Enemy, float, float> _rally;
        private IStructure _laserTarget;
        private BossAbilityDefinition[] _abilities = new BossAbilityDefinition[0];
        private float[] _timers = new float[0];
        private float[] _splitAt = new float[0];
        private float _damageMultiplier = 1f;
        private int _laserIndex = -1;
        private float _laserElapsed;

        /// <summary>True while the laser beam is on.</summary>
        public bool LaserActive => _laserIndex >= 0;

        public Vector3 LaserOrigin { get; private set; }

        public Vector3 LaserEnd { get; private set; }

        public Color LaserColor => _laserIndex >= 0 ? _abilities[_laserIndex].Color : Color.white;

        /// <summary>Raised with the centre, radius and colour of every pulse.</summary>
        public event Action<Vector3, float, Color> Pulsed;

        /// <summary>Raised with the boss position and colour whenever it releases minions.</summary>
        public event Action<Vector3, Color> MinionsReleased;

        /// <summary>Raised when the laser switches on (true) or off (false).</summary>
        public event Action<bool> LaserChanged;

        /// <summary>Raised with the launch point, the points hit and the colour whenever a volley flies.</summary>
        public event Action<Vector3, IReadOnlyList<Vector3>, Color> Volleyed;

        /// <summary>Raised with the boss position and colour whenever it rallies the field.</summary>
        public event Action<Vector3, Color> Rallied;

        /// <summary>Raised with the colour whenever armour grows back.</summary>
        public event Action<Color> Regrown;

        /// <param name="findTarget">The nearest thing worth a laser within a range of a point, or null.</param>
        /// <param name="rally">Hurries every living creature but the caller by a speed factor for a time.</param>
        public void Initialize(StructureRegistry structures, Action<EnemyDefinition, Enemy> spawnMinion, Func<Vector3, float, IStructure> findTarget, Action<Enemy, float, float> rally = null)
        {
            _structures = structures;
            _spawnMinion = spawnMinion;
            _findTarget = findTarget;
            _rally = rally;
            _enemy = GetComponent<Enemy>();
            _enemy.Initialized += OnInitialized;
        }

        /// <summary>
        /// Seconds until the next timed use of a power kind, or infinity when the boss has none (or only one that
        /// fires on health). Views read it to start a wind-up in time; it changes nothing.
        /// </summary>
        public float SecondsUntil(BossAbilityKind kind)
        {
            float soonest = float.PositiveInfinity;
            for (int i = 0; i < _abilities.Length; i++)
            {
                if (_abilities[i].Kind == kind && kind != BossAbilityKind.Split && _timers[i] < soonest)
                {
                    soonest = _timers[i];
                }
            }

            return soonest;
        }

        /// <summary>Scales the damage of every power, so the difficulty profile reaches the boss as well.</summary>
        public void SetDamageMultiplier(float multiplier) => _damageMultiplier = multiplier;

        private void OnDestroy()
        {
            if (_enemy != null)
            {
                _enemy.Initialized -= OnInitialized;
            }
        }

        // A pooled boss is switched off when it dies or leaves, and Update stops with it: the beam must go out here.
        private void OnDisable()
        {
            StopLaser();
        }

        // A pooled boss starts its clocks afresh each time it is sent out.
        private void OnInitialized(Enemy enemy)
        {
            _abilities = enemy.Definition.Abilities;
            if (_timers.Length != _abilities.Length)
            {
                _timers = new float[_abilities.Length];
                _splitAt = new float[_abilities.Length];
            }

            for (int i = 0; i < _abilities.Length; i++)
            {
                _timers[i] = _abilities[i].FirstDelay;
                _splitAt[i] = 1f - _abilities[i].TriggerFraction;
            }

            StopLaser();
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>Advances the powers by a time step. Update calls it with the frame time; tests call it directly.</summary>
        public void Step(float dt)
        {
            if (_enemy == null || !_enemy.IsAlive)
            {
                StopLaser();
                return;
            }

            if (_laserIndex >= 0)
            {
                BurnTarget(dt);
            }

            for (int i = 0; i < _abilities.Length; i++)
            {
                if (_abilities[i].Kind == BossAbilityKind.Split)
                {
                    TrySplit(i);
                    continue;
                }

                _timers[i] -= dt;
                if (_timers[i] > 0f)
                {
                    continue;
                }

                _timers[i] = _abilities[i].Interval;
                Use(i);
            }
        }

        private void Use(int index)
        {
            BossAbilityDefinition ability = _abilities[index];
            switch (ability.Kind)
            {
                case BossAbilityKind.Pulse:
                    _structures.DamageInRadius(_enemy.Position, ability.Radius, ability.Damage * _damageMultiplier);
                    Pulsed?.Invoke(_enemy.Position, ability.Radius, ability.Color);
                    break;
                case BossAbilityKind.Laser:
                    // Without a target in reach the boss tries again shortly instead of waiting a whole interval.
                    IStructure target = _laserIndex < 0 ? _findTarget?.Invoke(_enemy.Position, ability.Range) : null;
                    if (target == null)
                    {
                        _timers[index] = RetrySeconds;
                        break;
                    }

                    _laserIndex = index;
                    _laserTarget = target;
                    _laserElapsed = 0f;
                    LaserOrigin = _enemy.Position + Vector3.up * BeamHeight;
                    LaserEnd = target.Position + Vector3.up * TargetHeight;
                    LaserChanged?.Invoke(true);
                    break;
                case BossAbilityKind.Volley:
                    _structures.FindNearest(_enemy.Position, ability.Range, ability.TargetCount, _volleyTargets);
                    if (_volleyTargets.Count == 0)
                    {
                        _timers[index] = RetrySeconds;
                        break;
                    }

                    _volleyPoints.Clear();
                    for (int i = 0; i < _volleyTargets.Count; i++)
                    {
                        _volleyTargets[i].TakeDamage(ability.Damage * _damageMultiplier);
                        _volleyPoints.Add(_volleyTargets[i].Position);
                    }

                    Volleyed?.Invoke(_enemy.Position + Vector3.up * BeamHeight, _volleyPoints, ability.Color);
                    break;
                case BossAbilityKind.Rally:
                    _rally?.Invoke(_enemy, ability.SpeedFactor, ability.SweepSeconds);
                    Rallied?.Invoke(_enemy.Position, ability.Color);
                    break;
                case BossAbilityKind.Regrow:
                    if (_enemy.Health.ArmorFraction < 1f)
                    {
                        _enemy.Health.RestoreArmor(ability.Damage);
                        Regrown?.Invoke(ability.Color);
                    }

                    break;
                default:
                    ReleaseMinions(ability);
                    break;
            }
        }

        // Each time the health falls past the next share, a brood of minions breaks off.
        private void TrySplit(int index)
        {
            BossAbilityDefinition ability = _abilities[index];
            float fraction = _enemy.Health.Fraction;
            while (_splitAt[index] > 0f && fraction <= _splitAt[index])
            {
                _splitAt[index] -= ability.TriggerFraction;
                ReleaseMinions(ability);
            }
        }

        private void ReleaseMinions(BossAbilityDefinition ability)
        {
            for (int i = 0; i < ability.MinionCount; i++)
            {
                _spawnMinion?.Invoke(ability.Minion, _enemy);
            }

            MinionsReleased?.Invoke(_enemy.Position, ability.Color);
        }

        // The beam follows its target and burns it for the beam's duration, or until the target falls or walks out of reach.
        private void BurnTarget(float dt)
        {
            BossAbilityDefinition ability = _abilities[_laserIndex];
            _laserElapsed += dt;
            Vector3 aim = _laserTarget.Position;
            float reach = ability.Range * HoldRangeFactor;
            bool lost = !_laserTarget.IsAlive || (aim - _enemy.Position).sqrMagnitude > reach * reach;
            if (lost || _laserElapsed >= ability.SweepSeconds)
            {
                StopLaser();
                return;
            }

            LaserOrigin = _enemy.Position + Vector3.up * BeamHeight;
            LaserEnd = aim + Vector3.up * TargetHeight;
            _laserTarget.TakeDamage(ability.Damage * _damageMultiplier * dt);
        }

        private void StopLaser()
        {
            if (_laserIndex >= 0)
            {
                _laserIndex = -1;
                _laserTarget = null;
                LaserChanged?.Invoke(false);
            }
        }
    }
}
