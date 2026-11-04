using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Enemies
{
    /// <summary>
    /// Hands out pooled death effects, so a wave dying at once does not allocate a thing: the flying parts of a
    /// procedural creature, or a corpse of an imported model that plays its death clip (one pool per model).
    /// </summary>
    public sealed class EnemyDeathEffectPool
    {
        private readonly Transform _parent;
        private readonly ObjectPool<EnemyDeathEffect> _pool;
        private readonly Dictionary<ModelArtDefinition, ObjectPool<ModelCorpse>> _corpses = new Dictionary<ModelArtDefinition, ObjectPool<ModelCorpse>>();

        public EnemyDeathEffectPool(Transform parent)
        {
            _parent = parent;
            _pool = new ObjectPool<EnemyDeathEffect>(Create, e => e.gameObject.SetActive(true), e => e.gameObject.SetActive(false));
        }

        public void Play(IReadOnlyList<EnemyDeathEffect.Part> parts, EnemyDeathEffect.Request request)
        {
            _pool.Get().Play(parts, request, _pool.Release);
        }

        /// <summary>Plays the death clip of an imported model where its body stood.</summary>
        public void PlayModel(ModelArtDefinition art, Vector3 position, Quaternion rotation, float scale, EnemyDeathEffect.Request request)
        {
            if (!_corpses.TryGetValue(art, out ObjectPool<ModelCorpse> pool))
            {
                bool low = ModelQuality.IsLow;
                pool = new ObjectPool<ModelCorpse>(() => ModelCorpse.Create(_parent, art, scale, low), c => c.gameObject.SetActive(true), c => c.gameObject.SetActive(false));
                _corpses[art] = pool;
            }

            pool.Get().Play(position, rotation, scale, request, pool.Release);
        }

        private EnemyDeathEffect Create()
        {
            var go = new GameObject("EnemyDeath");
            go.transform.SetParent(_parent, false);
            return go.AddComponent<EnemyDeathEffect>();
        }
    }
}
