using Game.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.UI
{
    /// <summary>
    /// Small props for the cutscene sets taken from the level's Blender environment decor (rocks, crystals): one mesh
    /// variant under a root, sized to a given width, standing on the root by its foot or hanging from it by its top.
    /// Without the art the root stays empty.
    /// </summary>
    public static class CutsceneProps
    {
        public static Transform Decor(Transform parent, string name, EnvironmentSet environment, PropKind kind, int variant, float size, bool low, bool hang)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localRotation = Quaternion.Euler(CutsceneMath.Hash(variant, 71) * 30f, CutsceneMath.Hash(variant, 73) * 360f, 0f);
            EnvironmentDecor decor = environment != null ? environment.Decor(kind) : null;
            EnvironmentMesh mesh = decor != null && decor.Variants.Length > 0 ? decor.Variants[variant % decor.Variants.Length] : null;
            if (mesh == null || !mesh.IsValid)
            {
                return root;
            }

            var model = new GameObject("Mesh").transform;
            model.SetParent(root, false);
            Matrix4x4 node = mesh.Node;
            model.localPosition = node.GetPosition();
            model.localRotation = node.rotation;
            model.localScale = node.lossyScale;
            model.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh.Mesh;
            var renderer = model.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mesh.MaterialFor(low);
            renderer.shadowCastingMode = mesh.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;

            Bounds bounds = renderer.bounds;
            float largest = Mathf.Max(0.01f, Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)));
            root.localScale = Vector3.one * (size / largest * root.localScale.x);
            Bounds placed = renderer.bounds;
            model.position += Vector3.down * ((hang ? placed.max.y : placed.min.y) - root.position.y);
            return root;
        }
    }
}
