using Game.Core;
using Game.Enemies;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// The imported creature models as data: every enemy that wears one has a model, both materials and a travel or
    /// idle cycle; the Low material leaves out the second detail normal; the bosses wear the model of their own family.
    /// </summary>
    public sealed class CreatureArtTests
    {
        private const string EnemyFolder = "Assets/_Project/Data/Enemies";

        [Test]
        public void EveryModelledEnemy_HasModelMaterialsAndACycle()
        {
            int modelled = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyFolder }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                ModelArtDefinition art = enemy.Art;
                if (art == null)
                {
                    continue;
                }

                modelled++;
                Assert.IsNotNull(art.Model, $"{enemy.name}: no model");
                Assert.IsNotNull(art.Material, $"{enemy.name}: no material");
                Assert.IsTrue(art.Material.IsKeywordEnabled("_MASKMAP"), $"{enemy.name}: the material ignores the mask");
                Assert.IsFalse(art.LowMaterial.IsKeywordEnabled("_DETAIL2"), $"{enemy.name}: the Low material keeps the second detail");
                Assert.IsTrue(art.IndexOf(ModelClipRole.Move) >= 0 || art.IndexOf(ModelClipRole.Idle) >= 0, $"{enemy.name}: no cycle");
                if (art.Lod1 != null)
                {
                    foreach (ModelClip clip in art.Clips)
                    {
                        Assert.AreNotSame(clip.Clip, clip.FarClip, $"{enemy.name}: clip {clip.Clip.name} has no far-LOD copy");
                    }
                }
            }

            Assert.Greater(modelled, 20, "the Blender creatures are not wired to the bestiary");
        }

        // Curator decision 2026-11-04: in the data the Storm Matriarch is the bat boss and the Sky Sovereign the ray boss.
        [Test]
        public void SkyBosses_WearTheModelOfTheirOwnFamily()
        {
            var matriarch = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyFolder}/StormMatriarch.asset");
            var sovereign = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyFolder}/SkySovereign.asset");
            Assert.AreEqual("SkySovereign", matriarch.Art.Model.name, "the bat boss wears the bat boss export");
            Assert.AreEqual("StormMatriarch", sovereign.Art.Model.name, "the ray boss wears the ray boss export");
        }

        [Test]
        public void TravelCycles_KnowTheGroundTheyCover()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyFolder }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (enemy.Art == null || enemy.Flies || enemy.Art.IndexOf(ModelClipRole.Land) >= 0)
                {
                    continue;
                }

                int move = enemy.Art.IndexOf(ModelClipRole.Move);
                Assert.Greater(enemy.Art.Clips[move].DistancePerLoop, 0f, $"{enemy.name}: the walk would slide");
            }
        }
    }
}
