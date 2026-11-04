using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The upgrade screen's Blender display stage (README round 27) stands the right way up and is worn by the skin.</summary>
    public sealed class DisplayStageArtTests
    {
        private const string SkinPath = "Assets/_Project/Settings/UiSkin.asset";

        [Test]
        public void Skin_WearsACompleteStage()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            Assert.IsNotNull(skin);
            Assert.IsNotNull(skin.DisplayStage, "run Tools/Generate/Blender Display Stage");
            Assert.IsTrue(skin.DisplayStage.IsComplete);
        }

        [Test]
        public void Pedestal_HangsBelowItsPivotAndTheHaloFacesUp()
        {
            DisplayStageArt art = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath).DisplayStage;
            Range(art.Pedestal, art.PedestalNode, out float bottom, out float top);
            Assert.AreEqual(0f, top, 0.01f, "the model's feet stand on the pivot, the top face");
            Assert.AreEqual(-art.HeightShare, bottom, 0.01f, "the foot lies the pedestal's height below it");

            Vector3 normal = art.HaloNode.MultiplyVector(art.Halo.normals[0]);
            Assert.Greater(normal.y, 0.9f, "the halo quad faces up");
        }

        private static void Range(Mesh mesh, Matrix4x4 node, out float bottom, out float top)
        {
            bottom = float.MaxValue;
            top = float.MinValue;
            foreach (Vector3 vertex in mesh.vertices)
            {
                float y = node.MultiplyPoint3x4(vertex).y;
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }
        }
    }
}
