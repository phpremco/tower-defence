using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The pool surfaces of a continent as data: one <c>Game/Liquid</c> material per kind and profile (the Blender liquid
    /// textures with the continent's palette, README round 24) and the colour of the light a pool throws on its banks.
    /// A <see cref="VisualTheme"/> points at its set; a theme without one, or a kind the set lacks, keeps the painted sheet.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Liquid Set", fileName = "LiquidSet")]
    public sealed class LiquidSet : ScriptableObject
    {
        [SerializeField] private LiquidLook[] _looks = new LiquidLook[0];

        [Tooltip("The corrupted pools of The Core; no terrain kind uses it yet.")]
        [SerializeField] private LiquidLook _corruption = new LiquidLook();

        public LiquidLook[] Looks => _looks;
        public LiquidLook Corruption => _corruption;

        /// <summary>The look of a kind, or null when the set has no material for it.</summary>
        public LiquidLook For(TerrainKind kind)
        {
            foreach (LiquidLook look in _looks)
            {
                if (look != null && look.Kind == kind && look.Material != null)
                {
                    return look;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            foreach (LiquidLook look in _looks)
            {
                if (look == null || look.Material == null)
                {
                    Debug.LogWarning($"Liquid set '{name}' has a look without a material: that kind keeps the painted sheet.", this);
                }
            }
        }
    }
}
