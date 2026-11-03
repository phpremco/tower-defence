using Game.Combat;
using Game.Enemies;
using Game.Hero;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Turns heavy moments (explosions, the base taking a hit, a boss falling) into a kick of the camera. View only.</summary>
    public sealed class CameraShaker : MonoBehaviour
    {
        private const float BaseHit = 0.7f;
        private const float BaseHitMinInterval = 1.5f;
        private const float BossDeath = 0.8f;
        private const float Shockwave = 0.45f;
        private const float StructureWreck = 0.65f;
        private const float HeroDown = 0.4f;
        private const float BaseDestroyed = 1f;
        private const float BossWaveQuake = 0.5f;
        private const float BossGroundEntrance = 0.9f;
        private const float BossEntrance = 0.4f;

        private FixedCameraRig _rig;
        private LevelContext _context;
        private float _lastBaseHealth;
        private float _nextBaseShake;

        public void Initialize(FixedCameraRig rig, LevelContext context)
        {
            _rig = rig;
            _context = context;
            _lastBaseHealth = context.BaseHealth.Current;
            context.Projectiles.Exploded += OnExploded;
            context.Projectiles.Burst += OnBurst;
            context.BaseHealth.Changed += OnBaseChanged;
            context.BaseHealth.Died += OnBaseDied;
            context.EnemyKilled.Subscribe(OnEnemyKilled);
            context.StructureWrecked.Subscribe(OnStructureWrecked);
            context.BossWaveStarted.Subscribe(OnBossWave);
            context.EnemySpawned.Subscribe(OnEnemySpawned);
            if (context.Hero != null)
            {
                context.Hero.AbilityUsed += OnAbilityUsed;
                context.Hero.Downed += OnHeroDown;
            }
        }

        private void OnBossWave() => _rig.Shake(BossWaveQuake);

        // A boss breaking out of the ground shakes harder than one stepping through the gate.
        private void OnEnemySpawned(Enemy enemy)
        {
            if (enemy.Definition.IsBoss)
            {
                _rig.Shake(enemy.Origin == EnemyOrigin.Ground ? BossGroundEntrance : BossEntrance);
            }
        }

        private void OnHeroDown() => _rig.Shake(HeroDown);

        private void OnBaseDied() => _rig.Shake(BaseDestroyed);

        private void OnStructureWrecked(Vector3 position) => _rig.Shake(StructureWreck);

        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _context.Projectiles.Exploded -= OnExploded;
            _context.Projectiles.Burst -= OnBurst;
            _context.BaseHealth.Changed -= OnBaseChanged;
            _context.BaseHealth.Died -= OnBaseDied;
            _context.EnemyKilled.Unsubscribe(OnEnemyKilled);
            _context.StructureWrecked.Unsubscribe(OnStructureWrecked);
            _context.BossWaveStarted.Unsubscribe(OnBossWave);
            _context.EnemySpawned.Unsubscribe(OnEnemySpawned);
            if (_context.Hero != null)
            {
                _context.Hero.AbilityUsed -= OnAbilityUsed;
                _context.Hero.Downed -= OnHeroDown;
            }
        }

        // How hard a blast kicks is part of its look, so it comes from the impact definition.
        private void OnExploded(Vector3 position, float radius, ProjectileStyle style) => _rig.Shake(_context.Projectiles.Vfx.For(style).ShakeFor(radius));

        private void OnBurst(Vector3 position, float radius, ImpactVfxDefinition vfx) => _rig.Shake(vfx.ShakeFor(radius));

        private void OnBaseChanged(float current)
        {
            // A besieged base is hit every second by every attacker; a kick every so often is enough.
            if (current < _lastBaseHealth && Time.unscaledTime >= _nextBaseShake)
            {
                _rig.Shake(BaseHit);
                _nextBaseShake = Time.unscaledTime + BaseHitMinInterval;
            }

            _lastBaseHealth = current;
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            if (enemy.Definition.IsBoss)
            {
                _rig.Shake(BossDeath);
            }
        }

        private void OnAbilityUsed(int slot, HeroAbilityDefinition ability, Vector3 position)
        {
            if (ability.Kind == HeroAbilityKind.Shockwave)
            {
                _rig.Shake(Shockwave);
            }
        }
    }
}
