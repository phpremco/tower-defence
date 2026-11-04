using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    /// <summary>
    /// What a hit looks like, driven by an <see cref="ImpactVfxDefinition"/> and the effect sheets painted in Blender:
    /// a six-way fireball that leaves white-hot and cools to orange, a flash in the weapon's colour, spark streaks,
    /// glowing shards, dust from the ground, then smoke that rises and hangs; a shock ring and (high profile) a ring of
    /// refraction and heat haze; a short point light. A chain plays several blasts one after another (a boss going
    /// down). Pooled by ProjectilePool; every system is built once and only re-pointed per blast. The profile's
    /// particle budget scales the counts.
    /// </summary>
    public sealed class ImpactFlash : MonoBehaviour
    {
        private const int MaxChain = 6;
        private const int MaxFireballs = 12;
        private const int MaxFlashes = 8;
        private const int MaxSparks = 60;
        private const int MaxSmoke = 16;
        private const int MaxShards = 40;
        private const int MaxDust = 6;
        private const int MaxHaze = 4;
        private const int MaxLinger = 6;
        private const int LingerPuffs = 3;
        private const float MinRingDiameter = 0.2f;
        private const float RingHeight = 0.05f;
        private const float ShockHeight = 0.07f;
        private const float LightHeight = 0.6f;

        // At most this many shock refractions bend the picture at once: in a busy fight more rings read as a glitch.
        private const int MaxConcurrentShocks = 2;
        private const float SmokeLifeStretch = 1.25f;
        private const float LingerLifeStretch = 1.3f;
        private const float RingPeak = 1.6f;
        private const float SparkLength = 0.05f;
        private const float FireballJitter = 0.15f;
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private static readonly Color FireColor = new Color(1f, 0.7f, 0.3f);
        private static readonly Color SparkColor = new Color(1f, 0.85f, 0.55f);
        private static readonly Color SmokeColor = new Color(0.85f, 0.82f, 0.8f);
        private static readonly Color LingerColor = new Color(0.75f, 0.72f, 0.7f);

        private readonly Vector3[] _burstAt = new Vector3[MaxChain];
        private readonly float[] _burstScale = new float[MaxChain];

        private System.Action<ImpactFlash> _release;
        private ParticleSystem _fireball;
        private ParticleSystem _flash;
        private ParticleSystem _sparks;
        private ParticleSystem _shards;
        private ParticleSystem _dust;
        private ParticleSystem _smoke;
        private ParticleSystem _haze;
        private ParticleSystem _linger;
        private MeshRenderer _ring;
        private MeshRenderer _shock;

        // Shock rings showing on every pooled flash together (all flashes run on the main thread).
        private static int _activeShocks;
        private bool _shockCounted;
        private MaterialPropertyBlock _block;
        private Light _light;
        private ImpactVfxDefinition _vfx;
        private VfxQuality _quality = VfxQuality.High;
        private Color _tint;
        private float _radius;
        private float _age;
        private float _life;
        private float _ringDiameter;
        private float _lightPeak;
        private int _bursts;
        private int _nextBurst;
        private int _nextSmoke;
        private bool _lingered;

        public void Configure(VfxQuality quality) => _quality = quality;

        public void Show(Vector3 position, float radius, Color color, ImpactVfxDefinition vfx, System.Action<ImpactFlash> release)
        {
            if (_fireball == null)
            {
                Build();
            }

            _vfx = vfx;
            _radius = radius;
            _release = release;
            _age = 0f;
            transform.position = position;
            // Hits glow in the colour of the weapon; big blasts lean towards fire.
            _tint = radius > 0f ? Color.Lerp(color, FireColor, vfx.FireShare) : Color.Lerp(color, Color.white, 0.35f);

            bool low = _quality.LowTextures;
            VfxParticles.Use(_fireball, vfx.Fireball, low);
            VfxParticles.Heat(_fireball, vfx.Heat);
            VfxParticles.Use(_flash, vfx.Flash, low);
            VfxParticles.Use(_sparks, vfx.SparkSheet, low, VfxParticles.Frames.RandomRow);
            VfxParticles.Use(_shards, vfx.DebrisSheet, low, VfxParticles.Frames.RandomCell);
            VfxParticles.Use(_dust, vfx.DustSheet, low);
            VfxParticles.Heat(_dust, null);
            VfxParticles.Use(_smoke, vfx.SmokeSheet, low);
            VfxParticles.Heat(_smoke, null);
            VfxParticles.Use(_linger, vfx.LingerSmoke, low);
            VfxParticles.Heat(_linger, null);
            VfxParticles.Use(_haze, vfx.HeatHaze, low);

            PlanChain(position, vfx);
            ShowRings(vfx, radius, low);
            ShowLight(vfx);
            _life = LifeOf(vfx);
            Animate();
        }

        // The first blast is where the shot landed; later ones land round it, each a little bigger.
        private void PlanChain(Vector3 position, ImpactVfxDefinition vfx)
        {
            _bursts = Mathf.Clamp(vfx.Chain, 1, MaxChain);
            for (int i = 0; i < _bursts; i++)
            {
                Vector2 offset = i == 0 ? Vector2.zero : Random.insideUnitCircle.normalized * (vfx.ChainSpread * Random.Range(0.5f, 1f));
                _burstAt[i] = position + new Vector3(offset.x, 0f, offset.y);
                _burstScale[i] = 1f + vfx.ChainGrowth * i;
            }

            _nextBurst = 0;
            _nextSmoke = 0;
            _lingered = vfx.LingerSmoke == null;
        }

        private void ShowRings(ImpactVfxDefinition vfx, float radius, bool low)
        {
            _ringDiameter = vfx.Ring && radius > 0f ? radius * vfx.RingScale : 0f;
            bool ring = _ringDiameter > 0f && vfx.RingSheet != null;
            _ring.gameObject.SetActive(ring);
            if (ring)
            {
                _ring.sharedMaterial = vfx.RingSheet.MaterialFor(low);
            }

            // Only blasts that kick the camera bend it, and only a few at a time.
            bool shock = ring && _quality.Distortion && vfx.ShockDistortion != null && vfx.ShockStrength > 0f
                         && vfx.ShakeFor(radius) > 0f && _activeShocks < MaxConcurrentShocks;
            SetShock(shock);
            if (shock)
            {
                _shock.sharedMaterial = vfx.ShockDistortion.MaterialFor(low);
            }
        }

        private void ShowLight(ImpactVfxDefinition vfx)
        {
            _light.transform.localPosition = Vector3.up * LightHeight;
            _lightPeak = _quality.Lights ? vfx.LightIntensity : 0f;
            _light.enabled = _lightPeak > 0f;
            _light.color = _tint;
            _light.range = vfx.LightRange;
            _light.intensity = _lightPeak;
        }

        // The effect lives until its last part is gone: the chain, the fire, the smoke after it, the hanging smoke.
        private float LifeOf(ImpactVfxDefinition vfx)
        {
            float life = Mathf.Max(vfx.FlashSeconds * 2f, vfx.LightSeconds);
            life = Mathf.Max(life, SheetLife(vfx.Fireball));
            life = Mathf.Max(life, SheetLife(vfx.Flash));
            life = Mathf.Max(life, SheetLife(vfx.DustSheet));
            life = Mathf.Max(life, vfx.SmokeDelay + SheetLife(vfx.SmokeSheet) * SmokeLifeStretch);
            life = Mathf.Max(life, vfx.HeatHaze != null ? vfx.HeatHazeSeconds : 0f);
            // Sparks and shards fall for at most a second and a bit.
            life = Mathf.Max(life, 1.2f);
            life += vfx.ChainSeconds;
            if (vfx.LingerSmoke != null)
            {
                life = Mathf.Max(life, vfx.ChainSeconds + SheetLife(vfx.LingerSmoke) * LingerLifeStretch);
            }

            return life;
        }

        private static float SheetLife(VfxSheet sheet) => sheet != null ? sheet.Lifetime : 0f;

        private int Budget(int count, int max) => count <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(count * _quality.ParticleScale), 1, max);

        private void Update()
        {
            _age += Time.deltaTime;
            Animate();
            if (_age >= _life)
            {
                SetShock(false);
                _release(this);
            }
        }

        private void OnDisable() => SetShock(false);

        private void SetShock(bool on)
        {
            if (on != _shockCounted)
            {
                _activeShocks += on ? 1 : -1;
                _shockCounted = on;
            }

            if (_shock != null)
            {
                _shock.gameObject.SetActive(on);
            }
        }

        // A play session without a domain reload starts with no rings counted.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetShocks() => _activeShocks = 0;

        private void Animate()
        {
            while (_nextBurst < _bursts && _age >= _nextBurst * _vfx.ChainInterval)
            {
                Burst(_nextBurst);
                _nextBurst++;
            }

            while (_nextSmoke < _bursts && _age >= _nextSmoke * _vfx.ChainInterval + _vfx.SmokeDelay)
            {
                Smoke(_nextSmoke);
                _nextSmoke++;
            }

            if (!_lingered && _age >= _vfx.ChainSeconds)
            {
                Linger();
                _lingered = true;
            }

            AnimateRings();
            AnimateLight();
        }

        // One blast of the chain: fire, flash, sparks, shards and dust.
        private void Burst(int index)
        {
            Vector3 at = _burstAt[index];
            float scale = _burstScale[index];
            float fire = _vfx.FireballSizeFor(_radius) * scale;
            if (_vfx.Fireball != null)
            {
                Emit(_fireball, at, Budget(_vfx.Fireballs, MaxFireballs), fire * (1f - FireballJitter), fire * (1f + FireballJitter), _vfx.Fireball.Lifetime, Color.white, 0f, 0.25f, fire * 0.12f);
                if (_vfx.HeatHaze != null && _quality.Distortion)
                {
                    Emit(_haze, at + Vector3.up * fire * 0.3f, 1, fire * 1.1f, fire * 1.3f, _vfx.HeatHazeSeconds, Color.white, 0f, 0.2f, 0f);
                }
            }

            if (_vfx.Flash != null)
            {
                float flash = _vfx.FlashSizeFor(_radius) * scale;
                Emit(_flash, at, 1, flash, flash * 1.1f, _vfx.Flash.Lifetime, _tint, 0f, 0f, 0f);
            }

            if (_vfx.DustSheet != null && _vfx.DustSize > 0f)
            {
                float dust = _vfx.DustSize * (1f + 0.5f * _radius) * scale;
                Emit(_dust, at, 1, dust * 0.9f, dust * 1.1f, _vfx.DustSheet.Lifetime, Color.white, 0f, 0f, 0f);
            }

            ParticleSystem.MainModule sparks = _sparks.main;
            sparks.startColor = Color.Lerp(_tint, SparkColor, 0.5f);
            int sparkCount = Budget(_vfx.SparksFor(_radius), MaxSparks);
            Emit(_sparks, at + Vector3.up * 0.15f, sparkCount, SparkLength, SparkLength * 2f, Random.Range(0.25f, 0.55f), sparks.startColor.color, 1.8f, _vfx.SparkSpeed * 0.5f, _vfx.SparkSpeed * (1f + 0.5f * _radius));
            if (_radius > 0f && _vfx.DebrisSheet != null)
            {
                Emit(_shards, at + Vector3.up * 0.1f, Budget(_vfx.ShardsFor(_radius), MaxShards), 0.1f, 0.26f, Random.Range(0.7f, 1.2f), Color.white, 1.6f, _vfx.ShardSpeed * 0.4f, _vfx.ShardSpeed);
            }

            if (index > 0)
            {
                // Every later blast lights the field again.
                _light.intensity = _lightPeak;
                _light.enabled = _lightPeak > 0f;
                _light.transform.position = at + Vector3.up * LightHeight;
            }
        }

        private void Smoke(int index)
        {
            if (_vfx.SmokeSheet == null)
            {
                return;
            }

            float size = _vfx.SmokeSize * (1f + 0.4f * _radius) * _burstScale[index];
            Emit(_smoke, _burstAt[index] + Vector3.up * size * 0.15f, Budget(_vfx.SmokeFor(_radius), MaxSmoke), size * 0.8f, size * 1.2f, _vfx.SmokeSheet.Lifetime * Random.Range(1f, SmokeLifeStretch), SmokeColor, -0.04f, 0.2f, 0.5f);
        }

        private void Linger()
        {
            float size = _vfx.FireballSizeFor(_radius) * 0.8f;
            Emit(_linger, transform.position + Vector3.up * size * 0.2f, Budget(LingerPuffs, MaxLinger), size * 0.8f, size * 1.1f, _vfx.LingerSmoke.Lifetime * LingerLifeStretch, LingerColor, -0.02f, 0.1f, 0.3f);
        }

        private static void Emit(ParticleSystem system, Vector3 at, int count, float minSize, float maxSize, float life, Color colour, float gravity, float minSpeed, float maxSpeed)
        {
            if (count <= 0)
            {
                return;
            }

            ParticleSystem.MainModule main = system.main;
            main.gravityModifier = gravity;
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, Mathf.Max(minSpeed, maxSpeed));
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startLifetime = Mathf.Max(0.05f, life);
            main.startColor = colour;
            system.transform.position = at;
            system.Emit(count);
        }

        // The ring races out and fades; the refraction rides along it at a strength that dies with it.
        private void AnimateRings()
        {
            if (_ringDiameter <= 0f)
            {
                return;
            }

            float t = Mathf.Clamp01(_age / Mathf.Max(0.05f, _vfx.FlashSeconds * 1.5f));
            float eased = 1f - (1f - t) * (1f - t);
            float diameter = Mathf.Lerp(MinRingDiameter, _ringDiameter, eased);
            if (_ring.gameObject.activeSelf)
            {
                _ring.transform.localScale = new Vector3(diameter, diameter, 1f);
                _block.Clear();
                _block.SetColor(TintId, _tint);
                _block.SetFloat(IntensityId, RingPeak * _vfx.RingAlpha * (1f - t));
                _ring.SetPropertyBlock(_block);
                if (t >= 1f)
                {
                    _ring.gameObject.SetActive(false);
                }
            }

            if (_shock.gameObject.activeSelf)
            {
                _shock.transform.localScale = new Vector3(diameter * 1.1f, diameter * 1.1f, 1f);
                _block.Clear();
                _block.SetFloat(StrengthId, _vfx.ShockStrength * (1f - t));
                _shock.SetPropertyBlock(_block);
                if (t >= 1f)
                {
                    SetShock(false);
                }
            }
        }

        private void AnimateLight()
        {
            if (!_light.enabled)
            {
                return;
            }

            float since = _age - Mathf.Min(_nextBurst - 1, _bursts - 1) * _vfx.ChainInterval;
            float lit = 1f - Mathf.Clamp01(since / _vfx.LightSeconds);
            _light.intensity = _lightPeak * lit * lit;
            _light.enabled = lit > 0f;
        }

        private void Build()
        {
            _block = new MaterialPropertyBlock();
            _fireball = VfxParticles.Create(transform, "Fireball", MaxFireballs);
            _flash = VfxParticles.Create(transform, "Flash", MaxFlashes);
            _haze = VfxParticles.Create(transform, "HeatHaze", MaxHaze);
            ParticleSystem.ColorOverLifetimeModule haze = _haze.colorOverLifetime;
            haze.color = VfxParticles.Fade(0.15f, 0.4f);
            _dust = VfxParticles.Create(transform, "Dust", MaxDust);
            _smoke = VfxParticles.Create(transform, "Smoke", MaxSmoke);
            ParticleSystem.ColorOverLifetimeModule smoke = _smoke.colorOverLifetime;
            smoke.color = VfxParticles.Fade(0.12f, 0.55f);
            _linger = VfxParticles.Create(transform, "LingerSmoke", MaxLinger);
            ParticleSystem.ColorOverLifetimeModule linger = _linger.colorOverLifetime;
            linger.color = VfxParticles.Fade(0.2f, 0.5f);

            _sparks = VfxParticles.Create(transform, "Sparks", MaxSparks);
            var sparkRenderer = _sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            sparkRenderer.lengthScale = 2f;
            sparkRenderer.velocityScale = 0.06f;
            ParticleSystem.ShapeModule sparkShape = _sparks.shape;
            sparkShape.shapeType = ParticleSystemShapeType.Hemisphere;
            sparkShape.radius = 0.1f;

            _shards = VfxParticles.Create(transform, "Shards", MaxShards);
            ParticleSystem.MainModule shards = _shards.main;
            shards.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.RotationOverLifetimeModule spin = _shards.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            ParticleSystem.ShapeModule shardShape = _shards.shape;
            shardShape.shapeType = ParticleSystemShapeType.Hemisphere;
            shardShape.radius = 0.15f;
            CoolShards();

            // The shock ring and its refraction are flat cards on the ground.
            _ring = VfxQuad.Create(transform, "Ring", VfxQuad.Centred);
            _ring.transform.localPosition = new Vector3(0f, RingHeight, 0f);
            _ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _shock = VfxQuad.Create(transform, "ShockDistortion", VfxQuad.Centred);
            _shock.transform.localPosition = new Vector3(0f, ShockHeight, 0f);
            _shock.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up * LightHeight;
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.shadows = LightShadows.None;
            _light.renderMode = LightRenderMode.ForcePixel;
            _light.enabled = false;
        }

        // Shards leave glowing and go dark in flight, so they read as debris rather than more sparks.
        private void CoolShards()
        {
            ParticleSystem.ColorOverLifetimeModule cool = _shards.colorOverLifetime;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.45f, 0.4f, 0.38f), 0.35f), new GradientColorKey(new Color(0.35f, 0.32f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            cool.color = gradient;
        }
    }
}
