using Game.Core;
using Game.Levels;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Arrival in orbit: S-69-CRYS fills the lower half of the picture as a wide curve of land, cloud and blue rim,
    /// the mothership glides past above it with its habitat ring turning and its engines glowing, and landing
    /// capsules drop from its bay one after another to burn down into the atmosphere. The cast's
    /// <c>Mothership</c> prefab replaces the procedural ship when set. Posed from time alone.
    /// </summary>
    public sealed class OrbitSet : ICutsceneSet
    {
        private const float PlanetRadius = 60f;
        private const float DegreesPerSecond = 0.35f;
        private const float StartTurn = 4f;
        private const float CloudAlpha = 1f;
        private const float RingDegreesPerSecond = 7f;
        private const float EngineLight = 6f;
        private const float EngineRange = 9f;
        private const float EngineGlowSize = 1.5f;
        private const float EnginePulse = 0.08f;
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
        private static readonly Vector3 PlanetCentre = new Vector3(0f, -75f, 70f);
        private static readonly Vector3 ShipStart = new Vector3(-9f, 1f, 30f);
        private static readonly Vector3 ShipVelocity = new Vector3(0.16f, -0.01f, 0.03f);
        private static readonly Vector3 ShipFacing = new Vector3(0.8f, -0.05f, 0.6f);
        // The landing region lies on top of the curve, tipped a little towards the camera.
        private static readonly Vector3 SiteFacing = new Vector3(0f, 1f, -0.35f);
        private static readonly Vector3 SunDirection = new Vector3(0.35f, -0.75f, 0.55f);
        private static readonly Color EngineColour = new Color(0.55f, 0.8f, 1f);
        private static readonly Color BayColour = new Color(0.65f, 0.85f, 1f);

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

        private Transform[] _engineGlows = new Transform[0];
        private Transform[] _runningLights = new Transform[0];
        private Light _bayLight;
        private VisualTheme _theme;
        private CutsceneContext _context;
        private PlanetBody _planet;
        private Transform _ship;
        private Transform _ring;
        private Vector3 _ringCentre;
        private Vector3 _bay;
        private CapsuleDrop _capsules;
        private ArtModel _shipArt;
        private SpaceBackdrop _backdrop;

        public VisualTheme Theme => _theme;

        public void Build(Transform root, CutsceneContext context)
        {
            _context = context;
            _theme = context.FieldLevel.Theme;
            SpaceStyle space = CrysPlanet.SpaceOf(context.Catalog);
            _planet = CrysPlanet.Create(root, context.Catalog, SiteFacing, PlanetCentre, PlanetRadius, CloudAlpha, StartTurn, DegreesPerSecond);
            _planet.SetSun(SpaceKit.Sun(root, SunDirection, _theme));
            _backdrop = new SpaceBackdrop(root, space, SunDirection, context.ParticleScale < 1f);
            BuildShip(root);
            _capsules = new CapsuleDrop(root, context, _theme.BaseColor, BayAt, PlanetCentre, PlanetRadius);
        }

        public void Enter(CutsceneAction action) => SpaceKit.ApplyDarkness(_theme);

        public void Tick(float time, float progress)
        {
            _planet.Pose(time);
            _backdrop.Pose(_context.Camera.transform);
            _ship.SetPositionAndRotation(ShipAt(time), Quaternion.LookRotation(ShipFacing));
            _shipArt?.Sample(ModelClipRole.Idle, time, loop: true);
            if (_ring != null)
            {
                _ring.localRotation = Quaternion.AngleAxis(time * RingDegreesPerSecond, Vector3.forward);
                _ring.localPosition = _ringCentre - _ring.localRotation * _ringCentre;
            }

            Transform view = _context.Camera.transform;
            float pulse = 1f + EnginePulse * Mathf.Sin(time * 11f);
            foreach (Transform glow in _engineGlows)
            {
                glow.rotation = view.rotation;
                glow.localScale = Vector3.one * (EngineGlowSize * pulse);
            }

            _capsules.Pose(time);
            PoseLights(time, view);
        }

        public void Exit()
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
                _runningLights[i].rotation = view.rotation;
                _runningLights[i].localScale = Vector3.one * (RunningLightSize * (on ? 1f : RunningLightDim));
            }
        }

        private static Vector3 ShipAt(float time) => ShipStart + ShipVelocity * time;

        private Vector3 BayAt(float time) => ShipAt(time) + Quaternion.LookRotation(ShipFacing) * _bay;

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

            _engineGlows = new Transform[nozzles.Length];
            for (int i = 0; i < nozzles.Length; i++)
            {
                Transform glow = SpaceKit.Glow(_ship, $"Exhaust{i}", EngineColour * 3f);
                glow.localPosition = nozzles[i] + Vector3.back * 0.15f;
                _engineGlows[i] = glow;
            }

            var lightGo = new GameObject("EngineLight");
            lightGo.transform.SetParent(_ship, false);
            lightGo.transform.localPosition = nozzles[0] + Vector3.back * 1.2f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = EngineColour;
            light.intensity = EngineLight;
            light.range = EngineRange;
            light.shadows = LightShadows.None;
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
            for (int i = 0; i < count; i++)
            {
                Transform glow = SpaceKit.Glow(_ship, $"RunningLight{i + 1}", RunningLights[i].Colour);
                glow.localPosition = Local(sockets[i], Vector3.zero);
                _runningLights[i] = glow;
            }
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
