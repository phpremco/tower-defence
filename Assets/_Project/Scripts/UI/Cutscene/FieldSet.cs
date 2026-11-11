using System.Collections.Generic;
using Game.Core;
using Game.Hero;
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
        // The retro jets' plumes over the last of the fall (round 36): small hot cones from the four pods, and the buffeting they give the lens.
        private const float RetroFrom = 0.55f;
        private const float RetroFull = 0.62f;
        // Round 44 (Remco: "the ship seems to have no booster that is allowing him to land"): the pods' plumes are sized to the
        // capsule (they were 4 cm wide), burn steadily over the last of the fall and flare with every retro burst the shot sounds
        // (the burst cues of the landing shot are read, so sound and flame stay in step), and blow the dust up near the ground.
        private const float RetroRadius = 0.13f;
        private const float RetroLength = 3.4f;
        private const float RetroSplay = 0.3f;
        private const float RetroIntensity = 0.85f;
        private const float RetroSustain = 0.5f;
        private const float RetroPulse = 0.5f;
        private const float RetroPulseDecay = 0.28f;
        private const float RetroDustHeight = 9f;
        private const float RetroDustRate = 140f;
        private const float RetroGroundGap = 0.3f;
        private const float FallRumble = 0.3f;
        private const float RetroRumble = 0.45f;
        private const string CapsuleJet = "Jet";
        private static readonly Color RetroCore = new Color(0.9f, 0.96f, 1f) * 6f;
        private static readonly Color RetroEdge = new Color(0.35f, 0.65f, 1f) * 2.2f;
        private const float SunSweepDegrees = 140f;

        // Round 39 (Remco: "just poof he's out, then he turns 180 degrees, poof, then again poof 180 degrees"): a staged exit.
        // The ramp comes down in a hiss of venting steam with a warm light inside, he shows in the doorway in the thick of
        // the steam, steps out and down the ramp, looks round, walks on to the beacon spot and turns to it; every turn is
        // eased at a person's pace, and he only turns towards what he looks at.
        private const float HatchSteamFrom = 0.255f;
        private const float HatchSteamTo = 0.37f;
        private const float HatchSteamRate = 70f;
        private const float HatchLightFrom = 0.27f;
        private const float HatchLightTo = 0.33f;
        private const float HatchLightIntensity = 2.2f;
        private const float HatchLightRange = 3.5f;
        private static readonly Vector3 HatchLightOffset = new Vector3(0f, 0.75f, -0.15f);
        private static readonly Color HatchLightColour = new Color(1f, 0.9f, 0.75f);
        private static readonly Color SteamColour = new Color(0.82f, 0.86f, 0.9f, 0.5f);
        private const float AppearAt = 0.31f;
        private const float ExitFrom = 0.33f;
        private const float ExitTo = 0.45f;
        private const float LookTo = 0.58f;
        private const float WalkOverTo = 0.64f;
        // He steps off the ramp's foot this far before he stops to look round; he starts this far out of the ramp's hinge.
        private const float OffRampStep = 0.35f;
        private const float ThresholdInset = 0.04f;
        private const float MinWalk = 0.15f;
        // About 180 degrees in 0.75 s; while turning on the spot his feet step (the walk at a short stride).
        private const float TurnDegreesPerSecond = 240f;
        private const float LookBodyDegrees = 25f;
        private const float TurnStride = 0.55f;
        private const float StrideFullAtDegrees = 30f;
        private const float MetresPerHalfTurn = 0.9f;
        // The ramp is short: he keeps at least this much of a stride on it, so it reads as steps, not a glide.
        private const float StepOutStride = 0.35f;
        private const string RampBone = "Ramp";
        // The brood's last roar this far into the handoff shot, before it runs back into the ground (the generator's cue matches).
        public const float HandoffRoarSeconds = 0.3f;
        // Round 44 (Remco: "the queen is screaming, but he doesn't react to it"): the reaction shot cuts in on her scream, so he
        // jolts at once and then whips round to it; behind the gate her last roar jolts his aim.
        private const float ReactFlinchAt = 0.05f;
        // Footsteps (round 44): their sound follows the baked footfalls of the clip he plays; on the ramp (a foot this far above
        // the drawn ground) they ring on metal.
        private const float StepVolume = 0.4f;
        private const float RampFootHeight = 0.12f;
        // Round 39 (Remco: "the enemies shouldn't disappear until we clicked next"): while the gate dialogue stays open past
        // the stand shot's own length the brood holds, snapping and growling now and then, until the player reads on.
        private const float MenaceGap = 4.6f;
        private const float MenaceGrowl = 0.45f;
        private const float EyesOpenAfter = 1f;
        private const float EyesFullyOpen = 60f;
        private const float EyesShutSeconds = 0.6f;
        private const float RunShare = 0.42f;
        private const float DrawFrom = 0.5f;
        private const float DrawTo = 0.6f;
        private const float LookAroundDegrees = 45f;
        private const float ShiverDustRate = 30f;
        private const int Pebbles = 9;
        private const float PebbleSize = 0.22f;
        private const float PebbleJump = 0.05f;
        private const int VeinCrystals = 4;
        private const float VeinCrystalSize = 1.3f;
        private const float VeinClearance = 0.3f;
        private const float VeinDecorHeight = 0.4f;
        private const int VeinRings = 4;
        private const int VeinTurns = 8;
        private const float VeinRingStep = 0.35f;

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

        // Round 40 (Remco: "no facial change when he sees the enemies ... the backing up is so static, it looks fake"): with the
        // acting clips he looks aside, turns to the brood at the first growl, startles back with his hand to the gun, draws into a
        // nervous two-handed aim and holds it (also through the stand behind the gate, until the gate line is read); as they
        // retreat he lowers it slowly and stows it. The clips follow each other end to start, so nothing pops between beats.
        private const float ReactTurnAt = 0.3f;
        private const float HandoffLowerAt = 1.6f;
        private const float StowGap = 0.2f;

        // Round 42: his acting beats sound as the clock crosses them (the gasp as he startles, the holster as he draws, the
        // charge whine once the gun is in his hand), only when played forward: a seek or a jumped frame stays silent.
        private const float BeatWindow = 0.25f;
        private const float GaspVolume = 0.7f;
        private const float DrawVolume = 0.6f;
        private const float ChargeVolume = 0.45f;

        // The disbelief (without the acting clips): a look, a step back, then the Disbelief clip (hand to the chin guard, a slow head shake).
        private const float ReactStepFrom = 0.18f;
        private const float ReactStepTo = 0.46f;
        private const float ReactStepBack = 1.1f;
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
        private float _lastTime;

        private Transform _capsule;
        private Transform _hatch;
        private ArtModel _capsuleArt;
        private Material _shield;
        private LineRenderer _streak;
        private ParticleSystem _sparks;
        private ParticleSystem _retro;
        private ExhaustPlume[] _retroPlumes = new ExhaustPlume[0];
        private Transform[] _jets = new Transform[0];
        private ParticleSystem _landingDust;
        private ParticleSystem _retroDust;
        private float[] _retroBursts = new float[0];
        private Vector3 _fallStart;
        private Vector3 _door;
        private Vector3 _exitRoot;
        private Vector3 _capsuleSpot;
        private Vector3 _offRamp;
        private Vector3 _beaconStand;
        private Vector3[] _exitPath = new Vector3[0];
        private float _exitLength;
        private float _landingSeconds;
        private ParticleSystem _hatchSteam;
        private Light _hatchLight;

        private BeaconModel _beacon;
        private CutsceneHeroRig _hero;
        private FieldBase _base;
        private FieldDrill _drill;
        private FieldDrones _drones;
        private FieldSwarm _swarm;
        private FieldEyes _eyes;
        private ParticleSystem _shiverDust;
        private Light _heroLight;
        private EnvironmentInstancer[] _instancers = new EnvironmentInstancer[0];
        private float _harvestSeconds;
        private float _coreSeconds;
        private float _swarmShotSeconds;
        private float _reactShotSeconds;
        private float _standShotSeconds;
        // How long the stand shot ran (its own length on a seek, longer while the player kept its dialogue open), so the
        // swarm clock runs on without a jump into the handoff.
        private float _standHeld;
        private int _menaceBeat;

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
            _landingSeconds = cutscene.CameraLengthOf(CutsceneAction.PathfinderLands);
            _retroBursts = RetroBurstsOf(cutscene);

            BuildCapsule(root);
            BuildVein(root);
            _beacon = new BeaconModel(root, _theme.BaseColor, context.Cast != null ? context.Cast.Beacon : null, context.Cast != null ? context.Cast.BeaconArt : null);
            _beacon.Root.SetPositionAndRotation(_layout.BeaconSpot, _layout.LandingFacing);
            // The decor is drawn by now: the builders pick their landing spots clear of it.
            _base = new FieldBase(root, _layout, context, root.GetComponentsInChildren<EnvironmentInstancer>(true));
            _layout.PlaceCore(_base.CoreSocket);
            float contact = (DrillSinkFrom + FieldDrill.ContactShareOfSink * (DrillSinkTo - DrillSinkFrom)) * cutscene.CameraLengthOf(CutsceneAction.DrillDown);
            _drill = new FieldDrill(root, _layout, context, contact);
            Vector3 heap = _layout.Ground(_drill.Pickup);
            _drones = new FieldDrones(root, _base.PadTop, heap, context, _theme, _base.CrystalTop, () => _base.ParkingSpot(_layout, heap), _layout.SurfaceAt);
            _swarm = new FieldSwarm(root, _layout, context);
            _standHeld = _standShotSeconds;
            if (context.Cast != null && context.Cast.Hero != null)
            {
                _hero = new CutsceneHeroRig(root, context.Cast.Hero);
                _heroLight = KeyLight(_hero.Root);
            }

            BuildShiver(root);
            _instancers = root.GetComponentsInChildren<EnvironmentInstancer>(true);
            _eyes = new FieldEyes(root, _layout, _instancers, context.ParticleScale < 1f);
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
            _lastTime = 0f;
            _hero?.TookStep(false);
            // Only the stand shot played into the handoff carries its real length over; any other way in is a seek.
            if (action != CutsceneAction.Handoff)
            {
                _standHeld = _standShotSeconds;
                _menaceBeat = 0;
            }
        }

        public void Exit()
        {
            _drill.Silence();
            _base.Silence();
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
            PlayFootfall(time);
            PoseShiver(time, progress);
            PoseEyes(time);
            _lastProgress = progress;
            _lastTime = time;
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
            _capsule.position = Vector3.Lerp(_fallStart, _capsuleSpot, eased);
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
            float throttle = falling ? RetroThrottle(fall, progress * _landingSeconds) : 0f;
            PoseRetroPlumes(time, throttle, falling);
            float altitude = _capsule.position.y - _layout.Landing.y;
            ParticleKit.SetRate(_retroDust, RetroDustRate * throttle * (1f - Mathf.Clamp01(altitude / RetroDustHeight)));
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
            ParticleKit.SetRate(_hatchSteam, landing && progress >= HatchSteamFrom && progress < HatchSteamTo ? HatchSteamRate : 0f);
            float lit = landing ? CutsceneMath.Smooth(CutsceneMath.Span(progress, HatchLightFrom, HatchLightTo)) : 1f;
            _hatchLight.enabled = lit > 0f && !After(CutsceneAction.CoreIgnites);
            _hatchLight.intensity = HatchLightIntensity * lit;
        }

        // A steady burn that builds over the last of the fall, with a flare at each retro burst the shot sounds.
        private float RetroThrottle(float fall, float seconds)
        {
            float burn = CutsceneMath.Smooth(CutsceneMath.Span(fall, RetroFrom, RetroFull));
            if (burn <= 0f)
            {
                return 0f;
            }

            float pulse = 0f;
            foreach (float burst in _retroBursts)
            {
                float since = seconds - burst;
                if (since >= 0f)
                {
                    pulse = Mathf.Max(pulse, Mathf.Exp(-since / RetroPulseDecay));
                }
            }

            return Mathf.Clamp01(burn * RetroSustain + RetroPulse * pulse);
        }

        // The retro burst cues of the landing shot (its own seconds), so the flames flare where the sound fires.
        private static float[] RetroBurstsOf(CutsceneDefinition cutscene)
        {
            var bursts = new List<float>();
            foreach (CutsceneShot shot in cutscene.Shots)
            {
                if (shot.Action != CutsceneAction.PathfinderLands || shot.Cues == null)
                {
                    continue;
                }

                foreach (CutsceneCue cue in shot.Cues)
                {
                    if (cue.Kind == CueKind.Sound && cue.Sound == CutsceneSound.RetroBurst)
                    {
                        bursts.Add(cue.At);
                    }
                }

                break;
            }

            return bursts.ToArray();
        }

        // Each pod's plume leans out from the capsule's axis and down; the fall itself buffets the lens, the burn more.
        private void PoseRetroPlumes(float time, float throttle, bool falling)
        {
            Transform view = _context.Camera.transform;
            Vector3 axis = _capsule.position;
            for (int i = 0; i < _retroPlumes.Length; i++)
            {
                Vector3 mouth = _jets.Length > i ? _jets[i].position : _capsule.position + Vector3.up * 0.2f;
                Vector3 outward = mouth - axis;
                outward.y = 0f;
                Vector3 direction = Vector3.down + (outward.sqrMagnitude > 1e-4f ? outward.normalized * RetroSplay : Vector3.zero);
                // Near the ground the flame is cut short by it instead of running into it.
                float scale = _capsule.localScale.x;
                float clearance = Mathf.Max(RetroGroundGap, mouth.y - _layout.SurfaceAt(mouth.x, mouth.z));
                _retroPlumes[i].Pose(mouth, direction, RetroRadius * scale, Mathf.Min(RetroLength * scale, clearance), throttle, time + i * 0.37f, view);
            }

            if (falling)
            {
                _context.Rumble = FallRumble + RetroRumble * throttle;
            }
        }

        private void BuildRetroPlumes(Transform root)
        {
            _jets = _capsuleArt != null ? _capsuleArt.Numbered(CapsuleJet) : new Transform[0];
            int count = Mathf.Max(1, _jets.Length);
            _retroPlumes = new ExhaustPlume[count];
            for (int i = 0; i < count; i++)
            {
                _retroPlumes[i] = new ExhaustPlume(root, $"Retro{i}", RetroCore, RetroEdge, RetroIntensity, _context.ParticleScale >= 1f);
            }
        }

        // The steam that vents as the ramp comes down and the light inside the hatch that spills out over it.
        private void BuildHatch(Vector3 hinge)
        {
            Vector3 local = _capsule.InverseTransformPoint(hinge);
            _hatchSteam = ParticleKit.Create(_capsule, "HatchSteam", local + new Vector3(0f, 0.35f, 0.1f), SteamColour, 0.8f, 1.6f, 0.25f, 0.6f, 0.3f, 0.9f, -0.03f, _context.Scaled(160), 0.3f);
            ParticleKit.SetRate(_hatchSteam, 0f);
            var go = new GameObject("HatchLight");
            go.transform.SetParent(_capsule, false);
            go.transform.localPosition = local + HatchLightOffset;
            _hatchLight = go.AddComponent<Light>();
            _hatchLight.type = LightType.Point;
            _hatchLight.color = HatchLightColour;
            _hatchLight.range = HatchLightRange;
            _hatchLight.shadows = LightShadows.None;
            _hatchLight.intensity = 0f;
            _hatchLight.enabled = false;
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
                    _standHeld = Mathf.Max(_standHeld, time);
                    Menace(time);
                    break;
                case CutsceneAction.Handoff:
                    clock = _swarmShotSeconds + _reactShotSeconds + _standHeld + time;
                    break;
                default:
                    clock = 0f;
                    break;
            }

            // The retreat starts from the handoff, which only begins once the gate dialogue is read; before it they hold.
            _swarm.SetRetreat(At(CutsceneAction.Handoff) ? _swarmShotSeconds + _reactShotSeconds + _standHeld + HandoffRoarSeconds : float.PositiveInfinity);
            _swarm.Pose(clock, time);
            if (AtOrAfter(CutsceneAction.SwarmPours))
            {
                _context.Tremble = Mathf.Max(_context.Tremble, _swarm.Tremble(clock));
            }
        }

        // A growl from the brood every few seconds while the player keeps the stand's dialogue open past the shot's length.
        private void Menace(float time)
        {
            int beat = Mathf.FloorToInt((time - _standShotSeconds) / MenaceGap);
            if (beat <= _menaceBeat || time - _lastTime > MenaceGap)
            {
                _menaceBeat = Mathf.Max(_menaceBeat, beat);
                return;
            }

            _menaceBeat = beat;
            _context.Audio.Play(new CutsceneCue { Kind = CueKind.Sound, Sound = CutsceneSound.Growl, Strength = MenaceGrowl });
        }

        private void PoseHero(float time, float progress)
        {
            if (_hero == null)
            {
                return;
            }

            // The close shots of the commander get a soft cool key from the front, so his face reads against the night (round 40:
            // also as he climbs out of the capsule).
            _heroLight.enabled = At(CutsceneAction.GroundShivers) || At(CutsceneAction.MercerReacts) || At(CutsceneAction.GateStand) || (At(CutsceneAction.PathfinderLands) && progress >= AppearAt);
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
                    Jolt(time - ReactFlinchAt);
                    break;
                case CutsceneAction.GateStand:
                    PoseHeroRun(time, progress);
                    break;
                case CutsceneAction.Handoff:
                    PoseHeroHandoff(time);
                    Jolt(time - HandoffRoarSeconds);
                    break;
                default:
                    // The base goes up around the landing site; the commander watches it from the beacon.
                    Stand(_beaconStand, Quaternion.LookRotation(Flat(_layout.Base - _beaconStand)), Mathf.Sin(time * 0.5f) * 15f, 0f, time);
                    break;
            }

            // After the body: a sampled clip writes its own zero face curves, so the face goes on last (with the blinks).
            _hero.ApplyFace(time);
        }

        // The Queen's scream hits him: a startle on top of whatever he is doing, surprise and then fear in the face.
        private void Jolt(float since)
        {
            CutsceneCast cast = _context.Cast;
            float amount = CutsceneMath.Jolt(since, cast.FlinchAttack, cast.FlinchDecay);
            _hero.Flinch(amount);
            if (since >= 0f)
            {
                _hero.Face(cast.FlinchFace, since);
            }
        }

        // Footsteps follow his feet: a step sounds when the clip he plays crosses one of its baked footfalls, only when played forward.
        private void PlayFootfall(float time)
        {
            if (_hero == null)
            {
                return;
            }

            bool played = time >= _lastTime && time - _lastTime < BeatWindow;
            if (_hero.TookStep(played))
            {
                _context.Audio.Play(new CutsceneCue { Kind = CueKind.Sound, Sound = StepSurface(), Strength = StepVolume });
            }
        }

        // The ramp rings on metal, the run to the gate is on the road's stone, everything else is the plain's gravel.
        private CutsceneSound StepSurface()
        {
            if (At(CutsceneAction.GateStand))
            {
                return CutsceneSound.StepStone;
            }

            Vector3 foot = _hero.LowestFoot;
            bool onRamp = At(CutsceneAction.PathfinderLands) && foot.y > _layout.SurfaceAt(foot.x, foot.z) + RampFootHeight;
            return onRamp ? CutsceneSound.StepMetal : CutsceneSound.StepGravel;
        }

        // Round 40: the acting pass is there when the commander's model carries its clips; without them the older staging plays.
        private bool Acts => _hero.ClipLength(ModelClipRole.StartleBackstep) > 0f && _hero.ClipLength(ModelClipRole.DrawAimNervous) > 0f
            && _hero.ClipLength(ModelClipRole.AimHoldNervous) > 0f && _hero.ClipLength(ModelClipRole.TurnLookLeft) > 0f;

        // Where the Queen stands, seen from the dig site: what he turns to, startles from and backs away from.
        private float BroodYaw => Yaw(_swarm.QueenSpot - _layout.DigSite);

        // Where the startle left him: its travel back from the dig site, away from the Queen.
        private Vector3 StartleEnd
        {
            get
            {
                HeroActingClip startle = _hero.Acting(ModelClipRole.StartleBackstep);
                float back = startle != null ? startle.StartBack : 0f;
                return _layout.Ground(_layout.DigSite - Quaternion.Euler(0f, BroodYaw, 0f) * Vector3.forward * back);
            }
        }

        // The shot clock at which the nervous aim began in the stand behind the gate (its loop runs on into the handoff).
        private float AimFromSeconds => DrawFrom * _standShotSeconds;

        // He looks aside at the ground he came to dig; at the first growl he turns to the brood (the turn clip, the root turned by
        // its own turn at the first frame), startles back from them with the hand going to the gun, draws into a nervous aim
        // as the Queen roars, and holds it. Each clip starts where the last one ended.
        private void PoseActingReaction(float time)
        {
            _hero.Root.gameObject.SetActive(true);
            float brood = BroodYaw;
            HeroActingClip turnActing = _hero.Acting(ModelClipRole.TurnLookLeft);
            float turn = turnActing != null ? turnActing.StartTurn : 0f;
            float turnLength = _hero.ClipLength(ModelClipRole.TurnLookLeft);
            float startleAt = ReactTurnAt + turnLength;
            float drawAt = startleAt + _hero.ClipLength(ModelClipRole.StartleBackstep);
            float aimAt = drawAt + _hero.ClipLength(ModelClipRole.DrawAimNervous);
            Beat(startleAt, time, CutsceneSound.Gasp, GaspVolume);
            Beat(drawAt, time, CutsceneSound.PistolDraw, DrawVolume);
            Beat(drawAt + _hero.EventOf(ModelClipRole.DrawAimNervous), time, CutsceneSound.PistolCharge, ChargeVolume);
            if (time < ReactTurnAt)
            {
                // The idle reaches its first frame as the turn begins, which is the turn clip's first frame turned back.
                _hero.PoseClip(_layout.DigSite, brood - turn, ModelClipRole.Idle, time - ReactTurnAt, true, false);
                return;
            }

            if (time < startleAt)
            {
                float into = time - ReactTurnAt;
                _hero.PoseClip(_layout.DigSite, brood, ModelClipRole.TurnLookLeft, into, false, false);
                _hero.Face(ModelClipRole.TurnLookLeft, into);
                return;
            }

            Vector3 back = StartleEnd;
            if (time < drawAt)
            {
                float into = time - startleAt;
                _hero.PoseClip(back, brood, ModelClipRole.StartleBackstep, into, false, false);
                _hero.Face(ModelClipRole.StartleBackstep, into);
                return;
            }

            if (time < aimAt)
            {
                float into = time - drawAt;
                _hero.PoseClip(back, brood, ModelClipRole.DrawAimNervous, into, false, into >= _hero.EventOf(ModelClipRole.DrawAimNervous));
                _hero.Face(ModelClipRole.DrawAimNervous, into);
                return;
            }

            float held = time - aimAt;
            _hero.PoseClip(back, brood, ModelClipRole.AimHoldNervous, held, true, true);
            _hero.Face(ModelClipRole.AimHoldNervous, Mathf.Repeat(held, _hero.ClipLength(ModelClipRole.AimHoldNervous)));
        }

        private void Beat(float at, float time, CutsceneSound sound, float volume)
        {
            if (_lastTime < at && time >= at && time - _lastTime < BeatWindow)
            {
                _context.Audio.Play(new CutsceneCue { Kind = CueKind.Sound, Sound = sound, Strength = volume });
            }
        }

        // As the brood turns to run he trusts it slowly: the nervous aim cross-fades into the lowered weapon played slow, the
        // fear drains and the resolve sets in; then he stows the pistol.
        private void PoseHeroHandoff(float time)
        {
            if (!Acts)
            {
                Stand(_layout.GuardPost, _layout.GateFacing, 0f, 1f, time);
                return;
            }

            _hero.Root.gameObject.SetActive(true);
            CutsceneCast cast = _context.Cast;
            float yaw = _layout.GateFacing.eulerAngles.y;
            float aim = _standHeld - AimFromSeconds + time;
            float aimLength = _hero.ClipLength(ModelClipRole.AimHoldNervous);
            float lowered = (time - HandoffLowerAt) * cast.LowerSpeed;
            float lowerEnd = HandoffLowerAt + _hero.ClipLength(ModelClipRole.LowerWeapon) / cast.LowerSpeed;
            float stowAt = lowerEnd + StowGap;
            if (time < HandoffLowerAt)
            {
                _hero.PoseClip(_layout.GuardPost, yaw, ModelClipRole.AimHoldNervous, aim, true, true);
                _hero.Face(ModelClipRole.AimHoldNervous, Mathf.Repeat(aim, aimLength));
                return;
            }

            float share = cast.AimFadeSeconds > 0f ? CutsceneMath.Smooth(CutsceneMath.Span(time, HandoffLowerAt, HandoffLowerAt + cast.AimFadeSeconds)) : 1f;
            _hero.Face(ModelClipRole.AimHoldNervous, Mathf.Repeat(aim, aimLength), 1f - share);
            _hero.Face(cast.LowerFace, time - HandoffLowerAt, share);
            if (share < 1f)
            {
                _hero.PoseBlend(_layout.GuardPost, yaw, ModelClipRole.AimHoldNervous, aim, true, ModelClipRole.LowerWeapon, lowered, false, share, true);
                return;
            }

            if (time < stowAt || _hero.ClipLength(ModelClipRole.StowPistol) <= 0f)
            {
                _hero.PoseClip(_layout.GuardPost, yaw, ModelClipRole.LowerWeapon, lowered, false, true);
                return;
            }

            float stow = time - stowAt;
            _hero.PoseLayered(_layout.GuardPost, yaw, ModelClipRole.Idle, stow, ModelClipRole.StowPistol, stow, stow < _hero.EventOf(ModelClipRole.StowPistol));
        }

        // He cannot believe it: still facing them, he backs away from the gate, the hand comes up to the visor.
        private void PoseReaction(float time, float progress)
        {
            if (Acts)
            {
                PoseActingReaction(time);
                return;
            }

            _hero.Root.gameObject.SetActive(true);
            float step = CutsceneMath.Smooth(CutsceneMath.Span(progress, ReactStepFrom, ReactStepTo)) * ReactStepBack;
            bool stepping = progress > ReactStepFrom && progress < ReactStepTo;
            // Once he has backed away, the Disbelief clip (README round 35) from the moment the step ends.
            float since = Mathf.Max(0f, progress - ReactStepTo) * _reactShotSeconds;
            _hero.Disbelief(_layout.DigSite, _layout.DigFacing.eulerAngles.y, step, stepping, since, time);
        }

        // Where the reaction left him (a step back from the dig site), back to the gate at a run, round to face the
        // entrance behind its field and the blaster comes up. Round 40 (acting): he runs with the pistol he drew in his hand,
        // with the nervous aim's face, and at the post the aim comes up into the nervous hold, which loops for as long as the
        // player keeps the gate line open (the brood holds too).
        private void PoseHeroRun(float time, float progress)
        {
            _hero.Root.gameObject.SetActive(true);
            bool acts = Acts;
            Vector3 from = acts ? StartleEnd : _layout.Ground(_layout.DigSite - _layout.DigFacing * Vector3.forward * ReactStepBack);
            float aimLength = acts ? _hero.ClipLength(ModelClipRole.AimHoldNervous) : 0f;
            if (acts)
            {
                _hero.Face(ModelClipRole.AimHoldNervous, Mathf.Repeat(time, aimLength));
            }

            if (progress < RunShare)
            {
                Walk(from, _layout.GuardPost, CutsceneMath.Span(progress, 0f, RunShare), 1f, time, Yaw(_layout.GuardPost - from), acts);
                return;
            }

            // Round 39: round at a person's pace with his feet stepping, then the blaster comes up.
            float arrived = Yaw(_layout.GuardPost - from);
            float gate = _layout.GateFacing.eulerAngles.y;
            float yaw = CutsceneMath.EasedTurn(arrived, gate, (progress - RunShare) * _standShotSeconds, TurnDegreesPerSecond);
            if (acts && time >= AimFromSeconds)
            {
                float aim = time - AimFromSeconds;
                float fade = _context.Cast.AimFadeSeconds;
                float share = fade > 0f ? CutsceneMath.Smooth(Mathf.Clamp01(aim / fade)) : 1f;
                if (share < 1f)
                {
                    _hero.PoseBlend(_layout.GuardPost, yaw, ModelClipRole.Idle, time, true, ModelClipRole.AimHoldNervous, aim, true, share, true);
                    return;
                }

                _hero.PoseClip(_layout.GuardPost, yaw, ModelClipRole.AimHoldNervous, aim, true, true);
                return;
            }

            float draw = acts ? 0f : CutsceneMath.Smooth(CutsceneMath.Span(progress, DrawFrom, DrawTo));
            if (draw > 0f)
            {
                Stand(_layout.GuardPost, Quaternion.Euler(0f, yaw, 0f), 0f, draw, time);
                return;
            }

            TurnOnTheSpot(_layout.GuardPost, yaw, Mathf.Abs(Mathf.DeltaAngle(yaw, gate)), Mathf.Abs(Mathf.DeltaAngle(arrived, yaw)), 0f, time, acts);
        }

        // Round 39: in the doorway in the steam, out and down the ramp, a look round, on to the beacon spot and a turn to the
        // beacon as it goes up. Nothing snaps: each beat starts on the heading the last one ended on. Round 40: with the
        // Capsule_Exit clip he climbs out crouched through the doorway and down the ramp (a breath, a look left and right are
        // in the clip and its face), its root on the capsule's Exit socket.
        private void PoseHeroLanding(float time, float progress)
        {
            _hero.Root.gameObject.SetActive(progress >= AppearAt);
            if (progress < AppearAt)
            {
                return;
            }

            float seconds = progress * _landingSeconds;
            float outward = Yaw(_offRamp - _door);
            ModelClipRole exitClip = _context.Cast.CapsuleExitClip;
            float exitLength = _hero.ClipLength(exitClip);
            Vector3 start = _offRamp;
            float lookTo = LookTo;
            if (exitLength > 0f)
            {
                float into = Mathf.Max(0f, seconds - ExitFrom * _landingSeconds);
                lookTo = ExitFrom + exitLength / _landingSeconds;
                if (progress < lookTo)
                {
                    _hero.PoseClip(_exitRoot, outward, exitClip, into, false, false);
                    _hero.Face(exitClip, into);
                    return;
                }

                // The clip ends in the idle's first frame on its root: the idle runs on from there.
                start = _exitRoot;
                time = seconds - lookTo * _landingSeconds;
            }
            else if (progress < ExitFrom)
            {
                Stand(_exitPath[0], Quaternion.Euler(0f, outward, 0f), 0f, 0f, time);
                return;
            }
            else if (progress < ExitTo)
            {
                StepOut(CutsceneMath.Span(progress, ExitFrom, ExitTo), outward, time);
                return;
            }
            else if (progress < LookTo)
            {
                // A look round: the head sweeps, the body turns a little with it and back, the feet stepping as it turns.
                float phase = CutsceneMath.Span(progress, ExitTo, LookTo) * Mathf.PI * 2f;
                float look = Mathf.Sin(phase);
                float rate = Mathf.Abs(Mathf.Cos(phase)) * LookBodyDegrees;
                TurnOnTheSpot(_offRamp, outward + look * LookBodyDegrees, rate, Mathf.Abs(look) * LookBodyDegrees, look * (LookAroundDegrees - LookBodyDegrees), time);
                return;
            }

            bool walks = Vector3.Distance(Flat(start), Flat(_beaconStand)) > MinWalk && Vector3.Dot(_beaconStand - start, Quaternion.Euler(0f, outward, 0f) * Vector3.forward) > MinWalk;
            float over = walks ? Yaw(_beaconStand - start) : outward;
            float walkTo = Mathf.Max(WalkOverTo, lookTo + (WalkOverTo - LookTo));
            if (progress < walkTo && walks)
            {
                float yaw = CutsceneMath.EasedTurn(outward, over, seconds - lookTo * _landingSeconds, TurnDegreesPerSecond);
                Walk(start, _beaconStand, CutsceneMath.Span(progress, lookTo, walkTo), 0f, time, yaw, false);
                return;
            }

            Vector3 at = walks ? _beaconStand : start;
            float from = walks ? over : outward;
            float to = Yaw(_layout.BeaconSpot - at);
            float since = seconds - (walks ? walkTo : lookTo) * _landingSeconds;
            float turned = CutsceneMath.EasedTurn(from, to, since, TurnDegreesPerSecond);
            TurnOnTheSpot(at, turned, Mathf.Abs(Mathf.DeltaAngle(turned, to)), Mathf.Abs(Mathf.DeltaAngle(from, turned)), 0f, time, false);
        }

        // Without the exit clip: through the doorway and down the ramp to the ground with his walk, the feet keeping to the
        // distance covered.
        private void StepOut(float share, float yaw, float time)
        {
            float walked = share * _exitLength;
            Vector3 position = FieldLayout.PointAlong(_exitPath, walked);
            _hero.Root.gameObject.SetActive(true);
            float stride = Mathf.Clamp01(Mathf.Min(walked, _exitLength - walked) * 3f + StepOutStride);
            _hero.Pose(position, yaw, stride, 0f, walked * CutsceneHeroRig.GaitPerUnit, 0f, 0f, time);
        }

        // Turning where he stands: the feet step while the heading still has a way to go (a short stride that fades as the
        // turn ends), at the gait the turned angle gives, so the steps keep pace with the turn.
        private void TurnOnTheSpot(Vector3 position, float yaw, float remainingDegrees, float turnedDegrees, float headYaw, float time, bool pistolInHand = false)
        {
            float stride = TurnStride * Mathf.Clamp01(remainingDegrees / StrideFullAtDegrees);
            float gait = turnedDegrees / 180f * MetresPerHalfTurn * CutsceneHeroRig.GaitPerUnit;
            _hero.Root.gameObject.SetActive(true);
            if (pistolInHand)
            {
                _hero.Travel(position, yaw, stride, 0f, gait, time, true);
                return;
            }

            _hero.Pose(position, yaw, stride, 0f, gait, headYaw, 0f, time);
        }

        private static float Yaw(Vector3 direction) => Quaternion.LookRotation(Flat(direction)).eulerAngles.y;

        // The heading is passed in, so a walk can start by easing round from where he was facing; a drawn pistol stays in the hand.
        private void Walk(Vector3 from, Vector3 to, float share, float run, float time, float yaw, bool pistolInHand)
        {
            float distance = Vector3.Distance(from, to);
            Vector3 position = Vector3.Lerp(from, to, share);
            float stride = Mathf.Clamp01(Mathf.Min(share, 1f - share) * distance * 3f);
            _hero.Root.gameObject.SetActive(true);
            if (pistolInHand)
            {
                _hero.Travel(position, yaw, stride, run, share * distance * CutsceneHeroRig.GaitPerUnit, time, true);
                return;
            }

            _hero.Pose(position, yaw, stride, run, share * distance * CutsceneHeroRig.GaitPerUnit, 0f, 0f, time);
        }

        private void Stand(Vector3 position, Quaternion facing, float headYaw, float draw, float time)
        {
            _hero.Root.gameObject.SetActive(true);
            _hero.Pose(position, facing.eulerAngles.y, 0f, 0f, 0f, headYaw, draw, time);
        }

        // The eyes between the rocks open with the shiver (again in the radar shot, as more of them wake), watch through
        // the attack and shut at the last roar of the handoff.
        private void PoseEyes(float time)
        {
            float opening = At(CutsceneAction.GroundShivers) ? time - EyesOpenAfter : AtOrAfter(CutsceneAction.SwarmPours) ? EyesFullyOpen : -1f;
            float shut = At(CutsceneAction.Handoff) ? CutsceneMath.Span(time, HandoffRoarSeconds, HandoffRoarSeconds + EyesShutSeconds) : 0f;
            _eyes.Pose(opening, shut, time, _context.Camera);
        }

        // The ground trembles under the commander's boots: dust rises and loose stones jump.

        private void PoseShiver(float time, float progress)
        {
            bool shivering = At(CutsceneAction.GroundShivers);
            float strength = shivering ? CutsceneMath.Span(progress, 0.05f, 0.4f) : 0f;
            ParticleKit.SetRate(_shiverDust, ShiverDustRate * strength);
            if (shivering)
            {
                // Round 44: the quake rumble follows the jumping stones.
                _context.Tremble = strength;
            }
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
            // Round 40: on the ground the capsule is shown at the cast's scale, so Spencer fits its doorway (the orbit keeps its own).
            _capsule.localScale = Vector3.one * (_context.Cast != null ? _context.Cast.LandedCapsuleScale : 1f);
            GameObject custom = _context.Cast != null ? _context.Cast.Capsule : null;
            ModelArtDefinition art = _context.Cast != null ? _context.Cast.CapsuleArt : null;
            Vector3 door = _capsule.TransformPoint(parts.Door);
            Vector3 hinge = _capsule.TransformPoint(parts.HatchHinge);
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

                Transform ramp = _capsuleArt.Find(RampBone);
                if (ramp != null)
                {
                    hinge = ramp.position;
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
            // The larger capsule sits back by what it grew, so its door step stays where the scale-1 capsule had it: the walk to
            // the beacon and the clearance round the landing keep their round-39 distances.
            Vector3 outward = door - _layout.Landing;
            outward.y = 0f;
            Vector3 grown = outward * (1f - 1f / _capsule.localScale.x);
            _capsule.position -= grown;
            door -= grown;
            hinge -= grown;
            _capsuleSpot = _capsule.position;
            // The exit clip's root: the Exit socket on the capsule's own footing, where the clip's sill height was keyed from.
            _exitRoot = door;
            _door = _layout.Ground(door);
            _offRamp = _layout.Ground(_door + forward * OffRampStep);
            _beaconStand = _layout.Ground(_layout.BeaconSpot - forward * 0.75f);
            // From the doorway (the ramp's hinge, at the floor of the hatch) down the open ramp to its foot and a step on.
            _exitPath = new[] { hinge + forward * ThresholdInset, _door, _offRamp };
            _exitLength = FieldLayout.Length(_exitPath);
            BuildHatch(hinge);
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
            BuildRetroPlumes(root);
            _landingDust = ParticleKit.Create(root, "LandingDust", _layout.Landing + Vector3.up * 0.2f, DustColour, 1.2f, 2.6f, 0.5f, 1.2f, 1.5f, 4.5f, -0.03f, _context.Scaled(200), 1f);
            // The jets' blast lifting the dust off the landing ground before the touchdown.
            _retroDust = ParticleKit.Create(root, "RetroDust", _layout.Landing + Vector3.up * 0.15f, DustColour, 0.9f, 1.8f, 0.4f, 1f, 2f, 5f, -0.02f, _context.Scaled(220), 1.4f);
            ParticleKit.SetRate(_retroDust, 0f);
        }

        // The crystal vein the beacon is planted by: the level's own Blender crystals, one big and a few small round it.
        private void BuildVein(Transform root)
        {
            bool low = _context.ParticleScale < 1f;
            EnvironmentInstancer[] decor = root.GetComponentsInChildren<EnvironmentInstancer>(true);
            for (int i = 0; i < VeinCrystals; i++)
            {
                float angle = CutsceneMath.Hash(i, 31) * Mathf.PI * 2f;
                float distance = i == 0 ? 0f : 0.5f + CutsceneMath.Hash(i, 33) * 0.5f;
                Vector3 position = _layout.Ground(ClearOf(decor, _layout.Vein, angle, distance));
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

        // Round 39 (crystals stuck in rocks): a vein crystal takes its hashed spot round the vein when no rock or crystal of
        // the level stands there, else it turns round the vein and steps out until one is free.
        private static Vector3 ClearOf(EnvironmentInstancer[] decor, Vector3 vein, float angle, float distance)
        {
            for (int ring = 0; ring < VeinRings; ring++)
            {
                for (int turn = 0; turn < VeinTurns; turn++)
                {
                    float a = angle + turn * Mathf.PI * 2f / VeinTurns;
                    float d = distance + ring * VeinRingStep;
                    Vector3 spot = vein + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * d;
                    if (IsClear(decor, spot))
                    {
                        return spot;
                    }

                    if (d <= 0f)
                    {
                        break;
                    }
                }
            }

            return vein + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
        }

        private static bool IsClear(EnvironmentInstancer[] decor, Vector3 spot)
        {
            foreach (EnvironmentInstancer instancer in decor)
            {
                if (!instancer.IsClear(spot, VeinClearance, VeinDecorHeight))
                {
                    return false;
                }
            }

            return true;
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
