using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Placeholder look of the commander: an armoured figure with shoulder plates, a visor, a ring on the ground
    /// and an energy blade that swings when it hits. Reacts to Commander events only.
    /// </summary>
    [RequireComponent(typeof(Commander))]
    public sealed class HeroView : MonoBehaviour
    {
        private const float TurnSpeed = 14f;
        private const float IdleSpeed = 2.2f;
        private const float IdleHeight = 0.02f;
        private const float SwingSeconds = 0.26f;
        private const float SwingRaised = -80f;
        private const float SwingLow = 55f;
        private const float RingSeconds = 0.45f;
        private const float RingAlpha = 0.35f;
        private const float RingEmission = 2.5f;
        private const float BarWidth = 1f;
        private const float BarHeight = 0.09f;
        private const float BarY = 1.95f;
        private static readonly Color DownColor = new Color(0.14f, 0.15f, 0.18f);
        private static readonly Color HealthyColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color DamagedColor = new Color(1f, 0.55f, 0.2f);

        private Commander _hero;
        private Transform _figure;
        private Transform _arm;
        private Renderer[] _figureRenderers;
        private Material[][] _figureMaterials;
        private Transform _bar;
        private Renderer _barRenderer;
        private Transform _ring;
        private float _ringTime = float.PositiveInfinity;
        private float _swingTime = float.PositiveInfinity;
        private Color _trim;

        public void Initialize(Commander hero)
        {
            _hero = hero;
            _trim = hero.Definition.Color;
            Build();
            _hero.Changed += Refresh;
            _hero.Struck += OnStruck;
            _hero.ShockwaveReleased += OnShockwave;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.Changed -= Refresh;
                _hero.Struck -= OnStruck;
                _hero.ShockwaveReleased -= OnShockwave;
            }
        }

        private void Build()
        {
            var figure = new GameObject("Figure").transform;
            figure.SetParent(transform, false);
            _figure = figure;

            HeroModelFactory.Parts parts = HeroModelFactory.Create();
            Material[] materials = HeroModelFactory.CreateMaterials(_trim);

            var shell = new GameObject("Shell");
            shell.transform.SetParent(figure, false);
            shell.AddComponent<MeshFilter>().sharedMesh = parts.Body;
            shell.AddComponent<MeshRenderer>().sharedMaterials = materials;

            var arm = new GameObject("Arm").transform;
            arm.SetParent(figure, false);
            arm.localPosition = parts.Shoulder;
            arm.gameObject.AddComponent<MeshFilter>().sharedMesh = parts.Arm;
            arm.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
            _arm = arm;

            _figureRenderers = figure.GetComponentsInChildren<Renderer>();
            _figureMaterials = new Material[_figureRenderers.Length][];
            for (int i = 0; i < _figureRenderers.Length; i++)
            {
                _figureMaterials[i] = _figureRenderers[i].sharedMaterials;
            }

            // A golden ring on the ground keeps the hero easy to find among towers and enemies.
            GameObject standRing = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, _trim, transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.95f, 0.01f, 0.95f), 1.5f);
            standRing.name = "StandRing";

            GameObject bar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, HealthyColor, transform, new Vector3(0f, BarY, 0f), new Vector3(BarWidth, BarHeight, BarHeight), 1.5f);
            _bar = bar.transform;
            _barRenderer = bar.GetComponent<Renderer>();

            GameObject ring = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, _trim, transform, new Vector3(0f, 0.1f, 0f), new Vector3(1f, 0.02f, 1f), RingEmission);
            ring.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(_trim, RingAlpha, RingEmission);
            _ring = ring.transform;
            _ring.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            bool down = _hero.IsDown;
            Material wreck = PlaceholderVisuals.MaterialFor(DownColor, 0f);
            for (int i = 0; i < _figureRenderers.Length; i++)
            {
                _figureRenderers[i].sharedMaterials = down ? Wreck(wreck, _figureMaterials[i].Length) : _figureMaterials[i];
            }

            _figure.localRotation = down ? Quaternion.Euler(0f, _figure.localEulerAngles.y, 80f) : Quaternion.Euler(0f, _figure.localEulerAngles.y, 0f);
            _figure.localPosition = down ? new Vector3(0f, 0.2f, 0f) : Vector3.zero;

            float fraction = _hero.Health.Fraction;
            _bar.gameObject.SetActive(!down && fraction < 1f);
            _bar.localScale = new Vector3(BarWidth * Mathf.Max(0.001f, fraction), BarHeight, BarHeight);
            _barRenderer.sharedMaterial = PlaceholderVisuals.MaterialFor(Color.Lerp(DamagedColor, HealthyColor, fraction), 1.5f);
        }

        private static Material[] Wreck(Material wreck, int count)
        {
            var result = new Material[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = wreck;
            }

            return result;
        }

        private void OnStruck(Vector3 target) => _swingTime = 0f;

        private void OnShockwave(Vector3 centre, float radius)
        {
            _ring.gameObject.SetActive(true);
            _ringTime = 0f;
            _ring.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        }

        private void Update()
        {
            TurnTowardsFacing();
            Idle();
            Swing();
            Pulse();
        }

        // A slow breath keeps the commander alive between strikes.
        private void Idle()
        {
            if (_hero.IsDown)
            {
                return;
            }

            _figure.localPosition = new Vector3(0f, Mathf.Sin(Time.time * IdleSpeed) * IdleHeight, 0f);
        }

        private void TurnTowardsFacing()
        {
            if (_hero.IsDown)
            {
                return;
            }

            Vector3 facing = _hero.Facing;
            if (facing.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion wanted = Quaternion.LookRotation(facing);
            _figure.rotation = Quaternion.Slerp(_figure.rotation, wanted, 1f - Mathf.Exp(-TurnSpeed * Time.deltaTime));
        }

        // The blade is raised, brought down fast and returned: the angle follows a short triangle over the swing time.
        private void Swing()
        {
            if (_swingTime > SwingSeconds)
            {
                _arm.localRotation = Quaternion.Euler(SwingRaised * 0.35f, 0f, 0f);
                return;
            }

            _swingTime += Time.deltaTime;
            float t = Mathf.Clamp01(_swingTime / SwingSeconds);
            float angle = t < 0.4f ? Mathf.Lerp(SwingRaised, SwingLow, t / 0.4f) : Mathf.Lerp(SwingLow, SwingRaised * 0.35f, (t - 0.4f) / 0.6f);
            _arm.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        private void Pulse()
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
