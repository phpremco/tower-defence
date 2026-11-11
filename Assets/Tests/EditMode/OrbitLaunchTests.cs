using Game.Core;
using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Remco's playtest note 6 of 2026-11-08 (round 36): the orbit shot's capsules ride a Blender booster with a nozzle and
    /// strobes and a Deploy clip (the separation), and the new space effects find their shaders.
    /// </summary>
    public sealed class OrbitLaunchTests
    {
        private const string CastPath = "Assets/_Project/Data/Story/Cutscene/CutsceneCast.asset";

        [Test]
        public void TheCast_CarriesTheBooster_WithItsNozzleStrobesAndSeparation()
        {
            var cast = AssetDatabase.LoadAssetAtPath<CutsceneCast>(CastPath);
            Assert.IsNotNull(cast, "run Tools/Generate/Scenes and Starter Data");
            ModelArtDefinition booster = cast.BoosterArt;
            Assert.IsNotNull(booster, "the cast has no booster (Tools/PlayTest/r36_booster_import.cs)");

            var root = new GameObject("BoosterProbe");
            try
            {
                ArtModel model = ArtModel.CreateSampled(root.transform, booster, ownMaterial: false);
                Assert.IsNotNull(model.Find("Nozzle_1"), "no nozzle socket on the booster");
                Assert.AreEqual(2, model.Numbered("Light").Length, "the booster's two strobes are missing");
                Assert.Greater(model.LengthOf(ModelClipRole.Deploy), 0f, "no separation clip on the booster");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Round 37: Remco could not read the company logo on the hull; the cast carries a large livery that stays sharp at an angle.
        [Test]
        public void TheCast_CarriesTheHullLivery_SharpAtAnAngle()
        {
            var cast = AssetDatabase.LoadAssetAtPath<CutsceneCast>(CastPath);
            Assert.IsNotNull(cast, "run Tools/Generate/Scenes and Starter Data");
            Texture2D livery = cast.HullLivery;
            Assert.IsNotNull(livery, "the cast has no hull livery (Tools/Blender/build_hull_livery.py --unity, then the generator)");
            Assert.AreEqual(2, livery.width / livery.height, "the livery is drawn for a 2:1 plate");
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(livery));
            Assert.IsTrue(importer.mipmapEnabled, "no mipmaps: the wordmark shimmers at orbit distance");
            Assert.GreaterOrEqual(importer.anisoLevel, 4, "the flank is seen at an angle: anisotropic filtering keeps it sharp");
        }

        [Test]
        public void TheSpaceEffects_FindTheirShaders()
        {
            Assert.IsNotNull(Shader.Find(ExhaustPlume.ShaderName), ExhaustPlume.ShaderName);
            Assert.IsNotNull(Shader.Find(ExhaustPlume.ShimmerShader), ExhaustPlume.ShimmerShader);
            Assert.IsNotNull(Shader.Find(SpriteBatch.ShaderName), SpriteBatch.ShaderName);
        }
    }
}
