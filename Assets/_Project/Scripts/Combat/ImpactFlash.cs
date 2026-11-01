using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    /// <summary>
    /// What a hit looks like, driven by an <see cref="ImpactVfxDefinition"/>: a white-hot core inside a translucent
    /// fireball that swells and thins out, a shock ring, a spray of sparks, a puff of smoke, shards that cool from
    /// ember to cinder, and a short point light on the surroundings. Pooled by ProjectilePool; the particle budget
    /// of the platform scales the counts.
    /// </summary>
    public sealed class ImpactFlash : MonoBehaviour
    {
        private const float EffectSeconds = 1.6f;
        private const float StartScale = 0.2f;
        private const float MinFlashDiameter = 0.4f;
        // The fireball outlives the core: it keeps swelling and fading after the hot centre has gone.
        private const float ShellLifeShare = 2.2f;
        private const string FireballShader = "Game/Fireball";
        private const float ShellIntensity = 4f;
        private const float CoreShare = 0.45f;
        private const float CoreLifeShare = 0.7f;
        private const float CoreEmission = 4f;
        private const float RingIntensity = 2.5f;
        private const float RingHeight = 0.06f;
        private const float LightHeight = 0.6f;
        private const int MaxSparks = 60;
        private const int MaxSmoke = 32;
        private const int MaxShards = 40;
        private const float SmokeAlpha = 0.45f;
        private static readonly Color FlashColor = new Color(1f, 0.7f, 0.3f);
        private static readonly Color SparkColor = new Color(1f, 0.85f, 0.45f);
        // Lit grey rather than soot, so the smoke still reads on the dark ground of every level.
        private static readonly Color SmokeColor = new Color(0.5f, 0.45f, 0.42f);
        private static readonly Color EmberColor = new Color(1f, 0.6f, 0.25f);
        private static readonly Color CinderColor = new Color(0.22f, 0.18f, 0.16f);

        private System.Action<ImpactFlash> _release;
        private Transform _shell;
        private Material _shellMaterial;
        private Transform _core;
        private Transform _ring;
        private Material _ringMaterial;
        private Light _light;
        private ParticleSystem _sparks;
        private ParticleSystem _smoke;
        private ParticleSystem _shards;
        private ImpactVfxDefinition _vfx;
        private Color _flash;
        private float _particleScale = 1f;
        private float _age;
        private float _endScale;
        private float _ringScale;
        private float _lightPeak;

        /// <summary>Lights cost more than particles, so the low profile (particle scale under one) goes without them.</summary>
        public void Configure(float particleScale)
        {
            _particleScale = particleScale;
        }

        public void Show(Vector3 position, float radius, Color color, ImpactVfxDefinition vfx, System.Action<ImpactFlash> release)
        {
            if (_shell == null)
            {
                Build();
            }

            _vfx = vfx;
            // Hits glow in the colour of the weapon; big blasts lean towards fire.
            _flash = radius > 0f ? Color.Lerp(color, FlashColor, vfx.FireShare) : Color.Lerp(color, Color.white, 0.4f);
            _core.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.MaterialFor(Color.Lerp(_flash, Color.white, 0.7f), CoreEmission);

            transform.position = position;
            _endScale = Mathf.Max(MinFlashDiameter, radius * 2f * vfx.FlashScale);
            _ringScale = radius * vfx.RingScale;
            _age = 0f;
            _release = release;
            _shell.gameObject.SetActive(true);
            _core.gameObject.SetActive(true);
            _ring.gameObject.SetActive(vfx.Ring && radius > 0f);
            _ringMaterial.SetColor("_BaseColor", new Color(_flash.r * RingIntensity, _flash.g * RingIntensity, _flash.b * RingIntensity, vfx.RingAlpha));
            Animate();

            _lightPeak = _particleScale >= 1f ? vfx.LightIntensity : 0f;
            _light.enabled = _lightPeak > 0f;
            _light.color = _flash;
            _light.range = vfx.LightRange;
            _light.intensity = _lightPeak;

            ParticleSystem.MainModule sparks = _sparks.main;
            sparks.startColor = Color.Lerp(_flash, SparkColor, 0.5f) * 1.5f;
            sparks.startSpeed = new ParticleSystem.MinMaxCurve(vfx.SparkSpeed * 0.5f, vfx.SparkSpeed * (1f + 0.5f * radius));
            _sparks.Emit(Budget(vfx.SparksFor(radius), MaxSparks));
            if (radius > 0f)
            {
                _smoke.Emit(Budget(vfx.SmokeFor(radius), MaxSmoke));
                ParticleSystem.MainModule shards = _shards.main;
                shards.startSpeed = new ParticleSystem.MinMaxCurve(vfx.ShardSpeed * 0.4f, vfx.ShardSpeed);
                _shards.Emit(Budget(vfx.ShardsFor(radius), MaxShards));
            }
        }

        private int Budget(int count, int max) => Mathf.Min(max, Mathf.RoundToInt(count * _particleScale));

        private void Update()
        {
            _age += Time.deltaTime;
            Animate();

            if (_light.enabled)
            {
                float lit = 1f - Mathf.Clamp01(_age / _vfx.LightSeconds);
                _light.intensity = _lightPeak * lit * lit;
                _light.enabled = lit > 0f;
            }

            if (_age >= EffectSeconds)
            {
                _release(this);
            }
        }

        // The core flares and is gone; the shell swells fast, then thins to nothing; the ring races out.
        private void Animate()
        {
            float t = Mathf.Clamp01(_age / _vfx.FlashSeconds);
            float shellT = Mathf.Clamp01(_age / (_vfx.FlashSeconds * ShellLifeShare));
            float swell = 1f - (1f - shellT) * (1f - shellT);
            _shell.localScale = Vector3.one * Mathf.Lerp(StartScale, _endScale, swell);
            float thin = (1f - shellT) * (1f - shellT);
            _shellMaterial.SetColor("_Color", _flash * ShellIntensity);
            _shellMaterial.SetFloat("_Alpha", thin);
            _shell.gameObject.SetActive(shellT < 1f);

            float coreT = Mathf.Clamp01(_age / (_vfx.FlashSeconds * CoreLifeShare));
            _core.localScale = Vector3.one * Mathf.Max(StartScale * 0.5f, _endScale * CoreShare * (1f - coreT));
            _core.gameObject.SetActive(coreT < 1f);

            if (_ringScale > 0f)
            {
                float diameter = Mathf.Lerp(StartScale, _ringScale, Mathf.Sqrt(t));
                _ring.localScale = new Vector3(diameter, diameter, 1f);
                Color ringColor = _ringMaterial.GetColor("_BaseColor");
                ringColor.a = _vfx.RingAlpha * (1f - t);
                _ringMaterial.SetColor("_BaseColor", ringColor);
                _ring.gameObject.SetActive(t < 1f);
            }
        }

        private void Build()
        {
            GameObject shell = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, FlashColor, transform, Vector3.zero, Vector3.one);
            // The fresnel shell needs its own shader; a build without it falls back to plain tinted glass.
            Shader fireball = Shader.Find(FireballShader);
            _shellMaterial = fireball != null ? new Material(fireball) : new Material(PlaceholderVisuals.TransparentMaterialFor(FlashColor, 0.5f, ShellIntensity));
            _shellMaterial.SetTexture("_NoiseTex", SpriteKit.Noise());
            var shellRenderer = shell.GetComponent<Renderer>();
            shellRenderer.sharedMaterial = _shellMaterial;
            shellRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _shell = shell.transform;

            GameObject core = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, FlashColor, transform, Vector3.zero, Vector3.one, CoreEmission);
            core.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            _core = core.transform;

            // The shock ring is a flat textured quad on the ground: a cylinder would read as a solid disc from above.
            var ring = new GameObject("Ring");
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, RingHeight, 0f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.AddComponent<MeshFilter>().sharedMesh = Quad();
            var ringRenderer = ring.AddComponent<MeshRenderer>();
            _ringMaterial = new Material(GraphicsSettings.currentRenderPipeline.defaultParticleMaterial);
            _ringMaterial.SetTexture("_BaseMap", SpriteKit.Ring());
            ringRenderer.sharedMaterial = _ringMaterial;
            ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _ring = ring.transform;

            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up * LightHeight;
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.shadows = LightShadows.None;
            _light.enabled = false;

            _sparks = CreateSystem("Sparks", SparkColor * 1.5f, 0.25f, 0.55f, 0.04f, 0.09f, 1.8f, 0.2f, MaxSparks);
            _smoke = CreateSystem("Smoke", new Color(SmokeColor.r, SmokeColor.g, SmokeColor.b, SmokeAlpha), 0.8f, 1.5f, 0.4f, 0.8f, -0.15f, 2.4f, MaxSmoke);
            _smoke.GetComponent<ParticleSystemRenderer>().sharedMaterial = SmokeMaterial();
            _shards = CreateSystem("Shards", EmberColor, 0.6f, 1.1f, 0.05f, 0.11f, 1.6f, 0.7f, MaxShards);
            CoolShards();
            var shardRenderer = _shards.GetComponent<ParticleSystemRenderer>();
            shardRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            shardRenderer.lengthScale = 1.6f;
        }

        private static Mesh Quad()
        {
            var mesh = new Mesh { name = "RingQuad" };
            mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            return mesh;
        }

        private static Material SmokeMaterial()
        {
            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultParticleMaterial);
            material.SetTexture("_BaseMap", SpriteKit.Puff());
            return material;
        }

        // Shards leave as glowing embers and go dark in flight, so they read as debris rather than more sparks.
        private void CoolShards()
        {
            ParticleSystem.ColorOverLifetimeModule cool = _shards.colorOverLifetime;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(CinderColor, 0.3f), new GradientColorKey(CinderColor, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            cool.color = gradient;
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
