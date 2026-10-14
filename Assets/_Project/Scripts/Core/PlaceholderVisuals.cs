using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>
    /// Builds placeholder shapes in code. View components use this until real models replace them,
    /// so logic never needs to know what something looks like. Emission makes shapes glow under bloom.
    /// </summary>
    public static class PlaceholderVisuals
    {
        private const string BaseColorProperty = "_BaseColor";
        private const string EmissionColorProperty = "_EmissionColor";
        private const string EmissionKeyword = "_EMISSION";

        private static readonly Dictionary<(Color, float), Material> Materials = new Dictionary<(Color, float), Material>();

        public static GameObject CreatePrimitive(
            PrimitiveType type,
            Color color,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            float emission = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color, emission);
            return go;
        }

        private static readonly Dictionary<(Color, float), Material> TransparentMaterials = new Dictionary<(Color, float), Material>();

        /// <summary>A see-through glowing material, for overlays such as flight routes that should not dominate the field.</summary>
        public static Material TransparentMaterialFor(Color color, float alpha, float emission)
        {
            var key = (color, alpha + emission * 1000f);
            if (TransparentMaterials.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            var material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor(BaseColorProperty, new Color(color.r, color.g, color.b, alpha));
            if (emission > 0f)
            {
                material.EnableKeyword(EmissionKeyword);
                material.SetColor(EmissionColorProperty, color * emission);
            }

            TransparentMaterials[key] = material;
            return material;
        }

        public static Material MaterialFor(Color color, float emission)
        {
            if (Materials.TryGetValue((color, emission), out Material material) && material != null)
            {
                return material;
            }

            // The pipeline default material is always included in builds, unlike a shader found by name.
            material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetColor(BaseColorProperty, color);
            if (emission > 0f)
            {
                material.EnableKeyword(EmissionKeyword);
                material.SetColor(EmissionColorProperty, color * emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            Materials[(color, emission)] = material;
            return material;
        }
    }
}

