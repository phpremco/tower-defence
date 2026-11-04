using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The stage the upgrade screen's models turn on, built in Blender (ArtSource README round 27): the pedestal with its
    /// turning top ring, the additive halo laid round its foot and the research-bay backdrop. Written by the generator;
    /// <see cref="ModelPreview"/> only places, scales and turns it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Display Stage Art", fileName = "DisplayStageArt")]
    public sealed class DisplayStageArt : ScriptableObject
    {
        [Header("Pedestal (pivot at the centre of the top face, top radius 1)")]
        [SerializeField] private Mesh _pedestal;
        [SerializeField] private Matrix4x4 _pedestalNode = Matrix4x4.identity;
        [SerializeField] private Mesh _pedestalRing;
        [SerializeField] private Matrix4x4 _pedestalRingNode = Matrix4x4.identity;
        [SerializeField] private Material _pedestalMaterial;
        [SerializeField] private Material _pedestalLowMaterial;
        [Tooltip("The top ring turns against the turntable, degrees per second.")]
        [SerializeField] private float _ringDegreesPerSecond = -6f;
        [Tooltip("Height of the pedestal over its top radius: the model stands this high above the foot.")]
        [SerializeField] private float _heightShare = 0.2f;

        [Header("Halo round the foot")]
        [SerializeField] private Mesh _halo;
        [SerializeField] private Matrix4x4 _haloNode = Matrix4x4.identity;
        [SerializeField] private Texture2D _haloTexture;
        [SerializeField] private Texture2D _haloLowTexture;
        [ColorUsage(false, true)]
        [SerializeField] private Color _haloColour = new Color(0.6f, 0.88f, 1.1f);
        [SerializeField] private float _haloDegreesPerSecond = 3f;

        [Header("Backdrop (2:1)")]
        [SerializeField] private Texture2D _backdrop;
        [SerializeField] private Texture2D _backdropLow;

        public Mesh Pedestal => _pedestal;
        public Matrix4x4 PedestalNode => _pedestalNode;
        public Mesh PedestalRing => _pedestalRing;
        public Matrix4x4 PedestalRingNode => _pedestalRingNode;
        public Material PedestalMaterial(bool low) => low && _pedestalLowMaterial != null ? _pedestalLowMaterial : _pedestalMaterial;
        public float RingDegreesPerSecond => _ringDegreesPerSecond;
        public float HeightShare => _heightShare;
        public Mesh Halo => _halo;
        public Matrix4x4 HaloNode => _haloNode;
        public Texture2D HaloTexture(bool low) => low && _haloLowTexture != null ? _haloLowTexture : _haloTexture;
        public Color HaloColour => _haloColour;
        public float HaloDegreesPerSecond => _haloDegreesPerSecond;
        public Texture2D Backdrop(bool low) => low && _backdropLow != null ? _backdropLow : _backdrop;

        /// <summary>True when every piece is there; without them the preview shows the model on the plain background.</summary>
        public bool IsComplete => _pedestal != null && _pedestalMaterial != null && _halo != null && _haloTexture != null && _backdrop != null;

        private void OnValidate()
        {
            if (!IsComplete)
            {
                Debug.LogWarning($"DisplayStageArt '{name}' misses a piece; run Tools/Generate/Blender Display Stage.", this);
            }
        }
    }
}
