using UnityEngine;

namespace Game.Core
{
    /// <summary>One clip of an imported model with what the view needs to play it at the right speed and moment.</summary>
    [System.Serializable]
    public sealed class ModelClip
    {
        [SerializeField] private ModelClipRole _role;
        [SerializeField] private AnimationClip _clip;
        [Tooltip("The same clip keyed on the far LOD's lighter rig, or empty when there is no far LOD.")]
        [SerializeField] private AnimationClip _farClip;

        [Tooltip("Model units one loop of a travel cycle covers; zero plays the cycle at its own speed.")]
        [SerializeField] [Min(0f)] private float _distancePerLoop;

        [Tooltip("Seconds into the clip where its moment falls: a power's release, a strike's hit, a shield up.")]
        [SerializeField] [Min(0f)] private float _eventSeconds;

        [Tooltip("For a power: the name of the power kind it shows (Spawn, Pulse, Rally, Volley, Laser).")]
        [SerializeField] private string _trigger = string.Empty;

        [Tooltip("The bone the clip's moment comes from (a vent, an emitter), or empty for the body.")]
        [SerializeField] private string _socket = string.Empty;

        [Tooltip("Where on the socket bone the moment leaves, in the bone's own frame (the throat of a maw ahead of the head bone); zero is the bone itself.")]
        [SerializeField] private Vector3 _socketOffset;

        [Tooltip("How much of the pose this clip takes over when it plays on top of a cycle.")]
        [SerializeField] [Range(0.1f, 1f)] private float _weight = 1f;

        [Tooltip("Plays on the upper body only (from the art's upper-body bone down), over whatever the legs do.")]
        [SerializeField] private bool _upperBody;

        [Tooltip("For a travel clip authored with its pivot fixed: how far the body has moved by its last frame, in model units (x forward, y up). The owner moves by this much when the clip ends.")]
        [SerializeField] private Vector2 _endOffset;

        [Tooltip("A second moment inside the clip besides the main one (a burst of earth before the throw, a blade lighting up after the grab, a jet's thrust), or zero.")]
        [SerializeField] [Min(0f)] private float _cueSeconds;

        [Tooltip("Where the clip meets the world ahead of the pivot, in model units: the hole it digs or breaks out of, the bank or ledge it starts from.")]
        [SerializeField] private float _anchorForward;

        [Tooltip("Seconds into the clip of each heavy push (a gate crossing's weight frames): a blow, dust and a kick at each.")]
        [SerializeField] private float[] _momentSeconds = System.Array.Empty<float>();

        [Tooltip("How hard the body presses a structure under it, one value per frame at 30 fps (a gate crossing's gate_burden).")]
        [SerializeField] private float[] _burden = System.Array.Empty<float>();

        [Tooltip("Seconds into the clip where a foot plants, in order, baked from the foot bones (Tools/Generate/Hero Footfalls); empty when no foot lands.")]
        [SerializeField] private float[] _footfallSeconds = System.Array.Empty<float>();

        [Tooltip("For a power that lasts (a beam): the stretch of the clip, in seconds (x start, y end), that loops after its release while the power goes on, before the rest plays out; zero holds the release frame instead.")]
        [SerializeField] private Vector2 _sustain;

        [Tooltip("Seconds this clip blends in over what showed before (a cycle's cross-fade, a clip's fade-in on top of the cycle); zero takes the player's default (ClipMixer).")]
        [SerializeField] [Min(0f)] private float _blendSeconds;

        [Tooltip("The health bar height, in model units, that clears this clip where it lifts the body above the art's bar (a crest that flares in a power), per heading of the play camera round the model, evenly from its +Z (heading i at i * 360 / count degrees), measured by the art builder; empty when the art's bar already clears it.")]
        [SerializeField] private float[] _barTops = System.Array.Empty<float>();

        private const float FramesPerSecond = 30f;

        public ModelClipRole Role => _role;
        public AnimationClip Clip => _clip;

        /// <summary>The clip for the far LOD's rig, or the near one when the model has no far LOD.</summary>
        public AnimationClip FarClip => _farClip != null ? _farClip : _clip;
        public float DistancePerLoop => _distancePerLoop;
        public float EventSeconds => _eventSeconds;

        /// <summary>Seconds this clip blends in over, or zero for the player's default.</summary>
        public float BlendSeconds => _blendSeconds;
        public string Trigger => _trigger;
        public string Socket => _socket;

        /// <summary>Where on the socket bone the moment leaves, in the bone's own frame; zero is the bone itself.</summary>
        public Vector3 SocketOffset => _socketOffset;
        public float Weight => _weight;
        public Vector2 Sustain => _sustain;

        /// <summary>True when the clip has a stretch to loop while its power lasts.</summary>
        public bool Sustains => _sustain.y > _sustain.x;
        public bool UpperBody => _upperBody;

        /// <summary>Model units the body has travelled by the last frame of a fixed-pivot travel clip (x forward, y up).</summary>
        public Vector2 EndOffset => _endOffset;

        public float CueSeconds => _cueSeconds;

        /// <summary>Model units ahead of the pivot where the clip meets the world (a hole, a bank edge, a ledge face).</summary>
        public float AnchorForward => _anchorForward;
        public float Length => _clip != null ? _clip.length : 0f;

        /// <summary>True when this clip lifts the body above the art's bar from some heading (see <see cref="BarTopAt"/>).</summary>
        public bool LiftsBar => _barTops != null && _barTops.Length > 0;

        /// <summary>
        /// The bar height (model units) that clears this clip with the play camera looking along a heading in the model's frame
        /// (degrees, 0 along its +Z, 90 along +X), between the measured headings; zero when the art's own bar does.
        /// </summary>
        public float BarTopAt(float headingDegrees)
        {
            if (!LiftsBar)
            {
                return 0f;
            }

            int count = _barTops.Length;
            float steps = Mathf.Repeat(headingDegrees, 360f) / (360f / count);
            int below = Mathf.FloorToInt(steps) % count;
            return Mathf.Lerp(_barTops[below], _barTops[(below + 1) % count], steps - Mathf.Floor(steps));
        }

        /// <summary>Seconds into the clip of each heavy push, in order (empty for most clips).</summary>
        public float[] MomentSeconds => _momentSeconds ?? System.Array.Empty<float>();

        /// <summary>Seconds into the clip where a foot plants (baked from the foot bones), in order; empty when no foot lands.</summary>
        public float[] FootfallSeconds => _footfallSeconds ?? System.Array.Empty<float>();

        /// <summary>True when the clip says frame by frame how hard it presses the structure under it.</summary>
        public bool HasBurden => _burden != null && _burden.Length > 0;

        /// <summary>The structure burden at a time into the clip (frames interpolated), or zero without a curve.</summary>
        public float BurdenAt(float seconds)
        {
            if (!HasBurden)
            {
                return 0f;
            }

            float frame = Mathf.Clamp(seconds * FramesPerSecond, 0f, _burden.Length - 1);
            int below = Mathf.FloorToInt(frame);
            int above = Mathf.Min(below + 1, _burden.Length - 1);
            return Mathf.Lerp(_burden[below], _burden[above], frame - below);
        }
    }
}
