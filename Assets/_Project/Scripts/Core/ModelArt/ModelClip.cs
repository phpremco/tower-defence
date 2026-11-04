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
        public float Length => _clip != null ? _clip.length : 0f;
    }
}
