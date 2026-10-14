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
