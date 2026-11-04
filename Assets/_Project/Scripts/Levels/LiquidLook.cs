using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One kind's surface in a <see cref="LiquidSet"/>.</summary>
    [Serializable]
    public sealed class LiquidLook
    {
        [SerializeField] private TerrainKind _kind = TerrainKind.Water;
        [SerializeField] private Material _material;
        [Tooltip("The Switch variant: one layer, static caustics, no refraction, the half-size textures.")]
        [SerializeField] private Material _lowMaterial;
        [Tooltip("The light a pool of this kind throws on its banks (lava); black for none.")]
        [SerializeField] private Color _bankLight = Color.black;

        public LiquidLook()
        {
        }

        public LiquidLook(TerrainKind kind, Material material, Material lowMaterial, Color bankLight)
        {
            _kind = kind;
            _material = material;
            _lowMaterial = lowMaterial;
            _bankLight = bankLight;
        }

        public TerrainKind Kind => _kind;
        public Material Material => _material;
        public Color BankLight => _bankLight;

        public Material MaterialFor(bool low) => low && _lowMaterial != null ? _lowMaterial : _material;
    }
}
