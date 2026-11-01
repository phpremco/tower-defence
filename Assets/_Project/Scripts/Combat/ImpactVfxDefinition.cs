using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// What one kind of hit or blast looks and feels like: the size and life of the flash, the shock ring, the
    /// sparks, smoke and shards it throws, the light it casts and how hard it kicks the camera. Every value scales
    /// with the blast radius where that makes sense, so one definition serves a weapon on all its upgrade levels.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Impact Vfx", fileName = "Vfx")]
    public sealed class ImpactVfxDefinition : ScriptableObject
    {
        [Header("Flash")]
        [Tooltip("Size of the glowing core at full size, in blast radii (a hit without radius uses the minimum).")]
        [SerializeField] [Min(0f)] private float _flashScale = 1f;
        [SerializeField] [Min(0.05f)] private float _flashSeconds = 0.25f;
        [Tooltip("How much of the weapon colour survives in the flash: 0 keeps the weapon colour, 1 is pure fire.")]
        [SerializeField] [Range(0f, 1f)] private float _fireShare = 0.6f;

        [Header("Shock ring")]
        [SerializeField] private bool _ring = true;
        [Tooltip("Final ring diameter in blast radii.")]
        [SerializeField] [Min(0f)] private float _ringScale = 2.4f;
        [SerializeField] [Range(0f, 1f)] private float _ringAlpha = 0.5f;

        [Header("Particles")]
        [SerializeField] [Min(0)] private int _sparks = 6;
        [SerializeField] [Min(0)] private int _sparksPerRadius = 12;
        [SerializeField] [Min(0f)] private float _sparkSpeed = 3f;
        [SerializeField] [Min(0)] private int _smoke = 3;
        [SerializeField] [Min(0)] private int _smokePerRadius = 4;
        [SerializeField] [Min(0)] private int _shards;
        [SerializeField] [Min(0)] private int _shardsPerRadius;
        [SerializeField] [Min(0f)] private float _shardSpeed = 5f;

        [Header("Light")]
        [Tooltip("Peak intensity of the point light the blast throws on its surroundings; 0 for none.")]
        [SerializeField] [Min(0f)] private float _lightIntensity;
        [SerializeField] [Min(0f)] private float _lightRange = 4f;
        [SerializeField] [Min(0.05f)] private float _lightSeconds = 0.3f;

        [Header("Camera")]
        [SerializeField] [Range(0f, 1f)] private float _shake;
        [SerializeField] [Range(0f, 1f)] private float _shakePerRadius;

        public float FlashScale => _flashScale;
        public float FlashSeconds => _flashSeconds;
        public float FireShare => _fireShare;
        public bool Ring => _ring;
        public float RingScale => _ringScale;
        public float RingAlpha => _ringAlpha;
        public float SparkSpeed => _sparkSpeed;
        public float ShardSpeed => _shardSpeed;
        public float LightIntensity => _lightIntensity;
        public float LightRange => _lightRange;
        public float LightSeconds => _lightSeconds;

        public int SparksFor(float radius) => _sparks + Mathf.RoundToInt(_sparksPerRadius * radius);
        public int SmokeFor(float radius) => _smoke + Mathf.RoundToInt(_smokePerRadius * radius);
        public int ShardsFor(float radius) => _shards + Mathf.RoundToInt(_shardsPerRadius * radius);

        /// <summary>How hard the camera kicks for a blast of this radius; 0 leaves the camera still.</summary>
        public float ShakeFor(float radius) => _shake + _shakePerRadius * radius;
    }
}
