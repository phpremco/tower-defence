using Game.Combat;
using Game.Enemies;
using Game.Hero;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Turns heavy moments (big blasts, the base taking a hit, a boss falling) into a short kick of the camera, never a
    /// lasting shake: kicks die out within half a second, blasts from a busy battery kick at most once per interval,
    /// and a besieged base only now and then. How hard a blast kicks is part of its impact look (most hits do not).
    /// View only.
    /// </summary>
    public sealed class CameraShaker : MonoBehaviour
    {
        private const float BaseHit = 0.7f;
        private const float BaseHitMinInterval = 4f;
        private const float BlastMinInterval = 1.2f;
        private const float BigBlast = 0.6f;
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
        private float _nextBlastShake;

        public void Initialize(FixedCameraRig rig, LevelContext context)
        {
            _rig = rig;
            _context = context;
            _lastBaseHealth = context.BaseHealth.Current;
            context.Projectiles.Exploded += OnExploded;
            context.Projectiles.Burst += OnBurst;
            context.BaseHealth.Changed += OnBaseChanged;
            context.BaseHealth.Died += OnBaseDied;
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
        private void OnExploded(Vector3 position, float radius, ProjectileStyle style) => Blast(_context.Projectiles.Vfx.For(style).ShakeFor(radius));

        // A boss going down kicks through its own chain-blast look, like every other burst.
        private void OnBurst(Vector3 position, float radius, ImpactVfxDefinition vfx) => Blast(vfx.ShakeFor(radius));

        private void Blast(float strength)
        {
            // A big one (a boss going down) always kicks; everyday blasts wait their turn.
            if (strength <= 0f || (strength < BigBlast && Time.unscaledTime < _nextBlastShake))
            {
                return;
            }

            _rig.Shake(strength);
            _nextBlastShake = Time.unscaledTime + BlastMinInterval;
        }

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

        private void OnAbilityUsed(int slot, HeroAbilityDefinition ability, Vector3 position)
        {
            if (ability.Kind == HeroAbilityKind.Shockwave)
            {
                _rig.Shake(Shockwave);
            }
        }
    }
}
