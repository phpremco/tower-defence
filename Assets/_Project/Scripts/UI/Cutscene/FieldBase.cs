using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.Towers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// The base the colony builds on the field: the command post, the landing platform, the energy core and the road
    /// gates. In the timelapse the builder drones bring every module down from above, folded small under their bellies,
    /// set it down where it belongs, and it unfolds to full size there while they weld it; then they climb away for the
    /// next one and at the end land beside the base. The drones fly one continuous path through the whole shot (eased
    /// between the stops), so nothing jumps between modules. The core's crystal is not built: a hauler brings it later
    /// (<see cref="PoseCrystal"/>) and the core lights up. With the level's imported (Blender) base and gate the model's
    /// own modules come in its timelapse order; otherwise the procedural pieces stand in. Posed from progress alone.
    /// </summary>
    public sealed class FieldBase
    {
        private const float UnfoldShare = 0.1f;
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
        private const int DustBurst = 40;
        private const float SparkRate = 40f;

        // The drops, every module and gate evenly over this share of the timelapse, in build order.
        private const float DropsFrom = 0.06f;
        private const float DropsTo = 0.8f;

        // A module rides folded under its drone, no wider than this.
        private const float CarriedSize = 0.9f;
        private const float Hang = 0.9f;
        private const float TetherWidth = 0.09f;
        private const float TetherIntensity = 2.2f;
        private static readonly Color TetherColour = new Color(0.45f, 0.85f, 1f);

        // The flight legs, as shares of the timelapse: down from above with a module, weld beside it while it unfolds, up
        // again for the next; at the end the flock lands beside the base.
        private const float FlyDown = 0.11f;
        private const float Weld = 0.07f;
        private const float FlyUp = 0.07f;
        private const float ParkShare = 0.08f;
        private const float SupplyHeight = 8f;
        private const float DownArc = 0.6f;
        private const float UpArc = 1.2f;
        private const float HeadingProbe = 0.004f;
        private const float BuilderScale = 1.8f;
        private const float BuilderOrbit = 1.6f;
        private const float BuilderHeight = 1.4f;
        private const float BuilderClearance = 0.7f;
        private const float BuilderBob = 0.06f;
        private const float BuilderBobSpeed = 2.2f;
        private const float BuilderMaxBank = 12f;
        private const float BuilderBankPerDegree = 0.4f;
        private const float BuilderPitchPerSpeed = 0.06f;
        private const float BuilderMaxPitch = 10f;
        private const float RotorDegreesPerSecond = 2400f;
        private const float ParkRing = 3.2f;
        // Round 39: a parking spot must be this level across a drone's span (the drawn ground, not the cells' heights);
        // the drone then rests on the highest ground under its feet.
        private const float ParkSlopeTolerance = 0.18f;
        private const int ParkSlopeSamples = 8;
        // The search for open ground when a corner is taken: steps of the ring and of the turn either side of the corner.
        private const int ParkRings = 5;
        private const float ParkRingStep = 0.6f;
        private const int ParkTurns = 9;
        private const float ParkTurnStep = 12f;
        // A landed builder needs this much free ground round it (its rotors), level with the base, apart from the others;
        // ground cover lower than the height does not count.
        private const float ParkClearance = 0.8f;
        private const float ParkSpacing = 1.6f;
        private const float ParkLevelTolerance = 0.3f;
        // The haulers are larger than the builders (rotor span about 1.6 m): wider spacing, a wider search.
        private const float HaulerParkSpacing = 2.1f;
        private const int HaulerParkRings = 12;
        private const int HaulerParkTurns = 30;
        private const float RelaxedLevelTolerance = 1.5f;
        private const float ParkDecorHeight = 0.25f;
        private const int Builders = 4;

        // The crystal rides under the hauler at this share of its size and settles into the core as it is inserted.
        public const float CrystalCarriedShare = 0.7f;

        // The procedural pad's top, where the haulers start.
        private const float PadHeight = 0.4f;
        private const string FieldSocket = "Field";
        private const string CoreLightSocket = "Light_Core";
        private const string CommandLightSocket = "Light_Command";
        private static readonly string[] WeldSockets = { "Weld_L", "Weld_R" };
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color DustColour = new Color(0.55f, 0.58f, 0.62f, 0.4f);
        private static readonly Color SparkColour = new Color(1f, 0.85f, 0.45f);
        private static readonly CutsceneCue Clank = new CutsceneCue { Kind = CueKind.Sound, Sound = CutsceneSound.BuildClank, Strength = 0.6f };

        private readonly List<Module> _modules = new List<Module>();
        private readonly List<Builder> _builders = new List<Builder>();
        private readonly List<Material> _gateFields = new List<Material>();
        private readonly List<CrystalPart> _crystalParts = new List<CrystalPart>();
        private readonly CutsceneAudio _audio;
        private readonly Vector3 _baseCentre;
        private readonly IReadOnlyList<EnvironmentInstancer> _decor;
        private readonly List<Vector3> _parks = new List<Vector3>();
        private readonly List<Vector3> _landings = new List<Vector3>();
        private Material _crystal;
        private Color _crystalLit;
        private Color _coreColour;
        private Light _coreLight;
        private GameObject _corePool;
        // The turning parts and their rest rotations, near and far twins alike.
        private Transform[] _crystalSpins = new Transform[0];
        private Quaternion[] _crystalRests = new Quaternion[0];
        private Transform[] _dishes = new Transform[0];
        private Quaternion[] _dishRests = new Quaternion[0];
        private float _dishDegreesPerSecond;
        private Vector3 _coreSocket;
        private float _crystalTop;
        private float _lastUnfold;

        /// <summary>One piece of the base: where it stands when built and when in the timelapse it is set down.</summary>
        private sealed class Module
        {
            public Transform Root;
            public Vector3 RestPosition;
            public Quaternion RestRotation;
            public Vector3 RestScale;
            public bool Turns;
            public float Start;
            public Vector3 Centre;
            public float Top;
            public float Reach;
            public float Carried;
            public int Drone = -1;
            public float Pick = 2f;
            public ParticleSystem Dust;

            // The module's far mesh (its LOD twin) and any other mesh of the same piece: posed exactly like Root.
            public Transform[] Twins = new Transform[0];
        }

        /// <summary>A transform of the crystal at rest, so it can ride under the hauler and be put back.</summary>
        private struct CrystalPart
        {
            public Transform Transform;
            public Vector3 Position;
            public Vector3 Scale;
        }

        /// <summary>One leg of a builder's flight: from a point to a point over a share of the timelapse.</summary>
        private struct Leg
        {
            public float From;
            public float To;
            public Vector3 Start;
            public Vector3 End;
            public float Arc;
            public int Carry;
            public int Welds;
        }

        private sealed class Builder
        {
            public CutsceneDrone Drone;
            public ParticleSystem Sparks;
            public LineRenderer Tether;
            public readonly List<Leg> Legs = new List<Leg>();
        }

        /// <param name="decor">The field's drawn decoration, so the builders land clear of every rock and crystal.</param>
        public FieldBase(Transform parent, FieldLayout layout, CutsceneContext context, IReadOnlyList<EnvironmentInstancer> decor)
        {
            _decor = decor ?? new EnvironmentInstancer[0];
            LevelDefinition level = layout.Level;
            VisualTheme theme = level.Theme;
            Color glow = theme.BaseColor;
            _audio = context.Audio;
            _baseCentre = layout.Base;
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
            Schedule();
            BuildBuilders(parent, context);
            PlanFlights(layout);
        }

        /// <summary>The energy core's crystal at rest, where the hauler brings it.</summary>
        public Vector3 CoreSocket => _coreSocket;

        /// <summary>How far the crystal reaches above its pivot at full size, so the hauler holds it by the top.</summary>
        public float CrystalTop => _crystalTop;

        /// <summary>The top of the landing pad, where the haulers take off and come home.</summary>
        public Vector3 PadTop { get; private set; }

        /// <summary>Timelapse share of the whole base, 0 nothing built, 1 complete; the builders fly while it runs.</summary>
        public void PoseUnfold(float progress, float time, bool visible)
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                PoseModule(_modules[i], progress);
                // A clank and a puff of dust as a module touches down; only when played forward, never on a seek.
                Module module = _modules[i];
                if (_lastUnfold < module.Start && progress >= module.Start && progress - _lastUnfold < UnfoldShare)
                {
                    module.Dust.Emit(DustBurst);
                    _audio.Play(Clank);
                }
            }

            _lastUnfold = progress;
            Turn(_dishes, _dishRests, time * _dishDegreesPerSecond);

            for (int i = 0; i < _builders.Count; i++)
            {
                PoseBuilder(_builders[i], i, progress, time, visible);
            }
        }

        // Turns each part about world up (in its parent's axes) through its own pivot: an imported part's own Y carries the
        // FBX axis conversion (the old base's -90 X), so turning about it tipped the dish over (Remco: horizontal only).
        private static void Turn(Transform[] parts, Quaternion[] rests, float degrees)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                Transform part = parts[i];
                Vector3 up = part.parent != null ? part.parent.InverseTransformDirection(Vector3.up) : Vector3.up;
                part.localRotation = Quaternion.AngleAxis(degrees, up) * rests[i];
            }
        }

        private static Transform[] Transforms(Renderer[] renderers, out Quaternion[] rests)
        {
            var parts = new Transform[renderers.Length];
            rests = new Quaternion[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                parts[i] = renderers[i].transform;
                rests[i] = parts[i].localRotation;
            }

            return parts;
        }

        /// <summary>
        /// The core's glow, 0 dim to 1 fully lit (lit cores pulse gently); the crystal itself glows at least
        /// <paramref name="crystalGlow"/>, so the Crysium the hauler brings already shines on its way in.
        /// </summary>
        public void PoseCore(float ignite, float crystalGlow, float time)
        {
            float pulse = 1f + 0.12f * Mathf.Sin(time * CorePulseSpeed) * ignite;
            float glow = Mathf.Max(ignite, crystalGlow);
            float emission = Mathf.Lerp(CoreDimEmission, CoreLitEmission, glow * glow) * pulse;
            _crystal.SetColor(EmissionColorId, _crystalLit * (emission / CoreLitEmission));
            _coreLight.intensity = CoreLightIntensity * ignite * pulse;
            _corePool.SetActive(ignite > 0.05f);
            Turn(_crystalSpins, _crystalRests, time * 20f * ignite);
        }

        /// <summary>
        /// The crystal: hidden, riding under the hauler with its top at <paramref name="carriedTop"/> (when
        /// <paramref name="carried"/>), or sliding into its socket as <paramref name="insert"/> goes from 0 to 1.
        /// </summary>
        public void PoseCrystal(bool carried, Vector3 carriedTop, float insert)
        {
            bool shown = carried || insert > 0f;
            float share = carried ? CrystalCarriedShare : Mathf.Lerp(CrystalCarriedShare, 1f, CutsceneMath.Smooth(insert));
            Vector3 lift = carried ? carriedTop - (_coreSocket + Vector3.up * (_crystalTop * share)) : Vector3.zero;
            foreach (CrystalPart part in _crystalParts)
            {
                part.Transform.gameObject.SetActive(shown);
                part.Transform.position = _coreSocket + (part.Position - _coreSocket) * share + lift;
                part.Transform.localScale = part.Scale * share;
            }
        }

        /// <summary>Slides the scanlines of the gate fields.</summary>
        public void PoseGates(float time)
        {
            foreach (Material field in _gateFields)
            {
                GlowDecal.SetScroll(field, new Vector2(0f, time * GateScroll));
            }
        }

        private void PoseModule(Module module, float progress)
        {
            PoseRoot(module, progress);
            foreach (Transform twin in module.Twins)
            {
                twin.gameObject.SetActive(module.Root.gameObject.activeSelf);
                twin.SetPositionAndRotation(module.Root.position, module.Root.rotation);
                twin.localScale = module.Root.localScale;
            }
        }

        private void PoseRoot(Module module, float progress)
        {
            if (progress < module.Pick)
            {
                module.Root.gameObject.SetActive(false);
                return;
            }

            module.Root.gameObject.SetActive(true);
            if (progress < module.Start && module.Drone >= 0)
            {
                // Riding folded under its drone: the module's top hangs just below the drone, its middle under the belly.
                Vector3 drone = Position(_builders[module.Drone], progress);
                Vector3 top = drone - Vector3.up * Hang;
                Vector3 offset = (module.RestPosition - new Vector3(module.Centre.x, module.Top, module.Centre.z)) * module.Carried;
                module.Root.SetPositionAndRotation(top + offset, module.RestRotation);
                module.Root.localScale = module.RestScale * module.Carried;
                return;
            }

            float u = CutsceneMath.Span(progress, module.Start, module.Start + UnfoldShare);
            float grow = Mathf.LerpUnclamped(module.Carried, 1f, CutsceneMath.OutBack(u));
            Quaternion turn = module.Turns ? Quaternion.Euler(0f, (1f - CutsceneMath.Smooth(u)) * UnfoldTurnDegrees, 0f) : Quaternion.identity;
            module.Root.SetPositionAndRotation(module.RestPosition, module.RestRotation * turn);
            module.Root.localScale = module.RestScale * Mathf.Max(0.01f, grow);
        }

        // Each module in build order gets an even drop time and the next drone in turn; how small it folds follows its size.
        private void Schedule()
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                Module module = _modules[i];
                module.Start = Mathf.Lerp(DropsFrom, DropsTo, _modules.Count > 1 ? i / (float)(_modules.Count - 1) : 0f);
                float width = Mathf.Max(0.1f, module.Reach * 2f);
                module.Carried = Mathf.Min(1f, CarriedSize / width);
            }
        }

        // One continuous path per drone: it waits high above with its first module, comes down to set it, welds beside it,
        // climbs to above its next module, and so on; after its last it lands at its own spot beside the base.
        private void PlanFlights(FieldLayout layout)
        {
            if (_builders.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _modules.Count; i++)
            {
                _modules[i].Drone = i % _builders.Count;
            }

            for (int d = 0; d < _builders.Count; d++)
            {
                Builder builder = _builders[d];
                float time = 0f;
                Vector3 at = Vector3.zero;
                bool first = true;
                for (int i = 0; i < _modules.Count; i++)
                {
                    Module module = _modules[i];
                    if (module.Drone != d)
                    {
                        continue;
                    }

                    Vector3 drop = Drop(module);
                    Vector3 supply = drop + Vector3.up * SupplyHeight;
                    float down = Mathf.Max(time, module.Start - FlyDown);
                    if (first)
                    {
                        // It waits up there with its first module from the start of the timelapse.
                        at = supply;
                        first = false;
                        Add(builder, 0f, down, supply, supply, 0f, i, -1);
                    }
                    else
                    {
                        // Up and across to above the next module; it is picked up out of sight up there.
                        float up = Mathf.Max(time, down - FlyUp);
                        Add(builder, time, up, at, at, 0f, -1, -1);
                        Add(builder, up, down, at, supply, UpArc, -1, -1);
                    }

                    module.Pick = down;
                    Add(builder, down, module.Start, supply, drop, DownArc, i, -1);
                    Vector3 station = Station(module, d);
                    float welded = module.Start + Weld;
                    Add(builder, module.Start, welded, drop, station, 0f, -1, i);
                    at = station;
                    time = welded;
                }

                if (first)
                {
                    continue;
                }

                Vector3 park = Park(layout, d);
                Add(builder, time, time + ParkShare, at, park, UpArc * 0.5f, -1, -1);
                _landings.Add(park);
            }
        }

        private static void Add(Builder builder, float from, float to, Vector3 start, Vector3 end, float arc, int carry, int welds)
        {
            if (to <= from)
            {
                return;
            }

            builder.Legs.Add(new Leg { From = from, To = to, Start = start, End = end, Arc = arc, Carry = carry, Welds = welds });
        }

        // Where a drone hovers to set a module down: right above its folded top at its rest place.
        private Vector3 Drop(Module module)
        {
            float top = module.RestPosition.y + (module.Top - module.RestPosition.y) * module.Carried;
            return new Vector3(module.Centre.x, top + Hang, module.Centre.z);
        }

        // A drone's welding spot at a module: round it at the module's full size, turned per drone so they do not line up.
        private Vector3 Station(Module module, int drone)
        {
            float angle = (drone / (float)Builders + _modules.IndexOf(module) * 0.17f) * Mathf.PI * 2f;
            float reach = Mathf.Max(BuilderOrbit, module.Reach + BuilderClearance);
            float height = Mathf.Max(module.Centre.y + BuilderHeight, module.Top + BuilderClearance);
            return new Vector3(module.Centre.x + Mathf.Cos(angle) * reach, height, module.Centre.z + Mathf.Sin(angle) * reach);
        }

        // Where a drone lands when the base stands: round the command post, on the ground. Remco (playtest note 2): a corner
        // can hold a rock, so each drone tries its own corner first and then turns and steps out until it finds open, level
        // ground clear of every module, rock and crystal and of the drones that landed before it.
        // Landed, a builder faces the base (PoseBuilder), so it settles on its feet turned that way.
        private Vector3 Park(FieldLayout layout, int drone)
        {
            Vector3 spot = Park(layout, (drone + 0.5f) / Builders * Mathf.PI * 2f, ParkRings, ParkTurns, ParkSpacing);
            return _builders[drone].Drone.RestOn(spot, Quaternion.LookRotation(Flat(_baseCentre - spot)), layout.SurfaceAt);
        }

        /// <summary>Where each builder set down at the end of the timelapse (its holder, standing on its feet), for probes and tests.</summary>
        public IReadOnlyList<Vector3> Landings => _landings;

        /// <summary>
        /// Round 37: open, level ground round the command post for a hauler, on the side facing <paramref name="toward"/>
        /// first, clear of every module, rock and crystal and of every drone parked before it (the haulers no longer pile
        /// up on the pad, through each other and the storage tanks). The spot is kept, so the next one parks beside it.
        /// Returned on the drawn ground; the hauler settles its own feet on it (<see cref="CutsceneDrone.RestOn"/>).
        /// </summary>
        public Vector3 ParkingSpot(FieldLayout layout, Vector3 toward)
        {
            Vector3 way = toward - _baseCentre;
            return Park(layout, Mathf.Atan2(way.z, way.x), HaulerParkRings, HaulerParkTurns, HaulerParkSpacing);
        }

        // Round 39: the strict search (level with the base) found room for only one hauler; the other seven all took the same
        // fallback spot on the slope and stood inside each other. A second pass allows ground higher or lower than the base
        // (still locally level and clear), and the last resort lines the drones up instead of stacking them.
        private Vector3 Park(FieldLayout layout, float preferred, int rings, int turns, float spacing)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                float tolerance = pass == 0 ? ParkLevelTolerance : RelaxedLevelTolerance;
                for (int ring = 0; ring < rings; ring++)
                {
                    for (int turn = 0; turn < turns; turn++)
                    {
                        // 0, +1, -1, +2, -2 ... steps either side of the drone's own corner.
                        int side = (turn + 1) / 2 * (turn % 2 == 0 ? -1 : 1);
                        float angle = preferred + side * ParkTurnStep * Mathf.Deg2Rad;
                        Vector3 spot = Surface(layout, _baseCentre + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (ParkRing + ring * ParkRingStep));
                        if (IsOpenGround(layout, spot, spacing, tolerance))
                        {
                            _parks.Add(spot);
                            return spot;
                        }
                    }
                }
            }

            float reach = ParkRing + rings * ParkRingStep;
            float along = preferred + _parks.Count * spacing / reach;
            Vector3 fallback = Surface(layout, _baseCentre + new Vector3(Mathf.Cos(along), 0f, Mathf.Sin(along)) * reach);
            _parks.Add(fallback);
            return fallback;
        }

        private static Vector3 Surface(FieldLayout layout, Vector3 at) => new Vector3(at.x, layout.SurfaceAt(at.x, at.z), at.z);

        private bool IsOpenGround(FieldLayout layout, Vector3 spot, float spacing, float levelTolerance)
        {
            if (Mathf.Abs(spot.y - _baseCentre.y) > levelTolerance || !IsLevel(layout, spot, spacing * 0.5f))
            {
                return false;
            }

            foreach (Module module in _modules)
            {
                if (FlatDistance(spot, module.Centre) < module.Reach + ParkClearance)
                {
                    return false;
                }
            }

            foreach (Vector3 other in _parks)
            {
                if (FlatDistance(spot, other) < spacing)
                {
                    return false;
                }
            }

            foreach (EnvironmentInstancer decor in _decor)
            {
                if (decor != null && !decor.IsClear(spot, ParkClearance, ParkDecorHeight))
                {
                    return false;
                }
            }

            return true;
        }

        private static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        // Whether the drawn ground round a spot (a ring of samples at the drone's half span) stays within the slope tolerance.
        private static bool IsLevel(FieldLayout layout, Vector3 spot, float radius)
        {
            float low = spot.y;
            float high = spot.y;
            for (int i = 0; i < ParkSlopeSamples; i++)
            {
                float angle = i * Mathf.PI * 2f / ParkSlopeSamples;
                float height = layout.SurfaceAt(spot.x + Mathf.Cos(angle) * radius, spot.z + Mathf.Sin(angle) * radius);
                low = Mathf.Min(low, height);
                high = Mathf.Max(high, height);
            }

            return high - low <= ParkSlopeTolerance;
        }

        private static Vector3 Position(Builder builder, float progress)
        {
            List<Leg> legs = builder.Legs;
            if (legs.Count == 0)
            {
                return Vector3.zero;
            }

            if (progress <= legs[0].From)
            {
                return legs[0].Start;
            }

            foreach (Leg leg in legs)
            {
                if (progress <= leg.To)
                {
                    float eased = CutsceneMath.Smooth(CutsceneMath.Span(progress, leg.From, leg.To));
                    return Vector3.Lerp(leg.Start, leg.End, eased) + Vector3.up * (Mathf.Sin(eased * Mathf.PI) * leg.Arc);
                }
            }

            return legs[legs.Count - 1].End;
        }

        private static Leg LegAt(Builder builder, float progress)
        {
            foreach (Leg leg in builder.Legs)
            {
                if (progress <= leg.To)
                {
                    return leg;
                }
            }

            return builder.Legs.Count > 0 ? builder.Legs[builder.Legs.Count - 1] : default;
        }

        // Nose along the way it flies (banked into turns, dipped with speed), facing the module while it welds, level and
        // facing the base once landed.
        private void PoseBuilder(Builder builder, int index, float progress, float time, bool visible)
        {
            CutsceneDrone drone = builder.Drone;
            drone.Holder.gameObject.SetActive(visible && builder.Legs.Count > 0);
            if (!visible || builder.Legs.Count == 0)
            {
                ParticleKit.SetRate(builder.Sparks, 0f);
                builder.Tether.enabled = false;
                return;
            }

            Leg leg = LegAt(builder, progress);
            bool landed = progress >= builder.Legs[builder.Legs.Count - 1].To;
            Vector3 position = Position(builder, progress);
            Vector3 before = Position(builder, Mathf.Max(0f, progress - HeadingProbe));
            Vector3 after = Position(builder, Mathf.Min(1f, progress + HeadingProbe));
            Vector3 travel = Flat(after - before);
            bool welding = leg.Welds >= 0 && !landed;
            Quaternion facing;
            if (welding)
            {
                facing = Quaternion.LookRotation(Flat(_modules[leg.Welds].Centre - position));
            }
            else if (travel.sqrMagnitude > 0.0004f)
            {
                facing = Quaternion.LookRotation(travel);
            }
            else
            {
                facing = Quaternion.LookRotation(Flat(_baseCentre - position));
            }

            float speed = travel.magnitude / (HeadingProbe * 2f);
            float turn = Vector3.SignedAngle(Flat(position - before), Flat(after - position), Vector3.up);
            float bank = landed ? 0f : Mathf.Clamp(turn * BuilderBankPerDegree, -BuilderMaxBank, BuilderMaxBank);
            float pitch = landed ? 0f : Mathf.Min(BuilderMaxPitch, speed * BuilderPitchPerSpeed);
            Vector3 bob = landed ? Vector3.zero : Vector3.up * (BuilderBob * Mathf.Sin(time * BuilderBobSpeed + index * 2.1f));
            drone.Holder.SetPositionAndRotation(position + bob, facing * Quaternion.Euler(pitch, 0f, -bank));
            ModelClipRole role = welding ? ModelClipRole.Work : landed ? ModelClipRole.Idle : ModelClipRole.Move;
            drone.Pose(role, landed ? 0f : time + index * 0.37f, landed ? 0f : time * RotorDegreesPerSecond, false);
            ParticleKit.SetRate(builder.Sparks, welding ? SparkRate : 0f);
            bool carrying = leg.Carry >= 0 && progress >= _modules[leg.Carry].Pick && progress < _modules[leg.Carry].Start;
            builder.Tether.enabled = carrying;
            if (carrying)
            {
                builder.Tether.SetPosition(0, position + bob);
                builder.Tether.SetPosition(1, position + bob - Vector3.up * Hang);
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
            AddModule(command, parent, true);

            Material[] materials = ModelPalette.Create(glow);
            Transform pad = Part("Pad", parent, BaseModuleFactory.LandingPad(), materials, layout.Pad, layout.BaseFacing);
            AddModule(pad, parent, true);

            Transform core = Part("Core", parent, BaseModuleFactory.CoreCage(), materials, layout.Core, layout.BaseFacing);
            _crystal = SurfaceMaterials.Create(_coreColour, CoreDimEmission, 0.85f);
            _crystalLit = _coreColour * CoreLitEmission;
            Transform crystal = Part("Crystal", parent, BaseModuleFactory.CoreCrystal(), new[] { _crystal }, layout.Core, layout.BaseFacing);
            crystal.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            _crystalSpins = new[] { crystal };
            _crystalRests = new[] { crystal.localRotation };
            _coreSocket = crystal.position;
            _crystalTop = 1.2f;
            _crystalParts.Add(new CrystalPart { Transform = crystal, Position = crystal.position, Scale = crystal.localScale });
            _coreLight = AddLight(core, _coreColour, 0f, CoreLightRange, 1.2f);
            _corePool = GlowDecal.PlacePool(core, _coreColour * 0.3f, 2.2f);
            AddModule(core, parent, true);
        }

        // The level's base model where the level puts it: each module comes in the report's order (the crystal waits for
        // the hauler), the dish turns, the crystal wears its own copy of the material for the ignition, the lights hang
        // at their sockets.
        private void BuildArtBase(Transform parent, FieldLayout layout, BaseArtDefinition art, Color glow)
        {
            Transform command = new GameObject("Command").transform;
            command.SetParent(parent, false);
            command.SetPositionAndRotation(layout.Base, layout.BaseFacing);
            ArtModel model = ArtModel.CreateStill(command, art.Model);
            GlowDecal.PlacePool(command, glow * 0.18f, 1.4f);

            // The haulers take off from and land on the pad, so the shuttle that lives there stays out of the cutscene;
            // the bays stand shut as on the field before the first drone.
            HideModule(model, art.ShuttleModule);
            BaseDockView.CloseLids(art, model);

            foreach (string module in art.TimelapseOrder)
            {
                if (module == art.PulsingModule || module == art.ShuttleModule)
                {
                    continue;
                }

                Renderer[] meshes = model.RenderersEndingWith(module);
                if (meshes.Length == 0)
                {
                    continue;
                }

                // The near and the far mesh of a piece come down as one module.
                AddModule(meshes[0].transform, parent, false);
                var twins = new Transform[meshes.Length - 1];
                for (int i = 1; i < meshes.Length; i++)
                {
                    twins[i - 1] = meshes[i].transform;
                }

                _modules[_modules.Count - 1].Twins = twins;
            }

            Renderer[] dish = model.RenderersEndingWith(art.TurningModule);
            if (dish.Length > 0)
            {
                _dishes = Transforms(dish, out _dishRests);
                _dishDegreesPerSecond = art.TurnDegreesPerSecond;
            }

            Renderer[] crystal = model.RenderersEndingWith(art.PulsingModule);
            if (crystal.Length > 0)
            {
                _crystalSpins = Transforms(crystal, out _crystalRests);
            }

            _crystal = new Material(model.Material) { name = art.PulsingModule };
            _crystalLit = _crystal.GetColor(EmissionColorId);
            Bounds crystalBounds = crystal.Length > 0 ? crystal[0].bounds : new Bounds(command.position + Vector3.up, Vector3.one * 0.5f);
            _coreSocket = new Vector3(crystalBounds.center.x, crystalBounds.min.y, crystalBounds.center.z);
            _crystalTop = crystalBounds.size.y;
            foreach (Renderer renderer in crystal)
            {
                renderer.sharedMaterial = _crystal;
                Transform part = renderer.transform;
                _crystalParts.Add(new CrystalPart { Transform = part, Position = part.position, Scale = part.localScale });
            }

            Renderer[] pad = model.RenderersEndingWith("Base_Pad");
            if (pad.Length > 0)
            {
                PadTop = new Vector3(pad[0].bounds.center.x, pad[0].bounds.max.y, pad[0].bounds.center.z);
            }

            Transform coreAt = Socket(model, CoreLightSocket, crystal.Length > 0 ? crystal[0].transform : command);
            _coreLight = AddLight(coreAt, _coreColour, 0f, CoreLightRange, 0f);
            _corePool = GlowDecal.PlacePool(command, _coreColour * 0.3f, 2.2f);
            _corePool.transform.position = new Vector3(_coreSocket.x, layout.Base.y + 0.035f, _coreSocket.z);
            AddLight(Socket(model, CommandLightSocket, command), glow, CommandLightIntensity, CommandLightRange, 0f);
        }

        private static void HideModule(ArtModel model, string module)
        {
            if (string.IsNullOrEmpty(module))
            {
                return;
            }

            foreach (Renderer renderer in model.RenderersEndingWith(module))
            {
                renderer.gameObject.SetActive(false);
            }
        }

        private static Transform Socket(ArtModel model, string name, Transform fallback)
        {
            Transform socket = model.Find(name);
            return socket != null ? socket : fallback;
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

                Material fieldMaterial = GateFieldMaterial(colour * GateFieldEmission);
                _gateFields.Add(fieldMaterial);
                Sheet(gate, fieldMaterial, new Vector3(0f, fieldCentre, 0f), Quaternion.identity, new Vector3(fieldWidth, GateFieldHeight, 1f));
                Sheet(gate, fieldMaterial, new Vector3(0f, 0.06f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(fieldWidth, 0.6f, 1f));
                AddModule(gate, parent, true);
            }
        }

        // The Blender gate-field sheet (U across with its rims, V tiling scanlines) or the marker art's energy field.
        private static Material GateFieldMaterial(Color colour)
        {
            Texture2D sheet = CutsceneFxSet.Get(CutsceneSprite.GateField);
            return sheet != null ? GlowDecal.Additive(sheet, colour) : GlowDecal.ScanlineMaterial(colour);
        }

        private void BuildBuilders(Transform parent, CutsceneContext context)
        {
            CutsceneCast cast = context.Cast;
            if (cast == null || (cast.Builder == null && cast.BuilderArt == null))
            {
                return;
            }

            int count = Mathf.Min(Builders, context.Scaled(Builders));
            for (int i = 0; i < count; i++)
            {
                var holder = new GameObject($"Builder{i}").transform;
                holder.SetParent(parent, false);
                holder.localScale = Vector3.one * BuilderScale;
                var drone = new CutsceneDrone(holder, cast.BuilderArt, cast.Builder);
                // The imported builder welds with its two arms; the procedural one from under its body.
                Transform weld = drone.Socket(WeldSockets[i % WeldSockets.Length]);
                ParticleSystem sparks = weld != null
                    ? ParticleKit.Create(weld, "Weld", Vector3.zero, SparkColour * 2.5f, 0.2f, 0.5f, 0.03f, 0.06f, 1.5f, 4f, 1.2f, 80, 0.03f)
                    : ParticleKit.Create(holder, "Weld", Vector3.down * 0.4f, SparkColour * 2.5f, 0.2f, 0.5f, 0.03f, 0.06f, 1.5f, 4f, 1.2f, 80, 0.05f);
                // A beam of light from the belly to the module it carries, so the load reads from any distance.
                LineRenderer tether = SpaceKit.Line(parent, $"Tether{i}", 2, false, TetherIntensity);
                tether.widthMultiplier = TetherWidth;
                tether.startColor = TetherColour;
                tether.endColor = TetherColour * 0.6f;
                tether.enabled = false;
                _builders.Add(new Builder { Drone = drone, Sparks = sparks, Tether = tether });
                // Its feet are measured while it is still active, before it waits hidden for the timelapse.
                drone.Feet();
                holder.gameObject.SetActive(false);
            }
        }

        private void AddModule(Transform root, Transform parent, bool turns)
        {
            ParticleSystem dust = ParticleKit.UseCell(ParticleKit.Create(parent, $"{root.name}Dust", root.position + Vector3.up * 0.2f, DustColour, 0.8f, 1.6f, 0.3f, 0.7f, 0.6f, 2f, -0.05f, 80, 1.1f), MarkerArt.ParticleCell.Mote);
            // Measured as built, so the drones hold it and hover clear of it whatever its pivot or import scale.
            Bounds bounds = BoundsOf(root);
            float reach = Mathf.Max(bounds.extents.x, bounds.extents.z);
            _modules.Add(new Module
            {
                Root = root,
                RestPosition = root.position,
                RestRotation = root.rotation,
                RestScale = root.localScale,
                Turns = turns,
                Dust = dust,
                Centre = bounds.center,
                Top = bounds.max.y,
                Reach = reach,
            });
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
            return direction.sqrMagnitude > 0.000001f ? direction : Vector3.forward;
        }
    }
}
