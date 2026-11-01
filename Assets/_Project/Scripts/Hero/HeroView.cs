using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Hero
{
    /// <summary>
    /// The look of the commander: a human warrior in a pressure suit who stands with weight in the stance (one
    /// foot back, knees bent, torso turned a little side-on and hunched at the pelvis, head forward), shifts from
    /// foot to foot while waiting, breathes, runs with a real gait (knees folding as the leg comes forward, arms
    /// pumping against the legs, shoulders turning against the hips, a lean into the run), looks around at a
    /// post and fights with two weapon rigs: the blade up close and the gun at range, drawing whichever the
    /// commander uses and clipping the other away. The torso pivots at the pelvis and carries head, arms and
    /// cloak with it, so the figure bends as one body. When its health runs out it staggers, drops the weapon and
    /// crashes to the ground with a shower of sparks, lies smoking with its lights out, and powers back up with a
    /// flash when it revives. A tall beacon floats above it so it can always be found; it turns red while the
    /// hero is down. Reacts to Commander events only.
    /// </summary>
    [RequireComponent(typeof(Commander))]
    public sealed class HeroView : MonoBehaviour
    {
        private const float TurnSpeed = 14f;
        private const float FigureScale = 0.95f;
        private const float IdleSpeed = 2.2f;
        private const float IdleHeight = 0.015f;
        private const float BreathSpeed = 2.2f;
        private const float BreathDepth = 0.015f;
        private const float WalkBobHeight = 0.05f;
        private const float GaitPerUnit = 2.1f;
        private const float LegSwingDegrees = 36f;
        private const float KneeFoldDegrees = 62f;
        private const float KneeFoldLead = 0.6f;
        private const float RunKneeDegrees = 14f;
        private const float RunCrouch = 0.04f;
        private const float RestHipDegrees = -6f;
        private const float RestKneeDegrees = 24f;
        private const float StanceStaggerDegrees = 11f;
        private const float FrontKneeExtraDegrees = 8f;
        private const float BackKneeExtraDegrees = -6f;
        private const float FootSplayDegrees = 10f;
        private const float LegSettleSpeed = 8f;
        private const float WeightShiftSpeed = 0.9f;
        private const float WeightShiftKnee = 6f;
        private const float WeightShiftSlide = 0.02f;
        private const float WeightShiftRoll = 2f;
        private const float HunchDegrees = 16f;
        private const float RestTwistDegrees = 12f;
        private const float HipCounterTwistShare = 0.35f;
        private const float HeadForwardDegrees = 12f;
        private const float HeadScanDegrees = 28f;
        private const float HeadScanSpeed = 0.55f;
        private const float HeadNodDegrees = 6f;
        private const float HeadFollowSpeed = 6f;
        private const float WalkLeanDegrees = 10f;
        private const float RelaxedForearmDegrees = 62f;
        private const float RunForearmDegrees = 85f;
        private const float DrawnForearmDegrees = 12f;
        private const float ForearmSwingShare = 0.25f;
        private const float FreeArmRestPitch = 8f;
        private const float FreeArmRestYaw = -12f;
        private const float FreeArmSwingShare = 0.8f;
        private const float FreeArmAimPitch = -55f;
        private const float FreeArmAimYaw = 32f;
        private const float FreeArmAimForearm = 20f;
        private const float FreeArmGuardPitch = -35f;
        private const float FreeArmGuardYaw = -12f;
        private const float DrawFollowSpeed = 6f;
        private const float CloakTrailDegrees = 26f;
        private const float CloakSwayDegrees = 4f;
        private const float CloakSwaySpeed = 1.3f;
        private const float CloakLungeDegrees = 30f;
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
        private const float BarWidth = 0.8f;
        private const float BarHeight = 0.04f;
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
        private HeroWeaponRig _melee;
        private HeroWeaponRig _ranged;
        private HeroWeaponRig _active;
        private Transform _figure;
        private Transform _torso;
        private Transform _shell;
        private Transform _armUpperRight;
        private Transform _armLowerRight;
        private Transform _armUpperLeft;
        private Transform _armLowerLeft;
        private Transform _thighRight;
        private Transform _shinRight;
        private Transform _thighLeft;
        private Transform _shinLeft;
        private Transform _head;
        private Transform _cloak;
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
        private float _stride;
        private float _standCrouch;
        private float _crouch;
        private float _drawn;
        private float _headYaw;
        private float _headPitch;
        private float _scanPhase;
        private float _shiftPhase;
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
            _shiftPhase = Random.Range(0f, 10f);
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

        // The figure turns as a whole; the torso pivots at the pelvis and carries the body mesh, the head, the arms
        // and the cloak, so a hunch or a lean bends the figure instead of sliding the chest out from under the head.
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

            _torso = new GameObject("Torso").transform;
            _torso.SetParent(figure, false);
            _torso.localPosition = _parts.Pelvis;
            _shell = Part("Shell", _torso, _parts.Body, _poweredMaterials, -_parts.Pelvis, false);
            _head = Part("Head", _torso, _parts.Head, _poweredMaterials, _parts.Neck - _parts.Pelvis, false);
            _cloak = Part("Cloak", _torso, _parts.Cloak, _poweredMaterials, _parts.Nape - _parts.Pelvis, false);
            _armUpperRight = Part("ArmR", _torso, _parts.ArmUpper, _poweredMaterials, _parts.Shoulder - _parts.Pelvis, false);
            _armLowerRight = Part("ForearmR", _armUpperRight, _parts.ArmLower, _poweredMaterials, _parts.Elbow, false);
            _armUpperLeft = Part("ArmL", _torso, _parts.ArmUpper, _poweredMaterials, Mirror(_parts.Shoulder) - _parts.Pelvis, true);
            _armLowerLeft = Part("ForearmL", _armUpperLeft, _parts.ArmLower, _poweredMaterials, _parts.Elbow, false);
            _thighRight = Part("ThighR", figure, _parts.LegUpper, _poweredMaterials, _parts.Hip, false);
            _shinRight = Part("ShinR", _thighRight, _parts.LegLower, _poweredMaterials, _parts.Knee, false);
            _thighLeft = Part("ThighL", figure, _parts.LegUpper, _poweredMaterials, Mirror(_parts.Hip), true);
            _shinLeft = Part("ShinL", _thighLeft, _parts.LegLower, _poweredMaterials, _parts.Knee, false);
            _figureRenderers = figure.GetComponentsInChildren<Renderer>();

            // Bent knees bring the hips down; the figure sinks by the same amount so the feet stay on the ground.
            _standCrouch = 0.5f * (HipDrop(RestHipDegrees + StanceStaggerDegrees, RestKneeDegrees + BackKneeExtraDegrees)
                                   + HipDrop(RestHipDegrees - StanceStaggerDegrees, RestKneeDegrees + FrontKneeExtraDegrees));
            _crouch = _standCrouch;

            HeroDefinition definition = _hero.Definition;
            _melee = HeroWeaponRig.For(definition.Weapon);
            _melee.Build(definition.Weapon, _parts, figure, _shell, _armLowerRight, _trim);
            if (definition.RangedWeapon != null)
            {
                _ranged = HeroWeaponRig.For(definition.RangedWeapon);
                _ranged.Build(definition.RangedWeapon, _parts, figure, _shell, _armLowerRight, _trim);
            }

            _active = _melee;

            _thrusters = CreateThrusters(_shell, _parts.Thruster);
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

        // How far the hip comes down when the thigh and the knee bend by these angles, with the leg lengths of the model.
        private float HipDrop(float hipDegrees, float kneeDegrees) => HipDrop(-_parts.Knee.y, -_parts.Ankle.y, hipDegrees, kneeDegrees);

        private static Vector3 Mirror(Vector3 point) => new Vector3(-point.x, point.y, point.z);

        /// <summary>
        /// The commander standing at rest, without a Commander behind it: the same parts, materials and stance
        /// as in the level, for menus that show the figure. Feet on y = 0, facing +z, in level scale.
        /// </summary>
        public static GameObject BuildDisplayFigure(Transform parent, HeroDefinition definition)
        {
            const float DisplayArmPitch = 6f;
            const float DisplayArmYaw = -10f;

            HeroModelFactory.Parts parts = HeroModelFactory.Create();
            Color trim = definition.Color;
            Material[] materials = HeroModelFactory.CreateMaterials(trim);

            var figure = new GameObject("Figure").transform;
            figure.SetParent(parent, false);
            figure.localScale = Vector3.one * FigureScale;

            var torso = new GameObject("Torso").transform;
            torso.SetParent(figure, false);
            torso.localPosition = parts.Pelvis;
            Transform shell = Part("Shell", torso, parts.Body, materials, -parts.Pelvis, false);
            Part("Head", torso, parts.Head, materials, parts.Neck - parts.Pelvis, false).localRotation = Quaternion.Euler(HeadForwardDegrees * 0.5f, 0f, 0f);
            Part("Cloak", torso, parts.Cloak, materials, parts.Nape - parts.Pelvis, false);
            Transform armRight = Part("ArmR", torso, parts.ArmUpper, materials, parts.Shoulder - parts.Pelvis, false);
            Transform forearmRight = Part("ForearmR", armRight, parts.ArmLower, materials, parts.Elbow, false);
            Transform armLeft = Part("ArmL", torso, parts.ArmUpper, materials, Mirror(parts.Shoulder) - parts.Pelvis, true);
            Transform forearmLeft = Part("ForearmL", armLeft, parts.ArmLower, materials, parts.Elbow, false);
            Transform thighRight = Part("ThighR", figure, parts.LegUpper, materials, parts.Hip, false);
            Transform shinRight = Part("ShinR", thighRight, parts.LegLower, materials, parts.Knee, false);
            Transform thighLeft = Part("ThighL", figure, parts.LegUpper, materials, Mirror(parts.Hip), true);
            Transform shinLeft = Part("ShinL", thighLeft, parts.LegLower, materials, parts.Knee, false);

            torso.localRotation = Quaternion.Euler(HunchDegrees, RestTwistDegrees, 0f);
            armRight.localRotation = Quaternion.Euler(DisplayArmPitch, DisplayArmYaw, 0f);
            forearmRight.localRotation = Quaternion.Euler(RelaxedForearmDegrees, 0f, 0f);
            armLeft.localRotation = Quaternion.Euler(FreeArmRestPitch, FreeArmRestYaw, 0f);
            forearmLeft.localRotation = Quaternion.Euler(RelaxedForearmDegrees, 0f, 0f);
            thighRight.localRotation = Quaternion.Euler(RestHipDegrees + StanceStaggerDegrees, FootSplayDegrees, 0f);
            thighLeft.localRotation = Quaternion.Euler(RestHipDegrees - StanceStaggerDegrees, -FootSplayDegrees, 0f);
            shinRight.localRotation = Quaternion.Euler(RestKneeDegrees + BackKneeExtraDegrees, 0f, 0f);
            shinLeft.localRotation = Quaternion.Euler(RestKneeDegrees + FrontKneeExtraDegrees, 0f, 0f);

            float thigh = -parts.Knee.y;
            float shin = -parts.Ankle.y;
            float crouch = 0.5f * (HipDrop(thigh, shin, RestHipDegrees + StanceStaggerDegrees, RestKneeDegrees + BackKneeExtraDegrees)
                                   + HipDrop(thigh, shin, RestHipDegrees - StanceStaggerDegrees, RestKneeDegrees + FrontKneeExtraDegrees));
            figure.localPosition = Vector3.down * (crouch * FigureScale);

            if (definition.Weapon != null)
            {
                HeroWeaponRig.For(definition.Weapon).Build(definition.Weapon, parts, figure, shell, forearmRight, trim);
            }

            return figure.gameObject;
        }

        private static float HipDrop(float thigh, float shin, float hipDegrees, float kneeDegrees)
        {
            return thigh * (1f - Mathf.Cos(hipDegrees * Mathf.Deg2Rad)) + shin * (1f - Mathf.Cos((hipDegrees + kneeDegrees) * Mathf.Deg2Rad));
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

        // The weapon the commander used comes to the hand; whatever else was drawn goes back to its holster.
        private void OnStruck(Vector3 target, HeroWeaponDefinition weapon)
        {
            if (_down)
            {
                return;
            }

            HeroWeaponRig rig = weapon.Kind == HeroWeaponKind.Ranged && _ranged != null ? _ranged : _melee;
            if (rig != _active && _active.Drawn)
            {
                _active.Holster();
            }

            _active = rig;
            _active.Attack(target);
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
            _melee.Holster();
            _ranged?.Holster();
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
            HeroWeaponRig.Pose pose = _active.Tick(_hero.IsWalking, _legSwing);
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
            _thighRight.localRotation = Quaternion.Euler(LegSprawl * settle, 0f, 0f);
            _thighLeft.localRotation = Quaternion.Euler(-LegSprawl * 0.5f * settle, 0f, 0f);
            _shinRight.localRotation = Quaternion.Euler(RestKneeDegrees * (1f - settle * 0.5f), 0f, 0f);
            _shinLeft.localRotation = Quaternion.Euler(RestKneeDegrees + LegSprawl * settle, 0f, 0f);
            _armUpperRight.localRotation = Quaternion.Euler(ArmSprawlPitch * settle, ArmSprawlYaw * settle, 0f);
            _armUpperLeft.localRotation = Quaternion.Euler(-ArmSprawlPitch * 0.5f * settle, -ArmSprawlYaw * 0.6f * settle, 0f);
            _cloak.localRotation = Quaternion.Euler(-60f * settle, 0f, 0f);
            _torso.localRotation = Quaternion.identity;

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
            _active.Drop(transform.position.y);
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
            _head.localRotation = Quaternion.Euler(HeadDroop * t + HeadForwardDegrees * (1f - t), 0f, 0f);
        }

        // Breath, bob, lunge and crouch; the torso hunched at the pelvis, turned side-on while relaxed, leaning
        // into a run and turning against the hips with every stride; the weapon arm as the rig wants it with the
        // elbow opening as the weapon is drawn; the free arm pumping against the legs, bracing the gun or
        // balancing the blade.
        private void Hold(HeroWeaponRig.Pose pose)
        {
            Vector3 forward = _hero.Facing.sqrMagnitude > 0.0001f ? _hero.Facing.normalized : Vector3.forward;
            bool walking = _hero.IsWalking;
            float shift = walking ? 0f : Mathf.Sin(Time.time * WeightShiftSpeed + _shiftPhase);
            float bob = walking ? Mathf.Abs(Mathf.Sin(_gait)) * WalkBobHeight : Mathf.Sin(Time.time * IdleSpeed) * IdleHeight;
            if (_rise >= RiseSeconds)
            {
                _figure.localPosition = forward * pose.Lunge + Vector3.up * (bob - _crouch) + _figure.right * (shift * WeightShiftSlide);
            }

            _drawn = Mathf.MoveTowards(_drawn, _active.Drawn ? 1f : 0f, DrawFollowSpeed * Time.deltaTime);
            _torso.localScale = new Vector3(1f, 1f + BreathDepth * Mathf.Sin(Time.time * BreathSpeed), 1f);
            float restTwist = RestTwistDegrees * (1f - Mathf.Max(_drawn, _stride));
            float twist = pose.Twist + restTwist - _legSwing * HipCounterTwistShare;
            _torso.localRotation = Quaternion.Euler(HunchDegrees + pose.Lean + WalkLeanDegrees * _stride, twist, shift * WeightShiftRoll);

            _armUpperRight.localRotation = Quaternion.Euler(pose.ArmPitch, pose.ArmYaw, 0f);
            float relaxedForearm = Mathf.Lerp(RelaxedForearmDegrees, RunForearmDegrees, _stride);
            float forearm = Mathf.Lerp(relaxedForearm - _legSwing * ForearmSwingShare, DrawnForearmDegrees, _drawn);
            _armLowerRight.localRotation = Quaternion.Euler(forearm, 0f, 0f);

            bool aiming = _active == _ranged && _ranged != null;
            float freePitch = aiming ? FreeArmAimPitch : FreeArmGuardPitch;
            float freeYaw = aiming ? FreeArmAimYaw : FreeArmGuardYaw;
            float freeForearm = aiming ? FreeArmAimForearm : RelaxedForearmDegrees * 0.6f;
            float restPitch = FreeArmRestPitch + _legSwing * FreeArmSwingShare;
            _armUpperLeft.localRotation = Quaternion.Euler(Mathf.Lerp(restPitch, freePitch, _drawn), Mathf.Lerp(FreeArmRestYaw, freeYaw, _drawn), 0f);
            _armLowerLeft.localRotation = Quaternion.Euler(Mathf.Lerp(relaxedForearm + _legSwing * ForearmSwingShare, freeForearm, _drawn), 0f, 0f);

            // The cloak trails when the figure moves or lunges and stirs a little in the wind while it stands.
            float cloakSway = Mathf.Sin(Time.time * CloakSwaySpeed + _shiftPhase) * CloakSwayDegrees + Mathf.Sin(Time.time * CloakSwaySpeed * 2.3f) * CloakSwayDegrees * 0.4f;
            _cloak.localRotation = Quaternion.Euler(CloakTrailDegrees * _stride + pose.Lunge * CloakLungeDegrees + cloakSway, 0f, shift * WeightShiftRoll * 0.5f);
        }

        // Standing: one foot back and one forward, both knees bent, the weight rocking slowly from leg to leg.
        // Running: legs swing from the hip with the distance covered, the knee folds while the leg comes forward
        // (most just after it leaves the ground behind), and the stance knee stays a little soft. Thrusters burn
        // while moving.
        private void Walk()
        {
            Vector3 position = transform.position;
            float moved = (position - _lastPosition).magnitude;
            _lastPosition = position;
            bool walking = _hero.IsWalking;
            _gait += moved * GaitPerUnit;

            _stride = Mathf.Lerp(_stride, walking ? 1f : 0f, 1f - Mathf.Exp(-LegSettleSpeed * Time.deltaTime));
            float standing = 1f - _stride;
            float swing = Mathf.Sin(_gait) * LegSwingDegrees * _stride;
            _legSwing = swing;
            float foldRight = Mathf.Max(0f, -Mathf.Cos(_gait - KneeFoldLead)) * KneeFoldDegrees * _stride;
            float foldLeft = Mathf.Max(0f, Mathf.Cos(_gait - KneeFoldLead)) * KneeFoldDegrees * _stride;
            float stagger = StanceStaggerDegrees * standing;
            float knee = Mathf.Lerp(RestKneeDegrees, RunKneeDegrees, _stride);
            float shift = Mathf.Sin(Time.time * WeightShiftSpeed + _shiftPhase) * standing;

            _thighRight.localRotation = Quaternion.Euler(RestHipDegrees + stagger + swing, FootSplayDegrees, 0f);
            _thighLeft.localRotation = Quaternion.Euler(RestHipDegrees - stagger - swing, -FootSplayDegrees, 0f);
            _shinRight.localRotation = Quaternion.Euler(knee + BackKneeExtraDegrees * standing + foldRight + shift * WeightShiftKnee, 0f, 0f);
            _shinLeft.localRotation = Quaternion.Euler(knee + FrontKneeExtraDegrees * standing + foldLeft - shift * WeightShiftKnee, 0f, 0f);
            _crouch = Mathf.Lerp(_standCrouch, RunCrouch, _stride);
            ParticleKit.SetRate(_thrusters, walking ? ThrusterRate : 0f);
        }

        // Waiting at a post, the head scans the road; walking or fighting it looks straight ahead, always a little down.
        private void Scan()
        {
            if (_rise < RiseSeconds)
            {
                return;
            }

            bool busy = _hero.IsWalking || _active.Drawn;
            float yaw = busy ? 0f : Mathf.Sin(Time.time * HeadScanSpeed + _scanPhase) * HeadScanDegrees;
            float pitch = HeadForwardDegrees + (busy ? 0f : Mathf.Sin(Time.time * HeadScanSpeed * 1.7f + _scanPhase) * HeadNodDegrees);
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
        private ParticleSystem CreateThrusters(Transform body, Vector3 nozzle)
        {
            ParticleSystem system = ParticleKit.Create(body, "Thrusters", nozzle, ThrusterColor * 2f, 0.15f, 0.3f, 0.05f, 0.1f, 1.5f, 2.5f, 0f, 40, 0.08f);
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
