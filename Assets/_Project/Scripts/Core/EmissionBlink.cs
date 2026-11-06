using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// A warning light: the material's glow flashes up for a share of each period and sinks back to a dim rest, with a
    /// soft rise and fall so it reads as a lamp, not a switch. The material is the owner's own copy. Pure view code.
    /// </summary>
    public sealed class EmissionBlink : MonoBehaviour
    {
        private const string EmissionColor = "_EmissionColor";

        // Share of the lit time spent fading in and out at each end.
        private const float EdgeShare = 0.3f;

        private Material _material;
        private Color _painted;
        private float _period;
        private float _litShare;
        private float _litGlow;
        private float _dimGlow;
        private float _clock;

        public void Initialize(Material material, float period, float litShare, float litGlow, float dimGlow)
        {
            _material = material;
            _painted = material.GetColor(EmissionColor);
            _period = period;
            _litShare = litShare;
            _litGlow = litGlow;
            _dimGlow = dimGlow;
        }

        private void OnDisable()
        {
            if (_material != null)
            {
                _material.SetColor(EmissionColor, _painted * _dimGlow);
            }
        }

        private void Update()
        {
            _clock = (_clock + Time.deltaTime) % _period;
            float lit = _clock / (_period * _litShare);
            float glow = lit >= 1f ? 0f : Mathf.Clamp01(Mathf.Min(lit, 1f - lit) / (EdgeShare * 0.5f));
            _material.SetColor(EmissionColor, _painted * Mathf.Lerp(_dimGlow, _litGlow, glow));
        }
    }
}
