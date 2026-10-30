using System.Collections.Generic;
using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>
    /// The look of an enemy: a procedural creature whose legs step, jaws snap, feelers twitch, wings beat, fins
    /// ripple and hind body breathes; rollers curl into a spinning ball, floaters hover, armour plates drop off as
    /// the armour is chipped, blades and stings swing on a strike and spitters send a bolt. Each individual has its
    /// own rhythm and sway, climbs out of a gate, a pool, a nest or the ground when it spawns, bleeds when hurt and
    /// dies in the style of its kind.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyView : MonoBehaviour
    {
        private const float BarWidth = 0.6f;
        private const float BarHeight = 0.035f;
        private const float BarOffsetY = 1.0f;
        private const float BarEmission = 1.5f;
        private const float BodyEmission = 0.9f;
        private const float ModelTintStrength = 0.65f;
        private const float ModelEmission = 0.2f;
        private const float EyeEmission = 4f;
        private const float EyeSize = 0.14f;
        private const float ShadowSize = 0.6f;
        private const float ShadowHeight = 0.02f;
        private const float BarMargin = 0.25f;
        private const float FullHealthFraction = 0.999f;
        private const float GaitPerUnit = 7f;
        private const float GaitVariation = 0.15f;
        private const float LegSwingDegrees = 30f;
        private const float LegLiftDegrees = 24f;
        private const float KneeFoldDegrees = 38f;
        private const float RunnerSwingDegrees = 34f;
        private const float RunnerKneeDegrees = 42f;
        private const float RunnerRestHipDegrees = -9f;
        private const float RunnerRestKneeDegrees = 24f;
        private const float RunnerSplayDegrees = 7f;
        private const float IdleShiftDegrees = 2.5f;
        private const float IdleShiftSpeed = 0.9f;
        private const float DangleDegrees = 62f;
        private const float DangleSwayDegrees = 9f;
        private const float MandibleDegrees = 26f;
        private const float MandibleIdle = 0.2f;
        private const float MandibleBreathe = 0.12f;
        private const float ArmPitchDegrees = 45f;
        private const float AntennaTwitchDegrees = 12f;
        private const float WingBeatSpeed = 26f;
        private const float WingBeatDegrees = 34f;
        private const float BatBeatSpeed = 15f;
        private const float BatBeatDegrees = 40f;
        private const float FinBeatSpeed = 4.5f;
        private const float FinBeatDegrees = 22f;
        private const float FinRippleDegrees = 8f;
        private const float BreathScale = 0.05f;
        private const float BreathSpeed = 2.2f;
        private const float TailSwayDegrees = 16f;
        private const float TailFlickDegrees = 7f;
        private const float TailLiftDegrees = 6f;
        private const float OrbBob = 0.12f;
        private const float OrbSpinDegrees = 70f;
        private const float BobHeight = 0.05f;
        private const float HoverHeight = 0.04f;
        private const float HoverBob = 0.09f;
        private const float HoverSpeed = 3f;
        private const float StandStillDistance = 0.05f;
        private const float AttackSeconds = 0.5f;
        private const float RearShare = 0.4f;
        private const float AttackLunge = 0.35f;
        private const float SlamLunge = 0.6f;
        private const float AttackRearBack = 0.12f;
        private const float AttackRearDegrees = 28f;
        private const float AttackStrikeDegrees = 32f;
        private const float ClawDegrees = 50f;
        private const float BladeRestDegrees = -35f;
        private const float BladeRaiseDegrees = -40f;
        private const float BladeSwingDegrees = 120f;
        private const float StingSnapDegrees = 85f;
        private const float StingSwayDegrees = 6f;
        private const float WhipDegrees = 70f;
        private const float HitSeconds = 0.18f;
        private const float FlashSeconds = 0.08f;
        private const float FlashEmission = 2.5f;
        private const float HitKnockBack = 0.1f;
        private const float HitFlinchDegrees = -14f;
        private const float HitSquash = 0.12f;
        private const float SwayDistance = 0.04f;
        private const float SwayDegrees = 6f;
        private const float SwayPerGait = 0.5f;
        private const float FlyerSwaySpeed = 1.7f;
        private const float FlyerSwayDistance = 0.1f;
        private const float FlyerRollDegrees = 9f;
        private const float HasteLeanDegrees = 8f;
        private const float HasteTrailRate = 30f;
        private const float SpawnStartScale = 0.15f;
        private const float DeathGroundY = 0.03f;
        private const float DeathCurlDegrees = 80f;
        private const float CurlSeconds = 0.45f;
        private const float RollLegFoldDegrees = 115f;
        private const float RollKneeFoldDegrees = 50f;
        private const float RollBounce = 0.03f;
        private const float RollBellyShrink = 0.5f;
        private const float RollBellyRest = 0.22f;
        private const float BandStartDegrees = 20f;
        private const float BandSpanDegrees = 300f;
        private const float EmberRate = 24f;
        private const float EmberIdleRate = 5f;
        private const int EmergeBurst = 26;
        private const int BossEmergeMultiplier = 3;
        private const int DripBurst = 5;
        private const int BoltCount = 2;
        private const float BoltSize = 0.8f;
        private const float BossGlow = 1.9f;
        private const float BossPulseDepth = 0.35f;
        private const float BossPulseSpeed = 2.4f;
        private const float BossLightHeight = 0.9f;
        private const float BossLightIntensity = 2.6f;
        private const float BossLightRange = 7f;
        private const float HoverPoolRadius = 0.5f;
        private const float HoverPoolBrightness = 0.7f;
        private const float PlateDebrisSize = 0.3f;
        private static readonly Color BarColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color ArmorBarColor = new Color(0.7f, 0.85f, 1f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f);
        private static readonly Color EyeColor = new Color(1f, 0.95f, 0.6f);
        private static readonly Color WaterSplash = new Color(0.6f, 0.85f, 1f);
        private static readonly Color LavaSplash = new Color(1f, 0.5f, 0.15f);
        private static readonly Color SandSplash = new Color(0.8f, 0.7f, 0.45f);
        private static readonly Color WoodSplash = new Color(0.45f, 0.95f, 0.4f);
        private static readonly Color IceSplash = new Color(0.8f, 0.95f, 1f);
        private static readonly Color GroundDust = new Color(0.4f, 0.34f, 0.3f);
        private static readonly List<EnemyDeathEffect.Part> PartBuffer = new List<EnemyDeathEffect.Part>(32);

        // One animated piece: its root on the body, the knee for legs, which side it is on and its spec.
        private sealed class Rig
        {
            public Transform Root;
            public Transform Knee;
            public float Side;
            public CreaturePart Spec;
            public bool Hidden;
        }

        private Enemy _enemy;
        private EnemyDeathEffectPool _deaths;
        private Health _health;
        private Transform _body;
        private Transform _shell;
        private Vector3 _bodyRestPosition;
        private Transform _bar;
        private Transform _armorBar;
        private Transform _shadow;
        private bool _built;
        private Vector3 _lastPosition;
        private float _gait;
        private float _gaitRate = GaitPerUnit;
        private float _swayPhase;
        private float _creatureScale = 1f;
        private bool _procedural;
        private bool _dangle;
        private bool _straightLegs;
        private bool _bleeds;
        private float _hover;
        private float _ballRadius;
        private float _curl;
        private float _rollSpin;
        private float _clock;
        private float _barHeight;
        private float _attackTime = float.PositiveInfinity;
        private float _hitTime = float.PositiveInfinity;
        private float _spawnTime = float.PositiveInfinity;
        private float _spawnSeconds = 0.45f;
        private float _spawnDepth = 0.5f;
        private float _spawnStartScale = SpawnStartScale;
        private bool _surfaced;
        private float _lastFraction = 1f;
        private float _lastArmor = 1f;
        private bool _barHidden;
        private bool _boss;
        private bool _flashing;
        private float _whipSide = 1f;
        private Vector3 _attackTarget;
        private Material[] _shellMaterials;
        private Material[] _flashMaterials;
        private MeshFilter[] _partFilters;
        private MeshRenderer[] _partRenderers;
        private Quaternion[] _partCurls;
        private ParticleSystem _embers;
        private ParticleSystem _emergence;
        private ParticleSystem _drips;
        private ParticleSystem _rush;
        private EnemyBoltView[] _bolts;
        private int _nextBolt;
        private readonly List<Rig> _legs = new List<Rig>();
        private readonly List<Rig> _mandibles = new List<Rig>();
        private readonly List<Rig> _antennae = new List<Rig>();
        private readonly List<Rig> _wings = new List<Rig>();
        private readonly List<Rig> _abdomens = new List<Rig>();
        private readonly List<Rig> _tails = new List<Rig>();
        private readonly List<Rig> _bands = new List<Rig>();
        private readonly List<Rig> _orbs = new List<Rig>();
        private readonly List<Rig> _plates = new List<Rig>();
        private readonly List<Rig> _fins = new List<Rig>();
        private readonly List<Rig> _blades = new List<Rig>();
        private readonly List<Rig> _stingers = new List<Rig>();

        /// <summary>Gives the view the pool its death animation plays from. Without one the enemy simply disappears.</summary>
        public void Initialize(EnemyDeathEffectPool deaths) => _deaths = deaths;

        /// <summary>Keeps the health bar out of sight, for creatures shown outside a fight.</summary>
        public void HideHealthBar()
        {
            _barHidden = true;
            if (_bar != null)
            {
                _bar.gameObject.SetActive(false);
                _armorBar.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _enemy.Initialized += OnInitialized;
            _enemy.Attacked += OnAttacked;
            _enemy.Killed += OnKilled;
        }

        private void OnDestroy()
        {
            _enemy.Initialized -= OnInitialized;
            _enemy.Attacked -= OnAttacked;
            _enemy.Killed -= OnKilled;
        }

        private void LateUpdate()
        {
            if (!_built)
            {
                return;
            }

            if (_shadow != null)
            {
                Vector3 position = _enemy.Position;
                _shadow.position = new Vector3(position.x, ShadowHeight, position.z);
            }

            if (_procedural)
            {
                Animate();
                return;
            }

            if (_enemy.Heading.sqrMagnitude > 0.0001f)
            {
                _body.rotation = Quaternion.LookRotation(_enemy.Heading);
            }

            float grow = SpawnCurve(out float emerge);
            _body.localScale = Vector3.one * grow;
            _body.localPosition = _bodyRestPosition - Vector3.up * emerge;
        }

        private void OnInitialized(Enemy enemy)
        {
            if (!_built)
            {
                Build(enemy.Definition);
                _built = true;
            }

            if (_health != null)
            {
                _health.Changed -= OnHealthChanged;
                _health.ArmorChanged -= OnArmorChanged;
            }

            _lastPosition = enemy.Position;
            _health = enemy.Health;
            _lastFraction = 1f;
            _lastArmor = 1f;
            _hitTime = float.PositiveInfinity;
            _attackTime = float.PositiveInfinity;
            _curl = 0f;
            _rollSpin = 0f;
            _gaitRate = GaitPerUnit * Random.Range(1f - GaitVariation, 1f + GaitVariation);
            _swayPhase = Random.Range(0f, Mathf.PI * 2f);
            _clock = Random.Range(0f, 10f);
            _health.Changed += OnHealthChanged;
            _health.ArmorChanged += OnArmorChanged;
            ShowPlates(_plates.Count);
            SetBar(1f, _health.ArmorMax > 0f ? 1f : 0f);
            BeginSpawn(enemy);
        }

        // How deep and how slowly a creature climbs out depends on where it comes from; pools splash, ground dusts,
        // a nest flings its element about and a flyer born there climbs from the ground to its height.
        private void BeginSpawn(Enemy enemy)
        {
            _spawnTime = 0f;
            _surfaced = false;
            switch (enemy.Origin)
            {
                case EnemyOrigin.Water:
                case EnemyOrigin.Lava:
                case EnemyOrigin.Quicksand:
                    _spawnSeconds = 0.9f;
                    _spawnDepth = 0.95f;
                    _spawnStartScale = 0.6f;
                    break;
                case EnemyOrigin.Nest:
                    _spawnSeconds = 1f;
                    _spawnDepth = enemy.Definition.Flies ? 1.4f / Mathf.Max(0.3f, _creatureScale) : 0.9f;
                    _spawnStartScale = 0.5f;
                    break;
                case EnemyOrigin.Ground:
                    _spawnSeconds = 1.1f;
                    _spawnDepth = 1.15f;
                    _spawnStartScale = 0.7f;
                    break;
                default:
                    _spawnSeconds = 0.45f;
                    _spawnDepth = 0.5f;
                    _spawnStartScale = SpawnStartScale;
                    break;
            }

            if (enemy.Origin != EnemyOrigin.Gate)
            {
                Burst(enemy, 1f);
            }
        }

        private void Burst(Enemy enemy, float share)
        {
            if (_emergence == null)
            {
                _emergence = ParticleKit.Create(transform.parent, "Emergence", Vector3.zero, Color.white, 0.5f, 1.1f, 0.05f, 0.14f, 1f, 3f, 0.9f, EmergeBurst * BossEmergeMultiplier * 2, 0.35f);
            }

            Color color;
            switch (enemy.Origin)
            {
                case EnemyOrigin.Water:
                    color = WaterSplash;
                    break;
                case EnemyOrigin.Lava:
                    color = LavaSplash * 1.6f;
                    break;
                case EnemyOrigin.Quicksand:
                    color = SandSplash;
                    break;
                case EnemyOrigin.Nest:
                    color = ElementColor(enemy.Definition.Element) * 1.4f;
                    break;
                default:
                    color = GroundDust;
                    break;
            }

            color.a = 1f;
            Vector3 at = enemy.Position;
            at.y = 0f;
            _emergence.transform.position = at;
            ParticleSystem.MainModule main = _emergence.main;
            main.startColor = color;
            int count = Mathf.RoundToInt(EmergeBurst * share * (enemy.Definition.IsBoss ? BossEmergeMultiplier : 1));
            _emergence.Emit(count);
        }

        private static Color ElementColor(Element element)
        {
            switch (element)
            {
                case Element.Fire:
                    return LavaSplash;
                case Element.Water:
                    return WaterSplash;
                case Element.Wood:
                    return WoodSplash;
                case Element.Sand:
                    return SandSplash;
                case Element.Ice:
                    return IceSplash;
                default:
                    return GroundDust;
            }
        }

        // A procedural creature: a static shell plus every moving part on its own pivot.
        private void BuildCreature(Transform body, EnemyDefinition definition)
        {
            _procedural = true;
            _boss = definition.IsBoss;
            EnemyModelFactory.Parts parts = EnemyModelFactory.Create(definition.Shape);
            _creatureScale = definition.Scale * parts.ScaleFactor;
            _dangle = parts.LegsDangle;
            _straightLegs = parts.StraightLegs;
            _bleeds = parts.Bleeds;
            _hover = parts.HoverHeight * _creatureScale;
            _ballRadius = parts.BallRadius;
            body.localPosition = Vector3.zero;
            body.localScale = Vector3.one * _creatureScale;

            Material[] materials = EnemyModelFactory.CreateMaterials(definition.Color, definition.EyeColor, definition.AccentColor, definition.IsBoss ? BossGlow : 1f, parts.Skin);
            _shellMaterials = materials;
            if (definition.IsBoss)
            {
                AddBossPresence(body, definition, materials);
            }

            _flashMaterials = new Material[materials.Length];
            Material flash = SurfaceMaterials.Create(Color.white, FlashEmission, 0.3f);
            for (int i = 0; i < _flashMaterials.Length; i++)
            {
                _flashMaterials[i] = flash;
            }

            _shell = SpawnPart("Shell", body, parts.Body, materials, Vector3.zero, false);
            foreach (CreaturePart spec in parts.Moving)
            {
                var rig = new Rig
                {
                    Spec = spec,
                    Side = spec.Mirrored ? -1f : 1f,
                    Root = SpawnPart(spec.Role.ToString(), body, spec.Mesh, materials, spec.Pivot, spec.Mirrored),
                };
                if (spec.Lower != null)
                {
                    // The knee hangs inside the hip's frame, so the hip's mirror flips it along.
                    rig.Knee = SpawnPart("Knee", rig.Root, spec.Lower, materials, spec.Knee, false);
                }

                ListFor(spec.Role).Add(rig);
            }

            // Plates drop in index order, so they are kept sorted once.
            _plates.Sort((a, b) => a.Spec.Index.CompareTo(b.Spec.Index));

            if (parts.Embers)
            {
                Color ember = definition.Color * 1.6f;
                ember.a = 1f;
                _embers = ParticleKit.Create(transform, "Embers", Vector3.up * 0.2f, ember, 0.4f, 0.9f, 0.03f, 0.07f, 0.2f, 0.8f, -0.05f, 60, 0.25f);
            }

            if (_bleeds)
            {
                Color blood = definition.BloodColor * 1.5f;
                blood.a = 1f;
                _drips = ParticleKit.Create(transform.parent, "Drips", Vector3.zero, blood, 0.5f, 0.9f, 0.04f, 0.08f, 0.4f, 1.2f, 1.5f, DripBurst * 6, 0.15f);
            }

            Color rush = definition.AccentColor * 1.4f;
            rush.a = 1f;
            _rush = ParticleKit.Create(transform.parent, "Rush", Vector3.zero, rush, 0.25f, 0.45f, 0.02f, 0.05f, 0.1f, 0.4f, 0f, 40, 0.15f);

            if (definition.AttackStyle == AttackStyle.Bolt || definition.AttackStyle == AttackStyle.Sting)
            {
                _bolts = new EnemyBoltView[BoltCount];
                for (int i = 0; i < BoltCount; i++)
                {
                    _bolts[i] = EnemyBoltView.Create(transform.parent, definition.AccentColor);
                }
            }

            if (_hover > 0f)
            {
                GlowDecal.PlacePool(transform, definition.AccentColor * HoverPoolBrightness, HoverPoolRadius * _creatureScale, ShadowHeight);
            }

            _barHeight = parts.Height * _creatureScale + _hover + BarMargin;
        }

        // A boss burns brighter than its kin: its eyes and seams breathe, and a light in its colour spills over the ground.
        private static void AddBossPresence(Transform body, EnemyDefinition definition, Material[] materials)
        {
            var pulse = body.gameObject.AddComponent<EmissionPulse>();
            pulse.Initialize(new[] { materials[EnemyModelFactory.Eyes], materials[EnemyModelFactory.Glow], materials[EnemyModelFactory.Accent] }, BossPulseDepth, BossPulseSpeed);

            var lightGo = new GameObject("BossLight");
            lightGo.transform.SetParent(body, false);
            lightGo.transform.localPosition = Vector3.up * BossLightHeight;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = definition.Color;
            light.intensity = BossLightIntensity;
            light.range = BossLightRange;
            light.shadows = LightShadows.None;
        }

        private List<Rig> ListFor(CreatureRole role)
        {
            switch (role)
            {
                case CreatureRole.Leg:
                    return _legs;
                case CreatureRole.Mandible:
                    return _mandibles;
                case CreatureRole.Antenna:
                    return _antennae;
                case CreatureRole.Wing:
                    return _wings;
                case CreatureRole.Abdomen:
                    return _abdomens;
                case CreatureRole.Tail:
                    return _tails;
                case CreatureRole.Band:
                    return _bands;
                case CreatureRole.Plate:
                    return _plates;
                case CreatureRole.Fin:
                    return _fins;
                case CreatureRole.Blade:
                    return _blades;
                case CreatureRole.Stinger:
                    return _stingers;
                default:
                    return _orbs;
            }
        }

        private static Transform SpawnPart(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 position, bool mirrored)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go.transform;
        }

        private void Animate()
        {
            float dt = Time.deltaTime;
            _clock += dt;
            Vector3 position = _enemy.Position;
            float moved = (position - _lastPosition).magnitude;
            _lastPosition = position;
            bool walking = moved > StandStillDistance * dt;
            _gait += moved * _gaitRate / _creatureScale;

            // Rolling: the curl eases in and out, and the ball spins with the distance covered.
            _curl = Mathf.MoveTowards(_curl, _enemy.IsRolling ? 1f : 0f, dt / CurlSeconds);
            if (_curl > 0f && _ballRadius > 0f)
            {
                _rollSpin += moved / (_ballRadius * _creatureScale);
            }
            else
            {
                _rollSpin = 0f;
            }

            float strike = AttackCurve(out float rear);
            float knock = HitCurve();
            float grow = SpawnCurve(out float emerge);
            float breathe = Mathf.Sin(_clock * BreathSpeed + _swayPhase);
            AttackStyle style = _enemy.Definition.AttackStyle;

            AnimateLegs(walking, strike);
            AnimateMandibles(rear, strike);
            AnimateSmallParts(walking, breathe);
            AnimateWeapons(rear, strike, style);
            if (_bands.Count > 0)
            {
                AnimateBands();
            }

            // Walking bounces the body; standing still it breathes; rolling it bumps along; floating it drifts up and
            // down. An attack rears back, strikes and settles, a slam throws the whole body. Walkers weave a little
            // with their stride, flyers drift and roll, and anything hurried leans into its run.
            bool flyer = _dangle;
            bool hasted = _enemy.IsHasted;
            float bob = _hover > 0f
                ? _hover + Mathf.Sin(_clock * HoverSpeed + _swayPhase) * HoverBob * _creatureScale
                : _curl > 0.5f
                    ? Mathf.Abs(Mathf.Sin(_rollSpin * 2f)) * RollBounce
                    : walking ? Mathf.Abs(Mathf.Sin(_gait)) * BobHeight : breathe * HoverHeight;
            float swayWave = flyer ? Mathf.Sin(_clock * FlyerSwaySpeed + _swayPhase) : Mathf.Sin(_gait * SwayPerGait + _swayPhase) * (walking ? 1f : 0f);
            float sway = swayWave * (flyer ? FlyerSwayDistance : SwayDistance) * _creatureScale * (1f - _curl);
            // Standing still the weight shifts slowly from foot to foot, so nothing stands like a statue.
            float idle = walking || flyer ? 0f : 1f - _curl;
            float shift = Mathf.Sin(_clock * IdleShiftSpeed + _swayPhase) * IdleShiftDegrees * idle;
            float yaw = flyer ? 0f : swayWave * SwayDegrees * (1f - _curl) + shift * 1.2f;
            float roll = flyer ? -swayWave * FlyerRollDegrees : shift;
            if (style == AttackStyle.Whip)
            {
                roll += strike * FlyerRollDegrees * 2f * _whipSide;
            }

            float lunge = style == AttackStyle.Slam ? SlamLunge : style == AttackStyle.Bolt ? -AttackLunge * 0.3f : AttackLunge;
            Vector3 forward = _enemy.Heading.sqrMagnitude > 0.0001f ? _enemy.Heading.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float along = (strike * lunge - rear * AttackRearBack - knock * HitKnockBack) * _creatureScale;
            float hoverScale = _hover > 0f ? 1f : _creatureScale;
            _body.position = position + Vector3.up * (bob * hoverScale - emerge) + forward * along + right * sway;
            float lean = hasted && walking && !flyer ? HasteLeanDegrees : 0f;
            Quaternion pitch = Quaternion.Euler(rear * -AttackRearDegrees + strike * AttackStrikeDegrees + knock * HitFlinchDegrees + lean, yaw, roll);
            _body.rotation = Quaternion.LookRotation(forward) * pitch;
            float squash = style == AttackStyle.Slam ? strike * HitSquash : 0f;
            _body.localScale = Vector3.one * (_creatureScale * (1f + knock * HitSquash + squash) * grow);

            if (_embers != null)
            {
                ParticleKit.SetRate(_embers, walking ? EmberRate : EmberIdleRate);
            }

            _rush.transform.position = position - forward * (0.2f * _creatureScale) + Vector3.up * (0.1f * _creatureScale + bob * hoverScale);
            ParticleKit.SetRate(_rush, hasted && walking ? HasteTrailRate : 0f);

            // A boss is hit many times a second; flashing it white every time is a strobe, so it keeps its own glow.
            bool flash = _hitTime < FlashSeconds && !_boss;
            if (flash != _flashing)
            {
                _flashing = flash;
                for (int i = 0; i < _partRenderers.Length; i++)
                {
                    _partRenderers[i].sharedMaterials = flash ? _flashMaterials : _shellMaterials;
                }
            }
        }

        // Insect hips swing and lift with the gait, knees fold as the foot comes up; the front pair claws on a
        // strike. Runner legs swing fore and aft and the knee folds on the lift. Under a flyer the legs hang and
        // sway. A curling roller folds them in against its belly.
        private void AnimateLegs(bool walking, float strike)
        {
            float stride = walking ? 1f : 0f;
            for (int i = 0; i < _legs.Count; i++)
            {
                Rig leg = _legs[i];
                float side = leg.Side;
                float swing = Mathf.Sin(_gait + leg.Spec.Phase) * stride;
                float lift = Mathf.Max(0f, Mathf.Cos(_gait + leg.Spec.Phase)) * stride;
                if (_straightLegs)
                {
                    // A runner never locks its legs: the thigh sits a little back, the knee stays bent and the feet
                    // splay outward, and the gait swings around that stance.
                    float restHip = RunnerRestHipDegrees * (leg.Spec.Index == 0 ? -0.5f : 1f);
                    leg.Root.localRotation = Quaternion.Euler(restHip + swing * RunnerSwingDegrees - strike * ClawDegrees * 0.3f, 0f, -RunnerSplayDegrees * side);
                    if (leg.Knee != null)
                    {
                        leg.Knee.localRotation = Quaternion.Euler(RunnerRestKneeDegrees + lift * RunnerKneeDegrees, 0f, 0f);
                    }

                    continue;
                }

                float hipYaw;
                float hipRoll;
                float knee;
                if (_dangle)
                {
                    float sway = Mathf.Sin(_clock * 3f + leg.Spec.Phase) * DangleSwayDegrees;
                    hipYaw = sway * side;
                    hipRoll = -DangleDegrees * side;
                    knee = -25f * side;
                }
                else
                {
                    hipYaw = swing * LegSwingDegrees * side;
                    hipRoll = (lift * LegLiftDegrees + _curl * RollLegFoldDegrees) * side;
                    knee = -(lift * KneeFoldDegrees + _curl * RollKneeFoldDegrees) * side;
                    if (leg.Spec.Index == 0)
                    {
                        hipRoll += strike * ClawDegrees * side;
                    }
                }

                leg.Root.localRotation = Quaternion.Euler(0f, hipYaw, hipRoll);
                if (leg.Knee != null)
                {
                    leg.Knee.localRotation = Quaternion.Euler(0f, 0f, knee);
                }
            }
        }

        // Jaws hang a little open and work slowly; a strike opens them wide and snaps them shut. Arms (index one)
        // also swing down.
        private void AnimateMandibles(float rear, float strike)
        {
            float open = MandibleIdle + Mathf.Sin(_clock * 3f + _swayPhase) * MandibleBreathe + rear - strike * 0.8f;
            for (int i = 0; i < _mandibles.Count; i++)
            {
                Rig part = _mandibles[i];
                bool arm = part.Spec.Index > 0;
                float yaw = -open * MandibleDegrees * (arm ? 1.6f : 1f) * part.Side;
                float pitchDown = arm ? strike * ArmPitchDegrees - rear * ArmPitchDegrees * 0.5f : 0f;
                part.Root.localRotation = Quaternion.Euler(pitchDown, yaw, 0f);
            }
        }

        // Blades rest raised, lift further in the wind-up and come down on the strike. A sting arches over the
        // back, sways, and snaps forward over the head; a whip lashes to one side and then the other.
        private void AnimateWeapons(float rear, float strike, AttackStyle style)
        {
            for (int i = 0; i < _blades.Count; i++)
            {
                Rig blade = _blades[i];
                float idle = Mathf.Sin(_clock * 1.6f + _swayPhase + blade.Spec.Index) * 4f;
                blade.Root.localRotation = Quaternion.Euler(BladeRestDegrees + idle + rear * BladeRaiseDegrees + strike * BladeSwingDegrees, 0f, 0f);
            }

            for (int i = 0; i < _stingers.Count; i++)
            {
                Rig sting = _stingers[i];
                if (sting.Spec.Index == 1 && style == AttackStyle.Whip)
                {
                    float lash = Mathf.Sin(_clock * 2f + _swayPhase) * StingSwayDegrees + strike * WhipDegrees * _whipSide;
                    sting.Root.localRotation = Quaternion.Euler(0f, lash, 0f);
                    continue;
                }

                float sway = Mathf.Sin(_clock * 1.3f + _swayPhase + sting.Spec.Index) * StingSwayDegrees;
                float snap = (strike - rear * 0.2f) * StingSnapDegrees;
                sting.Root.localRotation = Quaternion.Euler(snap, sway * 0.5f, sway);
            }
        }

        private void AnimateSmallParts(bool walking, float breathe)
        {
            for (int i = 0; i < _antennae.Count; i++)
            {
                Rig part = _antennae[i];
                float phase = part.Spec.Phase + _swayPhase;
                part.Root.localRotation = Quaternion.Euler(Mathf.Sin(_clock * 7f + phase) * AntennaTwitchDegrees, Mathf.Cos(_clock * 5f + phase) * AntennaTwitchDegrees * part.Side, 0f);
            }

            bool bat = _dangle && _legs.Count > 0 && _wings.Count > 0 && _fins.Count == 0 && _tails.Count > 0;
            float beatSpeed = bat ? BatBeatSpeed : WingBeatSpeed;
            float beatDegrees = bat ? BatBeatDegrees : WingBeatDegrees;
            for (int i = 0; i < _wings.Count; i++)
            {
                Rig part = _wings[i];
                float beat = Mathf.Sin(_clock * beatSpeed + part.Spec.Phase) * beatDegrees;
                part.Root.localRotation = Quaternion.Euler(0f, 0f, beat * part.Side);
            }

            for (int i = 0; i < _fins.Count; i++)
            {
                Rig part = _fins[i];
                float beat = Mathf.Sin(_clock * FinBeatSpeed + part.Spec.Phase + _swayPhase);
                float ripple = Mathf.Sin(_clock * FinBeatSpeed * 1.5f + part.Spec.Phase) * FinRippleDegrees;
                part.Root.localRotation = Quaternion.Euler(ripple, 0f, beat * FinBeatDegrees * part.Side);
            }

            for (int i = 0; i < _abdomens.Count; i++)
            {
                Rig part = _abdomens[i];
                part.Root.localScale = Vector3.one * (1f + breathe * BreathScale);
                part.Root.localRotation = Quaternion.Euler(walking ? Mathf.Sin(_gait) * 3f : breathe * 2f, 0f, 0f);
            }

            for (int i = 0; i < _tails.Count; i++)
            {
                Rig part = _tails[i];
                // A tail is a whip, not a rudder: a slow sway with a quicker flick on top, and it lifts with the stride.
                float wave = Mathf.Sin(_gait * 0.5f + _clock * 1.5f + _swayPhase) * TailSwayDegrees + Mathf.Sin(_clock * 3.7f + _swayPhase * 2f) * TailFlickDegrees;
                float lift = (walking ? Mathf.Sin(_gait + 1f) : breathe * 0.5f) * TailLiftDegrees;
                part.Root.localRotation = Quaternion.Euler(lift, wave, wave * 0.3f);
            }

            for (int i = 0; i < _orbs.Count; i++)
            {
                Rig part = _orbs[i];
                float tilt = 20f + part.Spec.Index * 25f;
                part.Root.localPosition = part.Spec.Pivot + Vector3.up * (Mathf.Sin(_clock * 2f + _swayPhase + part.Spec.Phase) * OrbBob);
                part.Root.localRotation = Quaternion.Euler(tilt, _clock * OrbSpinDegrees * (part.Spec.Index % 2 == 0 ? 1f : -1.4f) + part.Spec.Phase * 57f, tilt * 0.5f);
            }
        }

        // The shell bands lie in a row along the back; curled up they sit on a ring around the ball's centre, head
        // and tail tucked together underneath, and the whole ring turns with the distance rolled.
        private void AnimateBands()
        {
            float t = Mathf.SmoothStep(0f, 1f, _curl);
            var centre = new Vector3(0f, _ballRadius, 0f);
            Quaternion spin = Quaternion.Euler(_rollSpin * Mathf.Rad2Deg, 0f, 0f);
            int count = _bands.Count;
            for (int i = 0; i < count; i++)
            {
                Rig band = _bands[i];
                float angle = (BandStartDegrees + BandSpanDegrees * band.Spec.Index / (count - 1)) * Mathf.Deg2Rad;
                Vector3 onRing = centre + new Vector3(0f, -Mathf.Cos(angle), Mathf.Sin(angle)) * _ballRadius;
                Quaternion facing = Quaternion.Euler(180f - angle * Mathf.Rad2Deg, 0f, 0f);
                Vector3 curledPosition = centre + spin * (onRing - centre);
                Quaternion curledRotation = spin * facing;
                band.Root.localPosition = Vector3.Lerp(band.Spec.Pivot, curledPosition, t);
                band.Root.localRotation = Quaternion.Slerp(Quaternion.identity, curledRotation, t);
            }

            // The soft belly shrinks into the middle of the ball so it never pokes out between the bands.
            _shell.localPosition = new Vector3(0f, (_ballRadius - RollBellyRest) * t, 0f);
            _shell.localScale = Vector3.one * (1f - RollBellyShrink * t);
        }

        // Zero at rest; rises during the wind-up (rear), peaks at the bite (strike) and falls back.
        private float AttackCurve(out float rear)
        {
            rear = 0f;
            if (_attackTime > AttackSeconds)
            {
                return 0f;
            }

            _attackTime += Time.deltaTime;
            float t = Mathf.Clamp01(_attackTime / AttackSeconds);
            if (t < RearShare)
            {
                rear = Mathf.SmoothStep(0f, 1f, t / RearShare);
                return 0f;
            }

            float k = (t - RearShare) / (1f - RearShare);
            rear = 1f - Mathf.SmoothStep(0f, 1f, k * 3f);
            rear = Mathf.Max(rear, 0f);
            return Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
        }

        private float HitCurve()
        {
            if (_hitTime > HitSeconds)
            {
                return 0f;
            }

            _hitTime += Time.deltaTime;
            return 1f - Mathf.Clamp01(_hitTime / HitSeconds);
        }

        // Fresh out of its entrance a creature swells up to size while climbing out of the ground or the pool;
        // a second splash marks the moment it breaks the surface.
        private float SpawnCurve(out float emerge)
        {
            if (_spawnTime > _spawnSeconds)
            {
                emerge = 0f;
                return 1f;
            }

            _spawnTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_spawnTime / _spawnSeconds));
            if (!_surfaced && t > 0.45f && _enemy.Origin != EnemyOrigin.Gate)
            {
                _surfaced = true;
                Burst(_enemy, 0.7f);
            }

            emerge = (1f - t) * _spawnDepth * _creatureScale;
            return Mathf.Lerp(_spawnStartScale, 1f, t);
        }

        private void Build(EnemyDefinition definition)
        {
            bool air = definition.Flies;
            float scale = definition.Scale;

            var body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            body.localPosition = new Vector3(0f, scale * 0.5f, 0f);
            _body = body;
            _bodyRestPosition = body.localPosition;

            if (definition.Shape != EnemyShape.None)
            {
                BuildCreature(body, definition);
            }
            else if (definition.Model != null)
            {
                // Models sit on y = 0, the body pivot is at half height, so lower the model by that amount.
                Color tint = Color.Lerp(Color.white, definition.Color, ModelTintStrength);
                ModelFit.Place(definition.Model, body, new Vector3(0f, -scale * 0.5f, 0f), 0f, definition.ModelScale, tint, ModelEmission);
            }
            else
            {
                PrimitiveType shape = air ? PrimitiveType.Sphere : PrimitiveType.Capsule;
                GameObject main = PlaceholderVisuals.CreatePrimitive(shape, definition.Color, body, Vector3.zero, new Vector3(scale, scale * (air ? 1f : 0.6f), scale * 1.3f), BodyEmission);
                if (!air)
                {
                    main.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }

                float eyeX = scale * 0.22f;
                float eyeZ = scale * 0.55f;
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, EyeColor, body, new Vector3(eyeX, scale * 0.1f, eyeZ), Vector3.one * EyeSize, EyeEmission);
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, EyeColor, body, new Vector3(-eyeX, scale * 0.1f, eyeZ), Vector3.one * EyeSize, EyeEmission);
            }

            // Every rendered piece of the body is a candidate to fly off when the creature dies; legs fold first.
            _partFilters = body.GetComponentsInChildren<MeshFilter>();
            _partRenderers = new MeshRenderer[_partFilters.Length];
            _partCurls = new Quaternion[_partFilters.Length];
            for (int i = 0; i < _partFilters.Length; i++)
            {
                _partRenderers[i] = _partFilters[i].GetComponent<MeshRenderer>();
                _partCurls[i] = DeathCurlFor(_partFilters[i].transform);
            }

            float barY = _procedural ? _barHeight : BarOffsetY;
            GameObject bar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, BarColor, transform, new Vector3(0f, barY, 0f), new Vector3(BarWidth, BarHeight, BarHeight), BarEmission);
            _bar = bar.transform;
            bar.SetActive(false);
            GameObject armorBar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, ArmorBarColor, transform, new Vector3(0f, barY + BarHeight * 1.6f, 0f), new Vector3(BarWidth, BarHeight * 0.7f, BarHeight), BarEmission);
            _armorBar = armorBar.transform;
            armorBar.SetActive(false);

            if (air)
            {
                GameObject shadow = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, ShadowColor, transform, Vector3.zero, new Vector3(ShadowSize, 0.01f, ShadowSize));
                _shadow = shadow.transform;
            }
        }

        // Legs fold up against the body as the creature dies; everything else keeps its pose.
        private Quaternion DeathCurlFor(Transform part)
        {
            for (int i = 0; i < _legs.Count; i++)
            {
                if (_legs[i].Root == part)
                {
                    return _straightLegs ? Quaternion.Euler(DeathCurlDegrees * 0.6f, 0f, 0f) : Quaternion.Euler(0f, 0f, DeathCurlDegrees * _legs[i].Side);
                }

                if (_legs[i].Knee == part)
                {
                    return _straightLegs ? Quaternion.Euler(DeathCurlDegrees * 0.8f, 0f, 0f) : Quaternion.Euler(0f, 0f, -DeathCurlDegrees * 0.7f * _legs[i].Side);
                }
            }

            return Quaternion.identity;
        }

        private void OnHealthChanged(float _)
        {
            float fraction = _health.Fraction;
            if (fraction < _lastFraction)
            {
                _hitTime = 0f;
                Bleed();
            }

            _lastFraction = fraction;
            SetBar(fraction, _health.ArmorFraction);
        }

        // Plates come off in order as the armour is chipped away, and grow back when a boss regrows it.
        private void OnArmorChanged(float _)
        {
            float fraction = _health.ArmorFraction;
            int shown = Mathf.CeilToInt(fraction * _plates.Count - 0.001f);
            int visible = CountVisiblePlates();
            if (shown < visible)
            {
                for (int i = shown; i < visible; i++)
                {
                    ShedPlate(_plates[i]);
                }
            }
            else if (shown > visible)
            {
                ShowPlates(shown);
            }

            _lastArmor = fraction;
            SetBar(_health.Fraction, fraction);
        }

        private int CountVisiblePlates()
        {
            int count = 0;
            for (int i = 0; i < _plates.Count; i++)
            {
                if (!_plates[i].Hidden)
                {
                    count++;
                }
            }

            return count;
        }

        private void ShowPlates(int count)
        {
            for (int i = 0; i < _plates.Count; i++)
            {
                bool show = i < count;
                _plates[i].Hidden = !show;
                _plates[i].Root.gameObject.SetActive(show);
            }
        }

        // The plate flies off the body as a piece of debris and the creature bleeds where it was.
        private void ShedPlate(Rig plate)
        {
            if (plate.Hidden)
            {
                return;
            }

            plate.Hidden = true;
            plate.Root.gameObject.SetActive(false);
            if (_deaths == null)
            {
                return;
            }

            PartBuffer.Clear();
            MeshFilter filter = plate.Root.GetComponent<MeshFilter>();
            PartBuffer.Add(new EnemyDeathEffect.Part(filter.sharedMesh, _shellMaterials, plate.Root.position, plate.Root.rotation, plate.Root.lossyScale, plate.Root.rotation));
            _deaths.Play(PartBuffer, new EnemyDeathEffect.Request
            {
                Centre = _body.position,
                Momentum = Vector3.zero,
                Color = _enemy.Definition.Color,
                Blood = _enemy.Definition.BloodColor,
                Size = PlateDebrisSize * _creatureScale,
                GroundY = DeathGroundY,
                Style = DeathStyle.Shatter,
            });
        }

        private void Bleed()
        {
            if (_drips == null)
            {
                return;
            }

            _drips.transform.position = _body.position;
            _drips.Emit(DripBurst);
        }

        // A bite lunges; a spitter or a sting also sends a bolt from the mouth or the tail tip to what it hit.
        private void OnAttacked(Vector3 target)
        {
            _attackTime = 0f;
            _attackTarget = target;
            _whipSide = -_whipSide;
            if (_bolts == null || !_built)
            {
                return;
            }

            Vector3 from = _body.position + Vector3.up * (0.2f * _creatureScale);
            if (_enemy.Definition.AttackStyle == AttackStyle.Sting && _stingers.Count > 0)
            {
                from = _stingers[0].Root.position + Vector3.up * (0.5f * _creatureScale);
            }

            EnemyBoltView bolt = _bolts[_nextBolt];
            _nextBolt = (_nextBolt + 1) % _bolts.Length;
            bolt.Fire(from, target + Vector3.up * 0.4f, BoltSize * Mathf.Max(0.6f, _creatureScale));
        }

        // The body is handed to the death pool as it stands right now; the pooled enemy itself is recycled at once.
        private void OnKilled(Enemy enemy)
        {
            if (_deaths == null || !_built || _partFilters == null)
            {
                return;
            }

            PartBuffer.Clear();
            for (int i = 0; i < _partFilters.Length; i++)
            {
                Transform part = _partFilters[i].transform;
                if (!part.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Material[] materials = _shellMaterials ?? _partRenderers[i].sharedMaterials;
                PartBuffer.Add(new EnemyDeathEffect.Part(_partFilters[i].sharedMesh, materials, part.position, part.rotation, part.lossyScale, part.rotation * _partCurls[i]));
            }

            float size = _procedural ? _creatureScale : enemy.Definition.Scale;
            Vector3 momentum = enemy.Heading * (enemy.Definition.Speed * enemy.SpeedMultiplier);
            _deaths.Play(PartBuffer, new EnemyDeathEffect.Request
            {
                Centre = _body.position,
                Momentum = momentum,
                Color = enemy.Definition.Color,
                Blood = enemy.Definition.BloodColor,
                Size = size,
                GroundY = DeathGroundY,
                Style = enemy.Definition.DeathStyle,
            });
        }

        // Thin lines that only appear once the creature has taken damage, so a fresh wave is not a row of bars;
        // the pale one above is the armour, gone when the plates are.
        private void SetBar(float fraction, float armor)
        {
            _bar.localScale = new Vector3(BarWidth * Mathf.Max(0.001f, fraction), BarHeight, BarHeight);
            bool hurt = !_barHidden && fraction < FullHealthFraction;
            _bar.gameObject.SetActive(hurt);
            _armorBar.localScale = new Vector3(BarWidth * Mathf.Max(0.001f, armor), BarHeight * 0.7f, BarHeight);
            _armorBar.gameObject.SetActive(hurt && armor > 0f);
        }
    }
}
