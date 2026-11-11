using Game.Core;
using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// The imported (Blender) look of the commander: the body with its clips, the hand weapons modelled in the grip space
    /// of their sockets, which sockets they ride in, the spine bones the aim turns, and the portrait bust. A hero
    /// definition points at one; without it the hero keeps the procedural figure. Pure look data: logic never reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Hero Art Definition", fileName = "HeroArt")]
    public sealed class HeroArtDefinition : ScriptableObject
    {
        [Header("Models")]
        [SerializeField] private ModelArtDefinition _body;
        [SerializeField] private ModelArtDefinition _pistol;
        [SerializeField] private ModelArtDefinition _swordHilt;
        [Tooltip("The blade meshes: drawn with an additive glow in the weapon's colour, not with this art's material.")]
        [SerializeField] private ModelArtDefinition _swordBlade;
        [Tooltip("Meshes of the weapon files kept for perks that do not exist yet (quick magazine, wide blade): hidden.")]
        [SerializeField] private string[] _hiddenParts = new string[0];

        [Header("Sockets")]
        [SerializeField] private string _handSocket = "Weapon_R";
        [SerializeField] private string _holsterSocket = "Holster_R";
        [SerializeField] private string _backSocket = "Back";
        [Tooltip("The bones the aim at a target turns, after the clips, sharing the turn equally.")]
        [SerializeField] private string[] _aimBones = { "Spine1", "Spine2" };
        [Tooltip("The bone the pack's thruster puffs and the down sparks come from.")]
        [SerializeField] private string _packBone = "Spine2";

        [Header("Weapons")]
        [Tooltip("The pistol's muzzle in its grip space.")]
        [SerializeField] private Vector3 _muzzle = new Vector3(0f, 0.062f, 0.192f);
        [Tooltip("Scale of the pistol model in the hand and the holster, so it reads from the game camera.")]
        [SerializeField] [Range(0.5f, 2.5f)] private float _pistolScale = 1.35f;
        [Tooltip("Where the light strip on top of the pistol starts, in grip space; it runs to the muzzle.")]
        [SerializeField] private Vector3 _pistolStripStart = new Vector3(0f, 0.07f, -0.02f);
        [Tooltip("Width of the light strip in metres.")]
        [SerializeField] [Min(0f)] private float _pistolStripWidth = 0.14f;
        [Tooltip("Brightness of the strip in the ranged weapon's colour (over 1 the bloom catches it); 0 hides it.")]
        [SerializeField] [Min(0f)] private float _pistolStripGlow = 3f;
        [Tooltip("Ground speed below which the hero walks instead of running.")]
        [SerializeField] [Min(0f)] private float _walkBelow = 1.5f;

        [Header("Jetpack")]
        [Tooltip("The pack's nozzles; each socket's Y axis points along the exhaust.")]
        [SerializeField] private string[] _jetSockets = { "Jet_L", "Jet_R" };
        [Tooltip("Colour of the jet flame: the colony's cool blue-white.")]
        [SerializeField] private Color _thrustColor = new Color(0.55f, 0.85f, 1f);
        [Tooltip("Length of the jet flame card in metres.")]
        [SerializeField] [Min(0.05f)] private float _thrustLength = 0.55f;
        [Tooltip("Width of the outer flame card as a share of its length.")]
        [SerializeField] [Range(0.2f, 1.5f)] private float _thrustWidth = 0.8f;
        [Tooltip("Brightness of the outer flame (over 1 the bloom catches it).")]
        [SerializeField] [Min(0f)] private float _thrustGlow = 2f;
        [Tooltip("Colour of the flame's hot core, drawn as a second, shorter and narrower card over the outer flame.")]
        [SerializeField] private Color _thrustCoreColor = new Color(0.9f, 0.97f, 1f);
        [Tooltip("Length of the core card as a share of the flame length.")]
        [SerializeField] [Range(0.1f, 1f)] private float _thrustCoreLength = 0.6f;
        [Tooltip("Width of the core card as a share of the outer flame's width.")]
        [SerializeField] [Range(0.1f, 1f)] private float _thrustCoreWidth = 0.4f;
        [SerializeField] [Min(0f)] private float _thrustCoreGlow = 4f;
        [Tooltip("The soft heat glow around each nozzle (a camera-facing radial card).")]
        [SerializeField] private Color _heatGlowColor = new Color(0.35f, 0.7f, 1f);
        [Tooltip("Diameter of the heat glow in metres.")]
        [SerializeField] [Min(0f)] private float _heatGlowSize = 0.7f;
        [SerializeField] [Min(0f)] private float _heatGlowStrength = 1.5f;
        [Tooltip("A point light at the pack while it burns; High (PC) only, the Low (Switch) profile skips it.")]
        [SerializeField] [Min(0f)] private float _heatLightIntensity = 3f;
        [SerializeField] [Min(0f)] private float _heatLightRange = 3f;

        [Header("Face")]
        [Tooltip("The face mesh's blend shape that closes the eyes, or empty for none.")]
        [SerializeField] private string _blinkShape = "Blink";
        [SerializeField] [Min(0.05f)] private float _blinkSeconds = 0.15f;
        [Tooltip("Seconds between two blinks: a random pick between x and y.")]
        [SerializeField] private Vector2 _blinkEvery = new Vector2(3f, 6f);
        [Tooltip("The acting clips' root offsets and face tracks (the Blender report's acting section), one per clip role.")]
        [SerializeField] private HeroActingClip[] _acting = System.Array.Empty<HeroActingClip>();

        [Header("Portrait")]
        [SerializeField] private PortraitArtDefinition _bust;

        public ModelArtDefinition Body => _body;
        public ModelArtDefinition Pistol => _pistol;
        public ModelArtDefinition SwordHilt => _swordHilt;
        public ModelArtDefinition SwordBlade => _swordBlade;
        public string[] HiddenParts => _hiddenParts;
        public string HandSocket => _handSocket;
        public string HolsterSocket => _holsterSocket;
        public string BackSocket => _backSocket;
        public string[] AimBones => _aimBones;
        public string PackBone => _packBone;
        public Vector3 Muzzle => _muzzle;
        public float WalkBelow => _walkBelow;
        public string[] JetSockets => _jetSockets;
        public Color ThrustColor => _thrustColor;
        public float ThrustLength => _thrustLength;
        public float ThrustWidth => _thrustWidth;
        public float ThrustGlow => _thrustGlow;
        public Color ThrustCoreColor => _thrustCoreColor;
        public float ThrustCoreLength => _thrustCoreLength;
        public float ThrustCoreWidth => _thrustCoreWidth;
        public float ThrustCoreGlow => _thrustCoreGlow;
        public Color HeatGlowColor => _heatGlowColor;
        public float HeatGlowSize => _heatGlowSize;
        public float HeatGlowStrength => _heatGlowStrength;
        public float HeatLightIntensity => _heatLightIntensity;
        public float HeatLightRange => _heatLightRange;
        public float PistolScale => _pistolScale;
        public Vector3 PistolStripStart => _pistolStripStart;
        public float PistolStripWidth => _pistolStripWidth;
        public float PistolStripGlow => _pistolStripGlow;
        public string BlinkShape => _blinkShape;
        public float BlinkSeconds => _blinkSeconds;
        public Vector2 BlinkEvery => _blinkEvery;
        public PortraitArtDefinition Bust => _bust;

        /// <summary>The acting data of a clip role (its root offset at the first frame and its face tracks), or null.</summary>
        public HeroActingClip ActingFor(ModelClipRole role)
        {
            foreach (HeroActingClip clip in _acting ?? System.Array.Empty<HeroActingClip>())
            {
                if (clip != null && clip.Role == role)
                {
                    return clip;
                }
            }

            return null;
        }

        /// <summary>Every face shape the acting clips drive, with the blink shape (for a <see cref="FaceRig"/>).</summary>
        public string[] FaceShapes()
        {
            var shapes = new System.Collections.Generic.List<string> { _blinkShape };
            foreach (HeroActingClip clip in _acting ?? System.Array.Empty<HeroActingClip>())
            {
                foreach (FaceCurve curve in clip.Face)
                {
                    if (!shapes.Contains(curve.Shape))
                    {
                        shapes.Add(curve.Shape);
                    }
                }
            }

            return shapes.ToArray();
        }

        private void OnValidate()
        {
            if (_body == null)
            {
                Debug.LogWarning($"HeroArtDefinition '{name}' has no body.", this);
            }
            else if (_body.IndexOf(ModelClipRole.Idle) < 0)
            {
                Debug.LogWarning($"HeroArtDefinition '{name}' has a body without an Idle clip.", this);
            }

            if (_blinkEvery.y < _blinkEvery.x || _blinkEvery.x <= _blinkSeconds)
            {
                Debug.LogWarning($"HeroArtDefinition '{name}' blinks between {_blinkEvery.x} and {_blinkEvery.y} s: the range must run upwards and exceed the blink itself.", this);
            }

            foreach (HeroActingClip clip in _acting ?? System.Array.Empty<HeroActingClip>())
            {
                foreach (FaceCurve curve in clip.Face)
                {
                    if (!curve.IsValid())
                    {
                        Debug.LogWarning($"HeroArtDefinition '{name}': the {clip.Role} face track '{curve.Shape}' needs a shape name, keys in time order and weights in 0..1.", this);
                    }
                }
            }
        }
    }
}
