using Game.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Combat
{
    /// <summary>Pooled projectiles and their impact flashes for one level.</summary>
    public sealed class ProjectilePool
    {
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

        public void Fire(Vector3 from, ITargetable target, float damage, float speed, float splashRadius, TargetLayers mask, float slowFactor = 1f, float slowSeconds = 0f)
        {
            Projectile projectile = _pool.Get();
            projectile.Launch(from, target, damage, speed, splashRadius, mask, _registry, _pool.Release, slowFactor, slowSeconds);
            Shot?.Invoke(from);
        }

        /// <summary>Raised with the muzzle position whenever something fires, so audio can react.</summary>
        public event System.Action<Vector3> Shot;

        /// <summary>Plays the impact flash alone, for explosions that are not projectiles, such as a dying enemy.</summary>
        public void Burst(Vector3 position, float radius) => _flashes.Get().Show(position, radius, _flashes.Release);

        private Projectile Create()
        {
            var go = new GameObject("Projectile");
            go.transform.SetParent(_parent, false);
            go.AddComponent<ProjectileView>();
            var projectile = go.AddComponent<Projectile>();
            projectile.Impacted += OnImpact;
            return projectile;
        }

        /// <summary>Raised with position and radius when a splash projectile lands, so audio can react.</summary>
        public event System.Action<Vector3, float> Exploded;

        private void OnImpact(Vector3 position, float radius)
        {
            if (radius > 0f)
            {
                Exploded?.Invoke(position, radius);
            }

            _flashes.Get().Show(position, radius, _flashes.Release);
        }

        private ImpactFlash CreateFlash()
        {
            var go = new GameObject("ImpactFlash");
            go.transform.SetParent(_parent, false);
            return go.AddComponent<ImpactFlash>();
        }
    }
}
