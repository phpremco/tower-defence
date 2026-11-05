using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// An extra object inside an imported model that wears a material of its own and can be shown or hidden on its own
    /// (the Frost Warden's ice armour, README round 28 "extra_objects"). Matched to the renderers by name prefix, so the
    /// near and the far LOD's copies (<c>_LOD1</c>) both wear it.
    /// </summary>
    [System.Serializable]
    public sealed class ModelPart
    {
        [Tooltip("Name (prefix) of the part's objects in the model, for example FrostWarden_IceArmour.")]
        [SerializeField] private string _object = string.Empty;
        [SerializeField] private Material _material;
        [SerializeField] private Material _lowMaterial;

        public ModelPart()
        {
        }

        public ModelPart(string objectName, Material material, Material lowMaterial)
        {
            _object = objectName;
            _material = material;
            _lowMaterial = lowMaterial;
        }

        public string Object => _object;
        public Material Material => _material;

        public Material MaterialFor(bool lowDetail) => lowDetail && _lowMaterial != null ? _lowMaterial : _material;

        /// <summary>True for a renderer of this part (its name starts with the part's object name).</summary>
        public bool Owns(string rendererName) => !string.IsNullOrEmpty(_object) && rendererName.StartsWith(_object, System.StringComparison.Ordinal);
    }
}
