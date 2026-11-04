using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The base the colony unfolds on the field: the command post, the landing platform, the energy core and the
    /// road gates. In the timelapse each module springs out of the ground in turn (scale and turn, a puff of dust)
    /// while builder drones hover and weld; later a crystal goes into the core and it ignites. With the level's
    /// imported (Blender) base and gate the model's own modules rise in its timelapse order and the gates wear their
    /// first level's model; otherwise the procedural pieces stand in. Posed from the shot's progress alone.
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
        private const float BuilderClearance = 0.6f;
        private const float BuilderTransitShare = 0.05f;
        private const float BuilderTransitOfGap = 0.6f;
        private const float BuilderHop = 0.6f;
        private const float BuilderBob = 0.06f;
        private const float BuilderBobSpeed = 2.2f;
        private const float BuilderPitch = 3f;
        private const float BuilderBankShare = 0.1f;
        private const float BuilderMaxBank = 9f;
        private const float RotorDegreesPerSecond = 2400f;
        private const int DustBurst = 40;
        private const float SparkRate = 40f;

        // The imported base's modules rise one after another over this share of the timelapse, the gates after them.
        private const float ArtModulesFrom = 0.03f;
        private const float ArtModulesTo = 0.48f;

        // The procedural pad's top, where the haulers start.
        private const float PadHeight = 0.4f;
        private const string FieldSocket = "Field";
        private const string CoreLightSocket = "Light_Core";
        private const string CommandLightSocket = "Light_Command";
        private static readonly string[] WeldSockets = { "Weld_L", "Weld_R" };
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color DustColour = new Color(0.55f, 0.58f, 0.62f, 0.4f);
        private static readonly Color SparkColour = new Color(1f, 0.85f, 0.45f);

        private readonly List<Module> _modules = new List<Module>();
        private readonly List<CutsceneDrone> _builders = new List<CutsceneDrone>();
        private readonly List<ParticleSystem> _builderSparks = new List<ParticleSystem>();
        private readonly List<Material> _gateFields = new List<Material>();
        private Material _crystal;
        private Color _crystalLit;
        private Color _coreColour;
        private Light _coreLight;
        private GameObject _corePool;
        private Transform _crystalTransform;
        private Quaternion _crystalRest = Quaternion.identity;
        private Transform _dish;
        private Quaternion _dishRest;
        private float _dishDegreesPerSecond;
        private Vector3 _coreSocket;
        private float _lastUnfold;

        /// <summary>One piece of the base, standing at its place and unfolding from its start share of the timelapse.</summary>
        private sealed class Module
        {
            public Transform Root;
            public Quaternion Rotation;
            public Vector3 Scale;
            public bool Turns;
            public float Start;
            public Vector3 Centre;
            public float Top;
            public float Reach;
            public ParticleSystem Dust;
        }

        public FieldBase(Transform parent, FieldLayout layout, CutsceneContext context)
        {
            LevelDefinition level = layout.Level;
            VisualTheme theme = level.Theme;
            Color glow = theme.BaseColor;
            _coreColour = Color.Lerp(theme.AccentColor, Color.white, 0.1f);
            PadTop = layout.Pad + Vector3.up * PadHeight;

            BaseArtDefinition art = theme.BaseArt;
            if (art != null && art.Model != null)
            {
                BuildArtBase(parent, layout, art, glow);
            }
            else
            {
                BuildProceduralBase(parent, layout, glow);
            }

            BuildGates(parent, level, layout);
            BuildBuilders(parent, context);
        }

        /// <summary>The energy core's crystal, where the delivery drone flies.</summary>
        public Vector3 CoreSocket => _coreSocket;

        /// <summary>The top of the landing pad, where the haulers take off and come home.</summary>
        public Vector3 PadTop { get; private set; }

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
                float spread = Mathf.Lerp(0.55f, 1f, CutsceneMath.Smooth(u));
                module.Root.localScale = Vector3.Scale(module.Scale, new Vector3(spread, Mathf.Max(0.01f, grow), spread));
                if (module.Turns)
                {
                    module.Root.rotation = module.Rotation * Quaternion.Euler(0f, (1f - CutsceneMath.Smooth(u)) * UnfoldTurnDegrees, 0f);
                }

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
            if (_dish != null)
            {
                _dish.localRotation = _dishRest * Quaternion.AngleAxis(time * _dishDegreesPerSecond, Vector3.up);
            }

            PoseBuilders(building, progress, time);
        }

        /// <summary>The core's glow, 0 dim to 1 fully lit; lit cores pulse gently.</summary>
        public void PoseCore(float ignite, float time)
        {
            float pulse = 1f + 0.12f * Mathf.Sin(time * CorePulseSpeed) * ignite;
            float emission = Mathf.Lerp(CoreDimEmission, CoreLitEmission, ignite * ignite) * pulse;
            _crystal.SetColor(EmissionColorId, _crystalLit * (emission / CoreLitEmission));
            _coreLight.intensity = CoreLightIntensity * ignite * pulse;
            _corePool.SetActive(ignite > 0.05f);
            _crystalTransform.localRotation = _crystalRest * Quaternion.Euler(0f, time * 20f * ignite, 0f);
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
                _builders[i].Holder.gameObject.SetActive(false);
                ParticleKit.SetRate(_builderSparks[i], 0f);
            }
        }

        private void BuildProceduralBase(Transform parent, FieldLayout layout, Color glow)
        {
            Transform command = new GameObject("Command").transform;
            command.SetParent(parent, false);
            command.SetPositionAndRotation(layout.Base, layout.BaseFacing);
            LevelVisualBuilder.BuildBaseDisplay(command, glow);
            AddLight(command, glow, CommandLightIntensity, CommandLightRange, 1.4f);
            GlowDecal.PlacePool(command, glow * 0.18f, 1.4f);
            AddModule(command, 0.05f, parent, true);

            Material[] materials = ModelPalette.Create(glow);
            Transform pad = Part("Pad", parent, BaseModuleFactory.LandingPad(), materials, layout.Pad, layout.BaseFacing);
            AddModule(pad, 0.22f, parent, true);

            Transform core = Part("Core", parent, BaseModuleFactory.CoreCage(), materials, layout.Core, layout.BaseFacing);
            _crystal = SurfaceMaterials.Create(_coreColour, CoreDimEmission, 0.85f);
            _crystalLit = _coreColour * CoreLitEmission;
            _crystalTransform = Part("Crystal", core, BaseModuleFactory.CoreCrystal(), new[] { _crystal }, layout.Core, layout.BaseFacing);
            _crystalTransform.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            _coreSocket = _crystalTransform.position + Vector3.up * 1.2f;
            _coreLight = AddLight(core, _coreColour, 0f, CoreLightRange, 1.2f);
            _corePool = GlowDecal.PlacePool(core, _coreColour * 0.3f, 2.2f);
            AddModule(core, 0.36f, parent, true);
        }

        // The level's base model where the level puts it: each module rises from its own pivot in the report's order, the
        // dish turns, the crystal wears its own copy of the material for the ignition, the lights hang at their sockets.
        private void BuildArtBase(Transform parent, FieldLayout layout, BaseArtDefinition art, Color glow)
        {
            Transform command = new GameObject("Command").transform;
            command.SetParent(parent, false);
            command.SetPositionAndRotation(layout.Base, layout.BaseFacing);
            ArtModel model = ArtModel.CreateStill(command, art.Model);
            GlowDecal.PlacePool(command, glow * 0.18f, 1.4f);

            string[] order = art.TimelapseOrder;
            for (int i = 0; i < order.Length; i++)
            {
                float start = Mathf.Lerp(ArtModulesFrom, ArtModulesTo, order.Length > 1 ? i / (float)(order.Length - 1) : 0f);
                foreach (Renderer renderer in model.RenderersEndingWith(order[i]))
                {
                    AddModule(renderer.transform, start, parent, false);
                }
            }

            Renderer[] dish = model.RenderersEndingWith(art.TurningModule);
            if (dish.Length > 0)
            {
                _dish = dish[0].transform;
                _dishRest = _dish.localRotation;
                _dishDegreesPerSecond = art.TurnDegreesPerSecond;
            }

            Renderer[] crystal = model.RenderersEndingWith(art.PulsingModule);
            _crystal = new Material(model.Material) { name = art.PulsingModule };
            _crystalLit = _crystal.GetColor(EmissionColorId);
            foreach (Renderer renderer in crystal)
            {
                renderer.sharedMaterial = _crystal;
            }

            _crystalTransform = crystal.Length > 0 ? crystal[0].transform : command;
            _crystalRest = _crystalTransform.localRotation;
            _coreSocket = crystal.Length > 0 ? crystal[0].bounds.center : command.position + Vector3.up;

            Renderer[] pad = model.RenderersEndingWith("Base_Pad");
            if (pad.Length > 0)
            {
                PadTop = new Vector3(pad[0].bounds.center.x, pad[0].bounds.max.y, pad[0].bounds.center.z);
            }

            Transform coreAt = Socket(model, CoreLightSocket, _crystalTransform);
            _coreLight = AddLight(coreAt, _coreColour, 0f, CoreLightRange, 0f);
            _corePool = GlowDecal.PlacePool(command, _coreColour * 0.3f, 2.2f);
            _corePool.transform.position = new Vector3(_coreSocket.x, layout.Base.y + 0.035f, _coreSocket.z);
            AddLight(Socket(model, CommandLightSocket, command), glow, CommandLightIntensity, CommandLightRange, 0f);
        }

        private static Transform Socket(ArtModel model, string name, Transform fallback)
        {
            Transform socket = model.Find(name);
            return socket != null ? socket : fallback;
        }

        // The builders fly together from module to module. At each one every builder takes its own station round
        // it (spread evenly, the same height), hovers level there facing the module and welds; between modules they
        // cross in a gentle arc, nose first and banked a little. Posed from progress alone.
        private void PoseBuilders(int building, float progress, float time)
        {
            bool active = progress > 0f && progress < 1f && _modules.Count > 0;
            int from = 0;
            int to = 0;
            float transit = 1f;
            if (active)
            {
                FlightLeg(progress, out from, out to, out transit);
            }

            for (int i = 0; i < _builders.Count; i++)
            {
                CutsceneDrone drone = _builders[i];
                drone.Holder.gameObject.SetActive(active);
                if (!active)
                {
                    ParticleKit.SetRate(_builderSparks[i], 0f);
                    continue;
                }

                Vector3 start = Station(from, i);
                Vector3 end = Station(to, i);
                float eased = CutsceneMath.Smooth(transit);
                float hop = Mathf.Sin(eased * Mathf.PI);
                Vector3 bob = Vector3.up * (BuilderBob * Mathf.Sin(time * BuilderBobSpeed + i * 2.1f));
                Vector3 position = Vector3.Lerp(start, end, eased) + Vector3.up * (hop * BuilderHop) + bob;

                Quaternion faceStart = Quaternion.LookRotation(Flat(_modules[from].Centre - start));
                Quaternion faceEnd = Quaternion.LookRotation(Flat(_modules[to].Centre - end));
                Quaternion working = Quaternion.Slerp(faceStart, faceEnd, eased);
                Vector3 travel = Flat(end - start);
                Quaternion flying = travel.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(travel) : working;
                Quaternion facing = Quaternion.Slerp(working, flying, hop);
                float bank = Vector3.SignedAngle(working * Vector3.forward, flying * Vector3.forward, Vector3.up);
                float lean = Mathf.Clamp(bank * BuilderBankShare, -BuilderMaxBank, BuilderMaxBank) * hop;
                drone.Holder.SetPositionAndRotation(position, facing * Quaternion.Euler(BuilderPitch * hop, 0f, -lean));

                bool welding = building >= 0 && transit >= 1f;
                drone.Pose(welding ? ModelClipRole.Work : ModelClipRole.Move, time + i * 0.37f, time * RotorDegreesPerSecond, false);
                ParticleKit.SetRate(_builderSparks[i], welding ? SparkRate : 0f);
            }
        }

        // Which module the flock leaves and which it heads for, and how far the crossing is (1 once arrived).
        private void FlightLeg(float progress, out int from, out int to, out float transit)
        {
            to = 0;
            for (int m = 1; m < _modules.Count; m++)
            {
                if (progress >= LeaveFor(m))
                {
                    to = m;
                }
            }

            from = Mathf.Max(0, to - 1);
            transit = to == 0 ? 1f : CutsceneMath.Span(progress, LeaveFor(to), _modules[to].Start);
        }

        // The flock sets off for a module a little before it breaks out of the ground, sooner when the modules are far apart in time.
        private float LeaveFor(int module)
        {
            float gap = _modules[module].Start - _modules[module - 1].Start;
            return _modules[module].Start - Mathf.Min(BuilderTransitShare, Mathf.Max(0f, gap) * BuilderTransitOfGap);
        }

        // A builder's hover spot at a module: evenly round it, turned per module so the flock does not line up.
        private Vector3 Station(int module, int builder)
        {
            Module target = _modules[module];
            float angle = (builder / (float)Mathf.Max(1, _builders.Count) + module * 0.17f) * Mathf.PI * 2f;
            float reach = Mathf.Max(BuilderOrbit, target.Reach + BuilderClearance);
            float height = Mathf.Max(target.Centre.y + BuilderHeight, target.Top + BuilderClearance);
            Vector3 around = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * reach;
            return new Vector3(target.Centre.x + around.x, height, target.Centre.z + around.z);
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
                GateArtDefinition art = spot.Blockade != null ? spot.Blockade.GateArt : null;
                ModelArtDefinition first = art != null ? art.ArtFor(0) : null;
                float fieldWidth = width - GateFieldInset;
                float fieldCentre = GateFieldTop - GateFieldHeight * 0.5f;
                if (first != null)
                {
                    // The first gate level's model stretched to the road, as in the level; the field fills its gap.
                    var holder = new GameObject("Model").transform;
                    holder.SetParent(gate, false);
                    holder.localScale = new Vector3(width / art.ModelWidth, 1f, 1f);
                    ArtModel model = ArtModel.CreateStill(holder, first);
                    Transform field = model.Find(FieldSocket);
                    fieldWidth = width * art.FieldShare;
                    fieldCentre = field != null ? gate.InverseTransformPoint(field.position).y : fieldCentre;
                }
                else
                {
                    Material[] materials = ModelPalette.Create(colour);
                    Mesh pylon = StructureModelFactory.GatePylon();
                    Part("PylonL", gate, pylon, materials, Vector3.left * (width * 0.5f));
                    Part("PylonR", gate, pylon, materials, Vector3.right * (width * 0.5f));
                    Part("Beam", gate, StructureModelFactory.GateBeam(width + GateOverhang), materials, Vector3.up * GateBeamHeight);
                }

                Material fieldMaterial = GlowDecal.ScanlineMaterial(colour * GateFieldEmission);
                _gateFields.Add(fieldMaterial);
                Sheet(gate, fieldMaterial, new Vector3(0f, fieldCentre, 0f), Quaternion.identity, new Vector3(fieldWidth, GateFieldHeight, 1f));
                Sheet(gate, fieldMaterial, new Vector3(0f, 0.06f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(fieldWidth, 0.6f, 1f));
                AddModule(gate, 0.55f + 0.1f * i, parent, true);
            }
        }

        private void BuildBuilders(Transform parent, CutsceneContext context)
        {
            CutsceneCast cast = context.Cast;
            if (cast == null || (cast.Builder == null && cast.BuilderArt == null))
            {
                return;
            }

            int count = Mathf.Min(3, context.Scaled(3));
            for (int i = 0; i < count; i++)
            {
                var holder = new GameObject($"Builder{i}").transform;
                holder.SetParent(parent, false);
                var drone = new CutsceneDrone(holder, cast.BuilderArt, cast.Builder);
                _builders.Add(drone);
                // The imported builder welds with its two arms; the procedural one from under its body.
                Transform weld = drone.Socket(WeldSockets[i % WeldSockets.Length]);
                ParticleSystem sparks = weld != null
                    ? ParticleKit.Create(weld, "Weld", Vector3.zero, SparkColour * 2.5f, 0.2f, 0.5f, 0.03f, 0.06f, 1.5f, 4f, 1.2f, 80, 0.03f)
                    : ParticleKit.Create(holder, "Weld", Vector3.down * 0.4f, SparkColour * 2.5f, 0.2f, 0.5f, 0.03f, 0.06f, 1.5f, 4f, 1.2f, 80, 0.05f);
                _builderSparks.Add(sparks);
                holder.gameObject.SetActive(false);
            }
        }

        private void AddModule(Transform root, float start, Transform parent, bool turns)
        {
            ParticleSystem dust = ParticleKit.Create(parent, $"{root.name}Dust", root.position + Vector3.up * 0.2f, DustColour, 0.8f, 1.6f, 0.3f, 0.7f, 0.6f, 2f, -0.05f, 80, 1.1f);
            // Measured as built, so the builders hover clear of the module whatever its pivot or import scale.
            Bounds bounds = BoundsOf(root);
            float reach = Mathf.Max(bounds.extents.x, bounds.extents.z);
            _modules.Add(new Module { Root = root, Rotation = root.rotation, Scale = root.localScale, Turns = turns, Start = start, Dust = dust, Centre = bounds.center, Top = bounds.max.y, Reach = reach });
        }

        private static Bounds BoundsOf(Transform root)
        {
            var bounds = new Bounds(root.position, Vector3.zero);
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                Bounds part = renderer.bounds;
                if (part.size.sqrMagnitude <= 0f)
                {
                    continue;
                }

                if (found)
                {
                    bounds.Encapsulate(part);
                }
                else
                {
                    bounds = part;
                    found = true;
                }
            }

            return bounds;
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
