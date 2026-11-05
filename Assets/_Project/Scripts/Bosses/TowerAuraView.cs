using Game.Core;
using UnityEngine;

namespace Game.Bosses
{
    /// <summary>
    /// A boss's warning on one structure, told by light and not by arrows (Docs/BOSSES.md): the warm glow on the tower the
    /// Cinder Matriarch has smelled, the blue haze on the gate the Frost Warden is heading for. A pool of light on the
    /// ground, a point light that breathes, and for a haze a slow drift of mist. One per boss, moved from target to target.
    /// </summary>
    public sealed class TowerAuraView : MonoBehaviour
    {
        private const float PoolRadius = 1.1f;
        private const float LightHeight = 1.3f;
        private const float LightRange = 3.2f;
        private const float BreathSpeed = 3.2f;
        private const float BreathDepth = 0.35f;
        private const float MistRate = 30f;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MeshRenderer _pool;
        private MaterialPropertyBlock _block;
        private Light _light;
        private ParticleSystem _mist;
        private Color _colour;
        private float _intensity;
        private float _phase;

        public static TowerAuraView Create(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<TowerAuraView>();
            GameObject pool = GlowDecal.PlacePool(go.transform, Color.white, PoolRadius);
            view._pool = pool.GetComponent<MeshRenderer>();
            view._block = new MaterialPropertyBlock();
            var lightObject = new GameObject("AuraLight");
            lightObject.transform.SetParent(go.transform, false);
            lightObject.transform.localPosition = Vector3.up * LightHeight;
            view._light = lightObject.AddComponent<Light>();
            view._light.type = LightType.Point;
            view._light.range = LightRange;
            view._light.shadows = LightShadows.None;
            view._mist = ParticleKit.Create(go.transform, "Haze", Vector3.up * 0.4f, Color.white, 1.2f, 2.4f, 0.35f, 0.8f, 0.05f, 0.25f, -0.05f, 40, 0.9f);
            ParticleKit.SetRate(view._mist, 0f);
            go.SetActive(false);
            return view;
        }

        /// <summary>Lights a structure in a colour (the light at an intensity), with a drifting haze or without.</summary>
        public void Show(Vector3 at, Color colour, float intensity, bool haze)
        {
            transform.position = new Vector3(at.x, at.y, at.z);
            _colour = colour;
            _intensity = intensity;
            _light.color = colour;
            ParticleSystem.MainModule main = _mist.main;
            main.startColor = new Color(colour.r, colour.g, colour.b, 0.35f);
            ParticleKit.SetRate(_mist, haze ? MistRate : 0f);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                ParticleKit.SetRate(_mist, 0f);
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            _phase += Time.deltaTime * BreathSpeed;
            float breath = 1f + Mathf.Sin(_phase) * BreathDepth;
            _light.intensity = _intensity * breath;
            Color glow = _colour * breath;
            _pool.GetPropertyBlock(_block);
            _block.SetColor(MarkerArt.Tint, glow);
            _block.SetColor(EmissionColorId, glow);
            _pool.SetPropertyBlock(_block);
        }
    }
}
