using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The imported meshes that stand in for one procedural decoration kind, one picked per spot by the spot's hash. The
    /// scale brings the import to the size the procedural kind had, so the scatter's scales and spacing still hold.
    /// </summary>
    [Serializable]
    public sealed class EnvironmentDecor
    {
        [SerializeField] private PropKind _kind;
        [SerializeField] private EnvironmentMesh[] _variants = new EnvironmentMesh[0];
        [SerializeField] [Min(0.01f)] private float _scale = 1f;
        [Tooltip("The colour of the light pool and the point light under a glowing formation.")]
        [SerializeField] private Color _glow = Color.white;

        public EnvironmentDecor(PropKind kind, EnvironmentMesh[] variants, float scale, Color glow)
        {
            _kind = kind;
            _variants = variants;
            _scale = scale;
            _glow = glow;
        }

        public PropKind Kind => _kind;
        public EnvironmentMesh[] Variants => _variants;
        public float Scale => _scale;
        public Color Glow => _glow;
    }
}
