using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Places imported models in the world. Models come with arbitrary pivots, so each one is re-centred on x and z
    /// and sat on y = 0, then scaled, turned and tinted. Tinting multiplies the model's own colours, so details stay visible.
    /// </summary>
    public static class ModelFit
    {
        private const string BaseColorProperty = "_BaseColor";
        private const string EmissionColorProperty = "_EmissionColor";
        private const string EmissionKeyword = "_EMISSION";

        private static readonly Dictionary<(Material, Color, float), Material> Cache = new Dictionary<(Material, Color, float), Material>();

        public static GameObject Place(
            GameObject prefab,
            Transform parent,
            Vector3 localPosition,
            float yawDegrees,
            float scale,
            Color tint,
            float emission = 0f)
        {
            var wrapper = new GameObject(prefab.name);
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.localPosition = localPosition;
            wrapper.transform.localRotation = Quaternion.identity;
            wrapper.transform.localScale = Vector3.one;

            GameObject instance = Object.Instantiate(prefab, wrapper.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            RemoveColliders(instance);

            // Measure in the wrapper's own space while it is still unrotated and unscaled, so the result holds
            // wherever the parent is and however it is turned.
            Bounds bounds = BoundsOf(instance);
            Vector3 center = wrapper.transform.InverseTransformPoint(bounds.center);
            Vector3 bottom = wrapper.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            instance.transform.localPosition = new Vector3(-center.x, -bottom.y, -center.z);

            wrapper.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            wrapper.transform.localScale = Vector3.one * scale;
            Tint(instance, tint, emission);
            return wrapper;
        }

        /// <summary>Replaces the materials of every renderer by tinted copies. White tint and zero emission keeps the originals.</summary>
        public static void Tint(GameObject root, Color tint, float emission)
        {
            if (tint == Color.white && emission <= 0f)
            {
                return;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                Material[] source = renderer.sharedMaterials;
                var result = new Material[source.Length];
                for (int i = 0; i < source.Length; i++)
                {
                    result[i] = TintedCopy(source[i], tint, emission);
                }

                renderer.sharedMaterials = result;
            }
        }

        private static Material TintedCopy(Material source, Color tint, float emission)
        {
            if (source == null)
            {
                return null;
            }

            (Material, Color, float) key = (source, tint, emission);
            if (Cache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            var copy = new Material(source);
            Color color = source.HasProperty(BaseColorProperty) ? source.GetColor(BaseColorProperty) : Color.white;
            Color tinted = new Color(color.r * tint.r, color.g * tint.g, color.b * tint.b, color.a);
            copy.SetColor(BaseColorProperty, tinted);
            if (emission > 0f)
            {
                copy.EnableKeyword(EmissionKeyword);
                copy.SetColor(EmissionColorProperty, tinted * emission);
                copy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            Cache[key] = copy;
            return copy;
        }

        private static Bounds BoundsOf(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void RemoveColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(collider);
            }
        }
    }
}

