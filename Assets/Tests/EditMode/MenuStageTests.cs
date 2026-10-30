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
        public void Terrain_SinksPoolsBelowTheirSurfaceAndFlattensThePorch()
        {
            VisualTheme theme = ScriptableObject.CreateInstance<VisualTheme>();
            var nest = new Vector3(4f, 0f, 5f);
            var mouth = nest + Vector3.back * 3f;
            var pond = new MenuTerrain.Pool(new Vector3(0f, 0f, -2f), 2f, TerrainKind.Water, theme);
            var lava = new MenuTerrain.Pool(new Vector3(8f, 0f, -1f), 2f, TerrainKind.Lava, theme);
            var terrain = new MenuTerrain(new Vector3(3f, 0f, 2f), 11f, nest, 4f, 2.5f, mouth, 0.3f, new[] { pond, lava }, theme, 1f);

            Assert.Less(terrain.Height(pond.Centre.x, pond.Centre.z), TerrainLiquidBuilder.SurfaceLevel(TerrainKind.Water), "the pond bed lies under the water");
            Assert.Less(terrain.Height(lava.Centre.x, lava.Centre.z), TerrainLiquidBuilder.SurfaceLevel(TerrainKind.Lava), "the lava bed lies under the lava");
            Assert.Greater(terrain.Height(pond.Centre.x + 4f, pond.Centre.z), TerrainLiquidBuilder.SurfaceLevel(TerrainKind.Water), "the bank stands above the water");
            Assert.AreEqual(0.3f, terrain.Height(mouth.x, mouth.z), 0.001f, "the porch is flat at its height");
            Assert.Greater(terrain.Height(nest.x, nest.z), 2f, "the mound rises");
            Assert.Less(terrain.Height(3f + 11f, 2f), -3f, "the cliff falls away");
            Assert.Greater(terrain.PoolCells(pond, 0.5f).Count, 30);

            Object.DestroyImmediate(theme);
        }
    }
}
