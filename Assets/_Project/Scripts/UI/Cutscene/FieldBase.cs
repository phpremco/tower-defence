using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The base the colony unfolds on the field: the command post, the landing platform, the energy core and the
    /// road gates. In the timelapse each module springs out of the ground in turn (scale and turn, a puff of dust)
    /// while builder drones hover and weld; later a crystal goes into the core and it ignites. Posed from the
    /// shot's progress alone.
    /// </summary>
    public sealed class FieldBase
    {
        private const float UnfoldShare = 0.12f;
        private const float UnfoldTurnDegrees = 80f;
        private const float GateOverhang = 0.3f;
        private const float GateBeamHeight = 1.08f;
        private const float GateFieldHeight = 0.9f;
        private const float GateFieldTop = 0.98f;
        private const float GateFieldInset = 0.22f;
        private const float GateFieldEmission = 1.4f;
        private const float GateScroll = 0.6f;
        private const float CommandLightIntensity = 1.6f;
        private const float CommandLightRange = 5f;
        private const float CoreDimEmission = 0.25f;
        private const float CoreLitEmission = 2.4f;
        private const float CoreLightIntensity = 2.6f;
        private const float CoreLightRange = 7f;
        private const float CorePulseSpeed = 2.2f;
        private const float BuilderOrbit = 1.6f;
        private const float BuilderHeight = 1.8f;
        private const float BuilderSpeed = 1.3f;
        private const float RotorDegreesPerSecond = 2400f;
        private const int DustBurst = 40;
        private const float SparkRate = 40f;
        private static readonly Color DustColour = new Color(0.55f, 0.58f, 0.62f, 0.4f);
        private static readonly Color SparkColour = new Color(1f, 0.85f, 0.45f);

        private readonly List<Module> _modules = new List<Module>();
        private readonly List<DroneModelBuilder.Rig> _builders = new List<DroneModelBuilder.Rig>();
        private readonly List<ParticleSystem> _builderSparks = new List<ParticleSystem>();
        private readonly List<Material> _gateFields = new List<Material>();
        private Material _crystal;
        private Color _coreColour;
        private Light _coreLight;
        private GameObject _corePool;
        private Transform _crystalTransform;
        private float _lastUnfold;

        /// <summary>One piece of the base, standing at its place and unfolding from its start share of the timelapse.</summary>
        private sealed class Module
        {
            public Transform Root;
            public Quaternion Rotation;
            public float Start;
            public ParticleSystem Dust;
        }

        public FieldBase(Transform parent, FieldLayout layout, CutsceneContext context)
        {
            LevelDefinition level = layout.Level;
            VisualTheme theme = level.Theme;
            Color glow = theme.BaseColor;
            _coreColour = Color.Lerp(theme.AccentColor, Color.white, 0.1f);

            Transform command = new GameObject("Command").transform;
            command.SetParent(parent, false);
            command.SetPositionAndRotation(layout.Base, layout.BaseFacing);
            LevelVisualBuilder.BuildBaseDisplay(command, glow);
            AddLight(command, glow, CommandLightIntensity, CommandLightRange, 1.4f);
            GlowDecal.PlacePool(command, glow * 0.18f, 1.4f);
            AddModule(command, 0.05f, parent);

            Material[] materials = ModelPalette.Create(glow);
            Transform pad = Part("Pad", parent, BaseModuleFactory.LandingPad(), materials, layout.Pad, layout.BaseFacing);
            AddModule(pad, 0.22f, parent);

            Transform core = Part("Core", parent, BaseModuleFactory.CoreCage(), materials, layout.Core, layout.BaseFacing);
            _crystal = SurfaceMaterials.Create(_coreColour, CoreDimEmission, 0.85f);
            _crystalTransform = Part("Crystal", core, BaseModuleFactory.CoreCrystal(), new[] { _crystal }, layout.Core, layout.BaseFacing);
            _crystalTransform.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            _coreLight = AddLight(core, _coreColour, 0f, CoreLightRange, 1.2f);
            _corePool = GlowDecal.PlacePool(core, _coreColour * 0.3f, 2.2f);
            AddModule(core, 0.36f, parent);

            BuildGates(parent, level, layout);
            BuildBuilders(parent, context);
        }

        /// <summary>The energy core's crystal, where the delivery drone flies.</summary>
        public Vector3 CoreSocket => _crystalTransform.position + Vector3.up * 1.2f;

        /// <summary>Unfold share of the whole base, 0 nothing built, 1 complete; builders weld while it is between.</summary>
        public void PoseUnfold(float progress, float time)
        {
            int building = -1;
            for (int i = 0; i < _modules.Count; i++)
            {
                Module module = _modules[i];
                float u = CutsceneMath.Span(progress, module.Start, module.Start + UnfoldShare);
                module.Root.gameObject.SetActive(u > 0f);
                float grow = CutsceneMath.OutBack(u);
                module.Root.localScale = new Vector3(Mathf.Lerp(0.55f, 1f, CutsceneMath.Smooth(u)), Mathf.Max(0.01f, grow), Mathf.Lerp(0.55f, 1f, CutsceneMath.Smooth(u)));
                module.Root.rotation = module.Rotation * Quaternion.Euler(0f, (1f - CutsceneMath.Smooth(u)) * UnfoldTurnDegrees, 0f);
                if (u > 0f && u < 1f)
                {
                    building = i;
                }

                // A puff of dust as a module breaks out of the ground; only when played forward, never on a seek.
                if (_lastUnfold < module.Start && progress >= module.Start && progress - _lastUnfold < UnfoldShare)
                {
                    module.Dust.Emit(DustBurst);
                }
            }

            _lastUnfold = progress;
            PoseBuilders(building, progress, time);
        }

        /// <summary>The core's glow, 0 dim to 1 fully lit; lit cores pulse gently.</summary>
        public void PoseCore(float ignite, float time)
        {
            float pulse = 1f + 0.12f * Mathf.Sin(time * CorePulseSpeed) * ignite;
            float emission = Mathf.Lerp(CoreDimEmission, CoreLitEmission, ignite * ignite) * pulse;
            _crystal.SetColor("_EmissionColor", _coreColour * emission);
            _coreLight.intensity = CoreLightIntensity * ignite * pulse;
            _corePool.SetActive(ignite > 0.05f);
            _crystalTransform.localRotation = Quaternion.Euler(0f, time * 20f * ignite, 0f);
        }

        /// <summary>Slides the scanlines of the gate fields.</summary>
        public void PoseGates(float time)
        {
            foreach (Material field in _gateFields)
            {
                field.SetTextureOffset("_BaseMap", new Vector2(0f, time * GateScroll));
            }
        }

        public void HideBuilders()
        {
            for (int i = 0; i < _builders.Count; i++)
            {
                _builders[i].Root.SetActive(false);
                ParticleKit.SetRate(_builderSparks[i], 0f);
            }
        }

        // The builders circle the module going up and weld at it; when nothing is going up they hover over the base.
        private void PoseBuilders(int building, float progress, float time)
        {
            for (int i = 0; i < _builders.Count; i++)
            {
                DroneModelBuilder.Rig rig = _builders[i];
                bool active = progress > 0f && progress < 1f;
                rig.Root.SetActive(active);
                if (!active)
                {
                    ParticleKit.SetRate(_builderSparks[i], 0f);
                    continue;
                }

                Transform target = _modules[Mathf.Max(0, building)].Root;
                float angle = time * BuilderSpeed + i * Mathf.PI * 2f / _builders.Count;
                Vector3 centre = target.position;
                Vector3 position = centre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * BuilderOrbit + Vector3.up * (BuilderHeight + 0.15f * Mathf.Sin(time * 3f + i));
                rig.Root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Flat(centre - position)));
                foreach (Transform rotor in rig.Rotors)
                {
                    rotor.localRotation = Quaternion.AngleAxis(time * RotorDegreesPerSecond, rig.RotorAxis);
                }

                ParticleKit.SetRate(_builderSparks[i], building >= 0 ? SparkRate : 0f);
            }
        }

        private void BuildGates(Transform parent, LevelDefinition level, FieldLayout layout)
        {
            float width = level.Road.Width * level.CellSize + GateOverhang;
            for (int i = 0; i < level.Blockades.Length; i++)
            {
                BlockadeSpot spot = level.Blockades[i];
                Color colour = spot.Blockade != null ? spot.Blockade.Color : level.Theme.BaseColor;
                var gate = new GameObject($"Gate{i}").transform;
                gate.SetParent(parent, false);
                gate.SetPositionAndRotation(LevelPlayPose.GatePosition(level, spot.Cell), Quaternion.LookRotation(level.Road.DirectionAt(spot.Cell)));
                Material[] materials = ModelPalette.Create(colour);
                Mesh pylon = StructureModelFactory.GatePylon();
                Part("PylonL", gate, pylon, materials, Vector3.left * (width * 0.5f));
                Part("PylonR", gate, pylon, materials, Vector3.right * (width * 0.5f));
                Part("Beam", gate, StructureModelFactory.GateBeam(width + GateOverhang), materials, Vector3.up * GateBeamHeight);
                Material field = GlowDecal.ScanlineMaterial(colour * GateFieldEmission);
                _gateFields.Add(field);
                Sheet(gate, field, new Vector3(0f, GateFieldTop - GateFieldHeight * 0.5f, 0f), Quaternion.identity, new Vector3(width - GateFieldInset, GateFieldHeight, 1f));
                Sheet(gate, field, new Vector3(0f, 0.06f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(width - GateFieldInset, 0.6f, 1f));
                AddModule(gate, 0.55f + 0.1f * i, parent);
            }
        }

        private void BuildBuilders(Transform parent, CutsceneContext context)
        {
            DroneDefinition builder = context.Cast != null ? context.Cast.Builder : null;
            if (builder == null)
            {
                return;
            }

            int count = Mathf.Min(3, context.Scaled(3));
            for (int i = 0; i < count; i++)
            {
                var holder = new GameObject($"Builder{i}").transform;
                holder.SetParent(parent, false);
                DroneModelBuilder.Rig rig = DroneModelBuilder.Build(holder, builder);
                rig.Root = holder.gameObject;
                _builders.Add(rig);
                ParticleSystem sparks = ParticleKit.Create(holder, "Weld", Vector3.down * 0.4f, SparkColour * 2.5f, 0.2f, 0.5f, 0.03f, 0.06f, 1.5f, 4f, 1.2f, 80, 0.05f);
                _builderSparks.Add(sparks);
                holder.gameObject.SetActive(false);
            }
        }

        private void AddModule(Transform root, float start, Transform parent)
        {
            ParticleSystem dust = ParticleKit.Create(parent, $"{root.name}Dust", root.position + Vector3.up * 0.2f, DustColour, 0.8f, 1.6f, 0.3f, 0.7f, 0.6f, 2f, -0.05f, 80, 1.1f);
            _modules.Add(new Module { Root = root, Rotation = root.rotation, Start = start, Dust = dust });
        }

        private static Transform Part(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return go.transform;
        }

        private static void Part(string name, Transform parent, Mesh mesh, Material[] materials, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        private static void Sheet(Transform parent, Material material, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            var go = new GameObject("Field");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = GlowDecal.Quad();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Light AddLight(Transform parent, Color colour, float intensity, float range, float height)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * height;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = colour;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction : Vector3.forward;
        }
    }
}
