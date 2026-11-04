using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One sheet of a <see cref="MistDefinition"/>.</summary>
    [Serializable]
    public sealed class MistLayer
    {
        [SerializeField] private Texture2D _density;
        [Tooltip("The Low profile's half-size copy, or empty to use the full one.")]
        [SerializeField] private Texture2D _lowDensity;
        [SerializeField] private float _height = 0.12f;
        [SerializeField] [Range(0f, 1f)] private float _alpha = 0.16f;
        [Tooltip("World size of one tile of the texture.")]
        [SerializeField] [Min(0.1f)] private float _tile = 7f;
        [Tooltip("Drift in tiles per second.")]
        [SerializeField] private Vector2 _scroll = new Vector2(0.012f, 0.003f);

        public MistLayer()
        {
        }

        public MistLayer(Texture2D density, Texture2D lowDensity, float height, float alpha, float tile, Vector2 scroll)
        {
            _density = density;
            _lowDensity = lowDensity;
            _height = height;
            _alpha = alpha;
            _tile = tile;
            _scroll = scroll;
        }

        public Texture2D Density => _density;
        public float Height => _height;
        public float Alpha => _alpha;
        public float Tile => _tile;
        public Vector2 Scroll => _scroll;

        public Texture2D DensityFor(bool low) => low && _lowDensity != null ? _lowDensity : _density;
    }
}
