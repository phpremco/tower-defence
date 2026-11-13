using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// What a ground shockwave looks like (the hero's pulse): a ring with a bright edge that races out to the ability's
    /// radius, a weaker echo ring behind it, a pool of light on the ground under them, a ring of refraction (high profile),
    /// a flash and a short light at the fist, and what the blow kicks up: sparks skimming outward, shards, and dust that
    /// rolls out behind the front. Every part fades in and out; nothing starts or ends on a hard edge.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Shockwave Vfx", fileName = "Vfx_Shockwave")]
    public sealed class ShockwaveVfxDefinition : ScriptableObject
    {
        [Header("Rings")]
        [Tooltip("The painted ring card (bright edge, soft inner falloff).")]
        [SerializeField] private VfxSheet _ringSheet;
        [Tooltip("Seconds the front takes to reach the radius (it eases out).")]
        [SerializeField] [Min(0.1f)] private float _ringSeconds = 0.5f;
        [Tooltip("Peak brightness of the ring; it fades over the last part of its run.")]
        [SerializeField] [Min(0f)] private float _ringIntensity = 2.4f;
        [Tooltip("How far the ring's colour leans to white (the hot edge): 0 keeps the ability's tint.")]
        [SerializeField] [Range(0f, 1f)] private float _whiteShare = 0.35f;
        [Tooltip("Seconds after the first ring the echo leaves; 0 for no echo.")]
        [SerializeField] [Min(0f)] private float _echoDelay = 0.13f;
        [SerializeField] [Range(0f, 1f)] private float _echoStrength = 0.5f;
        [Tooltip("The echo's reach as a share of the radius.")]
        [SerializeField] [Range(0.1f, 1f)] private float _echoReach = 0.82f;

        [Header("Ground glow")]
        [Tooltip("Peak brightness of the pool of light the wave leaves on the ground; 0 for none.")]
        [SerializeField] [Min(0f)] private float _glowIntensity = 0.9f;
        [SerializeField] [Min(0.1f)] private float _glowSeconds = 0.7f;

        [Header("Refraction (high profile)")]
        [SerializeField] private VfxSheet _distortion;
        [SerializeField] [Min(0f)] private float _distortionStrength = 0.03f;

        [Header("Flash and light")]
        [Tooltip("Additive flash flipbook at the fist, in the ability's tint.")]
        [SerializeField] private VfxSheet _flash;
        [SerializeField] [Min(0f)] private float _flashSize = 2.4f;
        [SerializeField] [Min(0f)] private float _lightIntensity = 7f;
        [SerializeField] [Min(0f)] private float _lightRange = 6f;
        [SerializeField] [Min(0.05f)] private float _lightSeconds = 0.35f;

        [Header("Kicked up")]
        [SerializeField] private VfxSheet _sparkSheet;
        [SerializeField] [Min(0)] private int _sparks = 32;
        [Tooltip("Sparks leave at about the front's speed, so they ride the ring out.")]
        [SerializeField] [Min(0f)] private float _sparkSpeed = 7f;
        [SerializeField] private VfxSheet _debrisSheet;
        [SerializeField] [Min(0)] private int _shards = 14;
        [SerializeField] [Min(0f)] private float _shardSpeed = 4.5f;
        [Tooltip("Puffs of dust that roll outward along the ground behind the front and swell as they thin.")]
        [SerializeField] [Min(0)] private int _dustPuffs = 14;
        [Tooltip("Size of a puff as it leaves, in metres.")]
        [SerializeField] [Min(0f)] private float _dustSize = 0.7f;
        [SerializeField] [Min(0.1f)] private float _dustSeconds = 0.9f;
        [Tooltip("Colour and cover of the dust; unlit, so it reads in a dark level too.")]
        [SerializeField] private Color _dustColor = new Color(0.78f, 0.85f, 0.95f, 0.6f);

        public VfxSheet RingSheet => _ringSheet;
        public float RingSeconds => _ringSeconds;
        public float RingIntensity => _ringIntensity;
        public float WhiteShare => _whiteShare;
        public float EchoDelay => _echoDelay;
        public float EchoStrength => _echoStrength;
        public float EchoReach => _echoReach;
        public float GlowIntensity => _glowIntensity;
        public float GlowSeconds => _glowSeconds;
        public VfxSheet Distortion => _distortion;
        public float DistortionStrength => _distortionStrength;
        public VfxSheet Flash => _flash;
        public float FlashSize => _flashSize;
        public float LightIntensity => _lightIntensity;
        public float LightRange => _lightRange;
        public float LightSeconds => _lightSeconds;
        public VfxSheet SparkSheet => _sparkSheet;
        public int Sparks => _sparks;
        public float SparkSpeed => _sparkSpeed;
        public VfxSheet DebrisSheet => _debrisSheet;
        public int Shards => _shards;
        public float ShardSpeed => _shardSpeed;
        public int DustPuffs => _dustPuffs;
        public float DustSize => _dustSize;
        public float DustSeconds => _dustSeconds;
        public Color DustColor => _dustColor;

        /// <summary>Whether an echo ring follows the first.</summary>
        public bool HasEcho => _echoDelay > 0f && _echoStrength > 0f;

        /// <summary>How long the rings, the glow and the light take together; the particles outlive them on their own.</summary>
        public float Life => Mathf.Max(Mathf.Max(_ringSeconds + (HasEcho ? _echoDelay : 0f), _glowSeconds), _lightSeconds);

        private void OnValidate()
        {
            if (_ringSheet == null)
            {
                Debug.LogWarning($"{name}: the shockwave has no ring sheet, so it draws no ring.", this);
            }

            if (_echoDelay >= _ringSeconds)
            {
                Debug.LogWarning($"{name}: the echo leaves after the first ring is gone ({_echoDelay} s against {_ringSeconds} s).", this);
            }
        }
    }
}
