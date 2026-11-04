using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One seamless tiling material set of the ground (road, ground, plaza, cliff, ...): its maps at full size and the
    /// 512 Low set, the world size one tile covers and how hot its glow burns. Mask: R occlusion, G smoothness,
    /// B height (for the height blend), A dust or sparkle.
    /// </summary>
    [Serializable]
    public sealed class EnvironmentLayer
    {
        [SerializeField] private Texture2D _albedo;
        [SerializeField] private Texture2D _normal;
        [SerializeField] private Texture2D _mask;
        [SerializeField] private Texture2D _emission;
        [SerializeField] private Texture2D _lowAlbedo;
        [SerializeField] private Texture2D _lowNormal;
        [SerializeField] private Texture2D _lowMask;
        [SerializeField] private Texture2D _lowEmission;
        [SerializeField] [Min(0.1f)] private float _tileSize = 2f;
        [SerializeField] [Min(0f)] private float _glow = 1f;

        public float TileSize => _tileSize;
        public float Glow => _glow;
        public bool IsValid => _albedo != null && _normal != null && _mask != null;

        public Texture2D Albedo(bool low) => low && _lowAlbedo != null ? _lowAlbedo : _albedo;
        public Texture2D Normal(bool low) => low && _lowNormal != null ? _lowNormal : _normal;
        public Texture2D Mask(bool low) => low && _lowMask != null ? _lowMask : _mask;
        public Texture2D Emission(bool low) => low && _lowEmission != null ? _lowEmission : _emission;
    }
}
