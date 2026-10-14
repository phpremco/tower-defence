using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Placeholder look of the hero: a glowing capsule with a visor, a health bar when hurt, a flat dark body
    /// while down and an expanding ring for the shockwave. Reacts to Hero events only.
    /// </summary>
    [RequireComponent(typeof(Commander))]
    public sealed class HeroView : MonoBehaviour
    {
        private const float BodyEmission = 0.9f;
        private const float RingSeconds = 0.45f;
        private const float RingEmission = 2.5f;
        private const float RingAlpha = 0.35f;
        private const float BarWidth = 0.9f;
        private const float BarHeight = 0.09f;
        private const float BarY = 1.55f;
        private static readonly Color VisorColor = new Color(1f, 0.95f, 0.5f);
        private static readonly Color DownColor = new Color(0.14f, 0.15f, 0.18f);
        private static readonly Color HealthyColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color DamagedColor = new Color(1f, 0.55f, 0.2f);

        private Commander _hero;
        private Transform _body;
        private Renderer _bodyRenderer;
        private Transform _bar;
        private Renderer _barRenderer;
        private Transform _ring;
        private float _ringTime = float.PositiveInfinity;
        private Color _color;

        public void Initialize(Commander hero)
        {
            _hero = hero;
            _color = hero.Definition.Color;
            Build();
            _hero.Changed += Refresh;
            _hero.Fired += OnFired;
            _hero.ShockwaveReleased += OnShockwave;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.Changed -= Refresh;
                _hero.Fired -= OnFired;
                _hero.ShockwaveReleased -= OnShockwave;
            }
        }

        private void Build()
        {
            GameObject body = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Capsule, _color, transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.5f, 0.6f, 0.5f), BodyEmission);
            _body = body.transform;
            _bodyRenderer = body.GetComponent<Renderer>();
            PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, VisorColor, _body, new Vector3(0f, 0.55f, 0.45f), new Vector3(0.7f, 0.18f, 0.25f), 2f);

            GameObject bar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, HealthyColor, transform, new Vector3(0f, BarY, 0f), new Vector3(BarWidth, BarHeight, BarHeight), 1.5f);
            _bar = bar.transform;
            _barRenderer = bar.GetComponent<Renderer>();

            GameObject ring = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, _color, transform, new Vector3(0f, 0.1f, 0f), new Vector3(1f, 0.02f, 1f), RingEmission);
            ring.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(_color, RingAlpha, RingEmission);
            _ring = ring.transform;
            _ring.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            bool down = _hero.IsDown;
            _bodyRenderer.sharedMaterial = PlaceholderVisuals.MaterialFor(down ? DownColor : _color, down ? 0f : BodyEmission);
            _body.localRotation = down ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
            _body.localPosition = down ? new Vector3(0f, 0.2f, 0f) : new Vector3(0f, 0.6f, 0f);

            float fraction = _hero.Health.Fraction;
            _bar.gameObject.SetActive(!down && fraction < 1f);
            _bar.localScale = new Vector3(BarWidth * Mathf.Max(0.001f, fraction), BarHeight, BarHeight);
            _barRenderer.sharedMaterial = PlaceholderVisuals.MaterialFor(Color.Lerp(DamagedColor, HealthyColor, fraction), 1.5f);
        }

        private void OnFired(Vector3 target)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                _body.rotation = Quaternion.LookRotation(flat);
            }
        }

        private void OnShockwave(Vector3 centre, float radius)
        {
            _ring.gameObject.SetActive(true);
            _ringTime = 0f;
            _ring.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        }

        private void Update()
        {
            if (_ringTime > RingSeconds)
            {
                return;
            }

            _ringTime += Time.deltaTime;
            float t = Mathf.Clamp01(_ringTime / RingSeconds);
            float diameter = Mathf.Lerp(0.2f, _hero.Definition.ShockwaveRadius * 2f, t);
            _ring.localScale = new Vector3(diameter, 0.02f, diameter);
            if (t >= 1f)
            {
                _ring.gameObject.SetActive(false);
            }
        }
    }
}
