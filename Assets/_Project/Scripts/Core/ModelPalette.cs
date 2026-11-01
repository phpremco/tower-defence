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

        /// <summary>A fifth slot for markings (crosses, hazard stripes, chevrons); only models built with five submeshes use it.</summary>
        public const int Accent = 4;

        private const float GlowEmission = 2.2f;

        public static Material[] Create(Color glow)
        {
            return new[]
            {
                SurfaceMaterials.Create(Color.Lerp(new Color(0.3f, 0.34f, 0.43f), glow, 0.3f), 0f, 0.5f, 0.45f),
                SurfaceMaterials.Create(glow, GlowEmission, 0.6f),
                // The level builds a sky probe from its theme, so metal has something to reflect.
                SurfaceMaterials.Create(new Color(0.8f, 0.84f, 0.92f), 0f, 0.65f, 0.6f),
                SurfaceMaterials.Create(new Color(0.07f, 0.08f, 0.11f), 0f, 0.3f),
            };
        }
    }
}
