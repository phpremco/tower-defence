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
        [Tooltip("A short kick only: big blasts (heavy artillery, a boss dying), never everyday hits.")]
        [SerializeField] [Range(0f, 1f)] private float _shake;
        [SerializeField] [Range(0f, 1f)] private float _shakePerRadius;

        [Header("Painted layers (Blender)")]
        [Tooltip("Six-way fireball; it leaves white-hot and cools along the heat curve. Empty for energy hits.")]
        [SerializeField] private VfxSheet _fireball;
        [SerializeField] [Min(0)] private int _fireballs = 1;
        [Tooltip("Fireball size in metres: base plus per blast radius.")]
        [SerializeField] [Min(0f)] private float _fireballSize = 1.5f;
        [SerializeField] [Min(0f)] private float _fireballSizePerRadius = 1.2f;
        [Tooltip("Heat over a fireball's life (multiplies the baked temperature): above 1 is white-hot.")]
        [SerializeField] private AnimationCurve _heat = new AnimationCurve(new Keyframe(0f, 1.9f), new Keyframe(0.1f, 1.25f), new Keyframe(0.3f, 0.8f), new Keyframe(1f, 0.45f));
        [Tooltip("Additive flash flipbook at the hit, tinted by the weapon.")]
        [SerializeField] private VfxSheet _flash;
        [SerializeField] [Min(0f)] private float _flashSize = 0.8f;
        [SerializeField] [Min(0f)] private float _flashSizePerRadius = 1f;
        [Tooltip("Six-way smoke that rises after the fire.")]
        [SerializeField] private VfxSheet _smokeSheet;
        [SerializeField] [Min(0f)] private float _smokeSize = 1.2f;
        [SerializeField] [Min(0f)] private float _smokeDelay = 0.15f;
        [Tooltip("How much the smoke (and the hanging smoke) covers what is behind it, 0 clear to 1 solid. Kept low so a blast never hides towers or creatures.")]
        [SerializeField] [Range(0f, 1f)] private float _smokeOpacity = 0.45f;
        [Tooltip("The smoke's life against its painted sheet's, below 1 so it clears quickly.")]
        [SerializeField] [Range(0.2f, 1.5f)] private float _smokeLifeScale = 0.7f;
        [SerializeField] private VfxSheet _sparkSheet;
        [SerializeField] private VfxSheet _debrisSheet;
        [SerializeField] private VfxSheet _ringSheet;
        [Tooltip("Six-way dust thrown up by a ground hit.")]
        [SerializeField] private VfxSheet _dustSheet;
        [SerializeField] [Min(0f)] private float _dustSize;

        [Header("Distortion (high profile only)")]
        [SerializeField] private VfxSheet _shockDistortion;
        [SerializeField] [Min(0f)] private float _shockStrength = 0.03f;
        [SerializeField] private VfxSheet _heatHaze;
        [SerializeField] [Min(0f)] private float _heatHazeSeconds = 0.9f;

        [Header("Chain")]
        [Tooltip("Sub-bursts one after the other (a boss going down): 1 is a single blast.")]
        [SerializeField] [Min(1)] private int _chain = 1;
        [SerializeField] [Min(0f)] private float _chainInterval = 0.25f;
        [Tooltip("How far the later bursts land from the first, in metres.")]
        [SerializeField] [Min(0f)] private float _chainSpread = 1f;
        [Tooltip("Each later burst is this much bigger than the one before.")]
        [SerializeField] [Min(0f)] private float _chainGrowth = 0.15f;
        [Tooltip("Smoke left hanging over the spot after a chain.")]
        [SerializeField] private VfxSheet _lingerSmoke;

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

        public VfxSheet Fireball => _fireball;
        public int Fireballs => _fireballs;
        public AnimationCurve Heat => _heat;
        public VfxSheet Flash => _flash;
        public VfxSheet SmokeSheet => _smokeSheet;
        public float SmokeSize => _smokeSize;
        public float SmokeDelay => _smokeDelay;
        public float SmokeOpacity => _smokeOpacity;
        public float SmokeLifeScale => _smokeLifeScale;
        public VfxSheet SparkSheet => _sparkSheet;
        public VfxSheet DebrisSheet => _debrisSheet;
        public VfxSheet RingSheet => _ringSheet;
        public VfxSheet DustSheet => _dustSheet;
        public float DustSize => _dustSize;
        public VfxSheet ShockDistortion => _shockDistortion;
        public float ShockStrength => _shockStrength;
        public VfxSheet HeatHaze => _heatHaze;
        public float HeatHazeSeconds => _heatHazeSeconds;
        public int Chain => _chain;
        public float ChainInterval => _chainInterval;
        public float ChainSpread => _chainSpread;
        public float ChainGrowth => _chainGrowth;
        public VfxSheet LingerSmoke => _lingerSmoke;

        public float FireballSizeFor(float radius) => _fireballSize + _fireballSizePerRadius * radius;

        public float FlashSizeFor(float radius) => _flashSize + _flashSizePerRadius * radius;

        /// <summary>When the last burst of a chain goes off, in seconds after the first.</summary>
        public float ChainSeconds => (_chain - 1) * _chainInterval;

        private void OnValidate()
        {
            if (_heat == null || _heat.length == 0)
            {
                Debug.LogWarning($"{name}: the heat curve is empty, fireballs would not glow.", this);
            }
        }
    }
}
