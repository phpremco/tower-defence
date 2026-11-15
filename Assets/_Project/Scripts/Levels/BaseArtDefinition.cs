using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The imported (Blender) look of the colony base: the modules (deck, command post, radar and dish, habitat, drone
    /// bays, storage, core, landing pad and shuttle, hardpoints) with their own pivots, the colony flag in the courtyard, the damage overlay shown once the
    /// base is badly hurt, the wreck left after the blast, which modules turn, pulse, blink, fly or open, and the ground
    /// the model covers. A <see cref="VisualTheme"/> points at one of these; without it the level keeps its procedural
    /// outpost. Look data, plus the footprint the build grid keeps free so no tower stands inside the base.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Base Art Definition", fileName = "BaseArt")]
    public sealed class BaseArtDefinition : ScriptableObject
    {
        [SerializeField] private ModelArtDefinition _model;
        [SerializeField] private ModelArtDefinition _damage;
        [SerializeField] private ModelArtDefinition _wreck;

        [Header("Life")]
        [Tooltip("Name of the module that turns on its own pivot about world up (the radar dish).")]
        [SerializeField] private string _turningModule = "Base_Dish";
        [SerializeField] private float _turnDegreesPerSecond = 24f;
        [Tooltip("Name of the module whose glow breathes and dies with the base (the core crystal).")]
        [SerializeField] private string _pulsingModule = "Base_Crystal";
        [Tooltip("The modules in the order the opening cutscene's timelapse raises them (the report's timelapse_order), by mesh name.")]
        [SerializeField] private string[] _timelapseOrder = new string[0];

        [Header("Beacon")]
        [Tooltip("Name of the module whose glow blinks (the beacon on the radar mast); empty for none.")]
        [SerializeField] private string _beaconModule = "Base_Beacon";
        [SerializeField] [Min(0.1f)] private float _beaconPeriod = 1.6f;
        [Tooltip("Share of the period the beacon is lit.")]
        [SerializeField] [Range(0.05f, 0.95f)] private float _beaconLitShare = 0.22f;
        [Tooltip("Emission of the lit beacon against its painted glow.")]
        [SerializeField] [Min(0f)] private float _beaconLitGlow = 3f;
        [Tooltip("Emission of the dark beacon against its painted glow.")]
        [SerializeField] [Range(0f, 1f)] private float _beaconDimGlow = 0.15f;

        [Header("Shuttle")]
        [Tooltip("Name of the module that takes off from and lands on the pad (the shuttle); empty for none. The opening cutscene leaves it out, since its haulers use the pad.")]
        [SerializeField] private string _shuttleModule = "Base_Shuttle";
        [Tooltip("Socket under the shuttle where its engine glows.")]
        [SerializeField] private string _exhaustSocket = "Socket_ShuttleExhaust";
        [SerializeField] [Min(1f)] private float _shuttleRestSeconds = 16f;
        [SerializeField] [Min(1f)] private float _shuttleAwaySeconds = 12f;
        [Tooltip("Seconds the climb and the descent each take, eased at both ends.")]
        [SerializeField] [Min(0.5f)] private float _shuttleFlightSeconds = 8f;
        [Tooltip("How high the shuttle climbs before it leaves the picture (it shrinks away up there).")]
        [SerializeField] [Min(0.5f)] private float _shuttleClimb = 22f;
        [Tooltip("How far it drifts along its nose while it climbs, so it leaves on a slant.")]
        [SerializeField] private float _shuttleDrift = 10f;
        [SerializeField] private Color _engineColor = new Color(0.55f, 0.8f, 1f);
        [Tooltip("Round 63 (Remco: \"it literally disappears as it starts to fly\"): seconds the engines spool up on the pad before lift-off, and spool down after touch-down.")]
        [SerializeField] [Min(0.1f)] private float _shuttleSpoolSeconds = 1.8f;
        [Tooltip("How far its heading turns over the climb (degrees, sign = side): a climbing, banking turn away from the base.")]
        [SerializeField] private float _shuttleTurnDegrees = 75f;
        [Tooltip("How far it banks into that turn at its height.")]
        [SerializeField] [Range(0f, 60f)] private float _shuttleBankDegrees = 26f;
        [Tooltip("How far its nose comes up as it gathers speed.")]
        [SerializeField] [Range(0f, 45f)] private float _shuttleClimbPitch = 14f;
        [Tooltip("The last share of the climb over which it shrinks into the sky (an orthographic camera shows no distance); it reaches nothing as the climb ends, so it never vanishes in view.")]
        [SerializeField] [Range(0.1f, 0.9f)] private float _shuttleShrinkShare = 0.55f;
        [Tooltip("The main engine's plume: mouth radius and full length in metres (the ExhaustPlume style).")]
        [SerializeField] [Min(0.01f)] private float _shuttlePlumeRadius = 0.07f;
        [SerializeField] [Min(0.05f)] private float _shuttlePlumeLength = 0.8f;
        [Tooltip("The lift jets' plumes: mouth radius and full length in metres.")]
        [SerializeField] [Min(0.01f)] private float _shuttleLiftPlumeRadius = 0.045f;
        [SerializeField] [Min(0.05f)] private float _shuttleLiftPlumeLength = 0.45f;
        [Tooltip("How bright the plumes burn at full thrust.")]
        [SerializeField] [Min(0f)] private float _shuttlePlumeIntensity = 1.4f;
        [Tooltip("The share of full thrust the engines idle at while they spool up on the pad.")]
        [SerializeField] [Range(0f, 1f)] private float _shuttleIdleThrust = 0.35f;
        [Tooltip("Seconds of flight the vapour trail behind it covers, and its width in metres.")]
        [SerializeField] [Min(0.1f)] private float _shuttleTrailSeconds = 1.6f;
        [SerializeField] [Min(0.01f)] private float _shuttleTrailWidth = 0.16f;
        [Tooltip("Dust thrown up off the pad by the engines while the shuttle is low, per second at full thrust.")]
        [SerializeField] [Min(0f)] private float _shuttleDustRate = 70f;
        [Tooltip("Height in metres under which the engines still raise dust from the pad.")]
        [SerializeField] [Min(0.1f)] private float _shuttleDustHeight = 2.5f;
        [Tooltip("The launch roar (a loop): it swells in with the spool-up, peaks at lift-off and fades away as the shuttle climbs; the same, softer, as it comes home.")]
        [SerializeField] private AudioClip _shuttleRoar;
        [SerializeField] [Range(0f, 1f)] private float _shuttleRoarVolume = 0.2f;
        [Tooltip("The roar's level on the way home against the launch.")]
        [SerializeField] [Range(0f, 1f)] private float _shuttleReturnShare = 0.6f;

        [Header("Drone bays")]
        [Tooltip("Prefix of the bay lids, numbered _1.._n like the dock sockets.")]
        [SerializeField] private string _bayDoorPrefix = "Base_BayDoor";
        [Tooltip("Prefix of the dock sockets on the well floors, numbered _1.._n.")]
        [SerializeField] private string _dockSocketPrefix = "Socket_DroneBay";
        [Tooltip("Degrees about the lid's local Z (its hinge) that close it; the export stands open.")]
        [SerializeField] private float _doorClosedDegrees = 100f;
        [SerializeField] [Min(0.1f)] private float _doorSeconds = 0.7f;
        [Tooltip("Seconds a drone takes to sink into its bay or rise out of it.")]
        [SerializeField] [Min(0.1f)] private float _dockSeconds = 1.2f;

        [Header("Footprint and places (the base's own frame: x across, z towards the road; measured by the generator)")]
        [Tooltip("The ground the model covers (x across, y towards the road).")]
        [SerializeField] private Vector2 _footprintMin = new Vector2(-1f, -1f);
        [SerializeField] private Vector2 _footprintMax = new Vector2(1f, 1f);
        [Tooltip("Round 63: the ground under the footprint is levelled to the base's own height this far past its edge, so the plinth stands on the ground and never sinks into the rolling land behind the road's end (metres).")]
        [SerializeField] [Min(0f)] private float _groundPadMargin = 0.5f;
        [Tooltip("Round 63: over this many metres past the levelled pad the ground blends back into the land round it.")]
        [SerializeField] [Min(0.1f)] private float _groundPadBlend = 2.5f;
        [Tooltip("Where the base's own guns stand (the TurretLink sockets), in the order they are fitted; empty keeps them in the cells beside the road end.")]
        [SerializeField] private Vector3[] _turretMounts = new Vector3[0];
        [Tooltip("The drone bays' dock sockets: the first drones launched live in these and idle above them.")]
        [SerializeField] private Vector3[] _dockPoints = new Vector3[0];
        [Tooltip("Where drones beyond the bays hover (only x and z count; they fly at their own height): over the low roofs and the deck's front lip, clear of the radar mast, the command post and the shuttle's climb.")]
        [SerializeField] private Vector3[] _idlePoints =
        {
            new Vector3(0.15f, 0f, -1.4f), new Vector3(1.3f, 0f, 1.5f), new Vector3(-1.3f, 0f, 1.5f),
            new Vector3(-0.4f, 0f, -0.6f), new Vector3(0f, 0f, 1.6f),
        };

        [Header("Flag")]
        [Tooltip("The colony's flag on its pole (the NTI flag), waving in the courtyard; empty for none.")]
        [SerializeField] private ModelArtDefinition _flag;
        [Tooltip("Where the flag's pole stands, in the base model's own frame (the export's, before the art's turn).")]
        [SerializeField] private Vector3 _flagPosition = new Vector3(0.2f, 0.23f, -0.75f);
        [Tooltip("How the flag stands, in the base model's own frame.")]
        [SerializeField] private Vector3 _flagEuler = new Vector3(0f, 180f, 0f);

        [Header("Damage")]
        [Tooltip("Health share below which the damage overlay shows and the sockets smoke.")]
        [SerializeField] [Range(0.05f, 0.95f)] private float _damagedBelow = 0.5f;
        [Tooltip("Seconds after the base falls before the wreck replaces it: the flash hides the swap.")]
        [SerializeField] [Min(0f)] private float _wreckDelay = 0.25f;

        [Header("Wreck look (it must read at the play zoom on a dark field)")]
        [Tooltip("Factor on the wreck's painted colour: the charred deck is lifted a little so its shape reads.")]
        [SerializeField] [Range(1f, 3f)] private float _wreckLift = 1.5f;
        [Tooltip("Factor on the wreck's painted glow (its embers and torn cables).")]
        [SerializeField] [Range(0f, 6f)] private float _wreckGlow = 1f;
        [Tooltip("Colour of the fire that keeps burning in the wreck: its light, its rim on the plates and its embers.")]
        [SerializeField] private Color _wreckFireColor = new Color(1f, 0.48f, 0.16f);
        [SerializeField] [Min(0f)] private float _wreckFireIntensity = 5f;
        [SerializeField] [Min(0.5f)] private float _wreckFireRange = 6f;
        [Tooltip("Height of the fire light over the deck.")]
        [SerializeField] [Min(0f)] private float _wreckFireHeight = 1.3f;
        [Tooltip("Strength of the warm rim the fire gives the wreck's edges.")]
        [SerializeField] [Range(0f, 3f)] private float _wreckRim = 1.2f;
        [Tooltip("Embers rising from the wreck per second, per fire.")]
        [SerializeField] [Min(0f)] private float _wreckEmberRate = 10f;
        [Tooltip("Smoke puffs per second, per fire.")]
        [SerializeField] [Min(0f)] private float _wreckSmokeRate = 3f;

        public ModelArtDefinition Model => _model;
        public ModelArtDefinition Damage => _damage;
        public ModelArtDefinition Wreck => _wreck;
        public string TurningModule => _turningModule;
        public float TurnDegreesPerSecond => _turnDegreesPerSecond;
        public string PulsingModule => _pulsingModule;
        public string[] TimelapseOrder => _timelapseOrder;
        public string BeaconModule => _beaconModule;
        public float BeaconPeriod => _beaconPeriod;
        public float BeaconLitShare => _beaconLitShare;
        public float BeaconLitGlow => _beaconLitGlow;
        public float BeaconDimGlow => _beaconDimGlow;
        public string ShuttleModule => _shuttleModule;
        public string ExhaustSocket => _exhaustSocket;
        public float ShuttleRestSeconds => _shuttleRestSeconds;
        public float ShuttleAwaySeconds => _shuttleAwaySeconds;
        public float ShuttleFlightSeconds => _shuttleFlightSeconds;
        public float ShuttleClimb => _shuttleClimb;
        public float ShuttleDrift => _shuttleDrift;
        public float ShuttleSpoolSeconds => _shuttleSpoolSeconds;
        public float ShuttleTurnDegrees => _shuttleTurnDegrees;
        public float ShuttleBankDegrees => _shuttleBankDegrees;
        public float ShuttleClimbPitch => _shuttleClimbPitch;
        public float ShuttleShrinkShare => _shuttleShrinkShare;
        public float ShuttlePlumeRadius => _shuttlePlumeRadius;
        public float ShuttlePlumeLength => _shuttlePlumeLength;
        public float ShuttleLiftPlumeRadius => _shuttleLiftPlumeRadius;
        public float ShuttleLiftPlumeLength => _shuttleLiftPlumeLength;
        public float ShuttlePlumeIntensity => _shuttlePlumeIntensity;
        public float ShuttleIdleThrust => _shuttleIdleThrust;
        public float ShuttleTrailSeconds => _shuttleTrailSeconds;
        public float ShuttleTrailWidth => _shuttleTrailWidth;
        public float ShuttleDustRate => _shuttleDustRate;
        public float ShuttleDustHeight => _shuttleDustHeight;
        public AudioClip ShuttleRoar => _shuttleRoar;
        public float ShuttleRoarVolume => _shuttleRoarVolume;
        public float ShuttleReturnShare => _shuttleReturnShare;
        public Color EngineColor => _engineColor;
        public string BayDoorPrefix => _bayDoorPrefix;
        public string DockSocketPrefix => _dockSocketPrefix;
        public float DoorClosedDegrees => _doorClosedDegrees;
        public float DoorSeconds => _doorSeconds;
        public float DockSeconds => _dockSeconds;
        public Vector2 FootprintMin => _footprintMin;
        public Vector2 FootprintMax => _footprintMax;
        public float GroundPadMargin => _groundPadMargin;
        public float GroundPadBlend => _groundPadBlend;
        public Vector3[] TurretMounts => _turretMounts;
        public Vector3[] DockPoints => _dockPoints;
        public ModelArtDefinition Flag => _flag;
        public Vector3 FlagPosition => _flagPosition;
        public Vector3 FlagEuler => _flagEuler;
        public float DamagedBelow => _damagedBelow;
        public float WreckDelay => _wreckDelay;
        public float WreckLift => _wreckLift;
        public float WreckGlow => _wreckGlow;
        public Color WreckFireColor => _wreckFireColor;
        public float WreckFireIntensity => _wreckFireIntensity;
        public float WreckFireRange => _wreckFireRange;
        public float WreckFireHeight => _wreckFireHeight;
        public float WreckRim => _wreckRim;
        public float WreckEmberRate => _wreckEmberRate;
        public float WreckSmokeRate => _wreckSmokeRate;
        public Vector3[] IdlePoints => _idlePoints;

        /// <summary>A point of the base's own frame on the field, the base standing at <paramref name="centre"/> with <paramref name="facing"/>.</summary>
        public static Vector3 ToWorld(Vector3 local, Vector3 centre, Quaternion facing) => centre + facing * local;

        /// <summary>
        /// True when a ground point lies under the model standing at <paramref name="centre"/> with
        /// <paramref name="facing"/>, grown by <paramref name="margin"/> on every side.
        /// </summary>
        public bool Covers(Vector3 point, Vector3 centre, Quaternion facing, float margin)
        {
            Vector3 local = Quaternion.Inverse(facing) * (point - centre);
            return local.x >= _footprintMin.x - margin && local.x <= _footprintMax.x + margin
                && local.z >= _footprintMin.y - margin && local.z <= _footprintMax.y + margin;
        }

        /// <summary>
        /// Round 63: how far a ground point lies outside the footprint of the model standing at <paramref name="centre"/> with
        /// <paramref name="facing"/> (0 on or inside it), in metres on the ground.
        /// </summary>
        public float DistanceOutside(Vector3 point, Vector3 centre, Quaternion facing)
        {
            Vector3 local = Quaternion.Inverse(facing) * (point - centre);
            float dx = Mathf.Max(0f, Mathf.Max(_footprintMin.x - local.x, local.x - _footprintMax.x));
            float dz = Mathf.Max(0f, Mathf.Max(_footprintMin.y - local.z, local.z - _footprintMax.y));
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void OnValidate()
        {
            if (_model == null)
            {
                Debug.LogWarning($"BaseArtDefinition '{name}' has no model.", this);
            }

            if (_footprintMax.x <= _footprintMin.x || _footprintMax.y <= _footprintMin.y)
            {
                Debug.LogWarning($"BaseArtDefinition '{name}' has an empty footprint.", this);
            }
        }
    }
}
