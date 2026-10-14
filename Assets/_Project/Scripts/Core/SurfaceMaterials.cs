using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>Lit materials for procedural models: a colour, an optional glow, and how shiny and metallic it is.</summary>
    public static class SurfaceMaterials
    {
        public static Material Create(Color color, float emission, float smoothness, float metallic = 0f)
        {
            // The pipeline default material is always included in builds, unlike a shader found by name.
            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            return material;
        }
    }
}
