using Game.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Combat
{
    /// <summary>Pooled projectiles and their impact flashes for one level. The library says what each blast looks like.</summary>
    public sealed class ProjectilePool
    {
        private static readonly Color BurstColor = new Color(1f, 0.7f, 0.3f);

        private readonly TargetRegistry _registry;
        private readonly Transform _parent;
        private readonly VfxLibrary _vfx;
        private readonly VfxQuality _quality;
        private readonly Camera _camera;
        private readonly ObjectPool<Projectile> _pool;
        private readonly ObjectPool<ImpactFlash> _flashes;

        public ProjectilePool(TargetRegistry registry, Transform parent, VfxLibrary vfx = null, VfxQuality? quality = null, Camera camera = null)
        {
            _registry = registry;
            _parent = parent;
            _vfx = vfx != null ? vfx : VfxLibrary.CreateFallback();
            _quality = quality ?? VfxQuality.High;
            _camera = camera;
            _pool = new ObjectPool<Projectile>(Create, p => p.gameObject.SetActive(true), p => p.gameObject.SetActive(false));
            _flashes = new ObjectPool<ImpactFlash>(CreateFlash, f => f.gameObject.SetActive(true), f => f.gameObject.SetActive(false));
        }

        public VfxLibrary Vfx => _vfx;

        public VfxQuality Quality => _quality;

        public void Fire(Vector3 from, ITargetable target, float damage, float speed, float splashRadius, TargetLayers mask, float slowFactor = 1f, float slowSeconds = 0f, ProjectileStyle style = ProjectileStyle.Bolt, Color color = default, float fuseRadius = 0f, object source = null, float retargetRadius = 0f, bool homing = true, int splashTargets = 0)
        {
            Projectile projectile = _pool.Get();
            projectile.Launch(from, target, damage, speed, splashRadius, mask, _registry, _pool.Release, slowFactor, slowSeconds, style, color == default ? BurstColor : color, fuseRadius, source, retargetRadius, homing, splashTargets);
            Shot?.Invoke(new ShotInfo(from, splashRadius, mask, slowFactor < 1f, style));
        }

        /// <summary>Raised whenever something fires, with where and what kind of shot, so audio can react.</summary>
        public event System.Action<ShotInfo> Shot;

        /// <summary>Raised with position, radius and style when a splash projectile lands, so audio can react.</summary>
        public event System.Action<Vector3, float, ProjectileStyle> Exploded;

        /// <summary>Raised for every blast that is not a projectile (a death, a mine, a pulse) with the look it used.</summary>
        public event System.Action<Vector3, float, ImpactVfxDefinition> Burst;

        /// <summary>Plays a blast that is not a projectile; without a definition it looks like a dying enemy.</summary>
        public void PlayBurst(Vector3 position, float radius, ImpactVfxDefinition vfx = null, Color color = default)
        {
            ImpactVfxDefinition look = vfx != null ? vfx : _vfx.Death;
            _flashes.Get().Show(position, radius, color == default ? BurstColor : color, look, _flashes.Release);
            Burst?.Invoke(position, radius, look);
        }

        private Projectile Create()
        {
            var go = new GameObject("Projectile");
            go.transform.SetParent(_parent, false);
            // Logic first: the view requires it and binds to it in its Awake.
            var projectile = go.AddComponent<Projectile>();
            go.AddComponent<ProjectileView>().Configure(_vfx, _quality.LowTextures, _camera);
            projectile.Impacted += OnImpact;
            return projectile;
        }

        private void OnImpact(Vector3 position, float radius, Color color, ProjectileStyle style)
        {
            if (radius > 0f)
            {
                Exploded?.Invoke(position, radius, style);
            }

            _flashes.Get().Show(position, radius, color, _vfx.For(style), _flashes.Release);
        }

        private ImpactFlash CreateFlash()
        {
            var go = new GameObject("ImpactFlash");
            go.transform.SetParent(_parent, false);
            var flash = go.AddComponent<ImpactFlash>();
            flash.Configure(_quality);
            return flash;
        }
    }
}
