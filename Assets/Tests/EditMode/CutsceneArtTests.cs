using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The round-26 art the opening cutscene and the themes wear (README round 26): the cave kit and the three Burrower
    /// eyes with their EyeRig colours, every glow / trail / crack sprite, the painted Earth in both sizes with its baked
    /// gloss, a lat-long sky on every Crystal Plains theme; and the story order of the shots after the reaction shot was
    /// appended to the actions.
    /// </summary>
    public sealed class CutsceneArtTests
    {
        private const string CastPath = "Assets/_Project/Data/Story/Cutscene/CutsceneCast.asset";
        private const string CutscenePath = "Assets/_Project/Data/Story/Cutscene/Cutscene_Opening.asset";
        private const string ThemeFolder = "Assets/_Project/Data/Levels";
        private const int EyeVariants = 3;

        private static CutsceneCast Cast() => AssetDatabase.LoadAssetAtPath<CutsceneCast>(CastPath);

        [Test]
        public void Underground_HasEveryKitPiece_AndThreeRiggedEyes()
        {
            UndergroundArt art = Cast().Underground;
            Assert.IsNotNull(art, "the cast has no underground art");
            Assert.IsTrue(art.IsComplete, "a cave kit piece or the eyes are missing");
            Assert.AreEqual(EyeVariants, art.Eyes.Length);
            foreach (BurrowerEyeArt eye in art.Eyes)
            {
                Mesh mesh = eye.Mesh;
                Assert.AreEqual(2, mesh.subMeshCount, $"{eye.Name}: eye and cornea");
                Color[] rig = mesh.colors;
                Assert.AreEqual(mesh.vertexCount, rig.Length, $"{eye.Name} lost its EyeRig vertex colours");
                Assert.Greater(eye.UpperLidDegrees, 0f);
                Assert.Greater(eye.LowerLidDegrees, 0f);
                Assert.AreEqual(1f, eye.Axis.magnitude, 0.001f);
            }
        }

        [Test]
        public void FxSet_HoldsEverySprite_InBothSizes()
        {
            CutsceneFxSet fx = Cast().Fx;
            Assert.IsNotNull(fx, "the cast has no cutscene sprites");
            foreach (CutsceneSprite sprite in System.Enum.GetValues(typeof(CutsceneSprite)))
            {
                Assert.IsNotNull(fx.Texture(sprite), $"no sprite for {sprite}");
            }
        }

        [Test]
        public void Earth_IsPainted_InBothSizes_WithItsGlossBaked()
        {
            CutsceneCast cast = Cast();
            EarthMaps high = cast.EarthMapsFor(false);
            EarthMaps low = cast.EarthMapsFor(true);
            Assert.IsNotNull(high, "the High Earth maps are incomplete");
            Assert.IsNotNull(low, "the Low Earth maps are incomplete");
            Assert.AreNotSame(high.Albedo, low.Albedo, "the Low profile wears the High maps");
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(high.Gloss));
            Assert.AreEqual(TextureImporterAlphaSource.FromInput, importer.alphaSource, "the gloss map loses the smoothness in its alpha");
            Assert.IsFalse(importer.sRGBTexture, "the gloss map is data");
        }

        [Test]
        public void EveryCrystalPlainsTheme_WearsTheBlenderSky()
        {
            int themes = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:VisualTheme Theme_C1_", new[] { ThemeFolder }))
            {
                var theme = AssetDatabase.LoadAssetAtPath<VisualTheme>(AssetDatabase.GUIDToAssetPath(guid));
                themes++;
                Assert.IsNotNull(theme.SkyCubemapFor(false), $"{theme.name} has no sky");
                Assert.IsNotNull(theme.SkyCubemapFor(true), $"{theme.name} has no Low sky");
                Assert.Greater(theme.SkyExposure, 1f, $"{theme.name}: the sky needs its exposure to restore its glow");
            }

            Assert.Greater(themes, 0, "no Crystal Plains theme found");
        }

        [Test]
        public void ShotOrder_FollowsTheStory_WithTheReactionAfterTheSwarm()
        {
            Assert.Less(CutsceneActions.Order(CutsceneAction.SwarmPours), CutsceneActions.Order(CutsceneAction.MercerReacts));
            Assert.Less(CutsceneActions.Order(CutsceneAction.MercerReacts), CutsceneActions.Order(CutsceneAction.GateStand));
            Assert.Less(CutsceneActions.Order(CutsceneAction.GateStand), CutsceneActions.Order(CutsceneAction.Handoff));
            var cutscene = AssetDatabase.LoadAssetAtPath<CutsceneDefinition>(CutscenePath);
            var orders = new List<int>();
            foreach (CutsceneShot shot in cutscene.Shots)
            {
                orders.Add(CutsceneActions.Order(shot.Action));
            }

            for (int i = 1; i < orders.Count; i++)
            {
                Assert.LessOrEqual(orders[i - 1], orders[i], $"shot {i + 1} ({cutscene.Shots[i].Action}) comes before an earlier step of the story");
            }

            Assert.IsTrue(orders.Contains(CutsceneActions.Order(CutsceneAction.MercerReacts)), "the cutscene has no reaction shot");
        }
    }
}
