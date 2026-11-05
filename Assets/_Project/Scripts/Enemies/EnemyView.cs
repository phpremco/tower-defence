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
        private const float BarWidth = 0.85f;
        private const float BarHeight = 0.085f;
        private const float PlateWidthScale = 1.1f;
        private const float PlateHeightScale = 3.2f;
        private static readonly Color PlateColor = new Color(0.02f, 0.02f, 0.04f, 0.5f);
        private const float MaxBarWidth = 2f;
        private const float BarOffsetY = 1.0f;
        private const float BarEmission = 0.7f;
        private const float BodyEmission = 0.9f;
        private const float FrostEmission = 0.6f;
        private const float FrostRate = 25f;
        private static readonly Color FrostColor = new Color(0.7f, 0.9f, 1f);
        private const float EyeEmission = 4f;
        private const float EyeSize = 0.14f;
        private const float ShadowSize = 0.6f;
        private const float ShadowHeight = 0.02f;
        // README: the painted shadow replaces the 0.6 disc at 0.6-0.9 across, black at 0.6 x its density.
        private const float BlobShadowSize = 0.8f;
        private static readonly Color BlobShadowTint = new Color(1f, 1f, 1f, 0.6f);
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
        private const float FinBeatDegrees = 14f;
        private const float FinRippleDegrees = 4f;
        private const float FinLagRadians = 0.7f;
        private const float FlapHeave = 0.05f;
        private const float FlapPitchDegrees = 4f;
        private const float BatFlapHeave = 0.012f;
        private const float BatFlapPitchDegrees = 1.5f;
        private const float WingSweepDegrees = 14f;
        private const float WingFeatherDegrees = 10f;
        // A pack hunter runs low and flat: little bounce, a long fore-and-aft stride and the body thrown forward.
        private const float RunnerPitchDegrees = 1.5f;
        private const float RunnerBounce = 0.5f;
        private const float RunnerLeanDegrees = 7f;
        private const float RunnerStretch = 1.35f;
        private const float HoverPulse = 0.04f;
        private const float HoverPulseSpeed = 2.6f;
        private const float GlideSway = 0.5f;
        private const float WavePhaseStep = 0.9f;
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
        private const float BipedBounce = 1.8f;
        private const float BipedSwayDistance = 0.07f;
        private const float BipedRollDegrees = 5f;
        private const float BipedSwaggerDegrees = 8f;
        private const float BipedLeanDegrees = 5f;
        private const float BossIdleShiftScale = 2.5f;
        private const float BossBreathBob = 0.02f;
        private const float BossBreathPitch = 1.5f;
        private const float BossBreathSpeed = 1.4f;
        private const float BossLookDegrees = 11f;
        private const float BossLookSpeed = 0.45f;
        private const float DisplayIntervalMin = 5f;
        private const float DisplayIntervalMax = 9f;
        private const float DisplaySeconds = 1.4f;
        private const float DisplayRearDegrees = 14f;
        private const float DisplayLift = 0.08f;
        private const float HeavyWalkerScale = 1.2f;
        private const int FootfallDust = 6;
        // An attack takes this share of the creature's own attack interval, within bounds: a slow heavy hitter
        // winds up longer than a snapping ant.
        private const float AttackIntervalShare = 0.45f;
        private const float AttackMinSeconds = 0.35f;
        private const float AttackMaxSeconds = 0.9f;
        private const float RearShare = 0.4f;
        private const float AttackLunge = 0.35f;
        private const float SlamLunge = 0.6f;
        private const float AttackRearBack = 0.12f;
        private const float AttackRearDegrees = 28f;
        private const float AttackStrikeDegrees = 32f;
        private const float BiteCrouch = 0.08f;
        private const float BiteCrouchDegrees = 14f;
        private const float BiteSnapDegrees = -12f;
        private const float BiteShakeDegrees = 9f;
        private const float SlamJump = 0.35f;
        private const float SlamRiseDegrees = -22f;
        private const float SlamLandDegrees = 30f;
        private const float SlashTwistDegrees = 18f;
        private const float StingDipDegrees = 10f;
        private const float BoltRearDegrees = -20f;
        private const float BoltSpitDegrees = 18f;
        private const float JawDegrees = 38f;
        private const int ImpactBurst = 10;
        private const int SlamDustBurst = 18;
        private const float ImpactHeight = 0.35f;
        private const float ClawDegrees = 50f;
        // A sword rests low in a guard, swings with the stride, rears high in the wind-up and comes down hard.
        private const float BladeRestDegrees = 15f;
        private const float BladeRaiseDegrees = -95f;
        private const float BladeSwingDegrees = 150f;
        private const float BladeIdleDegrees = 7f;
        private const float BladeWalkDegrees = 22f;
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
        private const float BossGlow = 1.4f;
        private const float BossPulseDepth = 0.35f;
        private const float BossPulseSpeed = 2.4f;
        private const float BossLightHeight = 0.9f;
        private const float BossLightIntensity = 2.6f;
        private const float BossLightRange = 7f;
        private const float HoverPoolRadius = 0.5f;
        private const float HoverPoolBrightness = 0.7f;
        private const float PlateDebrisSize = 0.3f;
        // A shed armour part of a modelled creature bursts into shards of its colour that fall back to the ground.
        private const int ShedShardBurst = 22;
        private const float ShedShardBrightness = 1.8f;
        private const float ShedShardGravity = 1.2f;
        // An imported model moves its own body: the procedural attack pose only adds a share of its lunge and pitch,
        // and none when the model has a strike clip of its own (Remco 2026-11-05: a hop stacked on the slugs' stretch
        // read as bobbing).
        private const float ModelPoseShare = 0.6f;
        private const float ModelStrikePoseShare = 0f;
        private const float ModelFlashAmount = 0.75f;
        private const float ModelFrostAmount = 0.5f;
        private const float BossFrostAmount = 0.18f;
        // A chilled boss cools only a shade: the overlay's own light on its dark body read as a grey wash (the Cinder
        // Matriarch smashing the Cryo that chills her, round 4b).
        private const float BossFrostShade = 0.12f;
        // A dead flyer comes to rest with its body centre this far (times its scale) above the ground.
        private const float FlyerRestHeight = 0.15f;
        private static readonly Color BarColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color ArmorBarColor = new Color(0.7f, 0.85f, 1f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f);
        private static readonly Color EyeColor = new Color(1f, 0.95f, 0.6f);
        private static readonly Color WaterSplash = new Color(0.6f, 0.85f, 1f);
        private static readonly Color LavaSplash = new Color(1f, 0.5f, 0.15f);
        private static readonly Color SandSplash = new Color(0.8f, 0.7f, 0.45f);
        private static readonly Color WoodSplash = new Color(0.45f, 0.95f, 0.4f);
        private static readonly Color IceSplash = new Color(0.8f, 0.95f, 1f);
        private static readonly Color BogSplash = new Color(0.45f, 0.6f, 0.25f);
        private static readonly Color RiftGravel = new Color(0.62f, 0.5f, 0.38f);
        private static readonly Color GroundDust = new Color(0.4f, 0.34f, 0.3f);
        private static readonly List<EnemyDeathEffect.Part> PartBuffer = new List<EnemyDeathEffect.Part>(32);
        // Made on first use: Unity forbids creating one in a field initializer of a MonoBehaviour.
        private static MaterialPropertyBlock _waveBlock;

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
        private WorldBar _worldBar;
        private Transform _bar;
        private Transform _armorBar;
        private Transform _barPlate;
        private float _barY;
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
        private bool _glides;
        private bool _bleeds;
        private float _hover;
        private float _ballRadius;
        private float _curl;
        private float _rollSpin;
        private float _clock;
        private float _nextDisplay;
        private float _displayTime = float.PositiveInfinity;
        private int _footSign;
        private float _barHeight;
        private float _attackTime = float.PositiveInfinity;
        private float _attackSeconds = AttackMinSeconds;
        private bool _boltPending;
        private bool _impactPending;
        private float _lastStrike;
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
        private Material[] _currentLook;
        private ParticleSystem _frost;
        private float _whipSide = 1f;
        private Vector3 _attackTarget;
        private Material[] _shellMaterials;
        private Material[] _flashMaterials;
        private Material[] _frostMaterials;
        private MeshFilter[] _partFilters;
        private MeshRenderer[] _partRenderers;
        private Quaternion[] _partCurls;
        private ParticleSystem _embers;
        private ParticleSystem _emergence;
        private ParticleSystem _drips;
        private ParticleSystem _rush;
        private ParticleSystem _impact;
        private EnemyBoltView[] _bolts;
        private readonly EnemyAttachments _attachments = new EnemyAttachments();
        private float _barWidth = BarWidth;
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
        private readonly List<Rig> _jaws = new List<Rig>();
        private readonly List<Renderer> _wavedRenderers = new List<Renderer>();
        private readonly List<SurfaceWave> _waves = new List<SurfaceWave>();
        private CreatureModel _model;
        private ModelShedParts _shed;
        private CreatureClips _clips;
        private BossAbilities _abilities;
        private BossAbilityKind[] _powerKinds;
        private bool _scripted;
        private bool _hidden;
        private readonly List<string> _hiddenParts = new List<string>(1);
        private Quaternion _tilt = Quaternion.identity;

        /// <summary>Gives the view the pool its death animation plays from. Without one the enemy simply disappears.</summary>
        public void Initialize(EnemyDeathEffectPool deaths) => _deaths = deaths;

        /// <summary>
        /// Shows the creature as if it climbed out long ago: for a cutscene that re-places a creature already on its way
        /// (a seek), so it does not climb out of the ground a second time.
        /// </summary>
        public void SkipSpawn()
        {
            _spawnTime = _spawnSeconds + 1f;
            _surfaced = true;
        }

        /// <summary>True when the creature wears an imported model with a clip of this role.</summary>
        public bool HasClip(ModelClipRole role) => _model != null && _model.Art.IndexOf(role) >= 0;

        /// <summary>The imported clip of a role (its length, moments and end offset), or null.</summary>
        public ModelClip ClipOf(ModelClipRole role)
        {
            int index = _model != null ? _model.Art.IndexOf(role) : -1;
            return index >= 0 ? _model.Art.Clips[index] : null;
        }

        /// <summary>The model's world scale: clip offsets in model units times this are world units.</summary>
        public float ModelScale => _creatureScale;

        /// <summary>
        /// Lets a boss mechanic play one clip of its own (a climb, a dive, a burrow) at a rate; held clips stay on their last
        /// frame. The creature's own cycle choice waits until <see cref="ReleaseScript"/>.
        /// </summary>
        public void PlayScripted(ModelClipRole role, float rate = 1f, bool hold = false)
        {
            int index = _model != null ? _model.Art.IndexOf(role) : -1;
            if (index < 0)
            {
                return;
            }

            _scripted = true;
            _model.Player.PlayOnce(index, 0f, hold, rate);
        }

        /// <summary>Loops a cycle of a boss mechanic (crawling on a ledge, travelling under the ground) at a rate.</summary>
        public void LoopScripted(ModelClipRole role, float rate = 1f)
        {
            int index = _model != null ? _model.Art.IndexOf(role) : -1;
            if (index < 0)
            {
                return;
            }

            _scripted = true;
            _model.Player.StopOnce();
            _model.Player.Loop(index, rate);
        }

        /// <summary>Seconds into the scripted one-off clip, or zero when none plays.</summary>
        public float ScriptedTime => _model != null ? _model.Player.OnceTime : 0f;

        /// <summary>Hands the clips back to the creature's own choice.</summary>
        public void ReleaseScript()
        {
            _scripted = false;
            _model?.Player.StopOnce();
        }

        /// <summary>A turn laid over the body (a climb's pitch, a landing's roll); identity for none.</summary>
        public Quaternion Tilt
        {
            set => _tilt = value;
        }

        /// <summary>Hides the whole body, its shadow and its bar (deep under a liquid), or shows them again.</summary>
        public void SetHidden(bool hidden)
        {
            if (_hidden == hidden || _model == null)
            {
                return;
            }

            _hidden = hidden;
            foreach (Renderer renderer in _model.Renderers)
            {
                renderer.enabled = !hidden;
            }

            if (!hidden)
            {
                foreach (string part in _hiddenParts)
                {
                    _model.SetPartVisible(part, false);
                }
            }

            if (_shadow != null)
            {
                _shadow.gameObject.SetActive(!hidden);
            }

            if (hidden)
            {
                _worldBar?.Show(false, false);
            }
            else if (_health != null)
            {
                SetBar(_health.Fraction, _health.ArmorMax > 0f ? _health.ArmorFraction : 0f);
            }
        }

        /// <summary>The bone of that name in each LOD's rig (a boss's icicles, for a view that grows them), or none.</summary>
        public Transform[] Bones(string bone) => _model != null ? _model.Bones(bone) : new Transform[0];

        /// <summary>Where a bone of the near model is in the world (a breath socket, an egg sac), or the body when it has none.</summary>
        public Vector3 SocketPosition(string bone) => _model != null ? _model.Socket(bone).position : transform.position;

        /// <summary>Shows or hides an extra part of the model (an ice armour); it stays so through hiding under a liquid.</summary>
        public void SetPartVisible(string part, bool visible)
        {
            if (visible)
            {
                _hiddenParts.Remove(part);
            }
            else if (!_hiddenParts.Contains(part))
            {
                _hiddenParts.Add(part);
            }

            if (!_hidden)
            {
                _model?.SetPartVisible(part, visible);
            }
        }

        /// <summary>Wears the art's glow look of that name (a boss's phase maps); empty or unknown puts its own glow back.</summary>
        public void SetEmissionSet(string setName)
        {
            _model?.SetEmissionSet(string.IsNullOrEmpty(setName) ? null : _model.Art.EmissionSet(setName), ModelQuality.IsLow);
        }

        /// <summary>Which cycle the creature travels in (a boss's hunting stride, its enraged walk); Move is its own.</summary>
        public void UseMoveCycle(ModelClipRole role) => _clips?.UseMove(role);

        /// <summary>Keeps the health bar out of sight, for creatures shown outside a fight.</summary>
        public void HideHealthBar()
        {
            _barHidden = true;
            _worldBar?.Show(false, false);
            if (_bar != null)
            {
                _bar.gameObject.SetActive(false);
                _armorBar.gameObject.SetActive(false);
                _barPlate.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _enemy.Initialized += OnInitialized;
            _enemy.Attacked += OnAttacked;
            _enemy.Killed += OnKilled;
            _enemy.Deactivated += OnDeactivated;
        }

        private void OnDestroy()
        {
            _enemy.Initialized -= OnInitialized;
            _enemy.Attacked -= OnAttacked;
            _enemy.Killed -= OnKilled;
            _enemy.Deactivated -= OnDeactivated;
            _model?.Dispose();
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

            if (_model != null)
            {
                AnimateModelled();
                return;
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
            _attackSeconds = Mathf.Clamp(enemy.Definition.AttackInterval * AttackIntervalShare, AttackMinSeconds, AttackMaxSeconds);
            _boltPending = false;
            _impactPending = false;
            _lastStrike = 0f;
            _curl = 0f;
            _rollSpin = 0f;
            _gaitRate = GaitPerUnit * Random.Range(1f - GaitVariation, 1f + GaitVariation);
            _swayPhase = Random.Range(0f, Mathf.PI * 2f);
            ApplyWaves();
            _clock = Random.Range(0f, 10f);
            _nextDisplay = _clock + Random.Range(DisplayIntervalMin, DisplayIntervalMax);
            _displayTime = float.PositiveInfinity;
            _footSign = 0;
            _health.Changed += OnHealthChanged;
            _health.ArmorChanged += OnArmorChanged;
            ShowPlates(_plates.Count);
            _shed?.SetShed(0);
            SetBar(1f, _health.ArmorMax > 0f ? 1f : 0f);
            _scripted = false;
            _tilt = Quaternion.identity;
            SetHidden(false);
            _clips?.Restart();
            foreach (string part in _hiddenParts)
            {
                _model?.SetPartVisible(part, true);
            }

            _hiddenParts.Clear();
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
                case EnemyOrigin.Ice:
                case EnemyOrigin.Bog:
                    _spawnSeconds = 0.9f;
                    _spawnDepth = 0.95f;
                    _spawnStartScale = 0.6f;
                    break;
                case EnemyOrigin.Nest:
                    _spawnSeconds = 1f;
                    _spawnDepth = enemy.Definition.Flies ? 1.4f / Mathf.Max(0.3f, _creatureScale) : 0.9f;
                    _spawnStartScale = 0.5f;
                    break;
                case EnemyOrigin.Rift:
                    // Claws its way up out of the crack, a little slower than a creature bursting from loose ground.
                    _spawnSeconds = 1.3f;
                    _spawnDepth = 1.1f;
                    _spawnStartScale = 0.75f;
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
                _emergence = _attachments.Track(ParticleKit.Create(transform.parent, "Emergence", Vector3.zero, Color.white, 0.5f, 1.1f, 0.05f, 0.14f, 1f, 3f, 0.9f, EmergeBurst * BossEmergeMultiplier * 2, 0.35f));
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
                case EnemyOrigin.Ice:
                    color = IceSplash * 1.3f;
                    break;
                case EnemyOrigin.Bog:
                    color = BogSplash;
                    break;
                case EnemyOrigin.Rift:
                    color = RiftGravel;
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
                case Element.Bog:
                    return BogSplash;
                case Element.Stone:
                    return RiftGravel;
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
            _glides = parts.Glides;
            _bleeds = parts.Bleeds;
            _hover = parts.HoverHeight * _creatureScale;
            _ballRadius = parts.BallRadius;
            body.localPosition = Vector3.zero;
            body.localScale = Vector3.one * _creatureScale;

            Material[] materials = EnemyModelFactory.CreateMaterials(definition.Color, definition.EyeColor, definition.AccentColor, definition.IsBoss ? BossGlow : 1f, parts.Skin);
            _shellMaterials = materials;
            if (definition.IsBoss)
            {
                AddBossPresence(body, definition, new[] { materials[EnemyModelFactory.Eyes], materials[EnemyModelFactory.Glow], materials[EnemyModelFactory.Accent] });
            }

            _flashMaterials = new Material[materials.Length];
            Material flash = SurfaceMaterials.Create(Color.white, FlashEmission, 0.3f);
            for (int i = 0; i < _flashMaterials.Length; i++)
            {
                _flashMaterials[i] = flash;
            }

            // Chilled: the whole creature goes pale blue, so a slow reads at a glance even on a crowd.
            _frostMaterials = new Material[materials.Length];
            Material frost = SurfaceMaterials.Create(FrostColor, FrostEmission, 0.6f);
            for (int i = 0; i < _frostMaterials.Length; i++)
            {
                _frostMaterials[i] = frost;
            }

            _shell = SpawnPart("Shell", body, parts.Body, materials, Vector3.zero, false);
            TrackWave(_shell, parts.BodyWave);
            foreach (CreaturePart spec in parts.Moving)
            {
                var rig = new Rig
                {
                    Spec = spec,
                    Side = spec.Mirrored ? -1f : 1f,
                    Root = SpawnPart(spec.Role.ToString(), body, spec.Mesh, materials, spec.Pivot, spec.Mirrored),
                };
                TrackWave(rig.Root, spec.Wave);
                if (spec.Lower != null)
                {
                    // The knee hangs inside the hip's frame, so the hip's mirror flips it along.
                    rig.Knee = SpawnPart("Knee", rig.Root, spec.Lower, materials, spec.Knee, false);
                }

                ListFor(spec.Role).Add(rig);
            }

            // Plates drop in index order, so they are kept sorted once.
            _plates.Sort((a, b) => a.Spec.Index.CompareTo(b.Spec.Index));
            BuildEffects(definition, parts.Embers);

            if (_hover > 0f)
            {
                GlowDecal.PlacePool(transform, definition.AccentColor * HoverPoolBrightness, HoverPoolRadius * _creatureScale, ShadowHeight);
            }

            _barHeight = parts.Height * _creatureScale + _hover + BarMargin;
        }

        // An imported creature: its model and clips carry the body's motion; the view adds the field around it (spawn,
        // drift, lunge, flash, frost, trails, bolts) the way it does for a procedural creature.
        private void BuildModelled(Transform body, EnemyDefinition definition)
        {
            ModelArtDefinition art = definition.Art;
            _boss = definition.IsBoss;
            _creatureScale = definition.Scale * art.Scale;
            _bleeds = art.Bleeds;
            _dangle = definition.Flies;
            body.localPosition = Vector3.zero;
            body.localScale = Vector3.one;
            _model = CreatureModel.Create(body, art, _creatureScale, ModelQuality.IsLow);
            _shellMaterials = _model.Materials;
            _shed = art.ShedBones.Length > 0 ? new ModelShedParts(_model) : null;
            _abilities = GetComponent<BossAbilities>();
            BossAbilityDefinition[] abilities = definition.Abilities;
            _powerKinds = new BossAbilityKind[abilities.Length];
            var triggers = new string[abilities.Length];
            for (int i = 0; i < abilities.Length; i++)
            {
                _powerKinds[i] = abilities[i].Kind;
                triggers[i] = abilities[i].Kind.ToString();
            }

            _clips = new CreatureClips(_model.Player, art, triggers);
            if (definition.IsBoss)
            {
                AddBossPresence(body, definition, _shellMaterials);
            }

            BuildEffects(definition, art.Embers);
            if (_clips.Lands)
            {
                GlowDecal.PlacePool(transform, definition.AccentColor * HoverPoolBrightness, HoverPoolRadius * _creatureScale, ShadowHeight);
            }

            _barHeight = art.Height * _creatureScale + BarMargin;
        }

        // The field around a creature: embers, drips, the haste trail, frost, sparks where its blows land and its bolts.
        private void BuildEffects(EnemyDefinition definition, bool embers)
        {
            if (embers)
            {
                Color ember = definition.Color * 1.6f;
                ember.a = 1f;
                _embers = _attachments.Track(ParticleKit.UseCell(ParticleKit.Create(transform, "Embers", Vector3.up * 0.2f, ember, 0.4f, 0.9f, 0.03f, 0.07f, 0.2f, 0.8f, -0.05f, 60, 0.25f), MarkerArt.ParticleCell.Ember));
            }

            if (_bleeds)
            {
                Color blood = definition.BloodColor * 1.5f;
                blood.a = 1f;
                _drips = _attachments.Track(ParticleKit.UseCell(ParticleKit.Create(transform.parent, "Drips", Vector3.zero, blood, 0.5f, 0.9f, 0.04f, 0.08f, 0.4f, 1.2f, 1.5f, DripBurst * 6, 0.15f), MarkerArt.ParticleCell.Drip));
            }

            Color rush = definition.AccentColor * 1.4f;
            rush.a = 1f;
            _rush = _attachments.Track(ParticleKit.UseCell(ParticleKit.Create(transform.parent, "Rush", Vector3.zero, rush, 0.25f, 0.45f, 0.02f, 0.05f, 0.1f, 0.4f, 0f, 40, 0.15f), MarkerArt.ParticleCell.Mote));
            _frost = _attachments.Track(ParticleKit.UseCell(ParticleKit.Create(transform.parent, "Frost", Vector3.zero, FrostColor, 0.5f, 1f, 0.03f, 0.07f, 0.15f, 0.5f, -0.1f, 40, 0.2f), MarkerArt.ParticleCell.FrostMote));
            // Sparks in the creature's colour where its blow lands, or a ring of dust when it slams the ground.
            _impact = _attachments.Track(ParticleKit.Create(transform.parent, "Impact", Vector3.zero, rush, 0.25f, 0.5f, 0.03f, 0.07f, 0.6f, 1.8f, 0.6f, SlamDustBurst * 2, 0.1f));

            if (definition.AttackStyle == AttackStyle.Bolt || definition.AttackStyle == AttackStyle.Sting)
            {
                _bolts = new EnemyBoltView[BoltCount];
                for (int i = 0; i < BoltCount; i++)
                {
                    _bolts[i] = _attachments.Track(EnemyBoltView.Create(transform.parent, definition.AccentColor));
                }
            }
        }

        // A boss burns brighter than its kin: its eyes and seams breathe, and a light in its colour spills over the ground.
        private static void AddBossPresence(Transform body, EnemyDefinition definition, Material[] glowing)
        {
            var pulse = body.gameObject.AddComponent<EmissionPulse>();
            pulse.Initialize(glowing, BossPulseDepth, BossPulseSpeed);

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

        private void TrackWave(Transform part, SurfaceWave wave)
        {
            if (wave.IsActive)
            {
                _wavedRenderers.Add(part.GetComponent<Renderer>());
                _waves.Add(wave);
            }
        }

        // The shader waves ride a property block, so they survive the flash and frost material swaps. Each individual
        // and each part starts at its own moment of the wave, so a school of rays does not ripple in unison.
        private void ApplyWaves()
        {
            if (_waves.Count == 0)
            {
                return;
            }

            _waveBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < _waves.Count; i++)
            {
                _waveBlock.Clear();
                _waves[i].Apply(_waveBlock, _swayPhase + i * WavePhaseStep);
                _wavedRenderers[i].SetPropertyBlock(_waveBlock);
            }
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
                case CreatureRole.Jaw:
                    return _jaws;
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
            PlayStrikeMoments(strike, style);

            // A boss shows itself off between attacks: it rears up, raises its weapons and opens its jaws, the
            // way the wind-up of an attack does, without striking.
            float display = _boss ? DisplayCurve(strike, rear) : 0f;
            float raise = Mathf.Max(rear, display);
            AnimateLegs(walking, strike);
            AnimateMandibles(raise, strike);
            AnimateSmallParts(walking, breathe);
            AnimateWeapons(raise, strike, style);
            if (_bands.Count > 0)
            {
                AnimateBands();
            }

            // Walking bounces the body; standing still it breathes; rolling it bumps along; floating it drifts up and
            // down. An attack rears back, strikes and settles, a slam throws the whole body. Walkers weave a little
            // with their stride, flyers drift and roll, and anything hurried leans into its run. A biped stomps:
            // the body drops onto each foot, rolls over the standing leg and swaggers with the shoulders.
            bool flyer = _dangle;
            bool hasted = _enemy.IsHasted;
            bool biped = _straightLegs && _legs.Count == 2;
            bool runner = _straightLegs && _legs.Count > 2;
            // A flyer rides its own wing beat: the body sinks as the wings rise and lifts on the downstroke, the
            // nose dipping into it. A ray's slow beat heaves it; a bat's quick one only trembles it.
            float flap = flyer ? Flap() : 0f;
            bool bat = _wings.Count > 0;
            float flapHeave = bat ? BatFlapHeave : FlapHeave;
            float flapPitch = bat ? BatFlapPitchDegrees : FlapPitchDegrees;
            float bob;
            if (_hover > 0f)
            {
                bob = _hover + Mathf.Sin(_clock * HoverSpeed + _swayPhase) * HoverBob * _creatureScale;
            }
            else if (flyer)
            {
                bob = -flap * flapHeave;
            }
            else if (_curl > 0.5f)
            {
                bob = Mathf.Abs(Mathf.Sin(_rollSpin * 2f)) * RollBounce;
            }
            else if (walking && !_glides)
            {
                // A runner bounds, higher than an insect scuttles; a crawler on its belly does not bounce at all.
                float bounce = biped ? BipedBounce : runner ? RunnerBounce : 1f;
                bob = (biped ? Mathf.Abs(Mathf.Cos(_gait)) : Mathf.Abs(Mathf.Sin(_gait))) * BobHeight * bounce;
            }
            else
            {
                bob = breathe * HoverHeight;
            }

            float swayWave = flyer ? Mathf.Sin(_clock * FlyerSwaySpeed + _swayPhase) : Mathf.Sin(_gait * SwayPerGait + _swayPhase) * (walking ? 1f : 0f);
            float sway = swayWave * (flyer ? FlyerSwayDistance : biped ? BipedSwayDistance : SwayDistance) * _creatureScale * (1f - _curl) * (_glides ? GlideSway : 1f);
            // Standing still the weight shifts slowly from foot to foot, so nothing stands like a statue; a boss
            // shifts more, breathes visibly and looks slowly about.
            float idle = walking || flyer ? 0f : 1f - _curl;
            float bossIdle = _boss ? 1f : 0f;
            float shift = Mathf.Sin(_clock * IdleShiftSpeed + _swayPhase) * IdleShiftDegrees * idle * (1f + bossIdle * (BossIdleShiftScale - 1f));
            float gaze = bossIdle * Mathf.Sin(_clock * BossLookSpeed + _swayPhase) * BossLookDegrees * (walking ? 0.4f : 1f) * (1f - _curl);
            float heave = bossIdle * Mathf.Sin(_clock * BossBreathSpeed + _swayPhase) * (1f - _curl);
            bob += heave * BossBreathBob;
            float yaw = flyer ? 0f : swayWave * (SwayDegrees + (biped ? BipedSwaggerDegrees : 0f)) * (1f - _curl) + shift * 1.2f + gaze;
            float roll = flyer ? -swayWave * FlyerRollDegrees : shift + (biped ? swayWave * BipedRollDegrees : 0f);
            if (style == AttackStyle.Whip)
            {
                roll += strike * FlyerRollDegrees * 2f * _whipSide;
            }

            // Every attack style moves the body its own way: a bite crouches and snaps with a shake of the head, a
            // slam leaps and comes down hard, a slash twists into the swing, a sting dips the front as the tail
            // comes over, a spitter throws its head back and forward, a whip rolls into the lash.
            AttackPose(style, rear, strike, out float attackPitch, out float attackYaw, out float attackLift, out float lunge);
            attackPitch -= display * DisplayRearDegrees;
            attackLift += display * DisplayLift;
            Vector3 forward = _enemy.Heading.sqrMagnitude > 0.0001f ? _enemy.Heading.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float along = (strike * lunge - rear * AttackRearBack - knock * HitKnockBack) * _creatureScale;
            float hoverScale = _hover > 0f ? 1f : _creatureScale;
            _body.position = position + Vector3.up * (bob * hoverScale - emerge + attackLift * _creatureScale) + forward * along + right * sway;
            float lean = (hasted && walking && !flyer ? HasteLeanDegrees : 0f) + (biped && walking ? BipedLeanDegrees : 0f) + (runner && walking ? RunnerLeanDegrees : 0f);
            // A four-legged runner rocks fore and aft with its bounds, the way a galloping dog does.
            float gallop = runner && walking ? Mathf.Sin(_gait) * RunnerPitchDegrees : 0f;
            Quaternion pitch = Quaternion.Euler(attackPitch + knock * HitFlinchDegrees + lean + heave * BossBreathPitch - flap * flapPitch + gallop, yaw + attackYaw, roll);
            PlayFootfalls(walking && !flyer && _hover <= 0f && _curl <= 0f && (_boss || _creatureScale >= HeavyWalkerScale));
            _body.rotation = Quaternion.LookRotation(forward) * pitch;
            float squash = style == AttackStyle.Slam ? strike * HitSquash : 0f;
            // A floating core throbs, so it never hangs in the air like a fixed lamp.
            float pulse = _hover > 0f ? Mathf.Sin(_clock * HoverPulseSpeed + _swayPhase) * HoverPulse : 0f;
            _body.localScale = Vector3.one * (_creatureScale * (1f + knock * HitSquash + squash + pulse) * grow);

            UpdateTrails(position, forward, walking, hasted, bob * hoverScale);

            // A boss is hit many times a second; flashing it white every time is a strobe, so it keeps its own glow.
            bool flash = _hitTime < FlashSeconds && !_boss;
            bool slowed = _enemy.IsSlowed;
            Material[] look = flash ? _flashMaterials : slowed ? _frostMaterials : _shellMaterials;
            if (look != _currentLook)
            {
                _currentLook = look;
                for (int i = 0; i < _partRenderers.Length; i++)
                {
                    _partRenderers[i].sharedMaterials = look;
                }
            }
        }

        // Embers while it moves, the haste trail behind a hurried creature, frost over a chilled one.
        private void UpdateTrails(Vector3 position, Vector3 forward, bool walking, bool hasted, float lift)
        {
            if (_embers != null)
            {
                ParticleKit.SetRate(_embers, walking ? EmberRate : EmberIdleRate);
            }

            _rush.transform.position = position - forward * (0.2f * _creatureScale) + Vector3.up * (0.1f * _creatureScale + lift);
            ParticleKit.SetRate(_rush, hasted && walking ? HasteTrailRate : 0f);
            _frost.transform.position = position + Vector3.up * (0.15f * _creatureScale + lift);
            ParticleKit.SetRate(_frost, _enemy.IsSlowed ? FrostRate : 0f);
        }

        // An imported creature: the clips move its legs, wings and body; on top the view turns it along its heading,
        // lets a flyer drift and roll, lunges into an attack, knocks it back on a hit, and lifts it out of its entrance.
        // Its own strike and power clips replace most of the procedural attack pose.
        private void AnimateModelled()
        {
            float dt = Time.deltaTime;
            _clock += dt;
            Vector3 position = _enemy.Position;
            float moved = (position - _lastPosition).magnitude;
            _lastPosition = position;
            bool walking = moved > StandStillDistance * dt;
            float strike = AttackCurve(out float rear);
            float knock = HitCurve();
            float grow = SpawnCurve(out float emerge);
            AttackStyle style = _enemy.Definition.AttackStyle;
            PlayStrikeMoments(strike, style);
            if (!_scripted)
            {
                AnticipatePowers();
            }

            _clips.Scripted = _scripted;
            _clips.Tick(dt, dt > 0f ? moved / dt : 0f, walking, _enemy.IsRolling, _creatureScale);
            _shed?.Apply();
            PlayClipMoment();

            Vector3 forward = _enemy.Heading.sqrMagnitude > 0.0001f ? _enemy.Heading.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float swayWave = _dangle ? Mathf.Sin(_clock * FlyerSwaySpeed + _swayPhase) : 0f;
            float sway = swayWave * FlyerSwayDistance * _creatureScale;
            AttackPose(style, rear, strike, out float attackPitch, out float attackYaw, out float attackLift, out float lunge);
            float pose = _clips.HasOwnStrike ? ModelStrikePoseShare : ModelPoseShare;
            float along = (strike * lunge * pose - rear * AttackRearBack * pose - knock * HitKnockBack) * _creatureScale;
            _body.position = position + Vector3.up * (attackLift * pose * _creatureScale - emerge) + forward * along + right * sway;
            float lean = _enemy.IsHasted && walking && !_dangle ? HasteLeanDegrees : 0f;
            float roll = -swayWave * FlyerRollDegrees;
            if (style == AttackStyle.Whip)
            {
                roll += strike * FlyerRollDegrees * 2f * _whipSide * pose;
            }

            _body.rotation = _tilt * Quaternion.LookRotation(forward) * Quaternion.Euler(attackPitch * pose + lean, attackYaw * pose, roll);
            _body.localScale = Vector3.one * grow;
            UpdateTrails(position, forward, walking, _enemy.IsHasted, 0f);

            // A boss is hit many times a second; flashing it white every time is a strobe, so it keeps its own glow.
            bool flash = _hitTime < FlashSeconds && !_boss;
            if (flash)
            {
                _model.SetOverlay(Color.white, ModelFlashAmount);
            }
            else if (_enemy.IsSlowed)
            {
                // A chilled boss keeps most of its own colours (the Cinder Matriarch is chilled all through her hunt).
                _model.SetOverlay(FrostColor * (_boss ? BossFrostShade : FrostEmission), _boss ? BossFrostAmount : ModelFrostAmount);
            }
            else
            {
                _model.SetOverlay(Color.white, 0f);
            }
        }

        // A boss's power clip starts early, so that its release frame falls on the moment the power fires.
        private void AnticipatePowers()
        {
            if (_abilities == null)
            {
                return;
            }

            for (int i = 0; i < _powerKinds.Length; i++)
            {
                _clips.Anticipate(i, _abilities.SecondsUntil(_powerKinds[i]));
            }
        }

        // The moment inside a clip (a brood leaving the vent, the acid leaving the gland, a sweep passing the front)
        // throws sparks in the creature's colour from the bone it comes out of.
        private void PlayClipMoment()
        {
            int clip = _clips.EventClip;
            if (clip < 0 || _impact == null)
            {
                return;
            }

            ModelClip moment = _model.Art.Clips[clip];
            if (moment.Role != ModelClipRole.Power)
            {
                return;
            }

            ParticleSystem.MainModule main = _impact.main;
            main.startColor = _rush.main.startColor;
            main.gravityModifier = 0.6f;
            _impact.transform.position = _model.Socket(moment.Socket).position;
            _impact.Emit(Mathf.RoundToInt(SlamDustBurst * Mathf.Clamp(_creatureScale, 0.6f, 2f)));
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
                    float stretch = _legs.Count > 2 ? RunnerStretch : 1f;
                    leg.Root.localRotation = Quaternion.Euler(restHip + swing * RunnerSwingDegrees * stretch - strike * ClawDegrees * 0.3f, 0f, -RunnerSplayDegrees * side);
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

        // The pose an attack puts the body in: pitch and yaw in degrees, a lift off the ground and how far the
        // body throws itself forward at the strike.
        private void AttackPose(AttackStyle style, float rear, float strike, out float pitch, out float yaw, out float lift, out float lunge)
        {
            yaw = 0f;
            lift = 0f;
            switch (style)
            {
                case AttackStyle.Bite:
                    // Crouch low with the nose down, then snap up and forward, shaking the head as the jaws close.
                    pitch = rear * BiteCrouchDegrees + strike * BiteSnapDegrees;
                    yaw = strike * Mathf.Sin(strike * Mathf.PI * 3f) * BiteShakeDegrees * _whipSide;
                    lift = -rear * BiteCrouch;
                    lunge = AttackLunge * 1.2f;
                    break;
                case AttackStyle.Slam:
                    // Spring up during the wind-up and crash down at the strike.
                    pitch = rear * SlamRiseDegrees + strike * SlamLandDegrees;
                    lift = rear * SlamJump;
                    lunge = SlamLunge;
                    break;
                case AttackStyle.Slash:
                    // Rear back and twist away from the blade, then swing through.
                    pitch = rear * -AttackRearDegrees + strike * AttackStrikeDegrees;
                    yaw = (rear - strike) * SlashTwistDegrees * _whipSide;
                    lunge = AttackLunge * 0.8f;
                    break;
                case AttackStyle.Sting:
                    // The front dips while the tail comes over the back.
                    pitch = rear * StingDipDegrees + strike * StingDipDegrees * 0.5f;
                    lunge = AttackLunge * 0.3f;
                    break;
                case AttackStyle.Bolt:
                    // The head goes back to gather the bolt and spits it forward.
                    pitch = rear * BoltRearDegrees + strike * BoltSpitDegrees;
                    lunge = -AttackLunge * 0.3f;
                    break;
                default:
                    pitch = rear * -AttackRearDegrees + strike * AttackStrikeDegrees;
                    lunge = AttackLunge;
                    break;
            }
        }

        // The moments inside an attack: a bolt leaves at the top of the strike, not when the wind-up starts, and
        // the blow landing throws sparks at the target, or dust when the whole body slams down.
        private void PlayStrikeMoments(float strike, AttackStyle style)
        {
            bool peaking = strike >= 0.5f && _lastStrike < 0.5f;
            _lastStrike = strike;
            if (!peaking)
            {
                return;
            }

            if (_boltPending)
            {
                _boltPending = false;
                FireBolt();
            }

            if (_impactPending && _impact != null)
            {
                _impactPending = false;
                ParticleSystem.MainModule main = _impact.main;
                bool slam = style == AttackStyle.Slam;
                main.startColor = slam ? GroundDust : _rush.main.startColor;
                main.gravityModifier = slam ? 0.3f : 0.6f;
                _impact.transform.position = slam
                    ? new Vector3(_body.position.x, DeathGroundY, _body.position.z)
                    : _attackTarget + Vector3.up * (ImpactHeight * Mathf.Max(0.6f, _creatureScale));
                _impact.Emit(Mathf.RoundToInt((slam ? SlamDustBurst : ImpactBurst) * Mathf.Clamp(_creatureScale, 0.6f, 2f)));
            }
        }

        private void FireBolt()
        {
            Vector3 from = _body.position + Vector3.up * (0.2f * _creatureScale);
            if (_enemy.Definition.AttackStyle == AttackStyle.Sting && _stingers.Count > 0)
            {
                from = _stingers[0].Root.position + Vector3.up * (0.5f * _creatureScale);
            }

            EnemyBoltView bolt = _bolts[_nextBolt];
            _nextBolt = (_nextBolt + 1) % _bolts.Length;
            bolt.Fire(from, _attackTarget + Vector3.up * 0.4f, BoltSize * Mathf.Max(0.6f, _creatureScale));
        }

        // Jaws hang a little open and work slowly; a strike opens them wide and snaps them shut. Arms (index one)
        // also swing down. A single lower jaw drops the same way and slams shut.
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

            for (int i = 0; i < _jaws.Count; i++)
            {
                _jaws[i].Root.localRotation = Quaternion.Euler(Mathf.Max(0f, open) * JawDegrees, 0f, 0f);
            }
        }

        // Blades rest raised, lift further in the wind-up and come down on the strike. A sting arches over the
        // back, sways, and snaps forward over the head; a whip lashes to one side and then the other.
        private void AnimateWeapons(float rear, float strike, AttackStyle style)
        {
            for (int i = 0; i < _blades.Count; i++)
            {
                Rig blade = _blades[i];
                float idle = Mathf.Sin(_clock * 1.6f + _swayPhase + blade.Spec.Index) * BladeIdleDegrees;
                // The arm swings against the legs, as an arm does, the second arm half a stride behind the first.
                float swing = Mathf.Sin(_gait + blade.Spec.Index * Mathf.PI) * BladeWalkDegrees * (1f - rear - strike);
                blade.Root.localRotation = Quaternion.Euler(BladeRestDegrees + idle + swing + rear * BladeRaiseDegrees + strike * BladeSwingDegrees, 0f, 0f);
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

            float beatSpeed = BeatSpeed();
            float beatDegrees = IsBat() ? BatBeatDegrees : WingBeatDegrees;
            for (int i = 0; i < _wings.Count; i++)
            {
                Rig part = _wings[i];
                float angle = _clock * beatSpeed + part.Spec.Phase;
                float beat = Mathf.Sin(angle) * beatDegrees;
                // The tip draws a figure of eight: the wing sweeps forward and twists its leading edge down on the
                // downstroke, back and up on the upstroke, instead of hinging up and down like a shutter.
                float lead = Mathf.Cos(angle);
                part.Root.localRotation = Quaternion.Euler(lead * WingFeatherDegrees, lead * WingSweepDegrees * part.Side, beat * part.Side);
            }

            for (int i = 0; i < _fins.Count; i++)
            {
                Rig part = _fins[i];
                // The slow beat hinges the whole fin a little; the wave the shader runs through the sheet does the
                // undulating. A second pair lags behind the first.
                float angle = FinAngle(part.Spec.Index);
                part.Root.localRotation = Quaternion.Euler(Mathf.Cos(angle) * FinRippleDegrees, 0f, Mathf.Sin(angle) * FinBeatDegrees * part.Side);
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

        private bool IsBat() => _dangle && _legs.Count > 0 && _wings.Count > 0 && _fins.Count == 0 && _tails.Count > 0;

        private float BeatSpeed() => IsBat() ? BatBeatSpeed : WingBeatSpeed;

        private float FinAngle(int index) => _clock * FinBeatSpeed + _swayPhase - index * FinLagRadians;

        // Where the wings are in their beat, from minus one (down) to one (up), in step with the fins or wings themselves.
        private float Flap()
        {
            if (_fins.Count > 0)
            {
                return Mathf.Sin(FinAngle(0));
            }

            return _wings.Count > 0 ? Mathf.Sin(_clock * BeatSpeed() + _wings[0].Spec.Phase) : 0f;
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
            if (_attackTime > _attackSeconds)
            {
                return 0f;
            }

            _attackTime += Time.deltaTime;
            float t = Mathf.Clamp01(_attackTime / _attackSeconds);
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

        // A hump from zero to one and back, every few seconds while nothing else is going on.
        private float DisplayCurve(float strike, float rear)
        {
            if (_displayTime < DisplaySeconds)
            {
                _displayTime += Time.deltaTime;
                return Mathf.Sin(Mathf.Clamp01(_displayTime / DisplaySeconds) * Mathf.PI);
            }

            if (_clock >= _nextDisplay && strike <= 0f && rear <= 0f && _curl <= 0f && _hitTime > HitSeconds)
            {
                _displayTime = 0f;
                _nextDisplay = _clock + Random.Range(DisplayIntervalMin, DisplayIntervalMax);
            }

            return 0f;
        }

        // A heavy walker throws up dust every time a front foot comes down: the foot lands when the gait passes
        // a quarter turn, the right one first, so the puff goes under whichever leg just planted.
        private void PlayFootfalls(bool stomping)
        {
            if (!stomping || _impact == null || _legs.Count < 2)
            {
                _footSign = 0;
                return;
            }

            int sign = Mathf.Cos(_gait) >= 0f ? 1 : -1;
            if (_footSign == 0)
            {
                _footSign = sign;
                return;
            }

            if (sign == _footSign)
            {
                return;
            }

            _footSign = sign;
            Vector3 foot = _legs[sign < 0 ? 0 : 1].Root.position;
            ParticleSystem.MainModule main = _impact.main;
            main.startColor = GroundDust;
            main.gravityModifier = 0.3f;
            _impact.transform.position = new Vector3(foot.x, DeathGroundY, foot.z);
            _impact.Emit(Mathf.RoundToInt(FootfallDust * Mathf.Clamp(_creatureScale, 0.8f, 2f)));
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

            if (definition.Art != null && definition.Art.Model != null)
            {
                BuildModelled(body, definition);
            }
            else if (definition.Shape != EnemyShape.None)
            {
                BuildCreature(body, definition);
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

            bool shaped = _procedural || _model != null;
            float barY = shaped ? _barHeight : BarOffsetY;
            // A big creature gets a wider line, so a boss shows its health as plainly as a grunt does.
            _barWidth = Mathf.Clamp(BarWidth * (shaped ? _creatureScale : definition.Scale), BarWidth, MaxBarWidth);
            _barY = barY;
            _worldBar = WorldBar.Create(MarkerArt.Active, transform, new Vector3(0f, barY, 0f), _barWidth, BarHeight, BarColor, ArmorBarColor, true);
            if (_worldBar == null)
            {
                BuildFallbackBars(barY);
            }

            if (air)
            {
                BuildShadow();
            }
        }

        // Without the Blender bar pieces (tests that build a creature on their own): plain cubes.
        private void BuildFallbackBars(float barY)
        {
            // A dark plate behind both lines, so the bars read against a bright floor as well as a dark one.
            GameObject plate = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, PlateColor, transform, new Vector3(0f, barY + BarHeight * 0.8f, BarHeight * 0.6f), new Vector3(_barWidth * PlateWidthScale, BarHeight * PlateHeightScale, BarHeight * 0.5f));
            _barPlate = plate.transform;
            MakeTranslucent(plate.GetComponent<Renderer>().sharedMaterial, PlateColor);
            plate.SetActive(false);
            GameObject bar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, BarColor, transform, new Vector3(0f, barY, 0f), new Vector3(_barWidth, BarHeight, BarHeight), BarEmission);
            _bar = bar.transform;
            bar.SetActive(false);
            GameObject armorBar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, ArmorBarColor, transform, new Vector3(0f, barY + BarHeight * 1.6f, 0f), new Vector3(_barWidth, BarHeight * 0.7f, BarHeight), BarEmission);
            _armorBar = armorBar.transform;
            armorBar.SetActive(false);
        }

        // A flyer's soft blob shadow on the ground (README round 25b), or a dark disc without the art.
        private void BuildShadow()
        {
            MarkerArt art = MarkerArt.Active;
            MeshRenderer blob = art != null ? art.Place(MarkerKind.BlobShadow, transform, "Shadow") : null;
            if (blob != null)
            {
                blob.transform.localScale = Vector3.one * BlobShadowSize;
                MarkerArt.SetTint(blob, new MaterialPropertyBlock(), BlobShadowTint);
                _shadow = blob.transform;
                return;
            }

            GameObject shadow = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, ShadowColor, transform, Vector3.zero, new Vector3(ShadowSize, 0.01f, ShadowSize));
            _shadow = shadow.transform;
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
                _clips?.Hit(_health.ArmorFraction > 0f);
            }

            _lastFraction = fraction;
            SetBar(fraction, _health.ArmorFraction);
        }

        // Plates come off in order as the armour is chipped away, and grow back when a boss regrows it.
        private void OnArmorChanged(float _)
        {
            float fraction = _health.ArmorFraction;
            if (_shed != null)
            {
                ShedModelParts(fraction);
            }

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

        // A modelled creature's armour parts come off in their shedding order, each throwing a burst of shards in the
        // creature's colour from where it sat; a regrown armour puts them back.
        private void ShedModelParts(float armorFraction)
        {
            int shed = _shed.Count - Mathf.CeilToInt(armorFraction * _shed.Count - 0.001f);
            for (int i = _shed.Shed; i < shed && _impact != null; i++)
            {
                ParticleSystem.MainModule main = _impact.main;
                Color shard = _enemy.Definition.Color * ShedShardBrightness;
                shard.a = 1f;
                main.startColor = shard;
                main.gravityModifier = ShedShardGravity;
                _impact.transform.position = _shed.Position(i);
                _impact.Emit(Mathf.RoundToInt(ShedShardBurst * Mathf.Clamp(_creatureScale, 0.6f, 2f)));
            }

            _shed.SetShed(shed);
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

        // An attack starts its wind-up; the bolt and the sparks wait for the strike itself (see PlayStrikeMoments).
        private void OnAttacked(Vector3 target)
        {
            _attackTime = 0f;
            _lastStrike = 0f;
            _attackTarget = target;
            _whipSide = -_whipSide;
            if (!_built)
            {
                return;
            }

            _boltPending = _bolts != null;
            _impactPending = _bolts == null;
            // The model's own strike lands on the attack's peak, where the bolt leaves and the sparks fly.
            _clips?.Attack(_attackSeconds * (RearShare + (1f - RearShare) * 0.5f));
        }

        // The body is handed to the death pool as it stands right now; the pooled enemy itself is recycled at once.
        // Everything that lives beside the creature (sparks, drips, bolts, dust) goes with it when it is taken off
        // the field, so nothing of a dead enemy lingers.
        private void OnDeactivated(Enemy enemy) => _attachments.Clear();

        private void OnKilled(Enemy enemy)
        {
            if (_deaths == null || !_built || _partFilters == null)
            {
                return;
            }

            if (_model != null)
            {
                _deaths.PlayModel(enemy.Definition.Art, _body.position, _body.rotation, _creatureScale, new EnemyDeathEffect.Request
                {
                    Centre = _body.position,
                    Momentum = enemy.Heading * (enemy.Definition.Speed * enemy.SpeedMultiplier),
                    Color = enemy.Definition.Color,
                    Blood = enemy.Definition.BloodColor,
                    Size = _creatureScale,
                    GroundY = DeathGroundY + (enemy.Definition.Art.BodyCentrePivot ? FlyerRestHeight * _creatureScale : 0f),
                    Style = enemy.Definition.DeathStyle,
                    Shed = _shed != null ? _shed.Shed : 0,
                });
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
        // The plate is a shade over the field, not a block: alpha blended, unlit by speculars, drawn with the
        // transparent queue so the bars on top of it still read.
        private static void MakeTranslucent(Material material, Color tint)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Smoothness", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", tint);
        }

        private void SetBar(float fraction, float armor)
        {
            if (_worldBar != null)
            {
                _worldBar.Set(fraction, armor, !_barHidden && fraction < FullHealthFraction);
                return;
            }

            // Both lines shrink from the right, like a gauge, instead of from both ends.
            _bar.localScale = new Vector3(_barWidth * Mathf.Max(0.001f, fraction), BarHeight, BarHeight);
            _bar.localPosition = new Vector3(-_barWidth * (1f - fraction) * 0.5f, _barY, 0f);
            bool hurt = !_barHidden && fraction < FullHealthFraction;
            _bar.gameObject.SetActive(hurt);
            _barPlate.gameObject.SetActive(hurt);
            _armorBar.localScale = new Vector3(_barWidth * Mathf.Max(0.001f, armor), BarHeight * 0.7f, BarHeight);
            _armorBar.localPosition = new Vector3(-_barWidth * (1f - armor) * 0.5f, _barY + BarHeight * 1.6f, 0f);
            _armorBar.gameObject.SetActive(hurt && armor > 0f);
        }
    }
}
