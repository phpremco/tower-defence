using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// Shows a <see cref="LavaTrail"/>: one pool of hot light per live patch, bright when laid and cooling to nothing over
    /// the patch's lifetime, with a few embers while it is fresh. The pieces are made once, one per slot of the trail's
    /// ring, and reused. Until the trail has a strip of its own from Blender (Docs/MODEL_TODO.md) it uses the painted glow
    /// pool.
    /// </summary>
    public sealed class LavaTrailView : MonoBehaviour
    {
        private const float PoolScale = 1.5f;
        private const float EmberRate = 6f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color Hot = new Color(1f, 0.45f, 0.08f, 1f) * 2.2f;
        private static readonly Color Ember = new Color(1f, 0.6f, 0.2f, 0.9f);

        private LavaTrail _trail;
        private Color _hot;
        private MeshRenderer[] _pools;
        private MaterialPropertyBlock _block;
        private ParticleSystem _embers;

        public static LavaTrailView Create(Transform parent, LavaTrail trail, float radius) => Create(parent, trail, radius, Hot, Ember, "LavaTrail");

        /// <summary>A trail in other colours (the rot an uprooted flower leaves behind it).</summary>
        public static LavaTrailView Create(Transform parent, LavaTrail trail, float radius, Color hot, Color ember, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<LavaTrailView>();
            view._trail = trail;
            view._hot = hot;
            view._block = new MaterialPropertyBlock();
            view._pools = new MeshRenderer[trail.Capacity];
            for (int i = 0; i < trail.Capacity; i++)
            {
                var slot = new GameObject("Patch").transform;
                slot.SetParent(go.transform, false);
                view._pools[i] = GlowDecal.PlacePool(slot, hot, radius * PoolScale).GetComponent<MeshRenderer>();
                slot.gameObject.SetActive(false);
            }

            view._embers = ParticleKit.Create(go.transform, "Embers", Vector3.zero, ember, 0.6f, 1.2f, 0.04f, 0.1f, 0.4f, 1f, -0.4f, 80, radius);
            ParticleKit.SetRate(view._embers, 0f);
            trail.Laid += view.OnLaid;
            return view;
        }

        private void OnDestroy()
        {
            if (_trail != null)
            {
                _trail.Laid -= OnLaid;
            }
        }

        private void OnLaid(int slot)
        {
            Transform piece = _pools[slot].transform.parent;
            piece.position = _trail.PointAt(slot);
            piece.gameObject.SetActive(true);
            _embers.transform.position = _trail.PointAt(slot);
            _embers.Emit(2);
        }

        private void Update()
        {
            for (int i = 0; i < _pools.Length; i++)
            {
                Transform piece = _pools[i].transform.parent;
                if (!piece.gameObject.activeSelf)
                {
                    continue;
                }

                if (!_trail.IsLive(i))
                {
                    piece.gameObject.SetActive(false);
                    continue;
                }

                float heat = 1f - _trail.AgeAt(i) / _trail.Lifetime;
                Color glow = _hot * (heat * heat);
                _pools[i].GetPropertyBlock(_block);
                _block.SetColor(MarkerArt.Tint, glow);
                _block.SetColor(EmissionColorId, glow);
                _pools[i].SetPropertyBlock(_block);
            }
        }

        /// <summary>Puts every patch out at once (the boss left the field).</summary>
        public void Clear()
        {
            foreach (MeshRenderer pool in _pools)
            {
                pool.transform.parent.gameObject.SetActive(false);
            }
        }

        public void SetEmbers(bool on) => ParticleKit.SetRate(_embers, on ? EmberRate : 0f);
    }
}
