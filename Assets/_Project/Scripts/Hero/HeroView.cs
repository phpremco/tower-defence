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
        private const float WalkBobSpeed = 11f;
        private const float WalkBobHeight = 0.07f;
        private const float SwingSeconds = 0.42f;
        private const float WindupShare = 0.3f;
        private const float StrikeShare = 0.28f;
        private const float WindupPitch = -60f;
        private const float WindupYaw = -65f;
        private const float StrikePitch = 25f;
        private const float StrikeYaw = 70f;
        private const float RestPitch = -25f;
        private const float LungeDistance = 0.32f;
        private const float LeanDegrees = 16f;
        private const float WalkLeanDegrees = 8f;
        private const float TrailSeconds = 0.22f;
        private const float TrailWidth = 0.35f;
        private static readonly Color TrailColor = new Color(0.4f, 0.95f, 1f, 0.9f);
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
        private Transform _shell;
        private TrailRenderer _trail;
        private float _lunge;
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
            _shell = shell.transform;

            var arm = new GameObject("Arm").transform;
            arm.SetParent(figure, false);
            arm.localPosition = parts.Shoulder;
            arm.gameObject.AddComponent<MeshFilter>().sharedMesh = parts.Arm;
            arm.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
            _arm = arm;
            _trail = CreateTrail(arm);

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

            Vector3 forward = _hero.Facing.sqrMagnitude > 0.0001f ? _hero.Facing.normalized : Vector3.forward;
            float bob = _hero.IsWalking ? Mathf.Abs(Mathf.Sin(Time.time * WalkBobSpeed)) * WalkBobHeight : Mathf.Sin(Time.time * IdleSpeed) * IdleHeight;
            _figure.localPosition = forward * _lunge + Vector3.up * bob;
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

        // Wind up behind the shoulder, cut across in a wide arc with a step and a lean into it, then settle back.
        // The trail behind the blade only shows during the cut.
        private void Swing()
        {
            float t = _swingTime / SwingSeconds;
            if (t >= 1f)
            {
                _arm.localRotation = Quaternion.Euler(RestPitch, 0f, 0f);
                _lunge = 0f;
                _trail.emitting = false;
                float walkLean = _hero.IsWalking && !_hero.IsDown ? WalkLeanDegrees : 0f;
                _shell.localRotation = Quaternion.Euler(walkLean, 0f, 0f);
                return;
            }

            _swingTime += Time.deltaTime;
            t = Mathf.Clamp01(_swingTime / SwingSeconds);

            float pitch;
            float yaw;
            float strike;
            if (t < WindupShare)
            {
                float k = Smooth(t / WindupShare);
                pitch = Mathf.Lerp(RestPitch, WindupPitch, k);
                yaw = Mathf.Lerp(0f, WindupYaw, k);
                strike = 0f;
            }
            else if (t < WindupShare + StrikeShare)
            {
                float k = Smooth((t - WindupShare) / StrikeShare);
                pitch = Mathf.Lerp(WindupPitch, StrikePitch, k);
                yaw = Mathf.Lerp(WindupYaw, StrikeYaw, k);
                strike = k;
            }
            else
            {
                float k = Smooth((t - WindupShare - StrikeShare) / (1f - WindupShare - StrikeShare));
                pitch = Mathf.Lerp(StrikePitch, RestPitch, k);
                yaw = Mathf.Lerp(StrikeYaw, 0f, k);
                strike = 1f - k;
            }

            _arm.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            _lunge = LungeDistance * strike;
            _shell.localRotation = Quaternion.Euler(LeanDegrees * strike, yaw * 0.25f, 0f);
            _trail.emitting = t >= WindupShare * 0.7f && t < WindupShare + StrikeShare + 0.12f;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private TrailRenderer CreateTrail(Transform arm)
        {
            var tip = new GameObject("BladeTip").transform;
            tip.SetParent(arm, false);
            tip.localPosition = new Vector3(0.04f, -0.33f, 1.45f);

            var trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.time = TrailSeconds;
            trail.widthMultiplier = TrailWidth;
            trail.minVertexDistance = 0.02f;
            trail.emitting = false;
            trail.sharedMaterial = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(TrailColor, 0f), new GradientColorKey(TrailColor, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            return trail;
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
