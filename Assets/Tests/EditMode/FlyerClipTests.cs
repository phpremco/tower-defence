using Game.Core;
using Game.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 50 (Remco: "the bats stop flying as they attack"): a flyer that holds its place to strike kept dropping into its
    /// resting glide, wings still. In the field it now keeps its travel beat at the authored rate while it stands; a creature
    /// on a display stage keeps its own idle.
    /// </summary>
    public sealed class FlyerClipTests
    {
        private const string ArtFolder = "Assets/_Project/Data/Art";
        private const string EnemyFolder = "Assets/_Project/Data/Enemies";
        private const string BatRow = "Bat";
        private const float Frame = 0.1f;

        [Test]
        public void AFlyerHoldingItsPlace_KeepsBeating_WhileADisplayedOneRests()
        {
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{BatRow}.asset");
            int move = art.IndexOf(ModelClipRole.Move);
            int idle = art.IndexOf(ModelClipRole.Idle);
            Assert.GreaterOrEqual(move, 0, "the bat has no flight cycle");
            Assert.GreaterOrEqual(idle, 0, "the bat has no resting cycle");
            Assert.AreNotEqual(move, idle);

            var owner = new GameObject("FlyerClipTest");
            CreatureModel model = CreatureModel.Create(owner.transform, art, 1f, true);
            try
            {
                var clips = new CreatureClips(model.Player, art, new string[0]) { StaysOnTheWing = true };
                clips.Tick(Frame, 0f, false, false, 1f);
                Assert.AreEqual(move, model.Player.Cycle, "a flyer standing in the field should keep its wing beat");
                Assert.AreEqual(1f, model.Player.CycleRate, 1e-5f, "standing, it beats at the authored rate");

                clips.StaysOnTheWing = false;
                clips.Tick(Frame, 0f, false, false, 1f);
                Assert.AreEqual(idle, model.Player.Cycle, "on a stage the creature rests in its idle");
            }
            finally
            {
                model.Player.Dispose();
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void EveryModelledFlyer_HasAFlightCycleToBeat()
        {
            int flyers = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyFolder }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (!enemy.Flies || enemy.Art == null)
                {
                    continue;
                }

                flyers++;
                Assert.GreaterOrEqual(enemy.Art.IndexOf(ModelClipRole.Move), 0, $"{enemy.name} flies but its art has no flight cycle");
            }

            Assert.Greater(flyers, 0, "no modelled flyer found");
        }
    }
}
