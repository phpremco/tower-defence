using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The field shots on the real first level: its terrain, props, liquids and mist (without gameplay markers), lit
    /// exactly as in play. On it the story is staged: the pathfinder capsule comes down burning, the commander
    /// steps out and plants a beacon by the crystals, the builder drones bring the base down module by module, the
    /// drill sinks into the ground, the haulers carry ore from the drill and the first crystal into the core, the
    /// ground shivers, the swarm bursts out of the ground and storms the first gate, the commander stares at it and
    /// runs back to hold the gate. Each action poses the whole field for its moment (earlier steps complete, later ones
    /// absent), from time alone; the drones and the swarm run on clocks that carry across the shots, so nothing jumps
    /// at a cut.
    /// </summary>
    public sealed class FieldSet : ICutsceneSet
    {
        private const float FallShare = 0.2f;
        private const float FallHeight = 42f;
        private const float FallBack = 16f;
        private const float FallSide = 7f;
        private const float StreakLength = 9f;
        private const float StreakWidth = 0.55f;
        private const float StreakIntensity = 1.6f;
        private const float HotEmission = 7f;
        private const float CoolShare = 0.6f;
        private const float HatchOpenDegrees = 105f;
        private const float LegsFrom = 0.12f;
        private const string CapsuleExit = "Exit";
        private const int LandingDust = 90;
        private const float SparkRate = 160f;
        private const float RetroRate = 120f;
        private const float SunSweepDegrees = 140f;
        private const float RunShare = 0.42f;
        private const float TurnShare = 0.5f;
        private const float DrawFrom = 0.5f;
        private const float DrawTo = 0.6f;
        private const float LookAroundDegrees = 45f;
        private const float ShiverDustRate = 30f;
        private const int Pebbles = 9;
        private const float PebbleSize = 0.22f;
        private const float PebbleJump = 0.05f;
        private const int VeinCrystals = 4;
        private const float VeinCrystalSize = 1.3f;

        // The drill shot: the rig rises over its first share, the string sinks between these shares of the shot.
        private const float DrillBuildTo = 0.15f;
        private const float DrillSinkFrom = 0.2f;
        private const float DrillSinkTo = 0.85f;
        private const float DrillCrackFrom = 0.3f;

        // The core shot: the crystal slides home over this share, then the core lights.
        private const float InsertFrom = 0.6f;
        private const float InsertTo = 0.68f;
        private const float IgniteFrom = 0.64f;
        private const float IgniteTo = 0.8f;
        private const float CarriedCrystalGlow = 0.8f;

        // The disbelief: a look, a step back, the hand to the visor, and down again.
        private const float ReactStepFrom = 0.18f;
        private const float ReactStepTo = 0.46f;
        private const float ReactStepBack = 1.1f;
        private const float ReactHandFrom = 0.26f;
        private const float ReactHandTo = 0.5f;
        private const float ReactHandDownFrom = 0.82f;
        // He stares at them: the head only searches a little, so the visor keeps facing the camera in front of him.
        private const float ReactHeadSearchDegrees = 5f;
        private const float HeroKeyIntensity = 1.6f;
        private const float ClearViewRadius = 1.3f;
        private const float HeroKeyRange = 4.5f;
        private static readonly Vector3 HeroKeyOffset = new Vector3(0.9f, 2f, 1.4f);
        private static readonly Color HeroKeyColour = new Color(0.7f, 0.85f, 1f);
        private static readonly Color HotColour = new Color(1f, 0.45f, 0.12f);
        private static readonly Color StreakHead = new Color(1f, 0.95f, 0.8f, 1f);
        private static readonly Color StreakTail = new Color(1f, 0.4f, 0.1f, 0f);
        private static readonly Color DustColour = new Color(0.55f, 0.57f, 0.6f, 0.45f);
        private static readonly Color SparkColour = new Color(1f, 0.65f, 0.25f);

        private readonly List<Transform> _pebbles = new List<Transform>();
        private readonly List<Vector3> _pebbleRest = new List<Vector3>();
        private CutsceneContext _context;
        private FieldLayout _layout;
        private VisualTheme _theme;
        private ThemeLighting.Lights _lights;
        private Quaternion _sunRest;
        private CutsceneAction _action;
        private float _lastProgress;

        private Transform _capsule;
        private Transform _hatch;
        private ArtModel _capsuleArt;
        private Material _shield;
        private LineRenderer _streak;
        private ParticleSystem _sparks;
        private ParticleSystem _retro;
        private ParticleSystem _landingDust;
        private Vector3 _fallStart;
        private Vector3 _door;
        private Vector3 _lookSpot;
        private Vector3 _beaconStand;

        private BeaconModel _beacon;
        private CutsceneHeroRig _hero;
        private FieldBase _base;
        private FieldDrill _drill;
        private FieldDrones _drones;
        private FieldSwarm _swarm;
        private ParticleSystem _shiverDust;
        private Light _heroLight;
        private EnvironmentInstancer[] _instancers = new EnvironmentInstancer[0];
        private float _harvestSeconds;
        private float _coreSeconds;
        private float _swarmShotSeconds;
        private float _reactShotSeconds;
        private float _standShotSeconds;

        public VisualTheme Theme => _theme;

        public void Build(Transform root, CutsceneContext context)
        {
            _context = context;
            LevelDefinition level = context.FieldLevel;
            _theme = level.Theme;
            _layout = new FieldLayout(level);
            _lights = ThemeLighting.Apply(_theme, root);
            _sunRest = _lights.Sun.transform.rotation;
            LevelVisualBuilder.Build(level, root, context.ParticleScale, context.FogEnabled, dressingOnly: true);

            CutsceneDefinition cutscene = context.Cutscene;
            _harvestSeconds = cutscene.CameraLengthOf(CutsceneAction.DronesHarvest);
            _coreSeconds = cutscene.CameraLengthOf(CutsceneAction.CoreIgnites);
            _swarmShotSeconds = cutscene.CameraLengthOf(CutsceneAction.SwarmPours);
            _reactShotSeconds = cutscene.CameraLengthOf(CutsceneAction.MercerReacts);
            _standShotSeconds = cutscene.CameraLengthOf(CutsceneAction.GateStand);

            BuildCapsule(root);
            BuildVein(root);
            _beacon = new BeaconModel(root, _theme.BaseColor, context.Cast != null ? context.Cast.Beacon : null, context.Cast != null ? context.Cast.BeaconArt : null);
            _beacon.Root.SetPositionAndRotation(_layout.BeaconSpot, _layout.LandingFacing);
            _base = new FieldBase(root, _layout, context);
            _layout.PlaceCore(_base.CoreSocket);
            float contact = (DrillSinkFrom + FieldDrill.ContactShareOfSink * (DrillSinkTo - DrillSinkFrom)) * cutscene.CameraLengthOf(CutsceneAction.DrillDown);
            _drill = new FieldDrill(root, _layout, context, contact);
            _drones = new FieldDrones(root, _base.PadTop, _layout.Ground(_drill.Pickup), context, _theme, _base.CrystalTop);
            _swarm = new FieldSwarm(root, _layout, context);
            if (context.Cast != null && context.Cast.Hero != null)
            {
                _hero = new CutsceneHeroRig(root, context.Cast.Hero);
                _heroLight = KeyLight(_hero.Root);
            }

            BuildShiver(root);
            _instancers = root.GetComponentsInChildren<EnvironmentInstancer>(true);
        }

        public void Enter(CutsceneAction action)
        {
            _action = action;
            _lastProgress = 0f;
            ThemeLighting.ApplyAtmosphere(_theme);
            _lights.Sun.transform.rotation = _sunRest;
            // The scheduled bursts (the drill biting, the ground breaking open) run on their own shot's particle clock.
            _drill.Enter(action == CutsceneAction.DrillDown);
            _swarm.Enter(action == CutsceneAction.SwarmPours);
        }

        public void Exit()
        {
            _drill.Silence();
            _drones.Hide();
        }

        public bool TryGetAnchor(CameraAnchor anchor, out Vector3 origin, out Quaternion facing) => _layout.TryGetAnchor(anchor, out origin, out facing);

        public void ClearView(IReadOnlyList<Vector3> eyes, IReadOnlyList<Vector3> targets)
        {
            foreach (EnvironmentInstancer instancer in _instancers)
            {
                instancer.ClearSightLines(eyes, targets, ClearViewRadius);
            }
        }

        public void Tick(float time, float progress)
        {
            PoseCapsule(time, progress);
            PoseBase(time, progress);
            PoseDrill(time, progress);
            PoseDrones(time, progress);
            PoseSwarm(time);
            PoseHero(time, progress);
            PoseShiver(time, progress);
            _lastProgress = progress;
        }

        // The actions' enum values are only appended; their story order is CutsceneActions.Order.
        private bool After(CutsceneAction step) => CutsceneActions.Order(_action) > CutsceneActions.Order(step);

        private bool AtOrAfter(CutsceneAction step) => CutsceneActions.Order(_action) >= CutsceneActions.Order(step);

        private bool At(CutsceneAction step) => _action == step;

        private void PoseCapsule(float time, float progress)
        {
            bool landing = At(CutsceneAction.PathfinderLands);
            float fall = landing ? CutsceneMath.Span(progress, 0f, FallShare) : 1f;
            float eased = 1f - Mathf.Pow(1f - fall, 2.2f);
            _capsule.position = Vector3.Lerp(_fallStart, _layout.Landing, eased);
            float heat = landing ? (fall < 1f ? 1f : 1f - CutsceneMath.Span(progress, FallShare, CoolShare)) : 0f;
            _shield.SetColor("_EmissionColor", HotColour * (HotEmission * heat * heat));

            bool falling = landing && fall < 1f;
            _streak.enabled = falling;
            if (falling)
            {
                Vector3 back = (_fallStart - _layout.Landing).normalized;
                _streak.SetPosition(0, _capsule.position + Vector3.up * 0.5f);
                _streak.SetPosition(1, _capsule.position + back * (StreakLength * (1f - eased) + 0.5f));
            }

            ParticleKit.SetRate(_sparks, falling && fall < 0.9f ? SparkRate : 0f);
            ParticleKit.SetRate(_retro, falling && fall > 0.55f ? RetroRate : 0f);
            if (landing && _lastProgress < FallShare && progress >= FallShare && progress - _lastProgress < FallShare)
            {
                _landingDust.Emit(LandingDust);
            }

            float open = landing ? CutsceneMath.Smooth(CutsceneMath.Span(progress, 0.26f, 0.32f)) : 1f;
            if (_capsuleArt != null)
            {
                PoseCapsuleArt(landing ? CutsceneMath.Span(progress, LegsFrom, FallShare) : 1f, open);
            }
            else
            {
                _hatch.localRotation = Quaternion.Euler(open * HatchOpenDegrees, 0f, 0f);
            }

            _beacon.Pose(landing ? CutsceneMath.Span(progress, 0.7f, 0.8f) : 1f, time);
        }

        // The legs unfold over the last of the fall (Land), then the ramp comes down (Open, which starts where Land ends).
        private void PoseCapsuleArt(float legs, float open)
        {
            if (open > 0f)
            {
                _capsuleArt.Sample(ModelClipRole.Open, open * _capsuleArt.LengthOf(ModelClipRole.Open), loop: false);
                return;
            }

            _capsuleArt.Sample(ModelClipRole.Land, legs * _capsuleArt.LengthOf(ModelClipRole.Land), loop: false);
        }


        private void PoseBase(float time, float progress)
        {
            float unfold = After(CutsceneAction.BaseUnfolds) ? 1f : At(CutsceneAction.BaseUnfolds) ? progress : 0f;
            _base.PoseUnfold(unfold, time, AtOrAfter(CutsceneAction.BaseUnfolds));
            _base.PoseGates(time);
            float ignite = After(CutsceneAction.CoreIgnites) ? 1f : At(CutsceneAction.CoreIgnites) ? CutsceneMath.Span(progress, IgniteFrom, IgniteTo) : 0f;
            // The Crysium the hauler carries in already shines; the core only lights once it sits in its socket.
            float carried = At(CutsceneAction.CoreIgnites) ? CarriedCrystalGlow : 0f;
            _base.PoseCore(ignite, carried, time);

            // The timelapse: the sun races across the sky, so the shadows of the new base sweep the ground.
            if (At(CutsceneAction.BaseUnfolds))
            {
                _lights.Sun.transform.rotation = Quaternion.Euler(_theme.SunPitch, _theme.SunYaw + (progress - 0.5f) * SunSweepDegrees, 0f);
            }
        }

        private void PoseDrill(float time, float progress)
        {
            if (At(CutsceneAction.DrillDown))
            {
                _drill.Pose(CutsceneMath.Span(progress, 0f, DrillBuildTo), CutsceneMath.Span(progress, DrillSinkFrom, DrillSinkTo), CutsceneMath.Span(progress, DrillCrackFrom, 1f), time, true);
                return;
            }

            bool built = After(CutsceneAction.DrillDown);
            _drill.Pose(built ? 1f : 0f, built ? 1f : 0f, built ? 1f : 0f, time, false);
        }

        // Parked on the pad from the drill shot, off in the harvest shot, the errand with the first crystal in the core
        // shot; the flock's clock runs on from the first take-off, so every drone is where the last shot left it.
        private void PoseDrones(float time, float progress)
        {
            if (!AtOrAfter(CutsceneAction.DrillDown))
            {
                _drones.Hide();
                _base.PoseCrystal(false, Vector3.zero, 0f);
                return;
            }

            if (At(CutsceneAction.DrillDown))
            {
                _drones.Park(time);
                _base.PoseCrystal(false, Vector3.zero, 0f);
                return;
            }

            if (At(CutsceneAction.DronesHarvest))
            {
                _drones.Pose(time, CutsceneMath.Span(progress, 0f, 0.35f), 1f);
                _base.PoseCrystal(false, Vector3.zero, 0f);
                return;
            }

            if (At(CutsceneAction.CoreIgnites))
            {
                _drones.Pose(_harvestSeconds + time, 1f, 1f, skip: 0);
                FieldDrones.Delivery delivery = _drones.PoseDelivery(progress, _harvestSeconds, _base.CoreSocket, time);
                _base.PoseCrystal(delivery.Carried, delivery.CarriedTop, CutsceneMath.Span(progress, InsertFrom, InsertTo));
                return;
            }

            bool attack = AtOrAfter(CutsceneAction.SwarmPours);
            _drones.Pose(_harvestSeconds + _coreSeconds + time, 1f, attack ? 0.15f : 1f);
            _base.PoseCrystal(false, Vector3.zero, 1f);
        }

        private void PoseSwarm(float time)
        {
            float clock;
            switch (_action)
            {
                case CutsceneAction.SwarmPours:
                    clock = time;
                    break;
                case CutsceneAction.MercerReacts:
                    clock = _swarmShotSeconds + time;
                    break;
                case CutsceneAction.GateStand:
                    clock = _swarmShotSeconds + _reactShotSeconds + time;
                    break;
                case CutsceneAction.Handoff:
                    clock = _swarmShotSeconds + _reactShotSeconds + _standShotSeconds + time;
                    break;
                default:
                    clock = 0f;
                    break;
            }

            _swarm.Pose(clock, time);
        }

        private void PoseHero(float time, float progress)
        {
            if (_hero == null)
            {
                return;
            }

            // The close shots of the commander get a soft cool key from the front, so his face reads against the night.
            _heroLight.enabled = At(CutsceneAction.GroundShivers) || At(CutsceneAction.MercerReacts) || At(CutsceneAction.GateStand);
            switch (_action)
            {
                case CutsceneAction.PathfinderLands:
                    PoseHeroLanding(time, progress);
                    break;
                case CutsceneAction.GroundShivers:
                case CutsceneAction.SwarmPours:
                    float scan = At(CutsceneAction.GroundShivers) ? Mathf.Sin(time * 1.4f) * LookAroundDegrees * CutsceneMath.Span(progress, 0.15f, 0.35f) : 0f;
                    Stand(_layout.DigSite, _layout.DigFacing, scan, 0f, time);
                    break;
                case CutsceneAction.MercerReacts:
                    PoseReaction(time, progress);
                    break;
                case CutsceneAction.GateStand:
                    PoseHeroRun(time, progress);
                    break;
                case CutsceneAction.Handoff:
                    Stand(_layout.GuardPost, _layout.GateFacing, 0f, 1f, time);
                    break;
                default:
                    // The base goes up around the landing site; the commander watches it from the beacon.
                    Stand(_beaconStand, Quaternion.LookRotation(Flat(_layout.Base - _beaconStand)), Mathf.Sin(time * 0.5f) * 15f, 0f, time);
                    break;
            }
        }

        // He cannot believe it: still facing them, he backs away from the gate, the hand comes up to the visor.
        private void PoseReaction(float time, float progress)
        {
            _hero.Root.gameObject.SetActive(true);
            float step = CutsceneMath.Smooth(CutsceneMath.Span(progress, ReactStepFrom, ReactStepTo)) * ReactStepBack;
            bool stepping = progress > ReactStepFrom && progress < ReactStepTo;
            float hand = CutsceneMath.Span(progress, ReactHandFrom, ReactHandTo) * (1f - CutsceneMath.Span(progress, ReactHandDownFrom, 1f));
            float head = Mathf.Sin(time * 0.9f) * ReactHeadSearchDegrees;
            _hero.React(_layout.DigSite, _layout.DigFacing.eulerAngles.y, step, stepping, hand, head, 0f, time);
        }

        // Where the reaction left him (a step back from the dig site), back to the gate at a run, round to face the
        // entrance behind its field and the blaster comes up.
        private void PoseHeroRun(float time, float progress)
        {
            _hero.Root.gameObject.SetActive(true);
            Vector3 from = _layout.Ground(_layout.DigSite - _layout.DigFacing * Vector3.forward * ReactStepBack);
            if (progress < RunShare)
            {
                Walk(from, _layout.GuardPost, CutsceneMath.Span(progress, 0f, RunShare), 1f, time);
                return;
            }

            Quaternion arrived = Quaternion.LookRotation(Flat(_layout.GuardPost - from));
            Quaternion facing = Quaternion.Slerp(arrived, _layout.GateFacing, CutsceneMath.Smooth(CutsceneMath.Span(progress, RunShare, TurnShare)));
            Stand(_layout.GuardPost, facing, 0f, CutsceneMath.Smooth(CutsceneMath.Span(progress, DrawFrom, DrawTo)), time);
        }

        // Out of the hatch, a look around, over to the crystals and the beacon goes up.
        private void PoseHeroLanding(float time, float progress)
        {
            _hero.Root.gameObject.SetActive(progress >= 0.3f);
            if (progress < 0.3f)
            {
                return;
            }

            if (progress < 0.4f)
            {
                Walk(_door, _lookSpot, CutsceneMath.Span(progress, 0.3f, 0.4f), 0f, time);
                return;
            }

            if (progress < 0.58f)
            {
                float look = Mathf.Sin(CutsceneMath.Span(progress, 0.4f, 0.58f) * Mathf.PI * 2f) * LookAroundDegrees;
                Stand(_lookSpot, Quaternion.LookRotation(Flat(_lookSpot - _door)), look, 0f, time);
                return;
            }

            if (progress < 0.68f)
            {
                Walk(_lookSpot, _beaconStand, CutsceneMath.Span(progress, 0.58f, 0.68f), 0f, time);
                return;
            }

            Stand(_beaconStand, Quaternion.LookRotation(Flat(_layout.BeaconSpot - _beaconStand)), 0f, 0f, time);
        }

        private void Walk(Vector3 from, Vector3 to, float share, float run, float time)
        {
            float distance = Vector3.Distance(from, to);
            Vector3 position = Vector3.Lerp(from, to, share);
            float stride = Mathf.Clamp01(Mathf.Min(share, 1f - share) * distance * 3f);
            float yaw = Quaternion.LookRotation(Flat(to - from)).eulerAngles.y;
            _hero.Root.gameObject.SetActive(true);
            _hero.Pose(position, yaw, stride, run, share * distance * CutsceneHeroRig.GaitPerUnit, 0f, 0f, time);
        }

        private void Stand(Vector3 position, Quaternion facing, float headYaw, float draw, float time)
        {
            _hero.Root.gameObject.SetActive(true);
            _hero.Pose(position, facing.eulerAngles.y, 0f, 0f, 0f, headYaw, draw, time);
        }

        // The ground trembles under the commander's boots: dust rises and loose stones jump.

        private void PoseShiver(float time, float progress)
        {
            bool shivering = At(CutsceneAction.GroundShivers);
            float strength = shivering ? CutsceneMath.Span(progress, 0.05f, 0.4f) : 0f;
            ParticleKit.SetRate(_shiverDust, ShiverDustRate * strength);
            for (int i = 0; i < _pebbles.Count; i++)
            {
                _pebbles[i].gameObject.SetActive(shivering || After(CutsceneAction.GroundShivers));
                float jump = Mathf.Abs(Mathf.Sin(time * (17f + i * 3.1f) + i)) * PebbleJump * strength;
                _pebbles[i].position = _pebbleRest[i] + Vector3.up * jump;
            }
        }

        private void BuildCapsule(Transform root)
        {
            CapsuleModelFactory.Parts parts = CapsuleModelFactory.Create();
            Material[] materials = ModelPalette.Create(_theme.BaseColor);
            _capsule = new GameObject("Capsule").transform;
            _capsule.SetParent(root, false);
            Quaternion facing = Quaternion.LookRotation(Flat(_layout.BeaconSpot - _layout.Landing));
            _capsule.SetPositionAndRotation(_layout.Landing, facing);
            GameObject custom = _context.Cast != null ? _context.Cast.Capsule : null;
            ModelArtDefinition art = _context.Cast != null ? _context.Cast.CapsuleArt : null;
            Vector3 door = _layout.Landing + facing * parts.Door;
            if (art != null)
            {
                // The imported capsule lands on its own legs and lowers its own ramp; Mercer steps out at its Exit point.
                _capsuleArt = ArtModel.CreateSampled(_capsule, art, ownMaterial: false);
                _shield = CapsuleArt.OwnShield(_capsuleArt);
                Transform exit = _capsuleArt.Find(CapsuleExit);
                if (exit != null)
                {
                    door = exit.position;
                }
            }
            else
            {
                if (custom != null)
                {
                    Object.Instantiate(custom, _capsule);
                }
                else
                {
                    AddMesh("Hull", _capsule, parts.Hull, materials);
                }

                _shield = SurfaceMaterials.Create(new Color(0.12f, 0.1f, 0.09f), 1f, 0.3f);
                AddMesh("Shield", _capsule, parts.Shield, new[] { _shield });
                _hatch = AddMesh("Hatch", _capsule, parts.Hatch, materials).transform;
                _hatch.localPosition = parts.HatchHinge;
            }

            Vector3 forward = facing * Vector3.forward;
            _door = _layout.Ground(door);
            _lookSpot = _layout.Ground(_door + forward * 0.7f);
            _beaconStand = _layout.Ground(_layout.BeaconSpot - forward * 0.75f);
            Vector3 side = _layout.LandingFacing * Vector3.right;
            _fallStart = _layout.Landing + Vector3.up * FallHeight - (_layout.LandingFacing * Vector3.forward) * FallBack + side * FallSide;

            // The Blender plasma trail, stretched once from the capsule (its start) to the tail.
            _streak = SpaceKit.Line(root, "Streak", 2, false, StreakIntensity, CutsceneSprite.PlasmaTrail);
            _streak.widthCurve = new AnimationCurve(new Keyframe(0f, StreakWidth), new Keyframe(1f, 0.02f));
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(StreakHead, 0f), new GradientColorKey(StreakTail, 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.3f), new GradientAlphaKey(0f, 1f) });
            _streak.colorGradient = gradient;
            _streak.shadowCastingMode = ShadowCastingMode.Off;
            _streak.enabled = false;

            _sparks = ParticleKit.Create(_capsule, "Sparks", Vector3.up * 0.4f, SparkColour * 3f, 0.3f, 0.9f, 0.05f, 0.14f, 1f, 4f, 0.4f, _context.Scaled(300), 0.4f);
            _retro = ParticleKit.Create(_capsule, "Retro", Vector3.up * 0.2f, new Color(0.6f, 0.85f, 1f) * 2.5f, 0.15f, 0.35f, 0.12f, 0.3f, 3f, 6f, 1.5f, _context.Scaled(200), 0.15f);
            _landingDust = ParticleKit.Create(root, "LandingDust", _layout.Landing + Vector3.up * 0.2f, DustColour, 1.2f, 2.6f, 0.5f, 1.2f, 1.5f, 4.5f, -0.03f, _context.Scaled(200), 1f);
        }

        // The crystal vein the beacon is planted by: the level's own Blender crystals, one big and a few small round it.
        private void BuildVein(Transform root)
        {
            bool low = _context.ParticleScale < 1f;
            for (int i = 0; i < VeinCrystals; i++)
            {
                float angle = CutsceneMath.Hash(i, 31) * Mathf.PI * 2f;
                float distance = i == 0 ? 0f : 0.5f + CutsceneMath.Hash(i, 33) * 0.5f;
                Vector3 position = _layout.Ground(_layout.Vein + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance);
                float size = i == 0 ? VeinCrystalSize : VeinCrystalSize * (0.45f + CutsceneMath.Hash(i, 37) * 0.3f);
                Transform crystal = CutsceneProps.Decor(root, $"Vein{i}", _theme.Environment, PropKind.Crystal, i, size, low, false);
                crystal.position = position;
            }

            var glow = new GameObject("VeinGlow");
            glow.transform.SetParent(root, false);
            glow.transform.position = _layout.Vein + Vector3.up * 0.8f;
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = _theme.AccentColor;
            light.intensity = 1.6f;
            light.range = 4f;
            light.shadows = LightShadows.None;
        }

        private void BuildShiver(Transform root)
        {
            _shiverDust = ParticleKit.Create(root, "ShiverDust", _layout.DigSite + Vector3.up * 0.1f, DustColour, 0.8f, 1.6f, 0.2f, 0.5f, 0.2f, 0.8f, -0.02f, _context.Scaled(120), 1.6f);
            for (int i = 0; i < Pebbles; i++)
            {
                float angle = i / (float)Pebbles * Mathf.PI * 2f + CutsceneMath.Hash(i, 41);
                float distance = 0.5f + CutsceneMath.Hash(i, 43) * 1.1f;
                Vector3 position = _layout.Ground(_layout.DigSite + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance);
                Transform pebble = CutsceneProps.Decor(root, $"Pebble{i}", _theme.Environment, PropKind.Rock, i, PebbleSize * (0.7f + CutsceneMath.Hash(i, 45) * 0.6f), _context.ParticleScale < 1f, false);
                pebble.position = position;
                _pebbles.Add(pebble);
                _pebbleRest.Add(position);
            }
        }

        private static Light KeyLight(Transform hero)
        {
            var go = new GameObject("KeyLight");
            go.transform.SetParent(hero, false);
            go.transform.localPosition = HeroKeyOffset;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = HeroKeyColour;
            light.intensity = HeroKeyIntensity;
            light.range = HeroKeyRange;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        private static GameObject AddMesh(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go;
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }
    }
}
