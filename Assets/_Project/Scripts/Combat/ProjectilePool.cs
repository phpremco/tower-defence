using Game.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Combat
{
    /// <summary>Pooled projectiles and their impact flashes for one level.</summary>
    public sealed class ProjectilePool
    {
        private static readonly Color BurstColor = new Color(1f, 0.7f, 0.3f);

        private readonly TargetRegistry _registry;
        private readonly Transform _parent;
        private readonly ObjectPool<Projectile> _pool;
        private readonly ObjectPool<ImpactFlash> _flashes;

        public ProjectilePool(TargetRegistry registry, Transform parent)
        {
            _registry = registry;
            _parent = parent;
            _pool = new ObjectPool<Projectile>(Create, p => p.gameObject.SetActive(true), p => p.gameObject.SetActive(false));
            _flashes = new ObjectPool<ImpactFlash>(CreateFlash, f => f.gameObject.SetActive(true), f => f.gameObject.SetActive(false));
        }

        public void Fire(Vector3 from, ITargetable target, float damage, float speed, float splashRadius, TargetLayers mask, float slowFactor = 1f, float slowSeconds = 0f, ProjectileStyle style = ProjectileStyle.Bolt, Color color = default)
        {
            Projectile projectile = _pool.Get();
            projectile.Launch(from, target, damage, speed, splashRadius, mask, _registry, _pool.Release, slowFactor, slowSeconds, style, color == default ? BurstColor : color);
            Shot?.Invoke(new ShotInfo(from, splashRadius, mask, slowFactor < 1f, style));
        }

        /// <summary>Raised whenever something fires, with where and what kind of shot, so audio can react.</summary>
        public event System.Action<ShotInfo> Shot;

        /// <summary>Plays the impact flash alone, for explosions that are not projectiles, such as a dying enemy.</summary>
        public void Burst(Vector3 position, float radius) => _flashes.Get().Show(position, radius, BurstColor, _flashes.Release);

        private Projectile Create()
        {
            var go = new GameObject("Projectile");
            go.transform.SetParent(_parent, false);
            // Logic first: the view requires it and binds to it in its Awake.
            var projectile = go.AddComponent<Projectile>();
            go.AddComponent<ProjectileView>();
            projectile.Impacted += OnImpact;
            return projectile;
        }

        /// <summary>Raised with position, radius and style when a splash projectile lands, so audio can react.</summary>
        public event System.Action<Vector3, float, ProjectileStyle> Exploded;

        private void OnImpact(Vector3 position, float radius, Color color, ProjectileStyle style)
        {
            if (radius > 0f)
            {
                Exploded?.Invoke(position, radius, style);
            }

            _flashes.Get().Show(position, radius, color, _flashes.Release);
        }

        private ImpactFlash CreateFlash()
        {
            var go = new GameObject("ImpactFlash");
            go.transform.SetParent(_parent, false);
            return go.AddComponent<ImpactFlash>();
        }
    }
}
