using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Arrival in orbit: S-69-CRYS fills the lower half of the picture as a wide curve of land, cloud and blue rim. Round 44
    /// (Remco: the ship should fly into the screen, stop, then launch once): in the arrival shot the mothership comes in from
    /// beside the camera nose first, brakes on its forward thrusters and settles over the planet; in the launch shot it holds
    /// there, its habitat ring turning and its engines idling, and launches once: a salvo of cargo capsules and Spencer's
    /// last (round 59), each burning down into the atmosphere on its own way. The cast's <c>Mothership</c> prefab replaces the procedural ship when set. Posed from time alone.
    /// </summary>
    public sealed class OrbitSet : ICutsceneSet
    {
        public const float PlanetRadius = 60f;
        private const float DegreesPerSecond = 0.35f;
        private const float StartTurn = 4f;
        private const float CloudAlpha = 1f;
        private const float RingDegreesPerSecond = 7f;
        private const float EngineLight = 6f;
        private const float EngineRange = 9f;
        private const float EnginePulse = 0.08f;
        private const float PlumeRadius = 0.42f;
        private const float PlumeLength = 7.5f;
        private const float PlumeThrottle = 0.8f;
        private const float PlumeIntensity = 0.85f;
        // The hand-held float of the orbit camera, in degrees.
        private const float CameraFloat = 0.3f;
        // A second, wider shell of air round the planet: the soft glow that spreads past the bright rim.
        private const float HaloLift = 1.14f;
        private const float HaloStrength = 0.45f;
        private const float HaloFalloff = 2.6f;
        private const float StrobeRange = 4.5f;
        private const float StrobePeak = 7f;
        private const string ShipBay = "Bay";
        private const string ShipNozzle = "Nozzle";
        private const string ShipLight = "Light";
        private const float BayLightRange = 4f;
        private const float BayLightRest = 0.6f;
        private const float BayLightPeak = 5f;
        // Inside the bay, behind its mouth (the Bay socket's +Z points down out of it).
        private const float BayLightInset = 0.6f;
        private const float RunningLightSize = 0.55f;
        private const float RunningLightDim = 0.08f;
        // A running light's glow sits this share of its size out towards the camera, so the hull it is mounted on does not cut it in half.
        private const float RunningLightLift = 0.5f;
        public static readonly Vector3 PlanetCentre = new Vector3(0f, -75f, 70f);
        // Where the ship settles and holds for the launch.
        private static readonly Vector3 ShipStart = new Vector3(-9f, 1f, 30f);

        // Round 44: the arrival. It covers this distance along its own heading over this many seconds, braking all the way
        // (eased out with this power), its forward thrusters burning hardest as it comes in and dying as it settles; the main
        // engines are cold while it brakes and idle once it holds.
        public const float ArrivalSeconds = 7f;
        private const float ArrivalDistance = 55f;
        private const float ArrivalEase = 2.5f;
        private const float BrakeFadeIn = 0.4f;
        private const int BrakeThrusters = 2;
        private const float BrakeRadius = 0.55f;
        private const float BrakeLength = 11f;
        private const float BrakeIntensity = 1f;
        // The thrusters burn at full throttle while the ship is still fast, then die with its speed.
        private const float BrakeGain = 1.6f;
        // The braking thrusters sit this share of the ship's half width out from its middle, at its nose.
        private const float BrakeSpread = 0.35f;
        // Round 60: the main engines follow the ship's speed during the arrival (full while fast, eased out as it stops, out
        // by the time it settles); holding station the engine light keeps this faint glow and the plumes are out.
        private const float MotionThrottle = 0.6f;
        private const float MotionGain = 2.2f;
        private const float HoldGlow = 0.05f;
        private static readonly Vector3 ShipFacing = new Vector3(0.8f, -0.05f, 0.6f);
        // The landing region lies on top of the curve, tipped a little towards the camera.
        private static readonly Vector3 SiteFacing = new Vector3(0f, 1f, -0.35f);
        private static readonly Vector3 SunDirection = new Vector3(0.35f, -0.75f, 0.55f);
        private static readonly Color EngineColour = new Color(0.55f, 0.8f, 1f);
        private static readonly Color PlumeCore = new Color(0.85f, 0.95f, 1f) * 6f;
        private static readonly Color PlumeEdge = new Color(0.22f, 0.45f, 1f) * 2f;
        // The white strobes that also light the hull round them (boom tips and nose; README round 19 sockets).
        private static readonly int[] HullStrobes = { 0, 3 };
        private static readonly Color BayColour = new Color(0.65f, 0.85f, 1f);

        // Round 37 (Remco: the logo on the hull did not read at all): the livery plate on the cargo frame's starboard side,
        // in the ship's own space (the frame's rails stand at x 0.99, its rack rings run z -1.98..0.92, y -0.99..0.99).
        private static readonly Vector3 LiveryCentre = new Vector3(1.06f, 0.02f, -0.54f);
        private static readonly Vector2 LiverySize = new Vector2(3f, 1.5f);
        private static readonly Color LiveryPlateColour = new Color(0.05f, 0.08f, 0.15f);
        private const float LiveryPlateDepth = 0.06f;
        private const float LiveryRim = 0.04f;
        private const float LiveryStandOff = 0.004f;
        private const float LiverySmoothness = 0.25f;
        private const float LiveryMetallic = 0.3f;
        private const float LiveryGlow = 0.45f;

        // README round 19, sockets Light_1..10: boom tips white, dorsal beacon yellow, nose white, bay corners amber,
        // radiator tips cyan, engine block white. Each blinks in its own rhythm: (colour, period s, phase, lit share).
        private static readonly (Color Colour, float Period, float Phase, float Lit)[] RunningLights =
        {
            (new Color(1f, 1f, 1f) * 4f, 1.6f, 0f, 0.08f),
            (new Color(1f, 1f, 1f) * 4f, 1.6f, 0.5f, 0.08f),
            (new Color(1f, 0.8f, 0.3f) * 3f, 1.1f, 0.2f, 0.45f),
            (new Color(1f, 1f, 1f) * 4f, 2.4f, 0.3f, 0.06f),
            (new Color(1f, 0.6f, 0.2f) * 2.5f, 0.9f, 0f, 0.5f),
            (new Color(1f, 0.6f, 0.2f) * 2.5f, 0.9f, 0.5f, 0.5f),
            (new Color(0.4f, 1f, 1f) * 2f, 3f, 0f, 0.85f),
            (new Color(0.4f, 1f, 1f) * 2f, 3f, 0.4f, 0.85f),
            (new Color(1f, 1f, 1f) * 4f, 2f, 0.65f, 0.07f),
            (new Color(1f, 1f, 1f) * 4f, 2f, 0.15f, 0.07f),
        };

        private ExhaustPlume[] _plumes = new ExhaustPlume[0];
        private Vector3[] _nozzles = new Vector3[0];
        private Light[] _strobeLights = new Light[0];
        private Light _engineLight;
        private SpaceDepth _depth;
        private Transform[] _runningLights = new Transform[0];
        private Vector3[] _runningSockets = new Vector3[0];
        private Light _bayLight;
        private VisualTheme _theme;
        private CutsceneContext _context;
        private CutsceneAction _action;
        private ExhaustPlume[] _brakes = new ExhaustPlume[0];
        private Vector3[] _brakeMouths = new Vector3[0];
        private PlanetBody _planet;
        private Transform _ship;
        private Transform _ring;
        private Vector3 _ringCentre;
        private Vector3 _bay;
        private CapsuleDrop _capsules;
        private MothershipDoors _doors;
        private ArtModel _shipArt;
        private SpaceBackdrop _backdrop;

        public VisualTheme Theme => _theme;

        public void Build(Transform root, CutsceneContext context)
        {
            _context = context;
            _theme = context.FieldLevel.Theme;
            SpaceStyle space = CrysPlanet.SpaceOf(context.Catalog);
            _planet = CrysPlanet.Create(root, context.Catalog, SiteFacing, PlanetCentre, PlanetRadius, CloudAlpha, StartTurn, DegreesPerSecond);
            Light sun = SpaceKit.Sun(root, SunDirection, _theme);
            _planet.SetSun(sun);
            BuildHalo(CrysPlanet.StyleOf(context.Catalog), sun);
            bool low = context.ParticleScale < 1f;
            _backdrop = new SpaceBackdrop(root, space, SunDirection, low);
            _depth = new SpaceDepth(root, ShipStart, low);
            BuildShip(root);
            _capsules = new CapsuleDrop(root, context, _theme.BaseColor, BayAt, Quaternion.LookRotation(ShipFacing), PlanetCentre, PlanetRadius);
        }

        public void Enter(CutsceneAction action)
        {
            _action = action;
            SpaceKit.ApplyDarkness(_theme);
        }

        public void Tick(float time, float progress)
        {
            bool arriving = _action == CutsceneAction.MothershipArrives;
            // The planet turns on one clock over both shots (the launch follows the arrival).
            float clock = arriving ? time : ArrivalSeconds + time;
            _planet.Pose(clock);
            _backdrop.Pose(_context.Camera.transform);
            _ship.SetPositionAndRotation(arriving ? ArrivingAt(time) : ShipStart, Quaternion.LookRotation(ShipFacing));
            _shipArt?.Sample(ModelClipRole.Idle, time, loop: true);
            if (_ring != null)
            {
                _ring.localRotation = Quaternion.AngleAxis(time * RingDegreesPerSecond, Vector3.forward);
                _ring.localPosition = _ringCentre - _ring.localRotation * _ringCentre;
            }

            Transform view = _context.Camera.transform;
            float pulse = 1f + EnginePulse * Mathf.Sin(time * 11f);
            float brake = arriving ? Brake(time) : 0f;
            // Round 60 (Remco: "the boosters stay on while it stands still"): the main engines burn with the ship's motion and
            // ease out to nothing as it settles; holding station only the engine light keeps a faint glow.
            float engines = arriving ? MainEngineThrottle(time) : 0f;
            Vector3 back = _ship.rotation * Vector3.back;
            for (int i = 0; i < _plumes.Length; i++)
            {
                _plumes[i].Pose(_ship.TransformPoint(_nozzles[i]), back, PlumeRadius, PlumeLength, PlumeThrottle * engines * pulse, clock + i * 0.71f, view);
            }

            Vector3 ahead = _ship.rotation * Vector3.forward;
            for (int i = 0; i < _brakes.Length; i++)
            {
                _brakes[i].Pose(_ship.TransformPoint(_brakeMouths[i]), ahead, BrakeRadius, BrakeLength, brake, clock + i * 0.53f, view);
            }

            if (_engineLight != null)
            {
                _engineLight.intensity = EngineLight * Mathf.Max(Mathf.Max(engines, brake), HoldGlow) * (pulse + 0.06f * Mathf.Sin(time * 37f));
            }

            // Before the launch every capsule is still in the bay.
            _capsules.Pose(arriving ? -1f : time);
            PoseDoors(arriving, time);
            PoseLights(clock, view);
            _depth.Pose(clock, view);
            _context.Rumble = arriving ? 0f : _capsules.Rumble(time);
            _context.Float = CameraFloat;
        }

        // Round 60: shut while the ship comes in, open over the salvo, shut again once the last capsule is clear.
        private void PoseDoors(bool arriving, float time)
        {
            if (_doors == null)
            {
                return;
            }

            _doors.Pose(arriving || _capsules.Count == 0 ? 0f : MothershipDoors.Opening(time, _capsules.Release(0), _capsules.Release(_capsules.Count - 1)));
        }

        /// <summary>Where the ship is during the arrival: along its heading towards its holding point, braking all the way.</summary>
        public static Vector3 ArrivingAt(float time)
        {
            float u = Mathf.Clamp01(time / ArrivalSeconds);
            float left = Mathf.Pow(1f - u, ArrivalEase);
            return ShipStart - ShipFacing.normalized * (ArrivalDistance * left);
        }

        /// <summary>Round 60: the main engines' throttle during the arrival, following the ship's speed and eased to 0 as it stops.</summary>
        public static float MainEngineThrottle(float time)
        {
            float u = Mathf.Clamp01(time / ArrivalSeconds);
            float speed = Mathf.Pow(1f - u, ArrivalEase - 1f);
            return MotionThrottle * CutsceneMath.Smooth(Mathf.Clamp01(speed * MotionGain)) * CutsceneMath.Smooth(Mathf.Clamp01(time / BrakeFadeIn));
        }

        /// <summary>The forward thrusters' throttle during the arrival: hardest as it comes in, gone as it settles.</summary>
        public static float Brake(float time)
        {
            float u = Mathf.Clamp01(time / ArrivalSeconds);
            if (u >= 1f)
            {
                return 0f;
            }

            // The deceleration of the ease-out, which falls off as (1 - u) to the power one less than the ease.
            return Mathf.Clamp01(BrakeGain * Mathf.Pow(1f - u, ArrivalEase - 1f)) * CutsceneMath.Smooth(Mathf.Clamp01(time / BrakeFadeIn));
        }

        public void Exit()
        {
        }

        public void ClearView(IReadOnlyList<Vector3> eyes, IReadOnlyList<Vector3> targets)
        {
        }

        public bool TryGetAnchor(CameraAnchor anchor, out Vector3 origin, out Quaternion facing)
        {
            origin = Vector3.zero;
            facing = Quaternion.identity;
            return false;
        }

        // The bay light swells with each capsule drop; the running lights face the camera and blink in their rhythms.
        private void PoseLights(float time, Transform view)
        {
            if (_bayLight != null)
            {
                _bayLight.intensity = Mathf.Lerp(BayLightRest, BayLightPeak, _capsules.BayGlow(time));
            }

            for (int i = 0; i < _runningLights.Length; i++)
            {
                var blink = RunningLights[i];
                bool on = Mathf.Repeat(time / blink.Period + blink.Phase, 1f) < blink.Lit;
                float size = RunningLightSize * (on ? 1f : RunningLightDim);
                Vector3 socket = _ship.TransformPoint(_runningSockets[i]);
                _runningLights[i].SetPositionAndRotation(socket + (view.position - socket).normalized * (size * RunningLightLift), view.rotation);
                _runningLights[i].localScale = Vector3.one * size;
            }

            for (int s = 0; s < _strobeLights.Length; s++)
            {
                var blink = RunningLights[HullStrobes[s]];
                float phase = Mathf.Repeat(time / blink.Period + blink.Phase, 1f);
                // A hard flash that dies quickly, so the hull round the strobe is lit for a blink.
                _strobeLights[s].intensity = phase < blink.Lit ? StrobePeak * (1f - phase / blink.Lit) : 0f;
            }
        }

        // The wide, faint glow of the air spreading past the bright rim, lit by the same sun.
        private void BuildHalo(PlanetStyle style, Light sun)
        {
            Material material = PlanetMaterials.Atmosphere(style, HaloLift);
            material.SetFloat("_Strength", style.AtmosphereStrength * HaloStrength);
            material.SetFloat("_Falloff", HaloFalloff);
            PlanetMaterials.SetSun(material, sun);
            var go = new GameObject("AtmosphereHalo");
            go.transform.SetParent(_planet.Root, false);
            go.transform.localScale = Vector3.one * HaloLift;
            go.AddComponent<MeshFilter>().sharedMesh = PlanetGlobe.SphereMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // The ship holds still for the launch, so the bay does too.
        private Vector3 BayAt(float time) => ShipStart + Quaternion.LookRotation(ShipFacing) * _bay;

        private void BuildShip(Transform root)
        {
            _ship = new GameObject("Mothership").transform;
            _ship.SetParent(root, false);
            MothershipModelFactory.Parts parts = MothershipModelFactory.Create();
            _bay = parts.Bay;
            GameObject custom = _context.Cast != null ? _context.Cast.Mothership : null;
            ModelArtDefinition art = _context.Cast != null ? _context.Cast.MothershipArt : null;
            Vector3[] nozzles = parts.Nozzles;
            if (art != null)
            {
                // The imported ship turns its own habitat ring (Spin); the capsules leave from its Bay, the glow sits at its nozzles.
                _shipArt = ArtModel.CreateSampled(_ship, art, ownMaterial: false);
                Transform bay = _shipArt.Find(ShipBay);
                _bay = Local(bay, _bay);
                BuildBayLight(bay);
                BuildRunningLights(_shipArt.Numbered(ShipLight));
                BuildLivery(_context.Cast != null ? _context.Cast.HullLivery : null);
                _doors = MothershipDoors.Build(_shipArt.Root);
                Transform[] found = _shipArt.Numbered(ShipNozzle);
                if (found.Length > 0)
                {
                    nozzles = new Vector3[found.Length];
                    for (int i = 0; i < found.Length; i++)
                    {
                        nozzles[i] = Local(found[i], parts.Nozzles[0]);
                    }
                }
            }
            else if (custom != null)
            {
                Object.Instantiate(custom, _ship);
                return;
            }
            else
            {
                Material[] materials = ModelPalette.Create(_theme.BaseColor);
                AddMesh("Hull", _ship, parts.Hull, materials);
                _ring = AddMesh("Ring", _ship, parts.Ring, materials);
                _ringCentre = parts.RingCentre;
            }

            bool shimmer = _context.ParticleScale >= 1f;
            _nozzles = new Vector3[nozzles.Length];
            _plumes = new ExhaustPlume[nozzles.Length];
            for (int i = 0; i < nozzles.Length; i++)
            {
                _nozzles[i] = nozzles[i] + Vector3.back * 0.1f;
                _plumes[i] = new ExhaustPlume(_ship.parent, $"Engine{i}", PlumeCore, PlumeEdge, PlumeIntensity, shimmer);
            }

            BuildBrakes();

            var lightGo = new GameObject("EngineLight");
            lightGo.transform.SetParent(_ship, false);
            lightGo.transform.localPosition = nozzles[0] + Vector3.back * 1.2f;
            _engineLight = lightGo.AddComponent<Light>();
            _engineLight.type = LightType.Point;
            _engineLight.color = EngineColour;
            _engineLight.intensity = EngineLight;
            _engineLight.range = EngineRange;
            _engineLight.shadows = LightShadows.None;
        }

        // The forward thrusters at the nose: found from the ship's own size (its front and width in its own space), so any hull
        // the cast hands in gets them where its nose is.
        private void BuildBrakes()
        {
            Renderer[] renderers = _ship.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;
            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = new Vector3((corner & 1) == 0 ? bounds.min.x : bounds.max.x, (corner & 2) == 0 ? bounds.min.y : bounds.max.y, (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                    Vector3 local = _ship.InverseTransformPoint(point);
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }

            Vector3 centre = (min + max) * 0.5f;
            float spread = (max.x - min.x) * 0.5f * BrakeSpread;
            bool shimmer = _context.ParticleScale >= 1f;
            _brakes = new ExhaustPlume[BrakeThrusters];
            _brakeMouths = new Vector3[BrakeThrusters];
            for (int i = 0; i < BrakeThrusters; i++)
            {
                float side = BrakeThrusters > 1 ? Mathf.Lerp(-spread, spread, i / (float)(BrakeThrusters - 1)) : 0f;
                _brakeMouths[i] = new Vector3(centre.x + side, centre.y, max.z);
                _brakes[i] = new ExhaustPlume(_ship.parent, $"Brake{i}", PlumeCore, PlumeEdge, BrakeIntensity, shimmer);
            }
        }

        private void BuildBayLight(Transform bay)
        {
            if (bay == null)
            {
                return;
            }

            var go = new GameObject("BayLight");
            go.transform.SetParent(_ship, false);
            go.transform.localPosition = _ship.InverseTransformPoint(bay.position - bay.forward * BayLightInset);
            _bayLight = go.AddComponent<Light>();
            _bayLight.type = LightType.Point;
            _bayLight.color = BayColour;
            _bayLight.intensity = BayLightRest;
            _bayLight.range = BayLightRange;
            _bayLight.shadows = LightShadows.None;
        }

        private void BuildRunningLights(Transform[] sockets)
        {
            int count = Mathf.Min(sockets.Length, RunningLights.Length);
            _runningLights = new Transform[count];
            _runningSockets = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Transform glow = SpaceKit.Glow(_ship, $"RunningLight{i + 1}", RunningLights[i].Colour, CutsceneSprite.RunningLight);
                glow.localPosition = Local(sockets[i], Vector3.zero);
                _runningSockets[i] = glow.localPosition;
                _runningLights[i] = glow;
            }

            int strobes = _context.ParticleScale < 1f ? 1 : HullStrobes.Length;
            var lights = new List<Light>();
            for (int s = 0; s < strobes && HullStrobes[s] < count; s++)
            {
                var go = new GameObject($"StrobeLight{s + 1}");
                go.transform.SetParent(_ship, false);
                go.transform.localPosition = _runningSockets[HullStrobes[s]];
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.white;
                light.intensity = 0f;
                light.range = StrobeRange;
                light.shadows = LightShadows.None;
                lights.Add(light);
            }

            _strobeLights = lights.ToArray();
        }

        // The company livery: a flat navy plate on the starboard side of the cargo frame (the flank the orbit camera sees
        // throughout the shot), carrying the NTI emblem and a large wordmark that read at 720p. The plate stands just
        // outside the frame's rails; the livery lights itself a little so the letters read on the hull's shadow side too.
        private void BuildLivery(Texture2D livery)
        {
            if (livery == null)
            {
                return;
            }

            Material plate = SurfaceMaterials.Create(LiveryPlateColour, 0f, LiverySmoothness, LiveryMetallic);
            var panel = new GameObject("LiveryPlate");
            panel.transform.SetParent(_ship, false);
            panel.transform.localPosition = LiveryCentre;
            var box = new PropMeshBuilder(1);
            box.AddBox(Vector3.zero, new Vector3(LiveryPlateDepth, LiverySize.y + LiveryRim * 2f, LiverySize.x + LiveryRim * 2f), 0);
            panel.AddComponent<MeshFilter>().sharedMesh = box.ToMesh("LiveryPlate");
            panel.AddComponent<MeshRenderer>().sharedMaterial = plate;

            Material face = SurfaceMaterials.Create(Color.white, 0f, LiverySmoothness, 0f);
            face.SetTexture("_BaseMap", livery);
            face.SetTexture("_EmissionMap", livery);
            face.EnableKeyword("_EMISSION");
            face.SetColor("_EmissionColor", Color.white * LiveryGlow);
            var decal = new GameObject("Livery");
            decal.transform.SetParent(_ship, false);
            decal.transform.localPosition = LiveryCentre + Vector3.right * (LiveryPlateDepth * 0.5f + LiveryStandOff);
            decal.AddComponent<MeshFilter>().sharedMesh = FlankQuad(LiverySize);
            var renderer = decal.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = face;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // A quad facing the ship's starboard (+x), its text running from aft to nose (+z), so it reads the right way round
        // from the starboard camera.
        private static Mesh FlankQuad(Vector2 size)
        {
            float w = size.x * 0.5f, h = size.y * 0.5f;
            var mesh = new Mesh { name = "LiveryQuad" };
            mesh.vertices = new[] { new Vector3(0f, -h, -w), new Vector3(0f, h, -w), new Vector3(0f, h, w), new Vector3(0f, -h, w) };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            mesh.normals = new[] { Vector3.right, Vector3.right, Vector3.right, Vector3.right };
            mesh.tangents = new[] { new Vector4(0f, 0f, 1f, 1f), new Vector4(0f, 0f, 1f, 1f), new Vector4(0f, 0f, 1f, 1f), new Vector4(0f, 0f, 1f, 1f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        // A socket of the imported ship in the ship's own space (where the procedural parts are given), or the fallback.
        private Vector3 Local(Transform socket, Vector3 fallback) => socket != null ? _ship.InverseTransformPoint(socket.position) : fallback;

        private static Transform AddMesh(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go.transform;
        }
    }
}
