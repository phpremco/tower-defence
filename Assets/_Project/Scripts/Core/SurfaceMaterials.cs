using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Lit materials for procedural models: a colour, an optional glow, and how shiny and metallic it is. Units wear
    /// the game's own lit shader with a rim of light along their silhouette; when that shader is missing the pipeline
    /// default stands in, so nothing ever renders pink.
    /// </summary>
    public static class SurfaceMaterials
    {
        public const string UnitShaderName = "Game/UnitLit";

        /// <summary>The colony's rim: cool white-blue on towers, drones, the hero and the base.</summary>
        public static readonly Color ColonyRim = new Color(0.55f, 0.8f, 1f);

        /// <summary>The aliens' rim: warm, so friend and foe part by their edge light alone.</summary>
        public static readonly Color AlienRim = new Color(1f, 0.55f, 0.3f);

        private const float DefaultRimStrength = 0.85f;
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static Shader _unitShader;
        private static bool _searched;

        public static Material Create(Color color, float emission, float smoothness, float metallic = 0f)
        {
            Shader shader = UnitShader();
            // The stock lit shader is kept in the build by the shader keepers; the URP default material is null there.
            var material = shader != null ? new Material(shader) : PipelineMaterials.Lit();
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (shader != null)
            {
                material.SetColor(RimColorId, ColonyRim);
                material.SetFloat(RimStrengthId, DefaultRimStrength);
            }

            return material;
        }

        /// <summary>Gives a unit material its side's rim light; does nothing on a material without the unit shader.</summary>
        public static Material SetRim(Material material, Color rim, float strength = DefaultRimStrength)
        {
            if (material.HasProperty(RimColorId))
            {
                material.SetColor(RimColorId, rim);
                material.SetFloat(RimStrengthId, strength);
            }

            return material;
        }

        private static Shader UnitShader()
        {
            if (!_searched)
            {
                _searched = true;
                Shader found = Shader.Find(UnitShaderName);
                _unitShader = found != null && found.isSupported ? found : null;
            }

            return _unitShader;
        }
    }
}
