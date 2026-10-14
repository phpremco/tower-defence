using Game.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Combat
{
    /// <summary>Pooled projectiles for one level.</summary>
    public sealed class ProjectilePool
    {
        private readonly TargetRegistry _registry;
        private readonly Transform _parent;
        private readonly ObjectPool<Projectile> _pool;

        public ProjectilePool(TargetRegistry registry, Transform parent)
        {
            _registry = registry;
            _parent = parent;
            _pool = new ObjectPool<Projectile>(Create, p => p.gameObject.SetActive(true), p => p.gameObject.SetActive(false));
        }

        public void Fire(Vector3 from, ITargetable target, float damage, float speed, float splashRadius, TargetLayers mask)
        {
            Projectile projectile = _pool.Get();
            projectile.Launch(from, target, damage, speed, splashRadius, mask, _registry, _pool.Release);
        }

        private Projectile Create()
        {
            var go = new GameObject("Projectile");
            go.transform.SetParent(_parent, false);
            go.AddComponent<ProjectileView>();
            return go.AddComponent<Projectile>();
        }
    }
}
