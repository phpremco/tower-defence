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
    public sealed class Tower : MonoBehaviour, IStructure
    {
        private TowerDefinition _definition;
        private TargetRegistry _registry;
        private StructureRegistry _structures;
        private ProjectilePool _projectiles;
        private PlayerModifiers _modifiers;
        private float _cooldown;

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
        public float Range => _definition.GetLevel(LevelIndex).Range * _modifiers.TowerRangeMultiplier;

        public int TotalSpent
        {
            get
            {
                int total = 0;
                for (int i = 0; i <= LevelIndex; i++)
                {
                    total += _definition.GetLevel(i).Cost;
                }

                return total;
            }
        }

        public int SellValue => Mathf.FloorToInt(TotalSpent * (IsWrecked ? _definition.ScrapFraction : _definition.SellRefundFraction));

        /// <summary>Blockades are part of the level: they cannot be sold, only repaired or rebuilt.</summary>
        public bool CanBeSold => !_definition.BlocksGroundEnemies;

        public bool BlocksGround => _definition.BlocksGroundEnemies && IsAlive;

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

        public void TakeDamage(float amount) => Health.TakeDamage(amount);

        public void RepairFully() => Health.Heal(Health.Max);

        public void Upgrade()
        {
            if (!CanUpgrade)
            {
                return;
            }

            LevelIndex++;
            Health.Extend(_definition.GetLevel(LevelIndex).HealthBonus * HealthMultiplier);
            LevelChanged?.Invoke(this);
        }

        private float HealthMultiplier => _definition.BlocksGroundEnemies ? _modifiers.BlockadeHealthMultiplier : _modifiers.TowerHealthMultiplier;

        private void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f || IsWrecked)
            {
                return;
            }

            TowerLevel level = _definition.GetLevel(LevelIndex);
            if (level.Damage <= 0f)
            {
                // A blockade without a gun yet only holds the line.
                return;
            }

            ITargetable target = _registry.FindBest(transform.position, Range, _definition.Targets);
            if (target == null)
            {
                return;
            }

            float damage = level.Damage * _modifiers.TowerDamageMultiplier * Power;
            _projectiles.Fire(transform.position + Vector3.up, target, damage, _definition.ProjectileSpeed, _definition.SplashRadius, _definition.Targets);
            Fired?.Invoke(target.Position);
            _cooldown = level.FireInterval / (_modifiers.TowerFireRateMultiplier * Mathf.Max(0.05f, Power));
        }

        private void OnHealthChanged(float _) => StateChanged?.Invoke(this);
    }
}
