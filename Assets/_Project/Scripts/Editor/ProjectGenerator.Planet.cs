using System.IO;
using Game.Levels;
using Game.UI;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Generator part for the planet of the map screen: paints its maps once and saves them as textures.</summary>
    public static partial class ProjectGenerator
    {
        private const string PlanetArtFolder = "Assets/_Project/Art/Planet";
        private const int GlobeMapWidth = 2048;
        private const int CloudMapWidth = 1024;
        private static readonly float[] CloudSeeds = { 0f, 200f };

        // Painting the planet takes seconds, so it happens here and the map screen only loads the pictures.
        private static void BakeGlobe(CampaignCatalog catalog)
        {
            GlobePainter.Maps maps = GlobePainter.Paint(GlobeMapWidth, GlobeMapWidth / 2, GlobePainter.RegionsOf(catalog));
            var so = new SerializedObject(catalog);
            so.FindProperty("_globeAlbedo").objectReferenceValue = SavePlanetTexture(maps.Albedo, "Globe_albedo", TextureImporterType.Default, true);
            so.FindProperty("_globeNormal").objectReferenceValue = SavePlanetTexture(maps.Normal, "Globe_normal", TextureImporterType.NormalMap, false);
            so.FindProperty("_globeGloss").objectReferenceValue = SavePlanetTexture(maps.Gloss, "Globe_gloss", TextureImporterType.Default, false);
            so.FindProperty("_globeLights").objectReferenceValue = SavePlanetTexture(maps.Emission, "Globe_lights", TextureImporterType.Default, true);
            so.FindProperty("_planetStyle").boxedValue = PlanetStyle.Crys();
            so.FindProperty("_spaceStyle").boxedValue = new SpaceStyle();
            SerializedProperty clouds = so.FindProperty("_globeClouds");
            clouds.arraySize = CloudSeeds.Length;
            for (int i = 0; i < CloudSeeds.Length; i++)
            {
                Texture2D deck = GlobePainter.PaintClouds(CloudMapWidth, CloudMapWidth / 2, CloudSeeds[i]);
                clouds.GetArrayElementAtIndex(i).objectReferenceValue = SavePlanetTexture(deck, $"Globe_clouds_{i}", TextureImporterType.Default, true);
                Object.DestroyImmediate(deck);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(maps.Albedo);
            Object.DestroyImmediate(maps.Normal);
            Object.DestroyImmediate(maps.Gloss);
            Object.DestroyImmediate(maps.Emission);
        }

        private static Texture2D SavePlanetTexture(Texture2D texture, string name, TextureImporterType type, bool srgb)
        {
            EnsureFolder(PlanetArtFolder);
            string path = $"{PlanetArtFolder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            // Repeat, so the seam at the date line blends away; the sphere wraps the map around.
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = GlobeMapWidth;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
