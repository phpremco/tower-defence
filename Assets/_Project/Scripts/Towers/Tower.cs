using System;
using Game.Combat;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>Finds a target in range and fires projectiles. Has no knowledge of how it looks.</summary>
    public sealed class Tower : MonoBehaviour
    {
        private TowerDefinition _definition;
        private TargetRegistry _registry;
        private ProjectilePool _projectiles;
        private float _cooldown;

        public TowerDefinition Definition => _definition;

        /// <summary>Zero-based upgrade level. Stars shown are LevelIndex + 1.</summary>
        public int LevelIndex { get; private set; }

        public bool CanUpgrade => LevelIndex < _definition.MaxLevelIndex;

        public int NextUpgradeCost => _definition.GetLevel(LevelIndex + 1).Cost;

        public event Action<Tower> LevelChanged;

        /// <summary>Raised with the world position of the target whenever the tower fires.</summary>
        public event Action<Vector3> Fired;

        public void Initialize(TowerDefinition definition, TargetRegistry registry, ProjectilePool projectiles)
        {
            _definition = definition;
            _registry = registry;
            _projectiles = projectiles;
            LevelIndex = 0;
            LevelChanged?.Invoke(this);
        }

        public void Upgrade()
        {
            if (!CanUpgrade)
            {
                return;
            }

            LevelIndex++;
            LevelChanged?.Invoke(this);
        }

        private void Update()
        {
            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            TowerLevel level = _definition.GetLevel(LevelIndex);
            ITargetable target = _registry.FindBest(transform.position, level.Range, _definition.Targets);
            if (target == null)
            {
                return;
            }

            _projectiles.Fire(transform.position + Vector3.up, target, level.Damage, _definition.ProjectileSpeed, _definition.SplashRadius, _definition.Targets);
            Fired?.Invoke(target.Position);
            _cooldown = level.FireInterval;
        }
    }
}
