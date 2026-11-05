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

        [Tooltip("How much of the pose this clip takes over when it plays on top of a cycle.")]
        [SerializeField] [Range(0.1f, 1f)] private float _weight = 1f;

        [Tooltip("Plays on the upper body only (from the art's upper-body bone down), over whatever the legs do.")]
        [SerializeField] private bool _upperBody;

        [Tooltip("For a travel clip authored with its pivot fixed: how far the body has moved by its last frame, in model units (x forward, y up). The owner moves by this much when the clip ends.")]
        [SerializeField] private Vector2 _endOffset;

        [Tooltip("An earlier moment inside the clip, before the main one (a burst of earth before the throw), or zero.")]
        [SerializeField] [Min(0f)] private float _cueSeconds;

        [Tooltip("Where the clip meets the world ahead of the pivot, in model units: the hole it digs or breaks out of, the bank or ledge it starts from.")]
        [SerializeField] private float _anchorForward;

        public ModelClipRole Role => _role;
        public AnimationClip Clip => _clip;

        /// <summary>The clip for the far LOD's rig, or the near one when the model has no far LOD.</summary>
        public AnimationClip FarClip => _farClip != null ? _farClip : _clip;
        public float DistancePerLoop => _distancePerLoop;
        public float EventSeconds => _eventSeconds;
        public string Trigger => _trigger;
        public string Socket => _socket;
        public float Weight => _weight;
        public bool UpperBody => _upperBody;

        /// <summary>Model units the body has travelled by the last frame of a fixed-pivot travel clip (x forward, y up).</summary>
        public Vector2 EndOffset => _endOffset;

        public float CueSeconds => _cueSeconds;

        /// <summary>Model units ahead of the pivot where the clip meets the world (a hole, a bank edge, a ledge face).</summary>
        public float AnchorForward => _anchorForward;
        public float Length => _clip != null ? _clip.length : 0f;
    }
}
