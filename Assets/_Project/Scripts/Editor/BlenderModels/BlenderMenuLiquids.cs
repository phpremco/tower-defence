using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The main menu's own water (2026-11-04): the level water refracts what lies under it, and the menu island's basins
    /// are painted dark, so its pools read near black. The menu gets copies of the level water materials made lighter,
    /// clearer of the basin (no refraction, fuller cover) and more reflective, in a liquid set of its own that only the
    /// diorama uses. The level water is left as it is.
    /// </summary>
    public static class BlenderMenuLiquids
    {
        private const string SetPath = BlenderArtBuilder.ArtDataFolder + "/Menu/Liquids_Menu.asset";
        private const string MaterialFolder = "Assets/_Project/Art/Materials/Liquids";
        private const string LevelWater = MaterialFolder + "/Liquid_Water.mat";
        private const string LevelWaterLow = MaterialFolder + "/Liquid_Water_Low.mat";
        private const string RefractKeyword = "_LIQUID_REFRACT";

        // The menu look: a lit lagoon blue, a cover that hides the dark basin, glassy and mirroring the sky at an angle.
        private static readonly Color Shallow = new Color(0.28f, 0.62f, 0.72f, 1f);
        private static readonly Color Deep = new Color(0.1f, 0.34f, 0.48f, 1f);
        private static readonly Color Caustics = new Color(1.2f, 1.6f, 1.9f, 1f);
        private static readonly Vector4 Cover = new Vector4(0.86f, 0.97f, 0f, 0f);
        private static readonly Vector4 Gloss = new Vector4(0.98f, 0.5f, 0f, 0f);
        private const float Reflection = 1.4f;
        // The island is lit dimly by the hive rig; the caustics glow a little so the pools stay water, not holes.
        private const float Glow = 2f;

        /// <summary>Writes the menu liquid set and its materials; null when the level water is missing.</summary>
        public static LiquidSet Build(LiquidSet levelSet)
        {
            var high = AssetDatabase.LoadAssetAtPath<Material>(LevelWater);
            if (high == null)
            {
                Debug.LogWarning("Blender menu: no level water material to make the menu water from.");
                return null;
            }

            Material menuHigh = Copy(high, "Liquid_Water_Menu");
            var low = AssetDatabase.LoadAssetAtPath<Material>(LevelWaterLow);
            Material menuLow = low != null ? Copy(low, "Liquid_Water_Menu_Low") : null;
            LiquidLook level = levelSet != null ? levelSet.For(TerrainKind.Water) : null;
            var set = BlenderArtBuilder.LoadOrCreate<LiquidSet>(SetPath);
            var so = new SerializedObject(set);
            SerializedProperty looks = so.FindProperty("_looks");
            looks.arraySize = 1;
            looks.GetArrayElementAtIndex(0).boxedValue = new LiquidLook(TerrainKind.Water, menuHigh, menuLow, level != null ? level.BankLight : Color.black);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            return set;
        }

        private static Material Copy(Material source, string name)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.CopyPropertiesFromMaterial(source);
                material.shaderKeywords = source.shaderKeywords;
            }

            material.SetColor("_ShallowColor", Shallow);
            material.SetColor("_DeepColor", Deep);
            material.SetColor("_CausticColor", Caustics);
            material.SetVector("_Alpha", Cover);
            material.SetVector("_Smoothness", Gloss);
            material.SetFloat("_Reflection", Reflection);
            material.SetFloat("_EmissionStrength", Glow);
            material.SetFloat("_Refract", 0f);
            material.DisableKeyword(RefractKeyword);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
