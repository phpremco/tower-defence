using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The look of one imported (Blender) model: the model and its far LOD, the material for each quality profile, the
    /// clips by role with their speeds and moments, and how the model sits on its owner. A definition (an enemy, later a
    /// tower or a drone) points at one of these; without it the owner keeps its procedural model. Pure look data: logic
    /// never reads it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Model Art Definition", fileName = "ModelArt")]
    public sealed class ModelArtDefinition : ScriptableObject
    {
        [Header("Model")]
        [SerializeField] private GameObject _model;
        [Tooltip("The far LOD with its own lighter rig, or empty for one level of detail.")]
        [SerializeField] private GameObject _lod1;
        [SerializeField] private Material _material;
        [Tooltip("The Low (Switch) material: the 512 maps and no second detail normal.")]
        [SerializeField] private Material _lowMaterial;

        [Header("Placement")]
        [Tooltip("Factor on the owner's own scale (a boss model is built at its species' size times this).")]
        [SerializeField] [Min(0.01f)] private float _scale = 1f;
        [Tooltip("The model's origin is its body centre (flyers at flight height), not the ground under it.")]
        [SerializeField] private bool _bodyCentrePivot;
        [Tooltip("Height of the model's top in model units, for the health bar.")]
        [SerializeField] [Min(0.05f)] private float _height = 0.6f;
        [Tooltip("Screen height share below which the far LOD takes over.")]
        [SerializeField] [Range(0.001f, 1f)] private float _lodScreenHeight = 0.06f;

        [Header("Moments")]
        [Tooltip("Seconds into the Death clip over which the glow dies away (x start, y end); zero keeps it lit.")]
        [SerializeField] private Vector2 _deathFade;
        [Tooltip("The model sheds glowing embers as it moves (a burning or charged creature).")]
        [SerializeField] private bool _embers;
        [Tooltip("The model drips its blood colour when hurt.")]
        [SerializeField] private bool _bleeds = true;

        [Header("Clips")]
        [SerializeField] private ModelClip[] _clips = new ModelClip[0];

        public GameObject Model => _model;
        public GameObject Lod1 => _lod1;
        public Material Material => _material;
        public Material LowMaterial => _lowMaterial != null ? _lowMaterial : _material;
        public float Scale => _scale;
        public bool BodyCentrePivot => _bodyCentrePivot;
        public float Height => _height;
        public float LodScreenHeight => _lodScreenHeight;
        public Vector2 DeathFade => _deathFade;
        public bool FadesOnDeath => _deathFade.y > _deathFade.x;
        public bool Embers => _embers;
        public bool Bleeds => _bleeds;
        public ModelClip[] Clips => _clips;

        /// <summary>The material for the active quality profile.</summary>
        public Material MaterialFor(bool lowDetail) => lowDetail ? LowMaterial : _material;

        /// <summary>Index of the first clip with a role, or -1.</summary>
        public int IndexOf(ModelClipRole role)
        {
            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i] != null && _clips[i].Role == role && _clips[i].Clip != null)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Index of the power clip shown for a power kind, or -1.</summary>
        public int IndexOfTrigger(string trigger)
        {
            for (int i = 0; i < _clips.Length; i++)
            {
                ModelClip clip = _clips[i];
                if (clip != null && clip.Role == ModelClipRole.Power && clip.Clip != null && clip.Trigger == trigger)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnValidate()
        {
            if (_model == null)
            {
                Debug.LogWarning($"ModelArtDefinition '{name}' has no model.", this);
            }

            if (_material == null)
            {
                Debug.LogWarning($"ModelArtDefinition '{name}' has no material.", this);
            }

            if (_clips.Length > 0 && IndexOf(ModelClipRole.Move) < 0 && IndexOf(ModelClipRole.Idle) < 0)
            {
                Debug.LogWarning($"ModelArtDefinition '{name}' has clips but neither a Move nor an Idle cycle.", this);
            }
        }
    }
}
