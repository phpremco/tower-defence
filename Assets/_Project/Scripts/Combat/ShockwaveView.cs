using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Combat
{
    /// <summary>
    /// Plays a ground shockwave from a <see cref="ShockwaveVfxDefinition"/>: two ring cards that race out and fade, a pool
    /// of light under them, a ring of refraction (high profile), a flash with a short point light, and sparks, shards and
    /// dust thrown outward along the ground. Built once and re-pointed per blast, so a pulse allocates nothing. The profile
    /// scales the particle counts and drops the refraction and the light.
    /// </summary>
    public sealed class ShockwaveView : MonoBehaviour
    {
        private const int MaxFlashes = 2;
        private const int MaxSparks = 48;
        private const int MaxShards = 24;
        private const int MaxDust = 20;
        private const float GlowHeight = 0.04f;
        private const float RingHeight = 0.06f;
        private const float EchoHeight = 0.055f;
        private const float ShockHeight = 0.08f;
        private const float FlashHeight = 0.3f;
        private const float LightHeight = 0.7f;
        private const float MinDiameter = 0.3f;

        // Where the painted ring's bright edge lies on its card, as a share of the card's half width.
        private const float CardEdge = 0.92f;

        // A ring is at full strength after this share of its run and fades out from the second share on.
        private const float RingFadeIn = 0.08f;
        private const float RingFadeFrom = 0.4f;
        private const float GlowFadeIn = 0.12f;
        private const float GlowReach = 1.1f;
        private const float ShockScale = 1.08f;

        // Sparks skim the ground and die near the front; shards hop; dust rolls out at walking pace and thins early.
        private const float SparkStart = 0.35f;
        private const float SparkLift = 0.9f;
        private const float SparkGravity = 1.1f;
        private const float SparkMinLife = 0.28f;
        private const float SparkMaxLife = 0.5f;
        private const float SparkMinSize = 0.14f;
        private const float SparkMaxSize = 0.3f;
        private const float ShardStart = 0.3f;
        private const float ShardMinLift = 1.6f;
        private const float ShardMaxLift = 3.4f;
        private const float ShardGravity = 1.6f;
        private const float ShardMinLife = 0.55f;
        private const float ShardMaxLife = 0.95f;
        private const float ShardMinSize = 0.12f;
        private const float ShardMaxSize = 0.3f;
        private const float DustStartShare = 0.25f;
        private const float DustTravelShare = 0.6f;
        private const float DustHeight = 0.15f;
        private const float DustFadeIn = 0.15f;
        private const float DustFadeFrom = 0.35f;
        private const float DustRise = -0.03f;

        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");

        private VfxQuality _quality = VfxQuality.High;
        private ShockwaveVfxDefinition _vfx;
        private MeshRenderer _ring;
        private MeshRenderer _echo;
        private MeshRenderer _shock;
        private MeshRenderer _glow;
        private Material _glowMaterial;
        private MaterialPropertyBlock _block;
        private ParticleSystem _flash;
        private ParticleSystem _sparks;
        private ParticleSystem _shards;
        private ParticleSystem _dust;
        private Light _light;
        private Color _tint;
        private Color _hot;
        private Vector3 _centre;
        private float _radius;
        private float _age;
        private float _life;
        private bool _burst;
        private bool _playing;

        /// <summary>True from <see cref="Play"/> until the last part has faded.</summary>
        public bool IsPlaying => _playing;

        /// <summary>Builds the parts under <paramref name="parent"/> (the level, not the hero: a wave stays where it was struck).</summary>
        public static ShockwaveView Create(Transform parent, VfxQuality quality)
        {
            var go = new GameObject("Shockwave");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<ShockwaveView>();
            view._quality = quality;
            view.Build();
            return view;
        }

        /// <summary>
        /// Starts a wave at <paramref name="centre"/> that reaches <paramref name="radius"/>, <paramref name="delay"/> seconds
        /// from now (the blow that causes it lands a moment after the ability is used).
        /// </summary>
        public void Play(Vector3 centre, float radius, Color tint, ShockwaveVfxDefinition vfx, float delay = 0f)
        {
            _vfx = vfx;
            _centre = centre;
            _radius = radius;
            _tint = tint;
            _hot = Color.Lerp(tint, Color.white, vfx.WhiteShare);
            _age = -Mathf.Max(0f, delay);
            _burst = false;
            _playing = true;
            transform.position = _centre;

            bool low = _quality.LowTextures;
            Dress(_ring, vfx.RingSheet, low);
            Dress(_echo, vfx.HasEcho ? vfx.RingSheet : null, low);
            Dress(_shock, _quality.Distortion && vfx.DistortionStrength > 0f ? vfx.Distortion : null, low);
            _glow.gameObject.SetActive(false);
            VfxParticles.Use(_flash, vfx.Flash, low);
            VfxParticles.Use(_sparks, vfx.SparkSheet, low, VfxParticles.Frames.RandomRow);
            VfxParticles.Use(_shards, vfx.DebrisSheet, low, VfxParticles.Frames.RandomCell);
            _life = vfx.Life;
            _light.enabled = false;
            enabled = true;
            Animate();
        }

        private static void Dress(MeshRenderer card, VfxSheet sheet, bool low)
        {
            card.gameObject.SetActive(false);
            card.sharedMaterial = sheet != null ? sheet.MaterialFor(low) : null;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            Animate();
            if (_age >= _life)
            {
                Hide();
            }
        }

        private void OnDisable() => Hide();

        private void OnDestroy()
        {
            if (_glowMaterial == null)
            {
                return;
            }

            // The material is this view's own copy; an EditMode test tears the view down outside play.
            if (Application.isPlaying)
            {
                Destroy(_glowMaterial);
            }
            else
            {
                DestroyImmediate(_glowMaterial);
            }
        }

        private void Hide()
        {
            _playing = false;
            enabled = false;
            if (_ring == null)
            {
                return;
            }

            _ring.gameObject.SetActive(false);
            _echo.gameObject.SetActive(false);
            _shock.gameObject.SetActive(false);
            _glow.gameObject.SetActive(false);
            _light.enabled = false;
        }

        private void Animate()
        {
            if (_age < 0f)
            {
                return;
            }

            if (!_burst)
            {
                _burst = true;
                Burst();
            }

            float run = Mathf.Clamp01(_age / _vfx.RingSeconds);
            AnimateRing(_ring, run, _radius, _vfx.RingIntensity);
            float echo = _vfx.HasEcho ? Mathf.Clamp01((_age - _vfx.EchoDelay) / _vfx.RingSeconds) : 1f;
            AnimateRing(_echo, _age >= _vfx.EchoDelay ? echo : -1f, _radius * _vfx.EchoReach, _vfx.RingIntensity * _vfx.EchoStrength);
            AnimateShock(run);
            AnimateGlow();
            AnimateLight();
        }

        // The front leaves fast and settles on the radius; the ring is brightest just after it leaves and is gone as it arrives.
        private void AnimateRing(MeshRenderer ring, float run, float radius, float peak)
        {
            bool shown = run >= 0f && run < 1f && ring.sharedMaterial != null && peak > 0f;
            ring.gameObject.SetActive(shown);
            if (!shown)
            {
                return;
            }

            float diameter = Diameter(run, radius) / CardEdge;
            ring.transform.localScale = new Vector3(diameter, diameter, 1f);
            _block.Clear();
            _block.SetColor(TintId, _hot);
            _block.SetFloat(IntensityId, peak * Envelope(run, RingFadeIn, RingFadeFrom));
            ring.SetPropertyBlock(_block);
        }

        private void AnimateShock(float run)
        {
            bool shown = run < 1f && _shock.sharedMaterial != null;
            _shock.gameObject.SetActive(shown);
            if (!shown)
            {
                return;
            }

            float diameter = Diameter(run, _radius) / CardEdge * ShockScale;
            _shock.transform.localScale = new Vector3(diameter, diameter, 1f);
            _block.Clear();
            _block.SetFloat(StrengthId, _vfx.DistortionStrength * Envelope(run, RingFadeIn, RingFadeFrom));
            _shock.SetPropertyBlock(_block);
        }

        // The light on the ground spreads with the front and lingers a little after the rings are gone.
        private void AnimateGlow()
        {
            float t = Mathf.Clamp01(_age / _vfx.GlowSeconds);
            bool shown = t < 1f && _vfx.GlowIntensity > 0f && _glowMaterial != null;
            _glow.gameObject.SetActive(shown);
            if (!shown)
            {
                return;
            }

            float diameter = Diameter(Mathf.Clamp01(_age / _vfx.RingSeconds), _radius) * GlowReach;
            _glow.transform.localScale = new Vector3(diameter, diameter, 1f);
            GlowDecal.SetColour(_glowMaterial, _tint * (_vfx.GlowIntensity * Envelope(t, GlowFadeIn, GlowFadeIn)));
        }

        private void AnimateLight()
        {
            float lit = _quality.Lights && _vfx.LightIntensity > 0f ? 1f - Mathf.Clamp01(_age / _vfx.LightSeconds) : 0f;
            _light.enabled = lit > 0f;
            if (lit > 0f)
            {
                _light.color = _hot;
                _light.range = _vfx.LightRange;
                _light.intensity = _vfx.LightIntensity * lit * lit;
            }
        }

        public static float Diameter(float run, float radius)
        {
            float eased = 1f - (1f - run) * (1f - run) * (1f - run);
            return Mathf.Lerp(MinDiameter, radius * 2f, eased);
        }

        // Up from nothing over the first share, down to nothing from the second share on, smooth at both ends.
        public static float Envelope(float t, float fadeIn, float fadeFrom) =>
            Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / Mathf.Max(0.001f, fadeIn))) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeFrom, 1f, t)));

        // The blow itself: the flash, and everything it kicks outward along the ground.
        private void Burst()
        {
            if (_vfx.Flash != null && _vfx.FlashSize > 0f)
            {
                var flash = new ParticleSystem.EmitParams
                {
                    position = _centre + Vector3.up * FlashHeight,
                    startSize = _vfx.FlashSize,
                    startLifetime = Mathf.Max(0.05f, _vfx.Flash.Lifetime),
                    startColor = _hot,
                    velocity = Vector3.zero,
                    applyShapeToPosition = false,
                };
                _flash.Emit(flash, 1);
            }

            Color spark = Color.Lerp(_tint, Color.white, 0.5f);
            int sparks = _vfx.SparkSheet != null ? Budget(_vfx.Sparks, MaxSparks) : 0;
            for (int i = 0; i < sparks; i++)
            {
                Vector3 direction = Outward(i, sparks);
                Throw(_sparks, direction, SparkStart, 0.12f, _vfx.SparkSpeed * Random.Range(0.55f, 1.1f), Random.Range(0.1f, SparkLift),
                    Random.Range(SparkMinLife, SparkMaxLife), Random.Range(SparkMinSize, SparkMaxSize), spark);
            }

            int shards = _vfx.DebrisSheet != null ? Budget(_vfx.Shards, MaxShards) : 0;
            for (int i = 0; i < shards; i++)
            {
                Vector3 direction = Outward(i, shards);
                Throw(_shards, direction, ShardStart, 0.1f, _vfx.ShardSpeed * Random.Range(0.4f, 1f), Random.Range(ShardMinLift, ShardMaxLift),
                    Random.Range(ShardMinLife, ShardMaxLife), Random.Range(ShardMinSize, ShardMaxSize), _hot);
            }

            int puffs = Budget(_vfx.DustPuffs, MaxDust);
            float drift = _radius * DustTravelShare / _vfx.DustSeconds;
            for (int i = 0; i < puffs; i++)
            {
                Vector3 direction = Outward(i, puffs);
                Throw(_dust, direction, _radius * DustStartShare, DustHeight, drift * Random.Range(0.7f, 1.2f), 0f,
                    _vfx.DustSeconds * Random.Range(0.8f, 1.15f), _vfx.DustSize * Random.Range(0.8f, 1.3f), _vfx.DustColor);
            }
        }

        // Evenly round the circle with a little jitter, so the throw reads as a ring without looking counted.
        private static Vector3 Outward(int index, int count)
        {
            float angle = (index + Random.Range(-0.35f, 0.35f)) * Mathf.PI * 2f / Mathf.Max(1, count);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        private void Throw(ParticleSystem system, Vector3 outward, float from, float height, float speed, float lift, float life, float size, Color colour)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = _centre + outward * from + Vector3.up * height,
                velocity = outward * speed + Vector3.up * lift,
                startLifetime = life,
                startSize = size,
                startColor = colour,
                rotation = Random.Range(0f, 360f),
                applyShapeToPosition = false,
            };
            system.Emit(emit, 1);
        }

        private int Budget(int count, int max) => BudgetFor(count, _quality.ParticleScale, max);

        /// <summary>How many of a part's particles a profile plays: its share of the count, at least one, at most the pool.</summary>
        public static int BudgetFor(int count, float particleScale, int max) => count <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(count * particleScale), 1, max);

        private void Build()
        {
            _block = new MaterialPropertyBlock();
            _glow = Card("Glow", GlowDecal.Quad(), GlowHeight);
            Material radial = GlowDecal.RadialMaterial(Color.white);
            if (radial != null)
            {
                // An own copy: the pool's colour is animated, and the shared material is every other pool's too.
                _glowMaterial = new Material(radial) { name = "Shockwave_Glow" };
                _glow.sharedMaterial = _glowMaterial;
            }

            _echo = Card("EchoRing", VfxQuad.Centred, EchoHeight);
            _ring = Card("Ring", VfxQuad.Centred, RingHeight);
            _shock = Card("Refraction", VfxQuad.Centred, ShockHeight);

            // Unlit vapour puffs from the kit (they swell as they rise): six-way dust is lit by the scene and all but vanishes at night.
            _dust = ParticleKit.UseSmoke(ParticleKit.Create(transform, "Dust", Vector3.zero, Color.white, 0.5f, 1f, 0.4f, 0.7f, 0f, 0f, DustRise, MaxDust));
            ParticleSystem.ColorOverLifetimeModule thin = _dust.colorOverLifetime;
            thin.color = VfxParticles.Fade(DustFadeIn, DustFadeFrom);

            _flash = VfxParticles.Create(transform, "Flash", MaxFlashes);

            _sparks = VfxParticles.Create(transform, "Sparks", MaxSparks);
            var sparkRenderer = _sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            sparkRenderer.lengthScale = 2f;
            sparkRenderer.velocityScale = 0.06f;
            ParticleSystem.MainModule sparks = _sparks.main;
            sparks.gravityModifier = SparkGravity;

            _shards = VfxParticles.Create(transform, "Shards", MaxShards);
            ParticleSystem.MainModule shards = _shards.main;
            shards.gravityModifier = ShardGravity;
            ParticleSystem.RotationOverLifetimeModule spin = _shards.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            // Charged fragments, not rubble: they leave glowing in the wave's colour and dim as they fall, so they read on dark
            // ground (a shard that cools to grey is invisible at night) and are gone before they would lie about.
            ParticleSystem.ColorOverLifetimeModule cool = _shards.colorOverLifetime;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f, 0.8f, 0.8f), 0.5f), new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            cool.color = gradient;

            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up * LightHeight;
            _light = lightObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.shadows = LightShadows.None;
            _light.renderMode = LightRenderMode.ForcePixel;
            _light.enabled = false;
            enabled = false;
        }

        // A flat card on the ground, hidden until its part plays.
        private MeshRenderer Card(string cardName, Mesh mesh, float height)
        {
            MeshRenderer card = VfxQuad.Create(transform, cardName, mesh);
            card.transform.localPosition = new Vector3(0f, height, 0f);
            card.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return card;
        }
    }
}
