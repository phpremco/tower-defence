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
        [Tooltip("The material of the second slot (a boss's crystals or ice), or empty for a one-material model.")]
        [SerializeField] private Material _secondMaterial;
        [SerializeField] private Material _lowSecondMaterial;

        [Tooltip("Extra objects of the model with a material of their own (a boss's ice armour), or none.")]
        [SerializeField] private ModelPart[] _parts = new ModelPart[0];

        [Header("Placement")]
        [Tooltip("Factor on the owner's own scale (a boss model is built at its species' size times this).")]
        [SerializeField] [Min(0.01f)] private float _scale = 1f;
        [Tooltip("Turn about the up axis so the model's front faces the owner's forward (the Blender exports face -Z).")]
        [SerializeField] [Range(-180f, 180f)] private float _yaw;
        [Tooltip("The model's origin is its body centre (flyers at flight height), not the ground under it.")]
        [SerializeField] private bool _bodyCentrePivot;
        [Tooltip("Height of the model's top in model units, for the health bar.")]
        [SerializeField] [Min(0.05f)] private float _height = 0.6f;
        [Tooltip("Screen height share below which the far LOD takes over.")]
        [SerializeField] [Range(0.001f, 1f)] private float _lodScreenHeight = 0.06f;
        [Tooltip("The bone whose branch the upper-body clips move (a hero's spine), or empty when every clip moves the whole body.")]
        [SerializeField] private string _upperBodyBone = string.Empty;

        [Header("Moments")]
        [Tooltip("Seconds into the Death clip over which the glow dies away (x start, y end); zero keeps it lit.")]
        [SerializeField] private Vector2 _deathFade;
        [Tooltip("The model sheds glowing embers as it moves (a burning or charged creature).")]
        [SerializeField] private bool _embers;
        [Tooltip("The model drips its blood colour when hurt.")]
        [SerializeField] private bool _bleeds = true;
        [Tooltip("Bones that are shed in this order as the armour is chipped away (scaled to zero), or none.")]
        [SerializeField] private string[] _shedBones = new string[0];

        [Header("Glow looks")]
        [Tooltip("Named looks of the glow a view can swap to (a boss's phase maps), or none.")]
        [SerializeField] private ModelEmissionSet[] _emissionSets = new ModelEmissionSet[0];
        [Tooltip("The glow look the body takes at the death clip's moment (a white release), or empty to keep its own.")]
        [SerializeField] private string _deathEmissionSet = string.Empty;

        [Header("Clips")]
        [SerializeField] private ModelClip[] _clips = new ModelClip[0];

        public GameObject Model => _model;
        public GameObject Lod1 => _lod1;
        public Material Material => _material;
        public Material LowMaterial => _lowMaterial != null ? _lowMaterial : _material;
        public Material SecondMaterial => _secondMaterial;
        public Material LowSecondMaterial => _lowSecondMaterial != null ? _lowSecondMaterial : _secondMaterial;
        public bool HasSecondMaterial => _secondMaterial != null;
        public ModelPart[] Parts => _parts;
        public float Scale => _scale;
        public float Yaw => _yaw;
        public bool BodyCentrePivot => _bodyCentrePivot;
        public float Height => _height;
        public float LodScreenHeight => _lodScreenHeight;
        public string UpperBodyBone => _upperBodyBone;
        public Vector2 DeathFade => _deathFade;
        public bool FadesOnDeath => _deathFade.y > _deathFade.x;
        public bool Embers => _embers;
        public bool Bleeds => _bleeds;
        public string[] ShedBones => _shedBones;
        public ModelClip[] Clips => _clips;
        public ModelEmissionSet[] EmissionSets => _emissionSets ?? new ModelEmissionSet[0];
        public string DeathEmissionSet => _deathEmissionSet;

        /// <summary>The glow look of that name, or null.</summary>
        public ModelEmissionSet EmissionSet(string setName)
        {
            foreach (ModelEmissionSet set in EmissionSets)
            {
                if (set != null && set.Name == setName)
                {
                    return set;
                }
            }

            return null;
        }

        /// <summary>The material for the active quality profile.</summary>
        public Material MaterialFor(bool lowDetail) => lowDetail ? LowMaterial : _material;

        /// <summary>The second slot's material for the active quality profile, or null for a one-material model.</summary>
        public Material SecondMaterialFor(bool lowDetail) => lowDetail ? LowSecondMaterial : _secondMaterial;

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

        /// <summary>Index of the first clip with a role whose socket is that bone, or -1.</summary>
        public int IndexOf(ModelClipRole role, string socket)
        {
            for (int i = 0; i < _clips.Length; i++)
            {
                ModelClip clip = _clips[i];
                if (clip != null && clip.Role == role && clip.Clip != null && clip.Socket == socket)
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

            // A structure or a machine rests in its bind pose between its own clips (repair, build, land, open); a creature
            // needs a cycle to rest in.
            bool structure = IndexOf(ModelClipRole.Repair) >= 0 || IndexOf(ModelClipRole.Build) >= 0 || IndexOf(ModelClipRole.Deploy) >= 0
                || IndexOf(ModelClipRole.Land) >= 0 || IndexOf(ModelClipRole.Open) >= 0;
            if (_clips.Length > 0 && !structure && IndexOf(ModelClipRole.Move) < 0 && IndexOf(ModelClipRole.Idle) < 0)
            {
                Debug.LogWarning($"ModelArtDefinition '{name}' has clips but neither a Move nor an Idle cycle.", this);
            }
        }
    }
}
