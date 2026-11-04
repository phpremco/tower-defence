using Game.Levels;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Brings the S-69-CRYS maps painted in Blender into the game: syncs them, fills the
    /// <see cref="PlanetTextureSet"/> (only its maps, so look tweaks in the asset survive) and hands it to the campaign,
    /// whose map globe and cutscene planets then wear it. Re-running updates the same asset in place.
    /// </summary>
    public static class BlenderPlanetBuilder
    {
        public const string SetPath = BlenderArtBuilder.ArtDataFolder + "/Planet/Planet_S69.asset";
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

        [MenuItem("Tools/Generate/Blender Planet (sync, S-69-CRYS maps)")]
        public static void BuildMenu()
        {
            PlanetTextureSet set = SyncAndBuild();
            bool applied = Apply(set);
            AssetDatabase.SaveAssets();
            Debug.Log($"Blender planet: set {(set != null && set.IsComplete ? "complete" : "missing maps")}, campaign {(applied ? "wears it" : "unchanged")}.");
        }

        /// <summary>Syncs the export and fills the set; null when the maps are not in Assets.</summary>
        public static PlanetTextureSet SyncAndBuild()
        {
            string asset = BlenderPlanetCatalog.Crys;
            BlenderSync.SyncTextures(asset, BlenderPlanetCatalog.Maps, BlenderPlanetCatalog.LowOnlyMaps);
            string folder = BlenderArtBuilder.AssetFolder(asset);
            string low = $"{folder}/{BlenderSync.LowFolder}";
            if (Load(folder, asset, BlenderPlanetCatalog.Albedo) == null)
            {
                Debug.LogWarning($"Blender planet: no maps for '{asset}' at {folder}.");
                return null;
            }

            var set = BlenderArtBuilder.LoadOrCreate<PlanetTextureSet>(SetPath);
            var so = new SerializedObject(set);
            WriteMaps(so.FindProperty("_high"), folder, low, asset);
            WriteMaps(so.FindProperty("_low"), low, low, asset);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(set);
            return set;
        }

        /// <summary>Hands the set to the campaign; false when there is none to hand.</summary>
        public static bool Apply(PlanetTextureSet set)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            if (catalog == null || set == null)
            {
                return false;
            }

            var so = new SerializedObject(catalog);
            so.FindProperty("_planetTextures").objectReferenceValue = set;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return true;
        }

        // The region ids always come from the Low folder (see BlenderPlanetCatalog).
        private static void WriteMaps(SerializedProperty maps, string folder, string lowFolder, string asset)
        {
            maps.FindPropertyRelative("Albedo").objectReferenceValue = Load(folder, asset, BlenderPlanetCatalog.Albedo);
            maps.FindPropertyRelative("Normal").objectReferenceValue = Load(folder, asset, BlenderPlanetCatalog.Normal);
            maps.FindPropertyRelative("Mask").objectReferenceValue = Load(folder, asset, BlenderPlanetCatalog.Mask);
            maps.FindPropertyRelative("Regions").objectReferenceValue = Load(lowFolder, asset, BlenderPlanetCatalog.Regions);
            maps.FindPropertyRelative("Emission").objectReferenceValue = Load(folder, asset, BlenderPlanetCatalog.Emission);
            maps.FindPropertyRelative("Clouds").objectReferenceValue = Load(folder, asset, BlenderPlanetCatalog.Clouds);
        }

        private static Texture2D Load(string folder, string asset, string map) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{asset}_{map}.png");
    }
}
