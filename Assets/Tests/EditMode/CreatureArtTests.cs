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

        private const string ArtFolder = "Assets/_Project/Data/Art";

        // Import stage 4b: art ready for creatures without game data yet, and the two-material continent bosses.
        private static readonly string[] FutureSpecies = { "Knitback", "Slagstrider", "Siftjaw", "Surgefin", "FireLarva" };
        private static readonly string[] TwoMaterialBosses = { "Gravemaw", "CinderMatriarch", "FrostWarden", "Rotbloom", "Shattered", "SealedOne" };

        [Test]
        public void Shardback_ShedsItsSixPrismClusters_AndTheP3SpeciesWearTheirModels()
        {
            var shardback = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyFolder}/Shardback.asset");
            var mireleech = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyFolder}/Mireleech.asset");
            Assert.AreEqual("Shardback", shardback.Art.Model.name);
            Assert.AreEqual("Mireleech", mireleech.Art.Model.name);
            CollectionAssert.AreEqual(new[] { "Shard_1", "Shard_2", "Shard_3", "Shard_4", "Shard_5", "Shard_6" }, shardback.Art.ShedBones);
            Assert.GreaterOrEqual(mireleech.Art.IndexOf(ModelClipRole.Attack), 0, "the Mireleech has no latch");
            Assert.AreEqual(13f / 30f, mireleech.Art.Clips[mireleech.Art.IndexOf(ModelClipRole.Attack)].EventSeconds, 0.001f, "the latch bites off its frame");
        }

        [Test]
        public void FutureCreatures_HaveArtReady_WithCyclesAndBothProfiles()
        {
            var ids = new System.Collections.Generic.List<string>(FutureSpecies);
            ids.AddRange(TwoMaterialBosses);
            foreach (string id in ids)
            {
                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}.asset");
                Assert.IsNotNull(art, $"no art for {id}");
                Assert.IsNotNull(art.Model, $"{id}: no model");
                Assert.IsNotNull(art.Lod1, $"{id}: no far LOD");
                Assert.AreNotSame(art.Material, art.LowMaterial, $"{id}: no Low material");
                Assert.IsTrue(art.IndexOf(ModelClipRole.Move) >= 0 || art.IndexOf(ModelClipRole.Idle) >= 0, $"{id}: no cycle");
                Assert.GreaterOrEqual(art.IndexOf(ModelClipRole.Death), 0, $"{id}: no death");
                bool twoMaterials = System.Array.IndexOf(TwoMaterialBosses, id) >= 0;
                Assert.AreEqual(twoMaterials, art.HasSecondMaterial, $"{id}: second material");
                if (twoMaterials)
                {
                    Assert.AreNotSame(art.SecondMaterial, art.LowSecondMaterial, $"{id}: no Low second material");
                    Assert.IsTrue(art.SecondMaterial.IsKeywordEnabled("_EMISSION"), $"{id}: the second material does not glow");
                    Assert.IsFalse(art.LowSecondMaterial.IsKeywordEnabled("_DETAIL2"), $"{id}: the Low second material keeps the second detail");
                }

                int move = art.IndexOf(ModelClipRole.Move);
                if (move >= 0)
                {
                    Assert.Greater(art.Clips[move].DistancePerLoop, 0f, $"{id}: the walk would slide");
                }
            }
        }

        [Test]
        public void CorruptedSkins_ShareTheSpeciesMeshAndClips_WithTheirOwnMaps()
        {
            foreach (string id in new[] { "Skitter", "Bulwark", "Shardback", "Knitback", "BroodMother", "Bat" })
            {
                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}.asset");
                var corrupted = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}_Corrupted.asset");
                Assert.IsNotNull(corrupted, $"no corrupted art for {id}");
                Assert.AreSame(art.Model, corrupted.Model, $"{id}: the corrupted skin wears another mesh");
                Assert.AreEqual(art.Clips.Length, corrupted.Clips.Length, $"{id}: the corrupted skin lost clips");
                Assert.AreNotSame(art.Material.GetTexture("_BaseMap"), corrupted.Material.GetTexture("_BaseMap"), $"{id}: the corrupted skin has the species' maps");
                StringAssert.Contains("Corrupted", corrupted.Material.GetTexture("_BaseMap").name);
            }
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
