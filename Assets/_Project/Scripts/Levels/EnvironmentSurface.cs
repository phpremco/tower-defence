using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The material of the ground for a level whose theme has an environment set: the tiling layers of the set blended
    /// by a splat map from the grid (see <c>Game/EnvironmentGround</c>), over the baked painting as a broad tint. Low
    /// (Switch) wears the 512 maps and the shader's light variant.
    /// </summary>
    public static class EnvironmentSurface
    {
        private const int MaxPlazas = 8;

        /// <summary>The ground material, or null when the shader or the set's layers are missing (the caller keeps the painted ground).</summary>
        public static Material Create(LevelDefinition level, VisualTheme theme, EnvironmentSet set, TerrainField field, TerrainPainter.Result painted)
        {
            Shader shader = set != null ? set.GroundShader : null;
            if (shader == null || !set.HasGround)
            {
                return null;
            }

            bool low = ModelQuality.IsLow;
            var material = new Material(shader) { name = $"EnvironmentGround_{level.name}" };
            material.SetTexture("_PaintMap", painted.Albedo);
            material.SetTexture("_PaintGlow", painted.Emission);
            material.SetTexture("_SplatMap", EnvironmentSplat.Build(level, field, set));
            material.SetVector("_FieldRect", new Vector4(field.MinX, field.MinZ, 1f / field.Width, 1f / field.Depth));

            SetLayer(material, "_Ground", set.Ground, low);
            SetLayer(material, "_Alt", set.GroundAlt, low);
            SetLayer(material, "_Road", set.Road, low);
            SetLayer(material, "_Plaza", set.Plaza, low);
            SetLayer(material, "_Cliff", set.Cliff, low);
            SetLayer(material, "_Element", set.Element, low);
            material.SetTexture("_DetailNormal", set.DetailNormal);

            material.SetVector("_TileA", new Vector4(set.Ground.TileSize, set.GroundAlt.TileSize, set.Road.TileSize, set.Plaza.TileSize));
            material.SetVector("_TileB", new Vector4(set.Cliff.TileSize, set.Element.TileSize, set.DetailTileSize, 0f));
            material.SetVector("_GlowA", new Vector4(set.Ground.Glow, set.GroundAlt.Glow, set.Plaza.Glow, set.Cliff.Glow));
            material.SetVector("_GlowB", new Vector4(set.Element.Glow, set.PaintGlow, set.Sparkle, 0f));
            material.SetVector("_Sharpness", set.Sharpness);
            material.SetVector("_Blend", new Vector4(set.HeightBlend, set.CliffSlope.x, set.CliffSlope.y, set.DetailStrength));
            material.SetVector("_Paint", new Vector4(set.PaintInfluence, set.PaintBlur, set.Occlusion, set.Smoothness));
            // The painting's plain ground is the theme's ground colour lifted by the painter's patches (in linear space).
            material.SetColor("_PaintReference", theme.GroundColor * set.PaintReference);
            material.SetColor("_SparkleColour", set.SparkleColour);
            SetPlazas(material, level);
            if (low)
            {
                material.EnableKeyword(EnvironmentShaders.LowKeyword);
            }

            return material;
        }

        private static void SetLayer(Material material, string prefix, EnvironmentLayer layer, bool low)
        {
            material.SetTexture(prefix + "Albedo", layer.Albedo(low));
            material.SetTexture(prefix + "Normal", layer.Normal(low));
            material.SetTexture(prefix + "Mask", layer.Mask(low));
            Texture2D emission = layer.Emission(low);
            if (emission != null)
            {
                material.SetTexture(prefix + "Emission", emission);
            }
        }

        private static void SetPlazas(Material material, LevelDefinition level)
        {
            List<Vector2> centres = EnvironmentSplat.PlazaCentres(level);
            var array = new Vector4[MaxPlazas];
            int count = Mathf.Min(MaxPlazas, centres.Count);
            for (int i = 0; i < count; i++)
            {
                array[i] = new Vector4(centres[i].x, centres[i].y, 0f, 0f);
            }

            material.SetVectorArray("_PlazaCentres", array);
            material.SetFloat("_PlazaCount", count);
        }
    }
}
