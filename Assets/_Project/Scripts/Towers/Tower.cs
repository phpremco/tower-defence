using System;
using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// Finds a target in range and fires projectiles, can be damaged, repaired, upgraded and sold.
    /// Has no knowledge of how it looks.
    /// </summary>
    public sealed class Tower : MonoBehaviour, IStructure, IGuardable, ISupportSource, IBridgeable
    {
        private const float SupportLevelGrowth = 0.5f;
        private const float MuzzleHeight = 1f;
        private const float MuzzleForward = 0.55f;
        private const float SupportRefreshSeconds = 0.25f;

        // A throw never wrecks a tower: it keeps at least this much health.
        private const float HealthKeptAfterToss = 1f;

        private TowerDefinition _definition;
        private TargetRegistry _registry;
        private StructureRegistry _structures;
        private ProjectilePool _projectiles;
        private PlayerModifiers _modifiers;
        private float _cooldown;
        private ITargetable _beamTarget;
        private bool _beamLive;
        private float _supportRange;
        private float _supportRate;
        private float _supportTimer;

        public TowerDefinition Definition => _definition;

        public Vector3 Position => transform.position;

        public Health Health { get; private set; }

        /// <summary>Zero-based upgrade level. Stars shown are LevelIndex + 1.</summary>
        public int LevelIndex { get; private set; }

        public bool CanUpgrade => !IsWrecked && LevelIndex < _definition.MaxLevelIndex;

        public int NextUpgradeCost => _definition.GetLevel(LevelIndex + 1).Cost;

        public bool IsWrecked => Health.IsDead;

        public bool IsAlive => !IsWrecked;

        /// <summary>The weakest the tower becomes while damaged, as a multiplier on damage and fire rate.</summary>
        public float Power => IsWrecked ? 0f : _definition.DamagedPowerFloor + (1f - _definition.DamagedPowerFloor) * Health.Fraction;

        /// <summary>Range after upgrades, in world units.</summary>
        public float Range => _definition.GetLevel(LevelIndex).Range * _modifiers.TowerRangeMultiplier * (_definition.IsSupport ? 1f : 1f + _supportRange);

        /// <summary>Value carried over from the towers this one was combined from, so selling it is fair.</summary>
        public int InheritedValue { get; set; }

        public float HealthFraction => Health.Fraction;

        public float SupportRadius => _definition.IsSupport && IsAlive ? _definition.GetLevel(LevelIndex).Range * _modifiers.TowerRangeMultiplier : 0f;

        public float RangeBonus => _definition.SupportRangeBonus * (1f + SupportLevelGrowth * LevelIndex);

        public float RateBonus => _definition.SupportRateBonus * (1f + SupportLevelGrowth * LevelIndex);

        public int TotalSpent
        {
            get
            {
                int total = InheritedValue;
                for (int i = 0; i <= LevelIndex; i++)
                {
                    total += _definition.GetLevel(i).Cost;
                }

                return total;
            }
        }

        public int SellValue => Mathf.FloorToInt(TotalSpent * (IsWrecked ? _definition.ScrapFraction : _definition.SellRefundFraction));

        /// <summary>
        /// How wide the structure is across the road, in world units. A gate spans the whole corridor; the level
        /// sets this before the tower is initialised, so the view can build the right width.
        /// </summary>
        public float Span { get; set; } = 1f;

        /// <summary>Blockades are part of the level: they cannot be sold, only repaired or rebuilt.</summary>
        public bool CanBeSold => !_definition.BlocksGroundEnemies && !_definition.IsBaseDefence;

        public bool BlocksGround => _definition.BlocksGroundEnemies && IsAlive && !IsBridged;

        /// <summary>A gate a boss stands on lets the walkers behind it follow over its top (Docs/BOSSES.md).</summary>
        public bool IsBridged => _definition.BlocksGroundEnemies && IsAlive && Burden > 0f;

        public float BridgeHeight => BridgedHeight;
        public bool IsBase => false;

        /// <summary>A wrecked blockade can be rebuilt; a wrecked tower is scrapped and built again instead.</summary>
        public bool NeedsRepair => _definition.BlocksGroundEnemies ? IsWrecked || Health.Current < Health.Max : !IsWrecked && Health.Current < Health.Max;

        public int RepairCost
        {
            get
            {
                if (!NeedsRepair)
                {
                    return 0;
                }

                return IsWrecked ? _definition.BuildCost : Mathf.CeilToInt((Health.Max - Health.Current) * _definition.RepairCostPerHealth);
            }
        }

        public event Action<Tower> LevelChanged;

        /// <summary>Raised when health changes or the tower becomes a wreck.</summary>
        public event Action<Tower> StateChanged;

        /// <summary>Raised with the world position of the target whenever the tower fires.</summary>
        public event Action<Vector3> Fired;

        /// <summary>
        /// Where a shot at a target position leaves, set by the view to the mouth of the barrel that fires it (the view may
        /// turn the head first); without it the tower guesses.
        /// </summary>
        public Func<Vector3, Vector3> MuzzleProvider { get; set; }

        /// <summary>Raised with the target's position and the seconds until the first shot when a beam locks onto a new target.</summary>
        public event Action<Vector3, float> Charging;

        /// <summary>Seconds until the tower may fire again; zero when it is ready.</summary>
        public float SecondsUntilReady => Mathf.Max(0f, _cooldown);

        /// <summary>Raised once when the tower goes from standing to wrecked.</summary>
        public event Action<Tower> Wrecked;

        private bool _wasWrecked;
        private float _upsetRemaining;
        private float _frozenRemaining;
        private float _overgrowthRate = 1f;
        private bool _drained;

        // The top of a gate's beam, where a walker following a boss over it climbs (ClimbLayer.GateHeight in the bosses).
        private const float BridgedHeight = 1.15f;

        /// <summary>True while the tower lies tilted after a boss threw it: it fires nothing until it has righted itself.</summary>
        public bool IsUpset => _upsetRemaining > 0f;

        /// <summary>Seconds until a thrown tower stands upright again.</summary>
        public float UpsetRemaining => Mathf.Max(0f, _upsetRemaining);

        /// <summary>How hard a boss standing on this structure weighs it down, from 0 (nothing) to 1 (full weight).</summary>
        public float Burden { get; private set; }

        /// <summary>Raised with the tower, the direction it was flung and the seconds it stays upset.</summary>
        public event Action<Tower, Vector3, float> Tossed;

        /// <summary>Raised when the weight on the structure changes, so the view can sag it.</summary>
        public event Action<Tower> BurdenChanged;

        /// <summary>
        /// A boss breaks out beside the tower and flings it: it loses a share of its full health (never its last hit point)
        /// and lies tilted, silent, for a while before it rights itself.
        /// </summary>
        public void Toss(Vector3 away, float healthShare, float upsetSeconds)
        {
            if (IsWrecked)
            {
                return;
            }

            float loss = Mathf.Min(Health.Max * healthShare, Health.Current - HealthKeptAfterToss);
            if (loss > 0f)
            {
                Health.TakeDamage(loss);
            }

            _upsetRemaining = Mathf.Max(_upsetRemaining, upsetSeconds);
            _beamTarget = null;
            _beamLive = false;
            Tossed?.Invoke(this, away, upsetSeconds);
        }

        /// <summary>True while a boss's breath holds the structure in an ice block: it stands and blocks, but fires nothing and takes no hits.</summary>
        public bool IsFrozen => _frozenRemaining > 0f && !IsWrecked;

        /// <summary>Seconds the ice block still holds.</summary>
        public float FrozenRemaining => IsFrozen ? _frozenRemaining : 0f;

        /// <summary>Raised when the structure freezes into an ice block (true) or thaws out of it (false).</summary>
        public event Action<Tower, bool> FrozenChanged;

        /// <summary>Raised when a boss smashes or shatters the structure into a wreck, with where the blow came from.</summary>
        public event Action<Tower, Vector3> Smashed;

        /// <summary>A boss's breath turns the structure into an ice block for a while (it keeps standing).</summary>
        public void Freeze(float seconds)
        {
            if (IsWrecked || seconds <= 0f)
            {
                return;
            }

            bool fresh = !IsFrozen;
            _frozenRemaining = Mathf.Max(_frozenRemaining, seconds);
            _beamTarget = null;
            _beamLive = false;
            if (fresh)
            {
                FrozenChanged?.Invoke(this, true);
            }
        }

        /// <summary>Breaks the ice early (the hero's repair): the structure is itself again, nothing lost.</summary>
        public void Thaw()
        {
            if (!IsFrozen)
            {
                return;
            }

            _frozenRemaining = 0f;
            FrozenChanged?.Invoke(this, false);
        }

        /// <summary>A boss's blow turns the structure into a wreck at once, whatever its health and guard (a rebuild brings it back).</summary>
        public void Smash(Vector3 from)
        {
            if (IsWrecked)
            {
                return;
            }

            bool wasFrozen = IsFrozen;
            _frozenRemaining = 0f;
            _upsetRemaining = 0f;
            if (wasFrozen)
            {
                FrozenChanged?.Invoke(this, false);
            }

            Smashed?.Invoke(this, from);
            Health.TakeDamage(Health.Current);
        }

        /// <summary>True while a boss's vines grow over the tower: it fires slower until splash or the hero burns them off.</summary>
        public bool IsOvergrown => _overgrowthRate < 1f;

        /// <summary>Raised when vines overgrow the tower (true) or are burned off it (false).</summary>
        public event Action<Tower, bool> OvergrowthChanged;

        /// <summary>A boss's spores overgrow the tower: it fires at a share of its rate until they are cleared.</summary>
        public void Overgrow(float fireRate)
        {
            bool fresh = !IsOvergrown;
            _overgrowthRate = Mathf.Clamp(fireRate, 0.05f, 1f);
            if (fresh && IsOvergrown)
            {
                OvergrowthChanged?.Invoke(this, true);
            }
        }

        /// <summary>Burns the vines off (a blast on the pad, the hero's repair): the tower fires at its full rate again.</summary>
        public void ClearOvergrowth()
        {
            if (!IsOvergrown)
            {
                return;
            }

            _overgrowthRate = 1f;
            OvergrowthChanged?.Invoke(this, false);
        }

        /// <summary>True while a boss drains the structure's gun: it holds no fire but still blocks.</summary>
        public bool IsDrained => _drained;

        /// <summary>Raised when a boss drains the gun (true) or the drain ends (false).</summary>
        public event Action<Tower, bool> DrainedChanged;

        /// <summary>A boss drains the gun (it still blocks the road) or gives it back.</summary>
        public void SetDrained(bool drained)
        {
            if (drained == _drained)
            {
                return;
            }

            _drained = drained;
            _beamTarget = null;
            _beamLive = false;
            DrainedChanged?.Invoke(this, drained);
        }

        /// <summary>Sets the weight of a boss standing on the structure (0 when it has left).</summary>
        public void SetBurden(float burden)
        {
            burden = Mathf.Clamp01(burden);
            if (!Mathf.Approximately(burden, Burden))
            {
                Burden = burden;
                BurdenChanged?.Invoke(this);
            }
        }

        public void Initialize(TowerDefinition definition, TargetRegistry registry, ProjectilePool projectiles, PlayerModifiers modifiers)
        {
            _definition = definition;
            _registry = registry;
            _projectiles = projectiles;
            _modifiers = modifiers;
            LevelIndex = 0;
            Health = new Health((definition.MaxHealth + definition.GetLevel(0).HealthBonus) * HealthMultiplier);
            Health.Changed += OnHealthChanged;
            LevelChanged?.Invoke(this);
        }

        /// <summary>Makes enemies able to attack this tower. Preview ghosts are never registered.</summary>
        public void Register(StructureRegistry structures)
        {
            _structures = structures;
            _structures.Add(this);
        }

        public void Retire()
        {
            _structures?.Remove(this);
            Health.Changed -= OnHealthChanged;
        }

        /// <summary>The hero standing here, if any. It takes a share of every hit.</summary>
        public IDamageGuard Guard { get; set; }

        public void TakeDamage(float amount)
        {
            // An ice block shrugs off blows; only the boss that froze it breaks it.
            if (IsFrozen)
            {
                return;
            }

            Health.TakeDamage(Guard != null ? Guard.Absorb(amount) : amount);
        }

        // A wreck is rebuilt, not patched up, so the hero's repair has nothing to do on one.
        public bool CanRestore => !IsWrecked && (Health.Current < Health.Max || IsFrozen || IsOvergrown);

        // The hero's repair also thaws a frozen gate (Docs/BOSSES.md, Frost Warden), so it is not lost.
        public void Restore(float fraction)
        {
            if (!IsWrecked)
            {
                Thaw();
                ClearOvergrowth();
                Health.Heal(Health.Max * fraction);
            }
        }

        public void RepairFully() => Health.Heal(Health.Max);

        public void Repair(float amount)
        {
            if (!IsWrecked)
            {
                Health.Heal(amount);
            }
        }

        public void Upgrade()
        {
            if (!CanUpgrade)
            {
                return;
            }

            LevelIndex++;
            Health.Extend(_definition.GetLevel(LevelIndex).HealthBonus * HealthMultiplier);
            // A refit is a rebuild: the crew patches every dent while the new parts go in.
            Health.Heal(Health.Max);
            LevelChanged?.Invoke(this);
        }

        /// <summary>Starts a freshly built tower at an inherited level, with every level's health bonus applied.</summary>
        public void SetLevel(int levelIndex)
        {
            while (LevelIndex < levelIndex && CanUpgrade)
            {
                Upgrade();
            }
        }

        private float HealthMultiplier => _definition.BlocksGroundEnemies ? _modifiers.BlockadeHealthMultiplier : _modifiers.TowerHealthMultiplier;

        private void Update()
        {
            RefreshSupport();
            if (IsWrecked)
            {
                _beamTarget = null;
                _beamLive = false;
                return;
            }

            if (_upsetRemaining > 0f)
            {
                _upsetRemaining -= Time.deltaTime;
                _beamTarget = null;
                _beamLive = false;
                return;
            }

            if (_frozenRemaining > 0f)
            {
                _frozenRemaining -= Time.deltaTime;
                _beamTarget = null;
                _beamLive = false;
                if (_frozenRemaining <= 0f)
                {
                    FrozenChanged?.Invoke(this, false);
                }

                return;
            }

            if (_drained)
            {
                _beamTarget = null;
                _beamLive = false;
                return;
            }

            ReleaseBeam();
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            TowerLevel level = _definition.GetLevel(LevelIndex);
            if (level.Damage <= 0f)
            {
                // A blockade without a gun yet only holds the line.
                return;
            }

            // A long frame (high game speed, low frame rate) may owe more than one shot; see Cadence.
            for (int shots = 0; Cadence.Due(_cooldown, shots); shots++)
            {
                ITargetable target = _beamTarget ?? FindTarget(level);
                if (target == null)
                {
                    _beamTarget = null;
                    _beamLive = false;
                    break;
                }

                // A beam is continuous: an idle beam charges before it burns, a live one swings straight on to the next
                // creature. Charging on every new target (until 2026-11-05) left the laser at a fifth of its damage in a
                // swarm, where the creature furthest along changes all the time.
                if (_definition.IsBeam && !ReferenceEquals(target, _beamTarget))
                {
                    _beamTarget = target;
                    if (!_beamLive)
                    {
                        _cooldown = _definition.BeamLockSeconds;
                        Charging?.Invoke(target.Position, _definition.BeamLockSeconds);
                        return;
                    }
                }

                Shoot(level, target);
                _beamLive = _definition.IsBeam;
                _cooldown = Cadence.Rearm(_cooldown, level.FireInterval / (_modifiers.TowerFireRateMultiplier * (1f + _supportRate) * Mathf.Max(0.05f, Power) * _overgrowthRate));
            }

            _cooldown = Cadence.Idle(_cooldown);
        }

        private ITargetable FindTarget(TowerLevel level) => _registry.FindBest(transform.position, Range, _definition.Targets);

        // A beam holds its creature until it dies or walks out of reach. A kill frees it at once: the beam sweeps on to the
        // next creature within its sweep time instead of finishing the pulse, which is what makes it the swarm gun.
        private void ReleaseBeam()
        {
            if (_beamTarget == null)
            {
                return;
            }

            bool killed = !_beamTarget.IsAlive;
            if (!killed && FlatDistanceSqr(transform.position, _beamTarget.Position) <= Range * Range)
            {
                return;
            }

            if (killed && _beamLive && _definition.BeamSweepSeconds > 0f)
            {
                _cooldown = Mathf.Min(_cooldown, _definition.BeamSweepSeconds);
            }

            _beamTarget = null;
        }

        private static float FlatDistanceSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private void Shoot(TowerLevel level, ITargetable target)
        {
            // The layer share follows the aimed creature (a gun that also aims up hits the sky softer).
            float damage = level.Damage * _modifiers.TowerDamageMultiplier * Power * _definition.DamageFactorAgainst(target.Layer);
            // The shot leaves from the mouth of the barrel: a little above the base and towards the target.
            Vector3 towards = target.Position - transform.position;
            towards.y = 0f;
            Vector3 muzzle = MuzzleProvider != null
                ? MuzzleProvider(target.Position)
                : transform.position + Vector3.up * MuzzleHeight + (towards.sqrMagnitude > 0.0001f ? towards.normalized * MuzzleForward : Vector3.zero);
            // A chilling tower grows its slow with its stars instead of its damage.
            float slowFactor = level.SlowFactor > 0f ? level.SlowFactor : _definition.SlowFactor;
            float slowSeconds = level.SlowSeconds > 0f ? level.SlowSeconds : _definition.SlowSeconds;
            _projectiles.Fire(muzzle, target, damage, _definition.ProjectileSpeed, _definition.SplashRadius, _definition.Targets, slowFactor, slowSeconds, _definition.ProjectileStyle, _definition.Color, _definition.FuseRadius, this, _definition.RetargetRadius, _definition.Homing, _definition.SplashTargets);
            Fired?.Invoke(target.Position);
        }

        // Support towers nearby boost range and fire rate. Checked a few times per second, not every frame.
        private void RefreshSupport()
        {
            _supportTimer -= Time.deltaTime;
            if (_supportTimer > 0f || _structures == null)
            {
                return;
            }

            _supportTimer = SupportRefreshSeconds;
            _structures.SupportAt(transform.position, out _supportRange, out _supportRate);
        }

        private void OnHealthChanged(float _)
        {
            if (IsWrecked && !_wasWrecked)
            {
                _wasWrecked = true;
                Wrecked?.Invoke(this);
            }
            else if (!IsWrecked)
            {
                _wasWrecked = false;
            }

            StateChanged?.Invoke(this);
        }
    }
}
