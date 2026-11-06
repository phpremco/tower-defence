using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The imported (Blender) look of the colony base: the modules (deck, command post, radar and dish, habitat, drone
    /// bays, storage, core, landing pad and shuttle, hardpoints) with their own pivots, the damage overlay shown once the
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
        [SerializeField] [Min(0.5f)] private float _shuttleFlightSeconds = 5f;
        [Tooltip("How high the shuttle climbs before it leaves the picture (it shrinks away up there).")]
        [SerializeField] [Min(0.5f)] private float _shuttleClimb = 7f;
        [Tooltip("How far it drifts along its nose while it climbs, so it leaves on a slant.")]
        [SerializeField] private float _shuttleDrift = 2.5f;
        [SerializeField] private Color _engineColor = new Color(0.55f, 0.8f, 1f);

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
        [Tooltip("Where the base's own guns stand (the TurretLink sockets), in the order they are fitted; empty keeps them in the cells beside the road end.")]
        [SerializeField] private Vector3[] _turretMounts = new Vector3[0];
        [Tooltip("The drone bays' dock sockets: the first drones launched live in these and idle above them.")]
        [SerializeField] private Vector3[] _dockPoints = new Vector3[0];

        [Header("Damage")]
        [Tooltip("Health share below which the damage overlay shows and the sockets smoke.")]
        [SerializeField] [Range(0.05f, 0.95f)] private float _damagedBelow = 0.5f;
        [Tooltip("Seconds after the base falls before the wreck replaces it: the flash hides the swap.")]
        [SerializeField] [Min(0f)] private float _wreckDelay = 0.25f;

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
        public Color EngineColor => _engineColor;
        public string BayDoorPrefix => _bayDoorPrefix;
        public string DockSocketPrefix => _dockSocketPrefix;
        public float DoorClosedDegrees => _doorClosedDegrees;
        public float DoorSeconds => _doorSeconds;
        public float DockSeconds => _dockSeconds;
        public Vector2 FootprintMin => _footprintMin;
        public Vector2 FootprintMax => _footprintMax;
        public Vector3[] TurretMounts => _turretMounts;
        public Vector3[] DockPoints => _dockPoints;
        public float DamagedBelow => _damagedBelow;
        public float WreckDelay => _wreckDelay;

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
