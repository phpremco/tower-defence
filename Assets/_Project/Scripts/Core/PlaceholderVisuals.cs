using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Core
{
    /// <summary>
    /// Builds placeholder shapes in code. View components use this until real models replace them,
    /// so logic never needs to know what something looks like.
    /// </summary>
    public static class PlaceholderVisuals
    {
        private const string BaseColorProperty = "_BaseColor";

        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        public static GameObject CreatePrimitive(PrimitiveType type, Color color, Transform parent, Vector3 localPosition, Vector3 localScale)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            return go;
        }

        public static void Recolor(GameObject go, Color color)
        {
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
        }

        private static Material MaterialFor(Color color)
        {
            if (Materials.TryGetValue(color, out Material material) && material != null)
            {
                return material;
            }

            // The pipeline default material is always included in builds, unlike a shader found by name.
            material = new Material(GraphicsSettings.currentRenderPipeline.defaultMaterial);
            material.SetColor(BaseColorProperty, color);
            Materials[color] = material;
            return material;
        }
    }
}
