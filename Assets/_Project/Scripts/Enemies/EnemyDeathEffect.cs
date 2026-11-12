using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Enemies
{
    /// <summary>
    /// One dying creature, played in the style of its species. Shatter: the parts fold, fly apart, tumble, hit the
    /// ground and sink away while ichor splashes. Crash: a flyer keeps its shape, tumbles out of the air and, on
    /// landing, dissolves into a puff of its element. Collapse: the body topples over as one, lies still and fades
    /// into the ground in a pool of goo. Burst: a flash and a shower of sparks, nothing solid left. Pure view code,
    /// pooled by <see cref="EnemyDeathEffectPool"/>.
    /// </summary>
    public sealed class EnemyDeathEffect : MonoBehaviour
    {
        /// <summary>A copy of one rendered piece of the creature at the moment it died.</summary>
        public readonly struct Part
        {
            public readonly Mesh Mesh;
            public readonly Material[] Materials;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;

            /// <summary>The pose the part folds into while the creature collapses, before the pieces fly apart.</summary>
            public readonly Quaternion Curled;

            public Part(Mesh mesh, Material[] materials, Vector3 position, Quaternion rotation, Vector3 scale, Quaternion curled)
            {
                Mesh = mesh;
                Materials = materials;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                Curled = curled;
            }
        }

        /// <summary>Everything one death needs beyond the parts: where, how big, which way and in what colours.</summary>
        public struct Request
        {
            public Vector3 Centre;
            public Vector3 Momentum;
            public Color Color;
            public Color Blood;
            public float Size;
            public float GroundY;
            public DeathStyle Style;

            /// <summary>For a modelled creature: how many of its armour parts had come off, so the corpse lies without them.</summary>
            public int Shed;

            /// <summary>For a modelled creature: its clip player, whose pose the corpse falls out of (null starts from the idle).</summary>
            public ModelClipPlayer Pose;
        }

        private const float CollapseSeconds = 0.26f;
        private const float CollapseSink = 0.07f;
        private const float Duration = 1.15f;
        private const float ShrinkStart = 0.6f;
        private const float LaunchSpeed = 2.4f;
        private const float LaunchUp = 2.8f;
        private const float Gravity = 9.5f;
        private const float SpinDegrees = 520f;
        private const float Bounce = 0.25f;
        private const float GroundFriction = 0.35f;
        private const float CrashSpin = 240f;
        private const float CrashDissolveSeconds = 0.55f;
        private const float CrashRestSeconds = 0.15f;
        // A collapse: the knees give way first (a short sink and lean), then the body falls faster and faster,
        // bounces once off the ground and lies still.
        private const float BuckleSeconds = 0.28f;
        private const float BuckleSink = 0.12f;
        private const float BuckleLeanDegrees = 14f;
        private const float ToppleSeconds = 0.42f;
        private const float ToppleDegrees = 88f;
        private const float BounceDegrees = 8f;
        private const float BounceSeconds = 0.24f;
        private const float RestSeconds = 0.9f;
        private const float FadeSeconds = 0.7f;
        private const float BurstShrinkSeconds = 0.18f;
        private const float BurstLaunch = 6f;
        private const int SplashBase = 14;
        private const int SplashPerSize = 10;
        private const int MaxSplash = 60;
        private const int PuffBase = 22;
        private const int PuffPerSize = 12;
        private const int BurstSparks = 48;
        private const float SplashSpeedMin = 1.5f;
        private const float SplashSpeedMax = 3.5f;
        private const float SplashLifeMin = 0.45f;
        private const float SplashLifeMax = 0.9f;
        private const float SplashSizeMin = 0.05f;
        private const float SplashSizeMax = 0.13f;
        private const float SplashBrightness = 1.7f;
        private const float SplashGravity = 1.6f;
        private const float PoolRadius = 0.45f;
        private const float PoolBrightness = 0.9f;

        private readonly List<Transform> _slots = new List<Transform>();
        private readonly List<MeshFilter> _filters = new List<MeshFilter>();
        private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();

        private Vector3[] _velocities = new Vector3[8];
        private Vector3[] _spins = new Vector3[8];
        private Vector3[] _baseScales = new Vector3[8];
        private Vector3[] _restPositions = new Vector3[8];
        private Quaternion[] _restRotations = new Quaternion[8];
        private Quaternion[] _curledRotations = new Quaternion[8];
        private Request _request;
        private Action<EnemyDeathEffect> _release;
        private ParticleSystem _splash;
        private ParticleSystem _puff;
        private Transform _pool;
        private Material _poolMaterial;
        // The pool's own flat scale (the painted pool lies flat at (d, 1, d), the plain quad is turned up at (d, d, 1)).
        private Vector3 _poolScale;
        private int _active;
        private float _age;
        private Vector3 _groupVelocity;
        private Vector3 _groupSpin;
        private bool _landed;
        private float _landedAt;
        private Vector3 _toppleAxis;
        private float _groupHalfWidth;

        public void Play(IReadOnlyList<Part> parts, Request request, Action<EnemyDeathEffect> release)
        {
            EnsureSlots(parts.Count);
            _request = request;
            _release = release;
            _active = parts.Count;
            _age = 0f;
            _landed = false;
            bool rigid = request.Style == DeathStyle.Crash || request.Style == DeathStyle.Collapse;

            // Rigid styles move the whole body as one, so the root sits at the centre and the parts hang off it.
            transform.SetPositionAndRotation(rigid ? request.Centre : Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
            _groupHalfWidth = 0f;

            for (int i = 0; i < parts.Count; i++)
            {
                Part part = parts[i];
                Transform slot = _slots[i];
                slot.gameObject.SetActive(true);
                _filters[i].sharedMesh = part.Mesh;
                _renderers[i].sharedMaterials = part.Materials;
                slot.SetPositionAndRotation(part.Position, part.Rotation);
                slot.localScale = part.Scale;
                _baseScales[i] = part.Scale;
                _restPositions[i] = part.Position;
                _restRotations[i] = part.Rotation;
                _curledRotations[i] = part.Curled;
                _groupHalfWidth = Mathf.Max(_groupHalfWidth, Mathf.Abs(part.Position.x - request.Centre.x), Mathf.Abs(part.Position.z - request.Centre.z));

                Vector3 away = part.Position - request.Centre;
                away.y = 0f;
                Vector3 outward = away.sqrMagnitude > 0.0001f ? away.normalized : UnityEngine.Random.insideUnitSphere;
                outward.y = 0f;
                float launch = request.Style == DeathStyle.Burst ? BurstLaunch : LaunchSpeed;
                _velocities[i] = outward * (launch * UnityEngine.Random.Range(0.4f, 1f)) + Vector3.up * (LaunchUp * UnityEngine.Random.Range(0.5f, 1f));
                _spins[i] = UnityEngine.Random.onUnitSphere * (SpinDegrees * UnityEngine.Random.Range(0.5f, 1f));
            }

            for (int i = parts.Count; i < _slots.Count; i++)
            {
                _slots[i].gameObject.SetActive(false);
            }

            switch (request.Style)
            {
                case DeathStyle.Crash:
                    _groupVelocity = request.Momentum + Vector3.up * 0.5f;
                    _groupSpin = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized * CrashSpin;
                    break;
                case DeathStyle.Collapse:
                    Vector3 side = Vector3.Cross(Vector3.up, request.Momentum.sqrMagnitude > 0.001f ? request.Momentum.normalized : Vector3.forward);
                    _toppleAxis = UnityEngine.Random.value < 0.5f ? side : -side;
                    Splash(request.Centre, request.Blood, request.Size * 0.6f);
                    break;
                case DeathStyle.Burst:
                    Sparks(request.Centre, request.Color, request.Size);
                    break;
                default:
                    Splash(request.Centre, request.Blood, request.Size);
                    break;
            }

            if (_pool != null)
            {
                _pool.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            switch (_request.Style)
            {
                case DeathStyle.Crash:
                    UpdateCrash();
                    break;
                case DeathStyle.Collapse:
                    UpdateCollapse();
                    break;
                case DeathStyle.Burst:
                    UpdateBurst();
                    break;
                default:
                    UpdateShatter();
                    break;
            }
        }

        private void UpdateShatter()
        {
            if (_age < CollapseSeconds)
            {
                Fold(_age / CollapseSeconds);
                return;
            }

            float t = Mathf.Clamp01((_age - CollapseSeconds) / Duration);
            float shrink = t > ShrinkStart ? 1f - (t - ShrinkStart) / (1f - ShrinkStart) : 1f;
            Tumble(shrink);
            if (t >= 1f)
            {
                _release(this);
            }
        }

        // Everything flies off at once and is gone in a blink; the sparks carry the moment.
        private void UpdateBurst()
        {
            float t = Mathf.Clamp01(_age / BurstShrinkSeconds);
            Tumble(1f - t);
            if (t >= 1f)
            {
                _release(this);
            }
        }

        // The body tumbles as one until it meets the ground, rests a moment, then dissolves into its element.
        private void UpdateCrash()
        {
            if (!_landed)
            {
                _groupVelocity.y -= Gravity * Time.deltaTime;
                transform.position += _groupVelocity * Time.deltaTime;
                transform.Rotate(_groupSpin * Time.deltaTime, Space.World);
                if (transform.position.y <= _request.GroundY + _request.Size * 0.12f)
                {
                    _landed = true;
                    _landedAt = _age;
                    Puff(transform.position, _request.Blood, _request.Size);
                }

                return;
            }

            float since = _age - _landedAt;
            if (since < CrashRestSeconds)
            {
                return;
            }

            float t = Mathf.Clamp01((since - CrashRestSeconds) / CrashDissolveSeconds);
            transform.localScale = Vector3.one * (1f - t);
            transform.position += Vector3.down * (Time.deltaTime * 0.3f * _request.Size);
            if (t >= 1f)
            {
                _release(this);
            }
        }

        // The body buckles at the knees, topples over about its own foot with the weight of a fall, bounces once,
        // lies in a spreading pool, then sinks and fades.
        private void UpdateCollapse()
        {
            float buckle = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / BuckleSeconds));
            float topple = Mathf.Clamp01((_age - BuckleSeconds) / ToppleSeconds);
            float angle = BuckleLeanDegrees * buckle + (ToppleDegrees - BuckleLeanDegrees) * topple * topple;
            float since = _age - BuckleSeconds - ToppleSeconds;
            if (since > 0f && since < BounceSeconds)
            {
                angle -= Mathf.Sin(since / BounceSeconds * Mathf.PI) * BounceDegrees;
            }

            Vector3 foot = new Vector3(_request.Centre.x, _request.GroundY, _request.Centre.z);
            transform.position = _request.Centre + Vector3.down * (BuckleSink * _request.Size * buckle);
            transform.rotation = Quaternion.identity;
            transform.RotateAround(foot, _toppleAxis, angle);
            if (!_landed && topple >= 1f)
            {
                _landed = true;
                ShowPool(foot, _request.Blood, _request.Size);
                Splash(_request.Centre, _request.Blood, _request.Size * 0.4f);
                Puff(foot, _request.Blood, _request.Size);
            }

            float rest = _age - BuckleSeconds - ToppleSeconds - RestSeconds;
            if (rest <= 0f)
            {
                return;
            }

            float t = Mathf.Clamp01(rest / FadeSeconds);
            transform.position += Vector3.down * (t * _request.Size * 0.6f);
            transform.localScale = Vector3.one * (1f - t * 0.5f);
            if (_poolMaterial != null)
            {
                _poolMaterial.SetColor("_EmissionColor", _request.Blood * (PoolBrightness * (1f - t)));
            }

            if (t >= 1f)
            {
                _pool.gameObject.SetActive(false);
                _release(this);
            }
        }

        // The legs fold in and the body sags for a moment, the way an insect dies, before everything flies apart.
        private void Fold(float t)
        {
            float ease = Mathf.SmoothStep(0f, 1f, t);
            for (int i = 0; i < _active; i++)
            {
                _slots[i].SetPositionAndRotation(
                    _restPositions[i] + Vector3.down * (CollapseSink * _request.Size * ease),
                    Quaternion.Slerp(_restRotations[i], _curledRotations[i], ease));
            }
        }

        private void Tumble(float shrink)
        {
            for (int i = 0; i < _active; i++)
            {
                Transform slot = _slots[i];
                Vector3 velocity = _velocities[i];
                velocity.y -= Gravity * Time.deltaTime;
                Vector3 position = slot.position + velocity * Time.deltaTime;

                if (position.y < _request.GroundY)
                {
                    position.y = _request.GroundY;
                    velocity.y = -velocity.y * Bounce;
                    velocity.x *= GroundFriction;
                    velocity.z *= GroundFriction;
                    _spins[i] *= GroundFriction;
                }

                _velocities[i] = velocity;
                slot.position = position;
                slot.Rotate(_spins[i] * Time.deltaTime, Space.World);
                slot.localScale = _baseScales[i] * shrink;
            }
        }

        private void EnsureSlots(int count)
        {
            while (_slots.Count < count)
            {
                var go = new GameObject("Part");
                go.transform.SetParent(transform, false);
                _filters.Add(go.AddComponent<MeshFilter>());
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                _renderers.Add(renderer);
                _slots.Add(go.transform);
            }

            if (_velocities.Length < count)
            {
                Array.Resize(ref _velocities, count);
                Array.Resize(ref _spins, count);
                Array.Resize(ref _baseScales, count);
                Array.Resize(ref _restPositions, count);
                Array.Resize(ref _restRotations, count);
                Array.Resize(ref _curledRotations, count);
            }
        }

        // A burst of glowing droplets that arc out and fall.
        private void Splash(Vector3 centre, Color color, float size)
        {
            if (_splash == null)
            {
                _splash = BuildBurst("Splash", SplashSpeedMin, SplashSpeedMax, SplashGravity, false);
            }

            Emit(_splash, centre, color, Mathf.Min(MaxSplash, SplashBase + Mathf.RoundToInt(SplashPerSize * size)));
        }

        // A soft cloud that rises and swells: steam, smoke, blown sand, dying embers.
        private void Puff(Vector3 centre, Color color, float size)
        {
            if (_puff == null)
            {
                _puff = BuildBurst("Puff", 0.3f, 1.2f, -0.15f, true);
            }

            Emit(_puff, centre, color, Mathf.Min(MaxSplash, PuffBase + Mathf.RoundToInt(PuffPerSize * size)));
        }

        private void Sparks(Vector3 centre, Color color, float size)
        {
            if (_splash == null)
            {
                _splash = BuildBurst("Splash", SplashSpeedMin, SplashSpeedMax, SplashGravity, false);
            }

            Emit(_splash, centre, color, Mathf.Min(MaxSplash, BurstSparks + Mathf.RoundToInt(SplashPerSize * size)));
        }

        private static void Emit(ParticleSystem system, Vector3 centre, Color color, int count)
        {
            system.transform.position = centre;
            ParticleSystem.MainModule main = system.main;
            Color glow = color * SplashBrightness;
            glow.a = 1f;
            main.startColor = glow;
            system.Emit(count);
        }

        private void ShowPool(Vector3 at, Color color, float size)
        {
            if (_pool == null)
            {
                GameObject go = GlowDecal.PlacePool(transform.parent, color, PoolRadius);
                _pool = go.transform;
                _poolScale = _pool.localScale;
                _poolMaterial = new Material(go.GetComponent<MeshRenderer>().sharedMaterial);
                go.GetComponent<MeshRenderer>().sharedMaterial = _poolMaterial;
            }

            _pool.gameObject.SetActive(true);
            _pool.position = new Vector3(at.x, at.y + 0.03f, at.z);
            _pool.localScale = _poolScale * size;
            _poolMaterial.SetColor("_EmissionColor", color * PoolBrightness);
        }

        private ParticleSystem BuildBurst(string name, float speedMin, float speedMax, float gravity, bool soft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform.parent, false);
            var system = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(soft ? 0.7f : SplashLifeMin, soft ? 1.4f : SplashLifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(soft ? 0.12f : SplashSizeMin, soft ? 0.3f : SplashSizeMax);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = MaxSplash * 2;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = soft ? 0.25f : 0.15f;

            ParticleSystem.SizeOverLifetimeModule sizeOverLife = system.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, soft ? 2.2f : 0.2f));

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(soft ? 0.7f : 1f, 0f), new GradientAlphaKey(soft ? 0.4f : 1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            go.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
            // The soft puff is smoke over the body; the splash is the creature's ichor.
            return soft ? ParticleKit.UseSmoke(system) : ParticleKit.UseCell(system, MarkerArt.ParticleCell.Drip);
        }
    }
}
