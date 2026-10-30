using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Enemies
{
    /// <summary>Hands out pooled death effects, so a wave dying at once does not allocate a thing.</summary>
    public sealed class EnemyDeathEffectPool
    {
        private readonly Transform _parent;
        private readonly ObjectPool<EnemyDeathEffect> _pool;

        public EnemyDeathEffectPool(Transform parent)
        {
            _parent = parent;
            _pool = new ObjectPool<EnemyDeathEffect>(Create, e => e.gameObject.SetActive(true), e => e.gameObject.SetActive(false));
        }

        public void Play(IReadOnlyList<EnemyDeathEffect.Part> parts, EnemyDeathEffect.Request request)
        {
            _pool.Get().Play(parts, request, _pool.Release);
        }

        private EnemyDeathEffect Create()
        {
            var go = new GameObject("EnemyDeath");
            go.transform.SetParent(_parent, false);
            return go.AddComponent<EnemyDeathEffect>();
        }
    }
}
