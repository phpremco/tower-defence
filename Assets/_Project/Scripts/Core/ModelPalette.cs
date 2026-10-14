using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The shared four-slot look of machines: coloured body, glowing parts, light metal and dark metal.
    /// Slot numbers match the submeshes that the procedural model factories write.
    /// </summary>
    public static class ModelPalette
    {
        public const int Body = 0;
        public const int Glow = 1;
        public const int Metal = 2;
        public const int Dark = 3;

        private const float GlowEmission = 2.2f;

        public static Material[] Create(Color glow)
        {
            return new[]
            {
                SurfaceMaterials.Create(Color.Lerp(new Color(0.16f, 0.18f, 0.24f), glow, 0.35f), 0f, 0.4f, 0.3f),
                SurfaceMaterials.Create(glow, GlowEmission, 0.6f),
                SurfaceMaterials.Create(new Color(0.78f, 0.82f, 0.9f), 0f, 0.55f, 0.6f),
                SurfaceMaterials.Create(new Color(0.07f, 0.08f, 0.11f), 0f, 0.3f),
            };
        }
    }
}
