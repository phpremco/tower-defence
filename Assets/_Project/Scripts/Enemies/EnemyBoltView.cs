using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// A glowing bolt an enemy spits at a structure: an element shot, a spray of acid. The damage has already
    /// landed when the bolt is fired; this only draws it flying there in an arc and bursting on arrival. Each view
    /// owns a few and reuses them.
    /// </summary>
    public sealed class EnemyBoltView : MonoBehaviour
    {
        private const float BaseSpeed = 7f;
        private const float ArcHeight = 0.35f;
        private const float CoreSize = 0.12f;
        private const float HaloSize = 0.26f;
        private const float HaloAlpha = 0.35f;
        private const float CoreEmission = 3.5f;
        private const int Sparks = 10;
        private const float MinHeading = 0.000001f;
        // The wake a lobbed bolt draws behind it (a boss's volley): as long as this many seconds of flight, as wide as the halo.
        private const float TrailSeconds = 0.35f;
        private const float TrailWidth = 0.22f;
        private const float TrailAlpha = 0.7f;
        private const float TrailEmission = 2.5f;
        // Where a lobbed bolt lands it bursts: a shell of its colour that swells over the gun and fades, and three times the sparks.
        private const float BurstSeconds = 0.35f;
        private const float BurstStartSize = 0.5f;
        private const float BurstEndSize = 1.45f;
        private const float BurstAlpha = 0.5f;
        private const float BurstEmission = 4f;
        private const int LobSparkFactor = 3;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Transform _core;
        private Transform _halo;
        private ParticleSystem _sparks;
        private Vector3 _from;
        private Vector3 _to;
        private float _seconds;
        private float _age = float.PositiveInfinity;
        private float _size = 1f;
        private float _arc = ArcHeight;
        private bool _painted;
        private TrailRenderer _trail;
        private Transform _burst;
        private Material _burstMaterial;
        private Color _colour;
        private float _burstAge = float.PositiveInfinity;
        private bool _lobbed;

        public bool InFlight => _age < _seconds;

        public static EnemyBoltView Create(Transform parent, Color color)
        {
            var go = new GameObject("EnemyBolt");
            go.transform.SetParent(parent, false);
            var bolt = go.AddComponent<EnemyBoltView>();
            MarkerArt art = MarkerArt.Active;
            if (art != null && art.Has(MarkerKind.EnemyBolt) && art.Has(MarkerKind.EnemyBoltGlow))
            {
                // README round 25b: a veined glob with a wake and barbs, inside a warm glow shell, flying head first.
                var block = new MaterialPropertyBlock();
                MeshRenderer core = art.Place(MarkerKind.EnemyBolt, go.transform);
                MeshRenderer glow = art.Place(MarkerKind.EnemyBoltGlow, go.transform);
                MarkerArt.SetTint(core, block, color);
                MarkerArt.SetTint(glow, block, color);
                bolt._core = core.transform;
                bolt._halo = glow.transform;
                bolt._painted = true;
            }
            else
            {
                bolt._core = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, Color.Lerp(color, Color.white, 0.5f), go.transform, Vector3.zero, Vector3.one * CoreSize, CoreEmission).transform;
                GameObject halo = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, color, go.transform, Vector3.zero, Vector3.one * HaloSize);
                halo.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(color, HaloAlpha, 1.5f);
                bolt._halo = halo.transform;
            }

            Color spark = color * 2f;
            spark.a = 1f;
            bolt._colour = color;
            bolt._sparks = ParticleKit.Create(parent, "BoltSparks", Vector3.zero, spark, 0.25f, 0.5f, 0.03f, 0.07f, 0.8f, 2.4f, 0.8f, Sparks * 3, 0.1f);
            go.SetActive(false);
            return bolt;
        }

        public void Fire(Vector3 from, Vector3 to, float size)
        {
            _lobbed = false;
            _arc = ArcHeight * size;
            Launch(from, to, size, Mathf.Max(0.12f, Vector3.Distance(from, to) / BaseSpeed));
        }

        /// <summary>
        /// A lobbed bolt (a boss's volley, round 88c): in the air for exactly <paramref name="seconds"/>, rising
        /// <paramref name="arc"/> world units on the way and drawing a wake, so the eye can follow it from the boss to the
        /// gun it lands on.
        /// </summary>
        public void Lob(Vector3 from, Vector3 to, float size, float seconds, float arc)
        {
            _lobbed = true;
            _arc = arc;
            if (_trail == null)
            {
                _trail = BuildTrail();
                BuildBurst();
            }

            Launch(from, to, size, Mathf.Max(0.12f, seconds));
            _trail.Clear();
            _trail.emitting = true;
        }

        private void Launch(Vector3 from, Vector3 to, float size, float seconds)
        {
            _from = from;
            _to = to;
            _size = size;
            _seconds = seconds;
            _age = 0f;
            transform.localScale = Vector3.one * size;
            transform.position = from;
            gameObject.SetActive(true);
            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = false;
            }
        }

        // The shell lives beside the bolt (the bolt itself is switched off when it lands) and is driven by BoltBurst below.
        private void BuildBurst()
        {
            _burstMaterial = new Material(PlaceholderVisuals.TransparentMaterialFor(_colour, BurstAlpha, BurstEmission));
            GameObject shell = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, _colour, transform.parent, Vector3.zero, Vector3.one * BurstStartSize);
            shell.name = "BoltBurst";
            var renderer = shell.GetComponent<Renderer>();
            renderer.sharedMaterial = _burstMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _burst = shell.transform;
            shell.AddComponent<BoltBurst>().Initialize(this);
            shell.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_burst != null)
            {
                Destroy(_burst.gameObject);
                Destroy(_burstMaterial);
            }
        }

        // Called every frame by the shell while it shows: it swells and fades in the bolt's colour.
        internal void DriveBurst(float deltaTime)
        {
            _burstAge += deltaTime;
            float t = Mathf.Clamp01(_burstAge / BurstSeconds);
            if (t >= 1f)
            {
                _burst.gameObject.SetActive(false);
                return;
            }

            float fade = (1f - t) * (1f - t);
            _burst.localScale = Vector3.one * Mathf.Lerp(BurstStartSize, BurstEndSize, Mathf.Sqrt(t));
            _burstMaterial.SetColor(BaseColorId, new Color(_colour.r, _colour.g, _colour.b, BurstAlpha * fade));
            _burstMaterial.SetColor(EmissionColorId, _colour * (BurstEmission * fade));
        }

        private TrailRenderer BuildTrail()
        {
            var trail = gameObject.AddComponent<TrailRenderer>();
            Color colour = _colour;
            trail.sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(colour, TrailAlpha, TrailEmission);
            trail.time = TrailSeconds;
            trail.minVertexDistance = 0.05f;
            // No hard edge at either end (LineKit, round 51): it grows out of the bolt and thins to nothing behind it.
            LineKit.ShapeEnds(trail);
            trail.widthMultiplier = TrailWidth;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            return trail;
        }

        /// <summary>Takes the bolt out of the air, with its sparks, when its owner leaves the field.</summary>
        public void Cancel()
        {
            _age = float.PositiveInfinity;
            _sparks.Clear(true);
            gameObject.SetActive(false);
            if (_burst != null)
            {
                _burst.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _seconds);
            Vector3 at = Vector3.Lerp(_from, _to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * _arc);
            Vector3 heading = at - transform.position;
            transform.position = at;
            float pulse = 1f + Mathf.Sin(_age * 30f) * 0.15f;
            if (_painted)
            {
                if (heading.sqrMagnitude > MinHeading)
                {
                    transform.rotation = Quaternion.LookRotation(heading);
                }

                _halo.localScale = Vector3.one * pulse;
                _core.localRotation = Quaternion.Euler(0f, 0f, _age * 400f);
            }
            else
            {
                _halo.localScale = Vector3.one * (HaloSize * pulse);
                _core.localRotation = Quaternion.Euler(0f, _age * 400f, 0f);
            }
            if (t >= 1f)
            {
                _sparks.transform.position = _to;
                _sparks.Emit(_lobbed ? Sparks * LobSparkFactor : Sparks);
                if (_lobbed)
                {
                    _burstAge = 0f;
                    _burst.position = _to;
                    _burst.gameObject.SetActive(true);
                    DriveBurst(0f);
                }

                gameObject.SetActive(false);
            }
        }
    }
}
