using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Hero
{
    /// <summary>
    /// The look of the commander: an armoured astronaut that walks on its own legs with swinging arms, looks around
    /// while it waits, breathes, fires its thrusters when it moves and fights with whatever weapon rig its
    /// definition names. When its health runs out it staggers, drops the weapon and crashes to the ground with a
    /// shower of sparks, lies smoking with its lights out, and powers back up with a flash when it revives. A tall
    /// beacon floats above it so it can always be found, even behind a tower on raised ground; it turns red while
    /// the hero is down. Reacts to Commander events only.
    /// </summary>
    [RequireComponent(typeof(Commander))]
    public sealed class HeroView : MonoBehaviour
    {
        private const float TurnSpeed = 14f;
        private const float FigureScale = 0.85f;
        private const float IdleSpeed = 2.2f;
        private const float IdleHeight = 0.02f;
        private const float BreathSpeed = 2.2f;
        private const float BreathDepth = 0.015f;
        private const float WalkBobHeight = 0.06f;
        private const float GaitPerUnit = 7.5f;
        private const float LegSwingDegrees = 38f;
        private const float LegSettleSpeed = 8f;
        private const float HeadScanDegrees = 28f;
        private const float HeadScanSpeed = 0.55f;
        private const float HeadNodDegrees = 6f;
        private const float HeadFollowSpeed = 6f;
        private const float WalkLeanDegrees = 8f;
        private const float StaggerSeconds = 0.3f;
        private const float FallSeconds = 0.6f;
        private const float StaggerPitch = -16f;
        private const float StaggerLift = 0.08f;
        private const float FallRoll = 84f;
        private const float FallBounce = 7f;
        private const float LyingHeight = 0.12f;
        private const float HeadDroop = 35f;
        private const float LegSprawl = 22f;
        private const float ArmSprawlPitch = 20f;
        private const float ArmSprawlYaw = 50f;
        private const float RiseSeconds = 0.7f;
        private const int ImpactSparks = 70;
        private const int ImpactDust = 24;
        private const int StaggerSparks = 25;
        private const float LyingSmokeRate = 4f;
        private const float LyingSparkRate = 1.5f;
        private const float VisorFlickerSpeed = 30f;
        private const float RingSeconds = 0.45f;
        private const float RingAlpha = 0.35f;
        private const float RingEmission = 2.5f;
        private const float ReviveRingSeconds = 0.6f;
        private const float ReviveRingRadius = 1.6f;
        private const float BarWidth = 1f;
        private const float BarHeight = 0.09f;
        private const float BarY = 1.8f;
        private const float BeaconBottom = 1.7f;
        private const float BeaconTop = 3f;
        private const float BeaconWidth = 0.07f;
        private const float BeaconAlpha = 0.3f;
        private const float BeaconEmission = 2.5f;
        private const float EmblemSize = 0.22f;
        private const float EmblemEmission = 3f;
        private const float EmblemSpin = 90f;
        private const float EmblemBobHeight = 0.08f;
        private const float EmblemBobSpeed = 1.6f;
        private const float DownBeaconPulse = 3.5f;
        private const float ThrusterRate = 45f;
        private static readonly Color HealthyColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color DamagedColor = new Color(1f, 0.55f, 0.2f);
        private static readonly Color DownColor = new Color(1f, 0.25f, 0.2f);
        private static readonly Color ThrusterColor = new Color(0.5f, 0.9f, 1f);
        private static readonly Color SparkColor = new Color(1f, 0.8f, 0.4f);
        private static readonly Color SmokeColor = new Color(0.25f, 0.24f, 0.24f, 0.4f);
        private static readonly Color DustColor = new Color(0.5f, 0.45f, 0.4f, 0.5f);

        private Commander _hero;
        private HeroModelFactory.Parts _parts;
        private HeroWeaponRig _weapon;
        private Transform _figure;
        private Transform _arm;
        private Transform _shell;
        private Transform _legLeft;
        private Transform _legRight;
        private Transform _head;
        private Transform _emblem;
        private ParticleSystem _thrusters;
        private ParticleSystem _sparks;
        private ParticleSystem _smoke;
        private ParticleSystem _dust;
        private Renderer[] _figureRenderers;
        private Material[] _poweredMaterials;
        private Material[] _deadMaterials;
        private Material _beaconMaterial;
        private Material _beaconDownMaterial;
        private Material _emblemMaterial;
        private Material _emblemDownMaterial;
        private Renderer _beaconRenderer;
        private Renderer _emblemRenderer;
        private Transform _bar;
        private Renderer _barRenderer;
        private Transform _ring;
        private float _ringTime = float.PositiveInfinity;
        private float _ringRadius;
        private float _ringSeconds;
        private float _gait;
        private float _legSwing;
        private float _headYaw;
        private float _headPitch;
        private float _scanPhase;
        private Vector3 _lastPosition;
        private Color _trim;
        private bool _down;
        private float _downTime;
        private bool _impacted;
        private float _rise = float.PositiveInfinity;
        private float _fallYaw;

        public void Initialize(Commander hero)
        {
            _hero = hero;
            _trim = hero.Definition.Color;
            _scanPhase = Random.Range(0f, 10f);
            Build();
            _lastPosition = transform.position;
            _hero.Changed += Refresh;
            _hero.Struck += OnStruck;
            _hero.ShockwaveReleased += OnShockwave;
            _hero.Downed += OnDowned;
            _hero.Revived += OnRevived;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.Changed -= Refresh;
                _hero.Struck -= OnStruck;
                _hero.ShockwaveReleased -= OnShockwave;
                _hero.Downed -= OnDowned;
                _hero.Revived -= OnRevived;
            }
        }

        private void Build()
        {
            var figure = new GameObject("Figure").transform;
            figure.SetParent(transform, false);
            // A little under a cell and a half tall: taller than a tower, but not a giant beside one.
            figure.localScale = Vector3.one * FigureScale;
            _figure = figure;

            _parts = HeroModelFactory.Create();
            _poweredMaterials = HeroModelFactory.CreateMaterials(_trim);
            _deadMaterials = HeroModelFactory.CreateMaterials(_trim, false);

            _shell = Part("Shell", figure, _parts.Body, _poweredMaterials, Vector3.zero, false);
            _arm = Part("Arm", figure, _parts.Arm, _poweredMaterials, _parts.Shoulder, false);
            _legRight = Part("LegR", figure, _parts.Leg, _poweredMaterials, _parts.Hip, false);
            _legLeft = Part("LegL", figure, _parts.Leg, _poweredMaterials, new Vector3(-_parts.Hip.x, _parts.Hip.y, _parts.Hip.z), true);
            _head = Part("Head", figure, _parts.Head, _poweredMaterials, _parts.Neck, false);
            _figureRenderers = new[] { _shell.GetComponent<Renderer>(), _arm.GetComponent<Renderer>(), _legRight.GetComponent<Renderer>(), _legLeft.GetComponent<Renderer>(), _head.GetComponent<Renderer>() };

            _weapon = HeroWeaponRig.For(_hero.Definition.Weapon);
            _weapon.Build(_hero.Definition.Weapon, _parts, figure, _shell, _arm, _trim);

            _thrusters = CreateThrusters(figure, _parts.Thruster);
            _sparks = ParticleKit.Create(_shell, "Sparks", new Vector3(0f, 1f, -0.3f), SparkColor * 2f, 0.3f, 0.7f, 0.025f, 0.06f, 1.5f, 4f, 1.2f, 100, 0.15f);
            _smoke = ParticleKit.Create(_shell, "Smoke", new Vector3(0f, 1f, -0.3f), SmokeColor, 1.2f, 2.2f, 0.2f, 0.4f, 0.3f, 0.6f, -0.12f, 30, 0.15f);
            _dust = ParticleKit.Create(transform, "Dust", new Vector3(0f, 0.1f, 0f), DustColor, 0.5f, 0.9f, 0.2f, 0.4f, 0.8f, 1.8f, -0.05f, 40, 0.4f);

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

            BuildBeacon();
        }

        private static Transform Part(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 localPosition, bool mirrored)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go.transform;
        }

        // A translucent pillar of light with a spinning emblem on top: visible over anything built nearby.
        private void BuildBeacon()
        {
            float height = BeaconTop - BeaconBottom;
            GameObject pillar = PlaceholderVisuals.CreatePrimitive(
                PrimitiveType.Cylinder, _trim, transform, new Vector3(0f, BeaconBottom + height * 0.5f, 0f), new Vector3(BeaconWidth, height * 0.5f, BeaconWidth), BeaconEmission);
            pillar.name = "Beacon";
            _beaconRenderer = pillar.GetComponent<Renderer>();
            _beaconMaterial = PlaceholderVisuals.TransparentMaterialFor(_trim, BeaconAlpha, BeaconEmission);
            _beaconDownMaterial = PlaceholderVisuals.TransparentMaterialFor(DownColor, BeaconAlpha, BeaconEmission);
            _beaconRenderer.sharedMaterial = _beaconMaterial;
            _beaconRenderer.shadowCastingMode = ShadowCastingMode.Off;

            GameObject emblem = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, _trim, transform, new Vector3(0f, BeaconTop, 0f), Vector3.one * EmblemSize, EmblemEmission);
            emblem.name = "Emblem";
            emblem.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            _emblemRenderer = emblem.GetComponent<Renderer>();
            _emblemMaterial = _emblemRenderer.sharedMaterial;
            _emblemDownMaterial = PlaceholderVisuals.MaterialFor(DownColor, EmblemEmission);
            _emblemRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _emblem = emblem.transform;
        }

        private void Refresh()
        {
            float fraction = _hero.Health.Fraction;
            _bar.gameObject.SetActive(!_hero.IsDown && fraction < 1f);
            _bar.localScale = new Vector3(BarWidth * Mathf.Max(0.001f, fraction), BarHeight, BarHeight);
            _barRenderer.sharedMaterial = PlaceholderVisuals.MaterialFor(Color.Lerp(DamagedColor, HealthyColor, fraction), 1.5f);

            // A downed hero sent to another post appears there already lying down.
            if (_hero.IsDown && !_down)
            {
                OnDowned();
            }
        }

        private void OnStruck(Vector3 target)
        {
            if (!_down)
            {
                _weapon.Attack(target);
            }
        }

        private void OnShockwave(Vector3 centre, float radius) => Pulse(_hero.Definition.ShockwaveRadius, _trim, RingSeconds);

        // Going down: the stagger starts at once; the fall, the impact and the powered-down look follow in Update.
        private void OnDowned()
        {
            _down = true;
            _downTime = 0f;
            _impacted = false;
            _rise = float.PositiveInfinity;
            _fallYaw = _figure.localEulerAngles.y;
            _sparks.Emit(StaggerSparks);
            ParticleKit.SetRate(_thrusters, 0f);
            _beaconRenderer.sharedMaterial = _beaconDownMaterial;
            _emblemRenderer.sharedMaterial = _emblemDownMaterial;
        }

        // Back up: a flash of the suit's colour, the lights come on and the figure rolls back onto its feet.
        private void OnRevived()
        {
            _down = false;
            _rise = 0f;
            _weapon.Holster();
            SetMaterials(_poweredMaterials);
            ParticleKit.SetRate(_smoke, 0f);
            ParticleKit.SetRate(_sparks, 0f);
            _beaconRenderer.sharedMaterial = _beaconMaterial;
            _emblemRenderer.sharedMaterial = _emblemMaterial;
            Pulse(ReviveRingRadius, _trim, ReviveRingSeconds);
        }

        private void SetMaterials(Material[] materials)
        {
            for (int i = 0; i < _figureRenderers.Length; i++)
            {
                _figureRenderers[i].sharedMaterials = materials;
            }
        }

        private void Update()
        {
            if (_down)
            {
                Fall();
                Beacon();
                Ring();
                return;
            }

            TurnTowardsFacing();
            Walk();
            Scan();
            HeroWeaponRig.Pose pose = _weapon.Tick(_hero.IsWalking, _legSwing);
            Hold(pose);
            Rise();
            Beacon();
            Ring();
        }

        // The stagger, the fall, the impact and the long lie-down. The figure pivots at its feet, so a roll lays
        // it on its side; the head droops and the limbs sprawl once it is down.
        private void Fall()
        {
            _downTime += Time.deltaTime;
            float stagger = Mathf.Clamp01(_downTime / StaggerSeconds);
            float fall = Mathf.Clamp01((_downTime - StaggerSeconds) / FallSeconds);
            float roll = FallRoll * fall * fall;
            if (fall >= 1f && !_impacted)
            {
                Impact();
            }

            float settle = _impacted ? Mathf.Clamp01((_downTime - StaggerSeconds - FallSeconds) / 0.25f) : 0f;
            float pitch = StaggerPitch * Mathf.Sin(stagger * Mathf.PI) * (1f - fall);
            _figure.localRotation = Quaternion.Euler(pitch, _fallYaw, roll + Mathf.Sin(settle * Mathf.PI) * FallBounce);
            _figure.localPosition = new Vector3(0f, StaggerLift * Mathf.Sin(stagger * Mathf.PI) * (1f - fall) + LyingHeight * fall, 0f);
            _head.localRotation = Quaternion.Euler(HeadDroop * settle, 0f, 0f);
            _legRight.localRotation = Quaternion.Euler(LegSprawl * settle, 0f, 0f);
            _legLeft.localRotation = Quaternion.Euler(-LegSprawl * 0.5f * settle, 0f, 0f);
            _arm.localRotation = Quaternion.Euler(ArmSprawlPitch * settle, ArmSprawlYaw * settle, 0f);
            _shell.localRotation = Quaternion.identity;

            // The visor stutters out during the fall and the suit goes dark on impact.
            if (!_impacted)
            {
                bool lit = Mathf.PerlinNoise(Time.time * VisorFlickerSpeed, 0.3f) > 0.45f * fall;
                SetMaterials(lit ? _poweredMaterials : _deadMaterials);
            }
        }

        private void Impact()
        {
            _impacted = true;
            _weapon.Drop(transform.position.y);
            _sparks.Emit(ImpactSparks);
            _dust.Emit(ImpactDust);
            SetMaterials(_deadMaterials);
            ParticleKit.SetRate(_smoke, LyingSmokeRate);
            ParticleKit.SetRate(_sparks, LyingSparkRate);
        }

        // Standing back up after a revive: the roll eases out and the limbs return to their rest.
        private void Rise()
        {
            if (_rise >= RiseSeconds)
            {
                return;
            }

            _rise += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_rise / RiseSeconds);
            t = t * t;
            _figure.localRotation = Quaternion.Euler(0f, _figure.localEulerAngles.y, FallRoll * t);
            _figure.localPosition = new Vector3(0f, LyingHeight * t, 0f);
            _head.localRotation = Quaternion.Euler(HeadDroop * t, 0f, 0f);
        }

        // Breath, bob, lunge and lean, and the arm as the weapon rig wants it.
        private void Hold(HeroWeaponRig.Pose pose)
        {
            Vector3 forward = _hero.Facing.sqrMagnitude > 0.0001f ? _hero.Facing.normalized : Vector3.forward;
            float bob = _hero.IsWalking ? Mathf.Abs(Mathf.Sin(_gait)) * WalkBobHeight : Mathf.Sin(Time.time * IdleSpeed) * IdleHeight;
            if (_rise >= RiseSeconds)
            {
                _figure.localPosition = forward * pose.Lunge + Vector3.up * bob;
            }

            _shell.localScale = new Vector3(1f, 1f + BreathDepth * Mathf.Sin(Time.time * BreathSpeed), 1f);
            _shell.localRotation = Quaternion.Euler(pose.Lean + (_hero.IsWalking ? WalkLeanDegrees : 0f), pose.Twist, 0f);
            _arm.localRotation = Quaternion.Euler(pose.ArmPitch, pose.ArmYaw, 0f);
        }

        // Legs swing with the distance walked and settle when the commander stops; thrusters burn while moving.
        private void Walk()
        {
            Vector3 position = transform.position;
            float moved = (position - _lastPosition).magnitude;
            _lastPosition = position;
            bool walking = _hero.IsWalking;
            _gait += moved * GaitPerUnit;

            float wanted = walking ? Mathf.Sin(_gait) * LegSwingDegrees : 0f;
            _legSwing = Mathf.Lerp(_legSwing, wanted, 1f - Mathf.Exp(-LegSettleSpeed * Time.deltaTime));
            _legRight.localRotation = Quaternion.Euler(_legSwing, 0f, 0f);
            _legLeft.localRotation = Quaternion.Euler(-_legSwing, 0f, 0f);
            ParticleKit.SetRate(_thrusters, walking ? ThrusterRate : 0f);
        }

        // Waiting at a post, the head scans the road; walking or fighting it looks straight ahead.
        private void Scan()
        {
            if (_rise < RiseSeconds)
            {
                return;
            }

            bool busy = _hero.IsWalking || _weapon.Drawn;
            float yaw = busy ? 0f : Mathf.Sin(Time.time * HeadScanSpeed + _scanPhase) * HeadScanDegrees;
            float pitch = busy ? 0f : Mathf.Sin(Time.time * HeadScanSpeed * 1.7f + _scanPhase) * HeadNodDegrees;
            float k = 1f - Mathf.Exp(-HeadFollowSpeed * Time.deltaTime);
            _headYaw = Mathf.Lerp(_headYaw, yaw, k);
            _headPitch = Mathf.Lerp(_headPitch, pitch, k);
            _head.localRotation = Quaternion.Euler(_headPitch, _headYaw, 0f);
        }

        private void TurnTowardsFacing()
        {
            Vector3 facing = _hero.Facing;
            if (facing.sqrMagnitude < 0.0001f || _rise < RiseSeconds)
            {
                return;
            }

            Quaternion wanted = Quaternion.LookRotation(facing);
            _figure.rotation = Quaternion.Slerp(_figure.rotation, wanted, 1f - Mathf.Exp(-TurnSpeed * Time.deltaTime));
        }

        // Short bright puffs out of the pack nozzles, pushed down and back, only while walking.
        private ParticleSystem CreateThrusters(Transform figure, Vector3 nozzle)
        {
            ParticleSystem system = ParticleKit.Create(figure, "Thrusters", nozzle, ThrusterColor * 2f, 0.15f, 0.3f, 0.05f, 0.1f, 1.5f, 2.5f, 0f, 40, 0.08f);
            system.transform.localRotation = Quaternion.Euler(60f, 180f, 0f);
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.08f;
            return system;
        }

        private void Pulse(float radius, Color colour, float seconds)
        {
            _ring.GetComponent<Renderer>().sharedMaterial = PlaceholderVisuals.TransparentMaterialFor(colour, RingAlpha, RingEmission);
            _ring.gameObject.SetActive(true);
            _ringTime = 0f;
            _ringRadius = radius;
            _ringSeconds = seconds;
            _ring.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        }

        private void Ring()
        {
            if (_ringTime > _ringSeconds)
            {
                return;
            }

            _ringTime += Time.deltaTime;
            float t = Mathf.Clamp01(_ringTime / _ringSeconds);
            float diameter = Mathf.Lerp(0.2f, _ringRadius * 2f, t);
            _ring.localScale = new Vector3(diameter, 0.02f, diameter);
            if (t >= 1f)
            {
                _ring.gameObject.SetActive(false);
            }
        }

        private void Beacon()
        {
            float spin = _down ? EmblemSpin * 0.4f : EmblemSpin;
            _emblem.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
            float bob = _down ? Mathf.Sin(Time.time * DownBeaconPulse) * EmblemBobHeight * 0.5f : Mathf.Sin(Time.time * EmblemBobSpeed) * EmblemBobHeight;
            _emblem.localPosition = new Vector3(0f, BeaconTop + bob, 0f);
        }
    }
}
