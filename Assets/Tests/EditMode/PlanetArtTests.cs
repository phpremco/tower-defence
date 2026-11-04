using System.IO;
using Game.Core;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The planet painted in Blender (README round 20) and the round-19/20 hero assets as data: the campaign wears a
    /// complete S-69-CRYS set whose region map puts every level site in its own region, the maps import as the planet
    /// shader reads them, and the mothership carries its far mesh with its own clips.
    /// </summary>
    public sealed class PlanetArtTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string MothershipPath = "Assets/_Project/Data/Art/Story/Cutscene_Mothership.asset";
        private const int HighWidth = 4096;
        private const int LowWidth = 2048;

        [Test]
        public void Campaign_WearsTheS69Set_WithBothSizesComplete()
        {
            PlanetTextureSet set = Catalog().PlanetTextures;
            Assert.IsNotNull(set, "the campaign has no complete painted planet");
            Assert.IsTrue(set.Low.IsComplete, "the Low (Switch) maps are incomplete");
            Assert.AreEqual(HighWidth, set.High.Albedo.width, "the High albedo is capped below its painted size");
            Assert.AreEqual(LowWidth, set.Low.Albedo.width, "the Low albedo is not the Low size");
        }

        [Test]
        public void RegionMap_PutsEveryLevelSite_InItsOwnRegion()
        {
            CampaignCatalog catalog = Catalog();
            PlanetTextureSet set = catalog.PlanetTextures;
            Assert.IsNotNull(set);
            // The imported texture is not readable; the PNG beside it holds the same exact ids.
            var regions = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Assert.IsTrue(regions.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(set.High.Regions))));
            try
            {
                for (int i = 0; i < catalog.Nodes.Count; i++)
                {
                    MapNode node = catalog.Nodes[i];
                    if (catalog.LayerOf(i) != MapLayer.Surface)
                    {
                        continue;
                    }

                    Vector2 uv = PlanetCoordinates.ToUv(node.Longitude, node.Latitude);
                    Color32 texel = regions.GetPixel(Mathf.FloorToInt(uv.x * regions.width) % regions.width, Mathf.Clamp(Mathf.FloorToInt(uv.y * regions.height), 0, regions.height - 1));
                    int id = Mathf.RoundToInt(texel.g / set.RegionValueStep);
                    Assert.AreEqual(set.RegionId(node.Region), id, $"{node.DisplayName} lies in region id {id}, not in {catalog.Regions[node.Region].DisplayName}");
                }
            }
            finally
            {
                Object.DestroyImmediate(regions);
            }
        }

        [Test]
        public void PlanetMaps_ImportAsTheShaderReadsThem()
        {
            PlanetTextureSet set = Catalog().PlanetTextures;
            Assert.IsNotNull(set);
            var regions = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(set.High.Regions));
            Assert.AreEqual(FilterMode.Point, regions.filterMode, "region ids must not blend");
            Assert.IsFalse(regions.mipmapEnabled, "region ids must not blend across mips");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, regions.textureCompression);
            Assert.IsTrue(Importer(set.High.Albedo).sRGBTexture, "albedo is colour");
            Assert.IsTrue(Importer(set.High.Emission).sRGBTexture, "emission is colour");
            Assert.IsFalse(Importer(set.High.Mask).sRGBTexture, "the mask is data");
            Assert.IsFalse(Importer(set.High.Clouds).sRGBTexture, "cloud cover is data");
            Assert.AreEqual(TextureImporterType.NormalMap, Importer(set.High.Normal).textureType);
            Assert.AreEqual(TextureWrapMode.Repeat, set.High.Albedo.wrapModeU, "the map wraps round in longitude");
            Assert.AreEqual(TextureWrapMode.Clamp, set.High.Albedo.wrapModeV, "the map stops at the poles");
        }

        [Test]
        public void Mothership_WearsItsFarMesh_AndItsSockets()
        {
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>(MothershipPath);
            Assert.IsNotNull(art);
            Assert.IsNotNull(art.Lod1, "the round-19 LOD1 is not worn");
            int idle = art.IndexOf(ModelClipRole.Idle);
            Assert.GreaterOrEqual(idle, 0, "no Spin clip");
            Assert.AreNotSame(art.Clips[idle].Clip, art.Clips[idle].FarClip, "the far rig has no clip of its own");
            Transform root = art.Model.transform;
            Assert.IsNotNull(Find(root, "Bay"), "no Bay socket");
            Assert.IsNotNull(Find(root, "Light_1"), "no running light sockets");
        }

        private static TextureImporter Importer(Texture texture) => (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));

        private static CampaignCatalog Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog);
            return catalog;
        }

        private static Transform Find(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform found = Find(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
