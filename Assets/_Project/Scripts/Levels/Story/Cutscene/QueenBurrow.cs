using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Round 63 (Remco: "the queen appears out of nowhere; it'd be cooler if she crawls out of a hole too"): the Queen's own
    /// burrow in the opening's attack. A bigger copy of the brood's burrows tears open a little behind the head of the group;
    /// once it has burst she climbs up out of it, nose high and hauling herself forward over the lip, in a cloud of dust and
    /// falling grit, then walks to the head of her brood. All times are on the swarm clock (seconds since the first tremor).
    /// </summary>
    [Serializable]
    public sealed class QueenBurrow
    {
        [Tooltip("Where her burrow opens: metres along the road from the entrance, and to its side (negative: left).")]
        [SerializeField] private Vector2 _spot = new Vector2(3.9f, 0.5f);
        [Tooltip("Swarm clock when her burrow starts to tremble (its burst follows the opening clip's own burst).")]
        [SerializeField] [Min(0f)] private float _openAt = 2.4f;
        [Tooltip("The burrow model's scale against the brood's burrows (she is far bigger than they are).")]
        [SerializeField] [Min(0.5f)] private float _scale = 1.7f;
        [Tooltip("Seconds after the burst before her head shows in the maw.")]
        [SerializeField] [Min(0f)] private float _climbDelay = 0.35f;
        [Tooltip("Seconds of the climb, from all but hidden to standing on the ground.")]
        [SerializeField] [Min(0.3f)] private float _climbSeconds = 2.1f;
        [Tooltip("How deep below the ground she starts, in metres (deep enough to hide her whole body).")]
        [SerializeField] [Min(0f)] private float _climbDepth = 2.6f;
        [Tooltip("How far she hauls herself forward over the lip while climbing, in metres.")]
        [SerializeField] [Min(0f)] private float _climbForward = 0.9f;
        [Tooltip("Her pitch, nose up, at the start of the climb; it eases to level as she comes over the lip.")]
        [SerializeField] [Range(0f, 80f)] private float _climbPitch = 42f;
        [Tooltip("How far her nose dips past level as she hauls her body over the lip, before she settles.")]
        [SerializeField] [Range(0f, 20f)] private float _overLipDip = 7f;
        [Tooltip("Dust and grit thrown off her while she climbs, per second at the height of the climb.")]
        [SerializeField] [Min(0f)] private float _climbDustRate = 26f;

        public static QueenBurrow Default { get; } = new QueenBurrow();

        public Vector2 Spot => _spot;
        public float OpenAt => _openAt;
        public float Scale => _scale;
        public float ClimbDelay => _climbDelay;
        public float ClimbSeconds => _climbSeconds;
        public float ClimbDepth => _climbDepth;
        public float ClimbForward => _climbForward;
        public float ClimbPitch => _climbPitch;
        public float OverLipDip => _overLipDip;
        public float ClimbDustRate => _climbDustRate;

        /// <summary>
        /// Her climb at share <paramref name="u"/> (0 down in the maw, 1 standing): how far below the ground she is, how far
        /// forward of the maw, and her pitch (positive: nose up). Eased so she starts and ends at rest; the nose comes level
        /// as the body clears the lip, dips a little as she hauls over it, then settles.
        /// </summary>
        public void Climb(float u, out float depth, out float forward, out float pitch)
        {
            u = Mathf.Clamp01(u);
            float rise = u * u * (3f - 2f * u);
            depth = _climbDepth * (1f - rise);
            float haul = Mathf.Clamp01((u - 0.35f) / 0.65f);
            forward = _climbForward * haul * haul * (3f - 2f * haul);
            float level = Mathf.Clamp01(u / 0.75f);
            float up = _climbPitch * (1f - level * level * (3f - 2f * level));
            float dip = Mathf.Sin(Mathf.Clamp01((u - 0.55f) / 0.45f) * Mathf.PI) * _overLipDip;
            pitch = up - dip;
        }
    }
}
