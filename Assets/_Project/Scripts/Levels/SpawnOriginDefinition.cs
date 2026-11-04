using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The living spawn origins and route gates as data (ArtSource README rounds 23 and 25b): the timing of an origin's
    /// cycle (read by <see cref="OriginDirector"/>), the opening each terrain wears, the burst's effect and the glow per
    /// state (read by <see cref="SpawnOriginView"/>), and the models of the ground and air gates. A level points at one.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Spawn Origin Definition", fileName = "SpawnOrigins")]
    public sealed class SpawnOriginDefinition : ScriptableObject
    {
        [Header("Timing (seconds)")]
        [Tooltip("The ground starts to open this long before the origin's first creature of the wave is due.")]
        [SerializeField] [Min(0f)] private float _openLead = 2.5f;
        [Tooltip("From the start of the opening to the burst (the Open clip's frame 54 at 30 fps).")]
        [SerializeField] [Min(0f)] private float _burst = 1.8f;
        [Tooltip("From the start of the opening until the first creature may come out (frame 76).")]
        [SerializeField] [Min(0f)] private float _emerge = 2.53f;
        [Tooltip("Length of the Open clip (96 frames).")]
        [SerializeField] [Min(0.1f)] private float _open = 3.2f;

        [Header("Origins")]
        [SerializeField] private SpawnOriginArt[] _origins = new SpawnOriginArt[0];
        [Tooltip("Glow of a marked burrow as a share of its full glow, low and high of a slow pulse (README: 0.35 to 1).")]
        [SerializeField] private Vector2 _markedGlow = new Vector2(0.35f, 1f);
        [SerializeField] [Min(0.1f)] private float _markedPulseSeconds = 2.5f;
        [Tooltip("Seconds the glow takes to come up to full once the ground starts to open (README: 30 frames).")]
        [SerializeField] [Min(0.01f)] private float _openGlowSeconds = 1f;
        [Tooltip("The burst's effect from the effect library, played at the origin with the element's colour.")]
        [SerializeField] private ImpactVfxDefinition _burstVfx;
        [SerializeField] [Min(0f)] private float _burstRadius = 1.2f;
        [Tooltip("The light in the maw: marked, open.")]
        [SerializeField] private Vector2 _lightIntensity = new Vector2(0.35f, 1.6f);
        [SerializeField] [Min(0f)] private float _lightRange = 2.8f;

        [Header("Route gates")]
        [SerializeField] private ModelArtDefinition _groundGate;
        [SerializeField] private ModelArtDefinition _airGate;
        [Tooltip("README round 25b: 1.0 keeps the old 1.9 footprint.")]
        [SerializeField] [Min(0.1f)] private float _gateScale = 1f;
        [SerializeField] [Min(0f)] private float _airGateHeight = 1.6f;
        [SerializeField] [Min(0.1f)] private float _airGateScale = 0.8f;
        [Tooltip("Glow of the gates as a multiple of their baked emission (README: 1.5 to 2.4).")]
        [SerializeField] [Min(0f)] private float _gateGlow = 1.8f;

        public OriginTiming Timing => new OriginTiming(_openLead, _burst, _emerge, _open);
        public Vector2 MarkedGlow => _markedGlow;
        public float MarkedPulseSeconds => _markedPulseSeconds;
        public float OpenGlowSeconds => _openGlowSeconds;
        public ImpactVfxDefinition BurstVfx => _burstVfx;
        public float BurstRadius => _burstRadius;
        public Vector2 LightIntensity => _lightIntensity;
        public float LightRange => _lightRange;
        public ModelArtDefinition GroundGate => _groundGate;
        public ModelArtDefinition AirGate => _airGate;
        public float GateScale => _gateScale;
        public float AirGateHeight => _airGateHeight;
        public float AirGateScale => _airGateScale;
        public float GateGlow => _gateGlow;

        /// <summary>The opening for a nest on that terrain, or null to keep the static one.</summary>
        public SpawnOriginArt For(TerrainKind terrain)
        {
            foreach (SpawnOriginArt origin in _origins)
            {
                if (origin != null && origin.Terrain == terrain && origin.Model != null)
                {
                    return origin;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            if (_emerge < _burst || _open < _emerge)
            {
                Debug.LogWarning($"Spawn origins '{name}': the burst must come before the first creature, and that before the opening ends.", this);
            }
        }
    }
}
