using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public sealed class MenuStageTests
    {
        private const string StagePath = "Assets/_Project/Data/Menu/MainMenuStage.asset";

        [Test]
        public void GeneratedStage_HasEveryThemeAndCreature()
        {
            var stage = AssetDatabase.LoadAssetAtPath<MenuStageDefinition>(StagePath);
            Assert.IsNotNull(stage, "run Tools/Generate/Scenes and Starter Data");
            Assert.IsTrue(stage.IsComplete, "all four themes");
            Assert.IsNotNull(stage.NestGuardian);
            Assert.IsNotNull(stage.Flyer);
            Assert.IsNotNull(stage.WaterCreature);
            Assert.IsNotNull(stage.LavaCreature);
            Assert.IsNotNull(stage.SandCreature);
            Assert.Greater(stage.NestCreatures.Length, 0);
            foreach (var creature in stage.NestCreatures)
            {
                Assert.IsNotNull(creature);
            }
        }

        [Test]
        public void BlenderIsland_GroundSinksEveryPoolBelowItsSurfaceAndHoldsThePorch()
        {
            var stage = AssetDatabase.LoadAssetAtPath<MenuStageDefinition>(StagePath);
            MenuDioramaArt art = stage != null ? stage.Art : null;
            Assert.IsNotNull(art, "run Tools/Generate/Blender Menu Diorama");
            Assert.IsTrue(art.IsComplete, "island, materials and height map");
            Assert.IsTrue(art.HeightMap.isReadable, "the height map is read on the CPU");
            var terrain = new MenuTerrain(art);

            Assert.AreEqual(5, art.Pools.Length, "five pools in the layout");
            foreach (MenuDioramaPool pool in art.Pools)
            {
                Vector3 centre = pool.Centre;
                Assert.Less(terrain.Height(centre.x, centre.z), pool.Surface, $"the {pool.Kind} bed lies under its surface");
                Assert.AreEqual(TerrainLiquidBuilder.SurfaceLevel(pool.Kind), pool.Surface, 0.001f, $"{pool.Kind} sits at the level's liquid height");
                Assert.GreaterOrEqual(pool.Outline.Length, 3, $"{pool.Kind} has a shore");
            }

            // The crawlers step out of the hive mouth onto a flat porch at 0.3 (MenuDiorama's route).
            Assert.AreEqual(0.3f, terrain.Height(4.8f, 1.7f), 0.08f, "the porch in front of the mouth");
            Assert.Greater(terrain.Height(4.8f, 6.5f), 1.5f, "the hive mound rises behind it");
        }

        [Test]
        public void BlenderIsland_EveryPlacementWearsAnImportedModel()
        {
            var stage = AssetDatabase.LoadAssetAtPath<MenuStageDefinition>(StagePath);
            MenuDioramaArt art = stage != null ? stage.Art : null;
            Assert.IsNotNull(art, "run Tools/Generate/Blender Menu Diorama");
            Assert.AreEqual(105, art.Placements.Length, "every item of Menu_Island_layout.json");
            foreach (MenuDioramaModel model in art.Models)
            {
                Assert.IsTrue(model.IsRigged || (model.Mesh != null && model.Mesh.IsValid), $"{model.Name} has a mesh and a material");
            }

            Assert.GreaterOrEqual(art.Lights.Length, 4, "the layout's lights");
        }
    }
}
