using Game.Enemies;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Turns heavy moments (explosions, the base taking a hit, a boss falling) into a kick of the camera. View only.</summary>
    public sealed class CameraShaker : MonoBehaviour
    {
        private const float ExplosionBase = 0.12f;
        private const float ExplosionPerRadius = 0.14f;
        private const float BaseHit = 0.7f;
        private const float BossDeath = 0.8f;
        private const float Shockwave = 0.45f;
        private const float StructureWreck = 0.65f;
        private const float HeroDown = 0.4f;
        private const float BaseDestroyed = 1f;
        private const float BossWaveQuake = 0.5f;
        private const float BossPulse = 0.6f;
        private const float BossGroundEntrance = 0.9f;
        private const float BossEntrance = 0.4f;

        private FixedCameraRig _rig;
        private LevelContext _context;
        private float _lastBaseHealth;

        public void Initialize(FixedCameraRig rig, LevelContext context)
        {
            _rig = rig;
            _context = context;
            _lastBaseHealth = context.BaseHealth.Current;
            context.Projectiles.Exploded += OnExploded;
            context.BaseHealth.Changed += OnBaseChanged;
            context.BaseHealth.Died += OnBaseDied;
            context.EnemyKilled.Subscribe(OnEnemyKilled);
            context.StructureWrecked.Subscribe(OnStructureWrecked);
            context.BossWaveStarted.Subscribe(OnBossWave);
            context.BossPulsed.Subscribe(OnBossPulsed);
            context.EnemySpawned.Subscribe(OnEnemySpawned);
            if (context.Hero != null)
            {
                context.Hero.ShockwaveReleased += OnShockwave;
                context.Hero.Downed += OnHeroDown;
            }
        }

        private void OnBossWave() => _rig.Shake(BossWaveQuake);

        private void OnBossPulsed(Vector3 centre) => _rig.Shake(BossPulse);

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
            _context.BaseHealth.Changed -= OnBaseChanged;
            _context.BaseHealth.Died -= OnBaseDied;
            _context.EnemyKilled.Unsubscribe(OnEnemyKilled);
            _context.StructureWrecked.Unsubscribe(OnStructureWrecked);
            _context.BossWaveStarted.Unsubscribe(OnBossWave);
            _context.BossPulsed.Unsubscribe(OnBossPulsed);
            _context.EnemySpawned.Unsubscribe(OnEnemySpawned);
            if (_context.Hero != null)
            {
                _context.Hero.ShockwaveReleased -= OnShockwave;
                _context.Hero.Downed -= OnHeroDown;
            }
        }

        private void OnExploded(Vector3 position, float radius) => _rig.Shake(ExplosionBase + ExplosionPerRadius * radius);

        private void OnBaseChanged(float current)
        {
            if (current < _lastBaseHealth)
            {
                _rig.Shake(BaseHit);
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

        private void OnShockwave(Vector3 centre, float radius) => _rig.Shake(Shockwave);
    }
}
