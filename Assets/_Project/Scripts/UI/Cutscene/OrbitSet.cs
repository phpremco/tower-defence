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
        private const int StarCount = 1600;
        private const float PlanetRadius = 60f;
        private const float DegreesPerSecond = 0.35f;
        private const float StartTurn = 4f;
        private const float CloudAlpha = 1f;
        private const float RimPower = 2.6f;
        private const float AtmosphereStrength = 1.5f;
        private const float RingDegreesPerSecond = 7f;
        private const float EngineLight = 6f;
        private const float EngineRange = 9f;
        private const float EngineGlowSize = 1.5f;
        private const float EnginePulse = 0.08f;
        private static readonly Vector3 PlanetCentre = new Vector3(0f, -75f, 70f);
        private static readonly Vector3 ShipStart = new Vector3(-9f, 1f, 30f);
        private static readonly Vector3 ShipVelocity = new Vector3(0.16f, -0.01f, 0.03f);
        private static readonly Vector3 ShipFacing = new Vector3(0.8f, -0.05f, 0.6f);
        // The landing region lies on top of the curve, tipped a little towards the camera.
        private static readonly Vector3 SiteFacing = new Vector3(0f, 1f, -0.35f);
        private static readonly Vector3 SunDirection = new Vector3(0.35f, -0.75f, 0.55f);
        private static readonly Color AtmosphereColour = new Color(0.4f, 0.65f, 1f);
        private static readonly Color EngineColour = new Color(0.55f, 0.8f, 1f);

        private Transform[] _engineGlows = new Transform[0];
        private VisualTheme _theme;
        private CutsceneContext _context;
        private PlanetBody _planet;
        private Transform _ship;
        private Transform _ring;
        private Vector3 _ringCentre;
        private Vector3 _bay;
        private CapsuleDrop _capsules;

        public VisualTheme Theme => _theme;

        public void Build(Transform root, CutsceneContext context)
        {
            _context = context;
            _theme = context.FieldLevel.Theme;
            SpaceKit.BuildStars(root, context.Scaled(StarCount));
            _planet = CrysPlanet.Create(root, context.Catalog, SiteFacing, PlanetCentre, PlanetRadius, CloudAlpha, AtmosphereColour * AtmosphereStrength, RimPower, StartTurn, DegreesPerSecond);
            SpaceKit.Sun(root, SunDirection, _theme);
            BuildShip(root);
            _capsules = new CapsuleDrop(root, context, _theme.BaseColor, BayAt, PlanetCentre, PlanetRadius);
        }

        public void Enter(CutsceneAction action) => SpaceKit.ApplyDarkness(_theme);

        public void Tick(float time, float progress)
        {
            _planet.Pose(time);
            _ship.SetPositionAndRotation(ShipAt(time), Quaternion.LookRotation(ShipFacing));
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

        private static Vector3 ShipAt(float time) => ShipStart + ShipVelocity * time;

        private Vector3 BayAt(float time) => ShipAt(time) + Quaternion.LookRotation(ShipFacing) * _bay;

        private void BuildShip(Transform root)
        {
            _ship = new GameObject("Mothership").transform;
            _ship.SetParent(root, false);
            MothershipModelFactory.Parts parts = MothershipModelFactory.Create();
            _bay = parts.Bay;
            GameObject custom = _context.Cast != null ? _context.Cast.Mothership : null;
            if (custom != null)
            {
                Object.Instantiate(custom, _ship);
                return;
            }

            Material[] materials = ModelPalette.Create(_theme.BaseColor);
            AddMesh("Hull", _ship, parts.Hull, materials);
            _ring = AddMesh("Ring", _ship, parts.Ring, materials);
            _ringCentre = parts.RingCentre;

            _engineGlows = new Transform[parts.Nozzles.Length];
            for (int i = 0; i < parts.Nozzles.Length; i++)
            {
                Transform glow = SpaceKit.Glow(_ship, $"Exhaust{i}", EngineColour * 3f);
                glow.localPosition = parts.Nozzles[i] + Vector3.back * 0.15f;
                _engineGlows[i] = glow;
            }

            var lightGo = new GameObject("EngineLight");
            lightGo.transform.SetParent(_ship, false);
            lightGo.transform.localPosition = parts.Nozzles[0] + Vector3.back * 1.2f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = EngineColour;
            light.intensity = EngineLight;
            light.range = EngineRange;
            light.shadows = LightShadows.None;
        }

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
