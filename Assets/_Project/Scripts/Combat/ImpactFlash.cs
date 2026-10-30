using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    /// <summary>
    /// What a hit looks like: a short expanding glow with a spray of sparks, and for splash hits a shock ring
    /// and a puff of smoke that scale with the blast. Pooled by ProjectilePool.
    /// </summary>
    public sealed class ImpactFlash : MonoBehaviour
    {
        private const float FlashSeconds = 0.25f;
        private const float EffectSeconds = 1.5f;
        private const float StartScale = 0.2f;
        private const float Emission = 3f;
        private const int SparksBase = 6;
        private const int SparksPerRadius = 12;
        private const int MaxSparks = 40;
        private const float SparkSpeed = 3f;
        private const float SparkSpeedPerRadius = 1.5f;
        private const float RingEmission = 2f;
        private const float RingAlpha = 0.5f;
        private const float RingHeight = 0.05f;
        private const int SmokeBase = 3;
        private const int SmokePerRadius = 4;
        private const float SmokeAlpha = 0.35f;
        private static readonly Color FlashColor = new Color(1f, 0.7f, 0.3f);
        private static readonly Color SparkColor = new Color(1f, 0.85f, 0.45f);
        private static readonly Color SmokeColor = new Color(0.25f, 0.22f, 0.2f);

        private System.Action<ImpactFlash> _release;
        private Transform _shape;
        private Transform _ring;
        private ParticleSystem _sparks;
        private ParticleSystem _smoke;
        private float _age;
        private float _endScale;
        private float _radius;

        public void Show(Vector3 position, float radius, Color color, System.Action<ImpactFlash> release)
        {
            if (_shape == null)
            {
                Build();
            }

            // Hits glow in the colour of the weapon; big blasts stay fiery.
            Color flash = radius > 0f ? Color.Lerp(color, FlashColor, 0.6f) : Color.Lerp(color, Color.white, 0.4f);
            _shape.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.MaterialFor(flash, Emission);
            _ring.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(flash, RingAlpha, RingEmission);
            ParticleSystem.MainModule sparkColor = _sparks.main;
            sparkColor.startColor = Color.Lerp(flash, SparkColor, 0.5f) * 1.5f;

            transform.position = position;
            _radius = radius;
            _endScale = Mathf.Max(StartScale * 2f, radius * 2f);
            _age = 0f;
            _release = release;
            _shape.gameObject.SetActive(true);
            _shape.localScale = Vector3.one * StartScale;
            _ring.gameObject.SetActive(radius > 0f);
            _ring.localScale = new Vector3(StartScale, RingHeight, StartScale);

            ParticleSystem.MainModule sparks = _sparks.main;
            sparks.startSpeed = new ParticleSystem.MinMaxCurve(SparkSpeed * 0.5f, SparkSpeed + SparkSpeedPerRadius * radius);
            _sparks.Emit(Mathf.Min(MaxSparks, SparksBase + Mathf.RoundToInt(SparksPerRadius * radius)));
            if (radius > 0f)
            {
                _smoke.Emit(SmokeBase + Mathf.RoundToInt(SmokePerRadius * radius));
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / FlashSeconds);
            _shape.localScale = Vector3.one * Mathf.Lerp(StartScale, _endScale, t);
            if (_radius > 0f)
            {
                float diameter = Mathf.Lerp(StartScale, _radius * 2.4f, Mathf.Sqrt(t));
                _ring.localScale = new Vector3(diameter, RingHeight, diameter);
            }

            if (t >= 1f)
            {
                _shape.gameObject.SetActive(false);
                _ring.gameObject.SetActive(false);
            }

            if (_age >= EffectSeconds)
            {
                _release(this);
            }
        }

        private void Build()
        {
            _shape = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, FlashColor, transform, Vector3.zero, Vector3.one, Emission).transform;
            GameObject ring = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, FlashColor, transform, new Vector3(0f, RingHeight, 0f), Vector3.one, RingEmission);
            ring.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(FlashColor, RingAlpha, RingEmission);
            _ring = ring.transform;

            _sparks = CreateSystem("Sparks", SparkColor * 1.5f, 0.25f, 0.55f, 0.04f, 0.09f, 1.8f, 0.2f, MaxSparks);
            _smoke = CreateSystem("Smoke", new Color(SmokeColor.r, SmokeColor.g, SmokeColor.b, SmokeAlpha), 0.7f, 1.3f, 0.35f, 0.7f, -0.15f, 2.2f, 24);
        }

        // A burst-only particle system: sparks are quick and bright and fall, smoke is slow and swells as it fades.
        private ParticleSystem CreateSystem(string name, Color color, float minLife, float maxLife, float minSize, float maxSize, float gravity, float endSize, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endSize));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GraphicsSettings.currentRenderPipeline.defaultParticleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return system;
        }
    }
}
