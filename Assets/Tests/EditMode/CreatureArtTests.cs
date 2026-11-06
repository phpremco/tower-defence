using Game.Core;
using Game.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
                if (move < 0)
                {
                    // Only a creature that is held where it lies (an egg) may lack a walk.
                    Assert.IsTrue(enemy.Boss == null && enemy.Speed <= 0.1f, $"{enemy.name} has no walk");
                    continue;
                }

                Assert.Greater(enemy.Art.Clips[move].DistancePerLoop, 0f, $"{enemy.name}: the walk would slide");
            }
        }

        // Remco 2026-11-06: the Sealed One's outer crown ring turned half a turn per haul and snapped back at every loop.
        // Each looping clip is sampled frame by frame, across the loop's seam too: no crown ring may jump.
        [Test]
        public void SealedOne_CrownRings_TurnWithoutASnap_ThroughEveryLoop()
        {
            const float FramesPerSecond = 30f;
            const float MaxDegreesPerFrame = 15f;
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/SealedOne.asset");
            var model = Object.Instantiate(art.Model);
            try
            {
                var rings = new[] { "Ring_1", "Ring_2", "Ring_3" };
                var bones = new Transform[rings.Length];
                foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
                {
                    int index = System.Array.IndexOf(rings, node.name);
                    if (index >= 0)
                    {
                        bones[index] = node;
                    }
                }

                CollectionAssert.AllItemsAreNotNull(bones, "the crown ring bones are missing");
                foreach (ModelClip clip in art.Clips)
                {
                    if (clip.Clip == null || !clip.Clip.isLooping)
                    {
                        continue;
                    }

                    int frames = Mathf.RoundToInt(clip.Clip.length * FramesPerSecond);
                    var previous = new Quaternion[bones.Length];
                    for (int f = 0; f <= frames + 1; f++)
                    {
                        // Past the last frame the loop starts again: the seam is checked like any other step.
                        clip.Clip.SampleAnimation(model, f > frames ? 1f / FramesPerSecond : f / FramesPerSecond);
                        for (int b = 0; b < bones.Length; b++)
                        {
                            if (f > 0)
                            {
                                Assert.Less(Quaternion.Angle(previous[b], bones[b].localRotation), MaxDegreesPerFrame, $"{clip.Clip.name}: {rings[b]} jumps at frame {f}");
                            }

                            previous[b] = bones[b].localRotation;
                        }
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(model);
            }
        }

        // Round 17: the dive ended 2.7 rad into the rings' turn and blended back to Submerged_Loop frame 0, so the rings
        // unwound ~26 degrees a frame. They now turn whole turns: no step inside the dive or across into the loop.
        [Test]
        public void SealedOne_CrownRings_DiveIntoTheLiquidWithoutUnwinding()
        {
            const float MaxDegreesPerFrame = 15f;
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/SealedOne.asset");
            AnimationClip dive = art.Clips[art.IndexOf(ModelClipRole.DiveIn)].Clip;
            AnimationClip submerged = art.Clips[art.IndexOf(ModelClipRole.Submerged)].Clip;
            var model = Object.Instantiate(art.Model);
            try
            {
                Transform[] rings = FindBones(model, "Ring_1", "Ring_2", "Ring_3");
                AssertNoStep(dive, model, rings, MaxDegreesPerFrame, false);
                dive.SampleAnimation(model, dive.length);
                Quaternion[] last = LocalRotations(rings);
                submerged.SampleAnimation(model, 0f);
                for (int b = 0; b < rings.Length; b++)
                {
                    Assert.Less(Quaternion.Angle(last[b], rings[b].localRotation), MaxDegreesPerFrame, $"{rings[b].name} snaps from the dive into the submerged loop");
                }
            }
            finally
            {
                Object.DestroyImmediate(model);
            }
        }

        // Round 17: the fire larva's legs flipped through 100-170 degrees in one frame (a world-planted foot ended up
        // under its own swinging hip). A fast scuttle may swing a leg ~50 degrees a frame; a flip is far beyond that.
        [Test]
        public void FireLarva_Legs_NeverFlipInAnyClip()
        {
            const float MaxDegreesPerFrame = 60f;
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/FireLarva.asset");
            var model = Object.Instantiate(art.Model);
            try
            {
                var legs = new System.Collections.Generic.List<Transform>();
                foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name.StartsWith("Leg_"))
                    {
                        legs.Add(node);
                    }
                }

                Assert.IsNotEmpty(legs, "the larva has no leg bones");
                foreach (ModelClip clip in art.Clips)
                {
                    if (clip.Clip != null)
                    {
                        AssertNoStep(clip.Clip, model, legs.ToArray(), MaxDegreesPerFrame, clip.Clip.isLooping);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(model);
            }
        }

        private static Transform[] FindBones(GameObject model, params string[] names)
        {
            var bones = new Transform[names.Length];
            foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
            {
                int index = System.Array.IndexOf(names, node.name);
                if (index >= 0)
                {
                    bones[index] = node;
                }
            }

            CollectionAssert.AllItemsAreNotNull(bones, $"bones missing: {string.Join(", ", names)}");
            return bones;
        }

        private static Quaternion[] LocalRotations(Transform[] bones)
        {
            var rotations = new Quaternion[bones.Length];
            for (int b = 0; b < bones.Length; b++)
            {
                rotations[b] = bones[b].localRotation;
            }

            return rotations;
        }

        /// <summary>Samples the clip at 30 fps (and, for a loop, once past its end) and fails on any bone step above the limit.</summary>
        private static void AssertNoStep(AnimationClip clip, GameObject model, Transform[] bones, float maxDegreesPerFrame, bool acrossSeam)
        {
            const float FramesPerSecond = 30f;
            int frames = Mathf.RoundToInt(clip.length * FramesPerSecond);
            Quaternion[] previous = null;
            for (int f = 0; f <= frames + (acrossSeam ? 1 : 0); f++)
            {
                clip.SampleAnimation(model, f > frames ? 1f / FramesPerSecond : Mathf.Min(f / FramesPerSecond, clip.length));
                if (previous != null)
                {
                    for (int b = 0; b < bones.Length; b++)
                    {
                        Assert.Less(Quaternion.Angle(previous[b], bones[b].localRotation), maxDegreesPerFrame, $"{clip.name}: {bones[b].name} jumps at frame {f}");
                    }
                }

                previous = LocalRotations(bones);
            }
        }
    }
}
