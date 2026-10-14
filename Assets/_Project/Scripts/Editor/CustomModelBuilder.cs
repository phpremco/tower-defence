using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    /// <summary>
    /// Turns the .glb files in Art/Models/Custom into a mesh asset, coloured materials and a prefab, so a plain
    /// grey AI model gets real colours without any editor work. Re-running updates the same assets.
    /// How a model is split into coloured parts is described in <see cref="ModelRecipe"/>.
    /// </summary>
    public static class CustomModelBuilder
    {
        private const string SourceFolder = "Assets/_Project/Art/Models/Custom";
        public const string OutputFolder = "Assets/_Project/Art/Models/Custom/Built";
        private const string MaterialFolder = OutputFolder + "/Materials";

        [MenuItem("Tools/Generate/Build Custom Models")]
        public static void BuildAll()
        {
            EnsureFolder(OutputFolder);
            EnsureFolder(MaterialFolder);

            foreach (string file in Directory.GetFiles(SourceFolder, "*.glb"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                Build(name, file.Replace('\\', '/'));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Build(string name, string path)
        {
            ModelRecipe recipe = ModelRecipe.For(name);
            Mesh mesh = GlbMeshReader.Read(path, recipe.Scale);
            if (mesh == null)
            {
                return;
            }

            recipe.Orient(mesh);
            recipe.Apply(mesh);
            var materials = new Material[recipe.Parts.Count];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = LoadOrCreateMaterial($"{name}_{recipe.Parts[i].Name}", recipe.Parts[i]);
            }

            string meshPath = $"{OutputFolder}/{name}.mesh.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }
            else
            {
                EditorUtility.CopySerialized(mesh, existing);
                mesh = existing;
            }

            var root = new GameObject(name);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterials = materials;
            PrefabUtility.SaveAsPrefabAsset(root, $"{OutputFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
            Debug.Log($"Built custom model '{name}' with {materials.Length} coloured parts.");
        }

        private static Material LoadOrCreateMaterial(string name, ModelRecipe.Part part)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Material template = GraphicsSettingsDefault();
                material = new Material(template);
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", part.Color);
            material.SetFloat("_Metallic", part.Metallic);
            material.SetFloat("_Smoothness", part.Smoothness);
            if (part.Emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", part.Color * part.Emission);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        // Shader.Find is unreliable here; the pipeline's default material is a valid URP Lit material to copy from.
        private static Material GraphicsSettingsDefault() => GraphicsSettings.currentRenderPipeline.defaultMaterial;

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
