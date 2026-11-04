using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// How an imported (Blender) model sits in a dialogue portrait: the model, the moment of a clip it is posed in (a
    /// relaxed frame of its idle), the point of the model the picture centres on, how far it turns away from the camera
    /// and how much its glow is lowered or raised for the small picture. Pure look data; the portrait code reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Portrait Art Definition", fileName = "PortraitArt")]
    public sealed class PortraitArtDefinition : ScriptableObject
    {
        [SerializeField] private ModelArtDefinition _model;

        [Tooltip("The clip the bust is posed in; without one it keeps the bind pose.")]
        [SerializeField] private ModelClipRole _poseRole = ModelClipRole.Idle;
        [SerializeField] [Min(0f)] private float _poseSeconds;

        [Tooltip("The point of the model (model units, after its own turn) the picture centres on: the face, a core, a badge.")]
        [SerializeField] private Vector3 _centre;

        [Tooltip("Turn of the model about its up axis on top of the portrait's own three-quarter turn.")]
        [SerializeField] [Range(-180f, 180f)] private float _turn;

        [Tooltip("Factor on the model's size: below one shrinks a wide model so it fits the frame.")]
        [SerializeField] [Min(0.05f)] private float _zoom = 1f;

        [Tooltip("Factor on the emission of the portrait's own material copy: a star that burns white in a still drops to gold.")]
        [SerializeField] [Min(0f)] private float _glow = 1f;

        public ModelArtDefinition Model => _model;
        public ModelClipRole PoseRole => _poseRole;
        public float PoseSeconds => _poseSeconds;
        public Vector3 Centre => _centre;
        public float Turn => _turn;
        public float Zoom => _zoom;
        public float Glow => _glow;

        private void OnValidate()
        {
            if (_model == null)
            {
                Debug.LogWarning($"PortraitArtDefinition '{name}' has no model.", this);
            }
        }
    }
}
