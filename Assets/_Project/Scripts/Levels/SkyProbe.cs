using Game.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Levels
{
    /// <summary>
    /// The theme's Blender sky (README round 26) as the reflection source, or without one a tiny cubemap painted from the theme: sky above, a band of light colour at the horizon, dark ground below.
    /// Set as the level's reflection source it gives shiny metal and water a gradient to mirror, where an empty
    /// scene would only reflect black and make every metal look like soot.
    /// </summary>
    public static class SkyProbe
    {
        private const int Size = 32;
        private const float HorizonWidth = 0.35f;
        private const float GroundDarkening = 0.25f;

        public static void Apply(VisualTheme theme)
        {
            Cubemap painted = theme.SkyCubemapFor(ModelQuality.IsLow);
            if (painted != null)
            {
                // The Blender sky stores its glow through a soft shoulder; the exposure from its report restores the range.
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = painted;
                RenderSettings.reflectionIntensity = theme.SkyExposure;
                return;
            }

            Cubemap cubemap = Build(theme.SkyColor, theme.LightColor * theme.LightIntensity, theme.FogColor * GroundDarkening);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cubemap;
            RenderSettings.reflectionIntensity = 1f;
        }

        private static Cubemap Build(Color sky, Color horizon, Color ground)
        {
            var cubemap = new Cubemap(Size, TextureFormat.RGBAHalf, false);
            var pixels = new Color[Size * Size];
            for (int face = 0; face < 6; face++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        Vector3 direction = Direction((CubemapFace)face, (x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f);
                        pixels[y * Size + x] = Shade(direction.y, sky, horizon, ground);
                    }
                }

                cubemap.SetPixels(pixels, (CubemapFace)face);
            }

            cubemap.Apply(false, true);
            return cubemap;
        }

        // Sky straight up, the horizon band around the middle and ground straight down, blended smoothly.
        private static Color Shade(float up, Color sky, Color horizon, Color ground)
        {
            float band = Mathf.Exp(-(up * up) / (HorizonWidth * HorizonWidth));
            Color vertical = up >= 0f ? Color.Lerp(horizon, sky, Mathf.Sqrt(up)) : Color.Lerp(horizon, ground, Mathf.Sqrt(-up));
            return Color.Lerp(vertical, horizon, band * 0.5f);
        }

        // Cubemap faces are stored with the usual flipped v axis.
        private static Vector3 Direction(CubemapFace face, float u, float v)
        {
            switch (face)
            {
                case CubemapFace.PositiveX:
                    return new Vector3(1f, -v, -u);
                case CubemapFace.NegativeX:
                    return new Vector3(-1f, -v, u);
                case CubemapFace.PositiveY:
                    return new Vector3(u, 1f, v);
                case CubemapFace.NegativeY:
                    return new Vector3(u, -1f, -v);
                case CubemapFace.PositiveZ:
                    return new Vector3(u, -v, 1f);
                default:
                    return new Vector3(-u, -v, -1f);
            }
        }
    }
}
