using Game.Levels;
using UnityEditor;

namespace Game.Editor
{
    /// <summary>
    /// Generator part for the planet's looks from space. The maps themselves come from Blender (step "Planet art");
    /// the globe used to be baked here in code, and this step removes those old pictures.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string PlanetArtFolder = "Assets/_Project/Art/Planet";
        private static readonly string[] OldGlobeMaps =
        {
            "Globe_albedo", "Globe_normal", "Globe_gloss", "Globe_lights", "Globe_clouds_0", "Globe_clouds_1",
        };

        private static void ApplyPlanetStyles(CampaignCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            so.FindProperty("_planetStyle").boxedValue = PlanetStyle.Crys();
            so.FindProperty("_spaceStyle").boxedValue = new SpaceStyle();
            so.ApplyModifiedPropertiesWithoutUndo();
            DeleteOldGlobeMaps();
        }

        private static void DeleteOldGlobeMaps()
        {
            foreach (string name in OldGlobeMaps)
            {
                string path = $"{PlanetArtFolder}/{name}.png";
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            if (AssetDatabase.IsValidFolder(PlanetArtFolder) && AssetDatabase.FindAssets(string.Empty, new[] { PlanetArtFolder }).Length == 0)
            {
                AssetDatabase.DeleteAsset(PlanetArtFolder);
            }
        }
    }
}
