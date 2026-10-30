using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Rings of refraction on the ground where things hit: explosions, the hero's blade, boss pulses and wrecked
    /// towers. A small pool of flat quads with the ripple shader; each runs outward for a third of a second. Pure
    /// view, driven by the level's events; silent when the shader or the opaque texture is missing.
    /// </summary>
    public sealed class RippleEffects : MonoBehaviour
    {
        private const string ShaderName = "Game/ImpactRipple";
        private const int PoolSize = 12;
        private const float Seconds = 0.35f;
        private const float Lift = 0.06f;
        private const float ExplosionRadiusScale = 1.6f;
        private const float MinRadius = 0.9f;
        private const float StrikeRadius = 1.3f;
        private const float WreckRadius = 2.2f;
        private const float BossPulseScale = 1.1f;
        private const float Strength = 0.035f;
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");

        private LevelContext _context;
        private Transform[] _quads;
        private MeshRenderer[] _renderers;
        private float[] _ages;
        private MaterialPropertyBlock _block;
        private int _next;

        public void Initialize(LevelContext context)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"Ripple shader '{ShaderName}' not found; impact ripples stay off.", this);
                enabled = false;
                return;
            }

            _context = context;
            _block = new MaterialPropertyBlock();
            var material = new Material(shader);
            _quads = new Transform[PoolSize];
            _renderers = new MeshRenderer[PoolSize];
            _ages = new float[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Ripple";
                Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(transform, false);
                quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                quad.SetActive(false);
                _quads[i] = quad.transform;
                _renderers[i] = renderer;
                _ages[i] = Seconds;
            }

            context.Projectiles.Exploded += OnExploded;
            context.BossPulsed.Subscribe(OnBossPulsed);
            context.StructureWrecked.Subscribe(OnWrecked);
            if (context.Hero != null)
            {
                context.Hero.Struck += OnStruck;
            }
        }

        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _context.Projectiles.Exploded -= OnExploded;
            _context.BossPulsed.Unsubscribe(OnBossPulsed);
            _context.StructureWrecked.Unsubscribe(OnWrecked);
            if (_context.Hero != null)
            {
                _context.Hero.Struck -= OnStruck;
            }
        }

        /// <summary>Starts a ripple of the given radius at a point; the oldest ripple is reused when all are busy.</summary>
        public void Play(Vector3 position, float radius)
        {
            int index = _next;
            _next = (_next + 1) % PoolSize;
            _ages[index] = 0f;
            Transform quad = _quads[index];
            quad.position = new Vector3(position.x, position.y + Lift, position.z);
            quad.localScale = Vector3.one * (Mathf.Max(MinRadius, radius) * 2f);
            quad.gameObject.SetActive(true);
        }

        private void Update()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                if (_ages[i] >= Seconds)
                {
                    continue;
                }

                _ages[i] += Time.deltaTime;
                float progress = Mathf.Clamp01(_ages[i] / Seconds);
                if (progress >= 1f)
                {
                    _quads[i].gameObject.SetActive(false);
                    continue;
                }

                _block.SetFloat(ProgressId, progress);
                _block.SetFloat(StrengthId, Strength);
                _renderers[i].SetPropertyBlock(_block);
            }
        }

        private void OnExploded(Vector3 position, float radius) => Play(position, radius * ExplosionRadiusScale);

        private void OnBossPulsed(Vector3 centre) => Play(centre, BossPulseScale * 3f);

        private void OnWrecked(Vector3 position) => Play(position, WreckRadius);

        // A blade landing bends the air; a bolt from the gun is too small a thing for a ripple.
        private void OnStruck(Vector3 position, Game.Hero.HeroWeaponDefinition weapon)
        {
            if (weapon.Kind == Game.Hero.HeroWeaponKind.Melee)
            {
                Play(position, StrikeRadius);
            }
        }
    }
}
