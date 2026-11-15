using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// How the hero's permanent gear looks (round 66): the small repair drone that follows him and lights him up while he
    /// mends, the armour plates that appear and grow with the armour tier, and the blade at its top tier (another colour
    /// and an aura). Pure look data, read by the hero's views; what the gear does is in the upgrade definitions.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hero Gear Art", fileName = "HeroGearArt")]
    public sealed class HeroGearArtDefinition : ScriptableObject
    {
        [Header("Companion repair drone")]
        [Tooltip("The drone's model (the repair drone's own, worn small).")]
        [SerializeField] private ModelArtDefinition _droneBody;
        [Tooltip("Size of the companion as a share of the model's own size.")]
        [SerializeField] [Range(0.1f, 1f)] private float _droneScale = 0.45f;
        [Tooltip("Where it hovers beside him: x to his right, y above his feet, z in front of him, in metres.")]
        [SerializeField] private Vector3 _droneOffset = new Vector3(-0.5f, 1.85f, -0.2f);
        [Tooltip("How fast it closes on its place (share of the gap per second); lower trails further behind when he moves.")]
        [SerializeField] [Min(0.5f)] private float _droneFollow = 5f;
        [SerializeField] [Min(0f)] private float _droneBobHeight = 0.05f;
        [SerializeField] [Min(0f)] private float _droneBobSpeed = 2.2f;
        [Tooltip("The socket on the drone model the beam leaves from, and the bones whose names start with this spin as rotors.")]
        [SerializeField] private string _droneBeamSocket = "Beam";
        [SerializeField] private string _droneRotorPrefix = "Rotor";
        [SerializeField] [Min(0f)] private float _droneRotorDegreesPerSecond = 1500f;
        [Tooltip("The bone on the hero the mending beam lands on.")]
        [SerializeField] private string _mendBone = "Spine2";
        [Tooltip("Where on that bone, in metres along it.")]
        [SerializeField] private float _mendAlong = 0.12f;
        [Tooltip("Colour of the mending beam and its glow: cool, the colony's.")]
        [SerializeField] private Color _mendColor = new Color(0.35f, 1f, 0.85f);
        [Tooltip("Brightness of the beam (over 1 the bloom catches it).")]
        [SerializeField] [Min(0f)] private float _mendBeamGlow = 0.7f;
        [Tooltip("How wide the cone of light opens where it meets him, as a share of the painted tool cone's own mouth.")]
        [SerializeField] [Range(0.02f, 1f)] private float _mendBeamWidth = 0.13f;
        [Tooltip("Diameter and strength of the soft glow on him while he mends.")]
        [SerializeField] [Min(0f)] private float _mendGlowSize = 0.6f;
        [SerializeField] [Min(0f)] private float _mendGlowStrength = 0.5f;
        [Tooltip("The drone's own small running light under its hull, so it reads at the play zoom: diameter in metres and strength; zero for none.")]
        [SerializeField] [Min(0f)] private float _droneLightSize = 0.3f;
        [SerializeField] [Min(0f)] private float _droneLightStrength = 0.45f;
        [Tooltip("Seconds the beam and glow take to come up and to die away.")]
        [SerializeField] [Min(0.01f)] private float _mendFadeSeconds = 0.35f;

        [Header("Armour plates")]
        [SerializeField] private ArmourPlateSpec[] _plates = new ArmourPlateSpec[0];
        [Tooltip("The plates' own colour: the suit's pale steel.")]
        [SerializeField] private Color _plateColor = new Color(0.78f, 0.84f, 0.92f);
        [Tooltip("Colour of each plate's frame (its outer band, rim and underside): the suit's dark blue.")]
        [SerializeField] private Color _plateFrameColor = new Color(0.1f, 0.2f, 0.5f);
        [Tooltip("Colour and brightness of the glowing ridge down each plate.")]
        [SerializeField] private Color _plateTrimColor = new Color(0.35f, 0.8f, 1f);
        [SerializeField] [Min(0f)] private float _plateTrimGlow = 2.2f;
        [Tooltip("How much bigger a plate is at the last armour tier than at the tier that adds it (1.3 = 30% bigger).")]
        [SerializeField] [Min(1f)] private float _plateGrowth = 1.3f;
        [Tooltip("Thickness of a plate's rim in metres.")]
        [SerializeField] [Min(0.002f)] private float _plateThickness = 0.02f;

        [Header("Blade at its top tier")]
        [Tooltip("The blade's colour once the blade damage upgrade stands at the highest tier open to the player.")]
        [SerializeField] private Color _masterBladeColor = new Color(1f, 0.16f, 0.1f);
        [Tooltip("Width of the long glow laid along the blade, in metres (it fades to nothing at its edge).")]
        [SerializeField] [Min(0.05f)] private float _auraWidth = 0.6f;
        [Tooltip("Brightness of the aura; it breathes between this and the low share of it.")]
        [SerializeField] [Min(0f)] private float _auraGlow = 2.2f;
        [SerializeField] [Range(0f, 1f)] private float _auraLowShare = 0.6f;
        [SerializeField] [Min(0.1f)] private float _auraPulseSeconds = 1.4f;
        [Tooltip("The soft round glow about the middle of the blade, as a multiple of the blade's length, and its strength.")]
        [SerializeField] [Min(0f)] private float _haloSize = 1.2f;
        [SerializeField] [Min(0f)] private float _haloStrength = 0.7f;
        [Tooltip("Embers shed along the blade per second (they stay behind in a swing); zero for none. The Low profile sheds a third.")]
        [SerializeField] [Min(0f)] private float _emberRate = 16f;
        [SerializeField] private Color _emberColor = new Color(1f, 0.3f, 0.12f);

        public ModelArtDefinition DroneBody => _droneBody;
        public float DroneScale => _droneScale;
        public Vector3 DroneOffset => _droneOffset;
        public float DroneFollow => _droneFollow;
        public float DroneBobHeight => _droneBobHeight;
        public float DroneBobSpeed => _droneBobSpeed;
        public string DroneBeamSocket => _droneBeamSocket;
        public string DroneRotorPrefix => _droneRotorPrefix;
        public float DroneRotorDegreesPerSecond => _droneRotorDegreesPerSecond;
        public string MendBone => _mendBone;
        public float MendAlong => _mendAlong;
        public Color MendColor => _mendColor;
        public float MendBeamGlow => _mendBeamGlow;
        public float MendBeamWidth => _mendBeamWidth;
        public float MendGlowSize => _mendGlowSize;
        public float MendGlowStrength => _mendGlowStrength;
        public float MendFadeSeconds => _mendFadeSeconds;
        public float DroneLightSize => _droneLightSize;
        public float DroneLightStrength => _droneLightStrength;
        public ArmourPlateSpec[] Plates => _plates;
        public Color PlateColor => _plateColor;
        public Color PlateFrameColor => _plateFrameColor;
        public Color PlateTrimColor => _plateTrimColor;
        public float PlateTrimGlow => _plateTrimGlow;
        public float PlateGrowth => _plateGrowth;
        public float PlateThickness => _plateThickness;
        public Color MasterBladeColor => _masterBladeColor;
        public float AuraWidth => _auraWidth;
        public float AuraGlow => _auraGlow;
        public float AuraLowShare => _auraLowShare;
        public float AuraPulseSeconds => _auraPulseSeconds;
        public float HaloSize => _haloSize;
        public float HaloStrength => _haloStrength;
        public float EmberRate => _emberRate;
        public Color EmberColor => _emberColor;

        private void OnValidate()
        {
            foreach (ArmourPlateSpec plate in _plates)
            {
                if (string.IsNullOrEmpty(plate.Bone) || plate.Size.x <= 0f || plate.Size.y <= 0f)
                {
                    Debug.LogWarning($"HeroGearArtDefinition '{name}' has an armour plate without a bone or a size.", this);
                }
            }
        }
    }
}
