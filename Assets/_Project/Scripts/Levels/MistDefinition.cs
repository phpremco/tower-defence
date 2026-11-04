using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The ground mist of a theme as data (ArtSource README round 24): sheets of the Blender mist textures at their own
    /// height, density, tile size and wind, tinted in the continent's mist colour. The Low (Switch) profile draws only the
    /// first <see cref="LowLayers"/> sheets and leaves the soft fade against geometry out.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Mist Definition", fileName = "Mist")]
    public sealed class MistDefinition : ScriptableObject
    {
        [Tooltip("The Game/Mist shader, referenced so a build keeps it.")]
        [SerializeField] private Shader _shader;
        [SerializeField] private MistLayer[] _layers = new MistLayer[0];
        [SerializeField] private Color _colour = new Color(0.55f, 0.6f, 0.75f);
        [Tooltip("Self light of the sheets, so mist shows over ground in shadow.")]
        [SerializeField] [Range(0f, 2f)] private float _glow = 0.6f;
        [Tooltip("The sheets reach this many cells past the field on every side.")]
        [SerializeField] [Min(0f)] private float _marginCells = 4f;
        [SerializeField] [Min(0f)] private float _softDistance = 0.3f;
        [SerializeField] [Min(0)] private int _lowLayers = 1;
        [Tooltip("Alpha of the sheets the Low profile keeps (README: 0.12).")]
        [SerializeField] [Range(0f, 1f)] private float _lowAlpha = 0.12f;

        public Shader Shader => _shader;
        public MistLayer[] Layers => _layers;
        public Color Colour => _colour;
        public float Glow => _glow;
        public float MarginCells => _marginCells;
        public float SoftDistance => _softDistance;
        public int LowLayers => _lowLayers;
        public float LowAlpha => _lowAlpha;

        private void OnValidate()
        {
            foreach (MistLayer layer in _layers)
            {
                if (layer == null || layer.Density == null)
                {
                    Debug.LogWarning($"Mist '{name}' has a layer without a density texture.", this);
                }
            }
        }
    }
}
