using Game.Core;
using Game.Editor;
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

        // Round 38: the worm gullet and its teeth are an object of their own with maps of their own; without the part
        // the throat would wear the body's skin, in the near and the far LOD alike. Round 45 gave sixteen bosses faces,
        // eyes, teeth and claws the same way, so every catalog row's extra parts are checked: built in the row's order,
        // each wearing its own material (the export's own part maps, 512 ones on Low) in both profiles, and owning a
        // renderer in the near model and in the far one where there is one.
        [Test]
        public void EveryBossExtraPart_WearsItsOwnMaterial_InBothLodsAndProfiles()
        {
            int checkedParts = 0;
            foreach (BlenderArtEntry entry in BlenderArtCatalog.Creatures)
            {
                if (entry.ExtraParts.Length == 0)
                {
                    continue;
                }

                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{entry.Id}.asset");
                Assert.IsNotNull(art, $"{entry.Id}: no art definition");
                Assert.AreEqual(entry.ExtraParts.Length, art.Parts.Length, $"{entry.Id}: parts built");
                for (int i = 0; i < entry.ExtraParts.Length; i++)
                {
                    AssertPartDressed(entry, art, art.Parts[i], $"{entry.Asset}_{entry.ExtraParts[i]}");
                    checkedParts++;
                }
            }

            Assert.GreaterOrEqual(checkedParts, 40, "the round-45 parts are missing from the catalog");
        }

        // Round 45: what a boss wears in play (CreatureModel), near and far LOD, High and Low: each part's renderers wear
        // that part's copy, never the body's; the Sealed One's phase looks swap the body and seal glow maps but leave the
        // parts' own maps alone (their strength follows the phase, which is fine for eyes in a breaking seal).
        [Test]
        public void EveryBossExtraPart_IsDressedInPlay_AndKeepsItsMapsThroughPhaseLooks()
        {
            foreach (BlenderArtEntry entry in BlenderArtCatalog.Creatures)
            {
                if (entry.ExtraParts.Length == 0)
                {
                    continue;
                }

                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{entry.Id}.asset");
                foreach (bool low in new[] { false, true })
                {
                    var owner = new GameObject("Owner");
                    CreatureModel model = CreatureModel.Create(owner.transform, art, 1f, low);
                    try
                    {
                        AssertPartsWorn(art, model, $"{entry.Id} (low {low})");
                        foreach (ModelEmissionSet set in art.EmissionSets)
                        {
                            model.SetEmissionSet(set, low);
                            AssertPartsWorn(art, model, $"{entry.Id} {set.Name} (low {low})");
                            Assert.AreSame(set.BodyFor(low), model.Materials[0].GetTexture("_EmissionMap"), $"{entry.Id} {set.Name}: the body kept its glow");
                        }
                    }
                    finally
                    {
                        model.Player.Dispose();
                        foreach (Material material in model.Materials)
                        {
                            Object.DestroyImmediate(material);
                        }

                        Object.DestroyImmediate(owner);
                    }
                }
            }
        }

        // Round 46: the small species got close-up faces (eyes, mouthparts) and claws the same way the bosses did; the
        // slugs have no limbs, so a face only. Checked per enemy as it is placed: the art it wears lists the parts and
        // every part is dressed in its own maps, near and far, High and Low.
        private static readonly string[] SmallFaceClawsEnemies =
        {
            "Skitter", "Cragback", "Skyray", "Roller", "Prowler", "Shalebat", "Wisp", "Stinger", "Bulwark", "Shardback",
            "Emberbat", "Mossbat", "Dustbat", "Tidebat", "Frostbat", "Mirebat",
        };

        private static readonly string[] SmallFaceOnlyEnemies = { "Oozer", "Mireleech" };

        // Species with art but no enemy data yet: their rows carry the parts too.
        private static readonly string[] SmallFaceClawsRowsWithoutEnemy = { "Knitback", "Slagstrider", "Siftjaw", "Surgefin" };

        [Test]
        public void EverySmallEnemyExtraPart_IsInTheCatalog_AndWornByTheEnemy()
        {
            foreach (string enemy in SmallFaceClawsEnemies)
            {
                AssertSmallEnemyParts(enemy, new[] { "Face", "Claws" });
            }

            foreach (string enemy in SmallFaceOnlyEnemies)
            {
                AssertSmallEnemyParts(enemy, new[] { "Face" });
            }

            foreach (string id in SmallFaceClawsRowsWithoutEnemy)
            {
                AssertSmallRowParts(id, new[] { "Face", "Claws" });
            }
        }

        // Round 46: the element bats' skins cover the body only, so the shared parts take the skin's glow: Moss and Dust
        // lean green and pale, the warm Tide and Frost keep the ember bat's own.
        [Test]
        public void ElementBatParts_TakeTheirSkinsGlow()
        {
            Color ember = PartGlow("Bat");
            Assert.AreNotEqual(ember, PartGlow("Bat_Moss"), "the moss bat's parts glow like the ember bat's");
            Assert.AreNotEqual(ember, PartGlow("Bat_Dust"), "the dust bat's parts glow like the ember bat's");
            Assert.AreEqual(ember, PartGlow("Bat_Tide"), "the tide bat's parts should keep the warm glow");
            Assert.AreEqual(ember, PartGlow("Bat_Frost"), "the frost bat's parts should keep the warm glow");
        }

        // Round 49 (approved set 1): the redesigned exports and their corrupted skins are in, on the same parts; the
        // Cragback carries its three crater sockets in both rigs.
        private static readonly string[] Redesign49Rows = { "Oozer", "Cragback", "Slagstrider", "Siftjaw", "Surgefin" };
        private static readonly string[] CragbackSmokeSockets = { "Smoke_1", "Smoke_2", "Smoke_3" };

        [Test]
        public void Redesign49Rows_AndTheirCorruptedSkins_ShareTheNewModel()
        {
            foreach (string id in Redesign49Rows)
            {
                AssertCorruptedRowSharesModel(id);
            }

            var cragback = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/Cragback.asset");
            foreach (GameObject model in new[] { cragback.Model, cragback.Lod1 })
            {
                foreach (string socket in CragbackSmokeSockets)
                {
                    Assert.IsTrue(HasNode(model.transform, socket), $"{model.name}: no {socket} socket");
                }
            }
        }

        // Round 49 (approved set 2): the bats, the bat boss, the scorpions, the Frost Warden, the Brood Mother and the Hive
        // Queen are in with their corrupted skins; the element bats still share the ember bat's model, and every power
        // socket a boss effect hangs on survived the moved rest poses (in the far rig too where the boss has one there).
        private static readonly string[] Redesign49Set2CorruptedRows = { "Bat", "BatBoss", "Stinger", "AcidEmpress", "BroodMother", "Queen" };
        private static readonly string[] ElementBatRows = { "Bat_Moss", "Bat_Dust", "Bat_Tide", "Bat_Frost" };
        private static readonly (string Id, string Socket, bool InFarRig)[] Redesign49Set2Sockets =
        {
            ("BatBoss", "Emitter", true), ("AcidEmpress", "Gland", true), ("FrostWarden", "Socket_Breath", true),
            ("BroodMother", "Vent", false),
        };

        [Test]
        public void Redesign49Set2Rows_ShareTheNewModel_AndKeepTheirPowerSockets()
        {
            foreach (string id in Redesign49Set2CorruptedRows)
            {
                AssertCorruptedRowSharesModel(id);
            }

            var bat = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/Bat.asset");
            foreach (string id in ElementBatRows)
            {
                var element = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}.asset");
                Assert.IsNotNull(element, $"{id}: no art definition");
                Assert.AreSame(bat.Model, element.Model, $"{id}: wears another model than the ember bat");
                Assert.AreSame(bat.Lod1, element.Lod1, $"{id}: wears another far model than the ember bat");
                Assert.AreEqual(bat.Height, element.Height, 1e-4f, $"{id}: measures another height than the ember bat");
            }

            foreach ((string id, string socket, bool inFarRig) in Redesign49Set2Sockets)
            {
                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}.asset");
                Assert.IsTrue(HasNode(art.Model.transform, socket), $"{id}: no {socket} socket");
                if (inFarRig)
                {
                    Assert.IsTrue(HasNode(art.Lod1.transform, socket), $"{id}: no {socket} socket in the far rig");
                }
            }
        }

        // Round 49 (approved set 3, the 49E second pass): the wolves, the rays, the Slime Monarch and the Knitback are in with
        // their corrupted skins (the Cinder Matriarch has none). The Slime Monarch's young still leave from the Sac spouts and
        // its acid vat carries the new Slosh bone; the Pack Alpha's howl collar still hangs on the Mane bones; the rays have
        // their new Jaw in the near rig (the far rig is too coarse for it).
        private static readonly string[] Redesign49Set3CorruptedRows = { "Prowler", "PackAlpha", "Skyray", "RayBoss", "SlimeMonarch", "Knitback" };
        private static readonly (string Id, string Socket, bool InFarRig)[] Redesign49Set3Sockets =
        {
            ("SlimeMonarch", "Sac_L", true), ("SlimeMonarch", "Sac_R", true), ("SlimeMonarch", "Slosh", true),
            ("PackAlpha", "Mane_L", true), ("PackAlpha", "Mane_R", true), ("Skyray", "Jaw", false), ("RayBoss", "Jaw", false),
        };

        [Test]
        public void Redesign49Set3Rows_ShareTheNewModel_AndKeepTheirSockets()
        {
            foreach (string id in Redesign49Set3CorruptedRows)
            {
                AssertCorruptedRowSharesModel(id);
            }

            foreach ((string id, string socket, bool inFarRig) in Redesign49Set3Sockets)
            {
                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}.asset");
                Assert.IsTrue(HasNode(art.Model.transform, socket), $"{id}: no {socket} socket");
                if (inFarRig)
                {
                    Assert.IsTrue(HasNode(art.Lod1.transform, socket), $"{id}: no {socket} socket in the far rig");
                }
            }

            var monarch = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/SlimeMonarch.asset");
            int spawn = monarch.IndexOf(ModelClipRole.Power, "Sac_L");
            Assert.GreaterOrEqual(spawn, 0, "SlimeMonarch: no spawn clip on the Sac_L spout");
        }

        // The 49E exports first came in without the Prowler's and the Slime Monarch's Strike (the builder skips a declared
        // clip the FBX lacks, silently): every clip the catalog declares for a set-3 row must be in its art, and the three
        // that attack must carry an Attack clip with its impact moment.
        private static readonly string[] Redesign49Set3Rows = { "Prowler", "PackAlpha", "Skyray", "RayBoss", "SlimeMonarch", "CinderMatriarch", "Knitback" };
        private static readonly string[] Redesign49Set3Attackers = { "Prowler", "Skyray", "SlimeMonarch" };

        [Test]
        public void Redesign49Set3Rows_CarryEveryDeclaredClip_AndTheAttackersStrike()
        {
            foreach (BlenderArtEntry entry in BlenderArtCatalog.Creatures)
            {
                string row = entry.Id.Replace(BlenderArtCatalog.CorruptedSuffix, string.Empty);
                if (System.Array.IndexOf(Redesign49Set3Rows, row) < 0)
                {
                    continue;
                }

                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{entry.Id}.asset");
                foreach (BlenderClipSpec spec in entry.Clips)
                {
                    Assert.IsTrue(HasClip(art, spec), $"{entry.Id}: no {spec.Clip} clip as {spec.Role}");
                }

                if (System.Array.IndexOf(Redesign49Set3Attackers, row) >= 0)
                {
                    int attack = art.IndexOf(ModelClipRole.Attack);
                    Assert.GreaterOrEqual(attack, 0, $"{entry.Id}: no Attack clip");
                    Assert.Greater(art.Clips[attack].EventSeconds, 0f, $"{entry.Id}: the Attack clip has no impact moment");
                }
            }
        }

        // Round 49 (set 3b): a bar height measured at rest alone sat inside wing beats and rearing bodies. Every enemy's bar
        // clears its body for the covered share of its travel and idle cycles (a small tolerance for rows built before the
        // cycle-aware measure whose cycles barely lift them).
        private const float BarHeightTolerance = 0.02f;

        [Test]
        public void EnemyBars_ClearTheBodyOverItsCycles()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyFolder }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                ModelArtDefinition art = enemy.Art;
                if (art == null || art.Model == null)
                {
                    continue;
                }

                var tops = CreatureBarHeight.CycleTops(art.Model, art);
                if (tops.Count == 0)
                {
                    continue;
                }

                float covered = CreatureBarHeight.Covered(tops);
                Assert.GreaterOrEqual(art.Height, covered * (1f - BarHeightTolerance), $"{enemy.name}: the bar sits inside the body for more than a quarter of its cycles");
            }
        }

        private static bool HasClip(ModelArtDefinition art, BlenderClipSpec spec)
        {
            foreach (ModelClip clip in art.Clips)
            {
                if (clip.Role == spec.Role && clip.Clip != null && clip.Clip.name == spec.Clip)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertCorruptedRowSharesModel(string id)
        {
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}.asset");
            var corrupted = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}{BlenderArtCatalog.CorruptedSuffix}.asset");
            Assert.IsNotNull(art, $"{id}: no art definition");
            Assert.IsNotNull(corrupted, $"{id}: no corrupted art definition");
            Assert.AreSame(art.Model, corrupted.Model, $"{id}: the corrupted row wears another model");
            // Unity equality: a row without a far model (the Queen) holds a fake-null reference on each asset.
            Assert.IsTrue(art.Lod1 == corrupted.Lod1, $"{id}: the corrupted row wears another far model");
            Assert.AreEqual(art.Parts.Length, corrupted.Parts.Length, $"{id}: the corrupted row has other parts");
            Assert.AreEqual(art.Height, corrupted.Height, 1e-4f, $"{id}: the corrupted row measures another height");
        }

        // The Cragback's craters smoke from data: its art (and its corrupted skin's) names a sound vent definition, the
        // view finds one vent per crater socket, and the Low profile spends fewer particles than High.
        [Test]
        public void CragbackVents_ResolveOnItsCraterSockets_AndLowSpendsLess()
        {
            foreach (string id in new[] { "Cragback", "Cragback" + BlenderArtCatalog.CorruptedSuffix })
            {
                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}.asset");
                Assert.IsNotNull(art.Vents, $"{id}: no vent definition");
                Assert.AreEqual(string.Empty, art.Vents.Problem(), $"{id}: vent data");
                Assert.Greater(art.Vents.SmokeRate, 0f, $"{id}: the vents do not smoke");
                Assert.Less(art.Vents.SmokeColor.a, 1f, $"{id}: the smoke should stay light (see-through)");
                Assert.Less(art.Vents.RateShare(true), art.Vents.RateShare(false), $"{id}: Low should emit less than High");
                int high = VentParticleBudget(art, false, out int vents);
                Assert.AreEqual(CragbackSmokeSockets.Length, vents, $"{id}: vents found");
                Assert.Less(VentParticleBudget(art, true, out _), high, $"{id}: Low particle budget");
            }

            foreach (string guid in AssetDatabase.FindAssets("t:ModelArtDefinition", new[] { ArtFolder }))
            {
                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (art.Vents != null)
                {
                    Assert.IsTrue(HasNodeStartingWith(art.Model.transform, art.Vents.SocketPrefix), $"{art.name}: vents without sockets");
                }
            }
        }

        private static int VentParticleBudget(ModelArtDefinition art, bool low, out int vents)
        {
            var owner = new GameObject("Owner");
            CreatureModel model = CreatureModel.Create(owner.transform, art, 1f, low);
            try
            {
                CreatureVents created = CreatureVents.Create(model, 1f, low);
                vents = created != null ? created.Count : 0;
                int budget = 0;
                foreach (ParticleSystem system in owner.GetComponentsInChildren<ParticleSystem>(true))
                {
                    budget += system.main.maxParticles;
                }

                return budget;
            }
            finally
            {
                model.Player.Dispose();
                foreach (Material material in model.Materials)
                {
                    Object.DestroyImmediate(material);
                }

                Object.DestroyImmediate(owner);
            }
        }

        private static bool HasNode(Transform root, string name) => HasNodeStartingWith(root, name, true);

        private static bool HasNodeStartingWith(Transform root, string prefix, bool exact = false)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (exact ? node.name == prefix : node.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Color PartGlow(string id)
        {
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}.asset");
            Assert.IsNotNull(art, $"{id}: no art definition");
            return art.Parts[0].MaterialFor(false).GetColor("_EmissionColor");
        }

        private static void AssertSmallEnemyParts(string enemyName, string[] parts)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyFolder}/{enemyName}.asset");
            Assert.IsNotNull(enemy, $"{enemyName}: no enemy definition");
            Assert.IsTrue(BlenderArtCatalog.EnemyArt.TryGetValue(enemyName, out string id), $"{enemyName}: no art row");
            Assert.IsNotNull(enemy.Art, $"{enemyName}: wears no art");
            Assert.AreEqual(id, enemy.Art.name, $"{enemyName}: wears the wrong art");
            AssertSmallRowParts(id, parts);
        }

        private static void AssertSmallRowParts(string id, string[] parts)
        {
            BlenderArtEntry entry = null;
            foreach (BlenderArtEntry row in BlenderArtCatalog.Creatures)
            {
                if (row.Id == id)
                {
                    entry = row;
                }
            }

            Assert.IsNotNull(entry, $"{id}: no catalog row");
            CollectionAssert.AreEqual(parts, entry.ExtraParts, $"{id}: catalog parts");
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{id}.asset");
            Assert.IsNotNull(art, $"{id}: no art definition");
            Assert.AreEqual(parts.Length, art.Parts.Length, $"{id}: parts built");
            for (int i = 0; i < parts.Length; i++)
            {
                AssertPartDressed(entry, art, art.Parts[i], $"{entry.Asset}_{parts[i]}");
                Assert.IsNotNull(art.Lod1, $"{id}: no far model");
            }
        }

        private static void AssertPartsWorn(ModelArtDefinition art, CreatureModel model, string label)
        {
            int slots = art.HasSecondMaterial ? 2 : 1;
            foreach (Renderer renderer in model.Renderers)
            {
                for (int i = 0; i < art.Parts.Length; i++)
                {
                    if (!art.Parts[i].Owns(renderer.name))
                    {
                        continue;
                    }

                    Material worn = model.Materials[slots + i];
                    Assert.AreSame(worn, renderer.sharedMaterial, $"{label}: {renderer.name} wears {renderer.sharedMaterial.name}");
                    Texture own = art.Parts[i].MaterialFor(false).GetTexture("_EmissionMap");
                    Texture now = worn.GetTexture("_EmissionMap");
                    Assert.AreEqual(own != null ? own.name : null, now != null ? now.name : null, $"{label}: {renderer.name} glow map");
                }
            }
        }

        private static void AssertPartDressed(BlenderArtEntry entry, ModelArtDefinition art, ModelPart part, string objectName)
        {
            Assert.AreEqual(objectName, part.Object, $"{entry.Id}: part order");
            Assert.IsTrue(part.CastShadows, $"{objectName} casts no shadow");
            foreach (bool low in new[] { false, true })
            {
                Material material = part.MaterialFor(low);
                Assert.IsNotNull(material, $"{objectName}: no material (low {low})");
                Assert.AreNotSame(art.MaterialFor(low), material, $"{objectName} wears the body material (low {low})");
                Texture albedo = material.GetTexture("_BaseMap");
                Assert.IsNotNull(albedo, $"{objectName}: no albedo (low {low})");
                Assert.AreEqual($"{objectName}_Albedo", albedo.name, $"{objectName}: albedo (low {low})");
                string path = AssetDatabase.GetAssetPath(albedo);
                Assert.AreEqual(low, path.Contains("/Low/"), $"{objectName}: albedo {path} (low {low})");
                Assert.IsTrue(material.IsKeywordEnabled("_MASKMAP"), $"{objectName}: the material ignores the mask (low {low})");
            }

            Assert.AreNotSame(part.MaterialFor(false), part.MaterialFor(true), $"{objectName}: no Low material");
            foreach (GameObject model in new[] { art.Model, art.Lod1 })
            {
                if (model == null)
                {
                    continue;
                }

                bool owned = false;
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    owned |= part.Owns(renderer.name);
                }

                Assert.IsTrue(owned, $"{model.name} has no {objectName} renderer");
            }
        }

        // Round 18: Gravemaw's spine rode paths whose uniform spline ran backwards (and whose inside corners were
        // tighter than the spine rides above them), so spine, head and limbs flipped 130-180 degrees in one frame.
        // Every clip that rides such a path is checked on every bone that shows (hidden debris collapses to scale 0).
        // Round 19: the gate clips too (the spades spun about themselves with the elbow plane, ~115 degrees in three
        // frames, and a leg that let go of a hold jumped to its scrabble in one frame, 60-90 degrees).
        [Test]
        public void Gravemaw_PathClips_NeverFlipAVisibleBone()
        {
            const float MaxDegreesPerFrame = 60f;
            string[] clips =
            {
                "Emerge", "Burrow_Down", "Climb_Up", "Climb_Down", "Dive_Into_Liquid", "Surface_Burst", "Surface_Throw",
                "Gate_Mount", "Gate_Straddle_Loop", "Gate_Dismount",
            };
            AssertArtClipsSmooth("Gravemaw", clips, null, MaxDegreesPerFrame);
        }

        // Round 18: the Frost Warden's fists turned by a straight lerp between nearly opposite directions (110-140
        // degrees in one frame). The hammer blows stay fast (up to ~75 degrees a frame); a flip is far beyond that.
        [Test]
        public void FrostWarden_Fists_TurnWithoutAFlip()
        {
            AssertArtClipsSmooth("FrostWarden", new[] { "FreezePulse", "Shatter_Through", "Dive_Into_Liquid" }, "Arm_", 90f);
        }

        // Round 18: the Bulwark's chop dropped the cocked sword arm 108 degrees in one frame.
        [Test]
        public void Bulwark_Strike_SwingsTheSwordWithoutAPop()
        {
            AssertArtClipsSmooth("Bulwark", new[] { "Strike" }, "UpperArm_", 60f);
        }

        /// <summary>Every named clip of the art, every bone whose name starts with the prefix (all when null) and that shows on both frames.</summary>
        private static void AssertArtClipsSmooth(string artName, string[] clipNames, string bonePrefix, float maxDegreesPerFrame)
        {
            const float FramesPerSecond = 30f;
            const float VisibleScale = 0.1f;
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"{ArtFolder}/{artName}.asset");
            var model = Object.Instantiate(art.Model);
            try
            {
                var bones = new System.Collections.Generic.List<Transform>();
                foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
                {
                    if (node != model.transform && (bonePrefix == null || node.name.StartsWith(bonePrefix)))
                    {
                        bones.Add(node);
                    }
                }

                int checkedClips = 0;
                foreach (ModelClip clip in art.Clips)
                {
                    if (clip.Clip == null || System.Array.IndexOf(clipNames, clip.Clip.name) < 0)
                    {
                        continue;
                    }

                    checkedClips++;
                    int frames = Mathf.RoundToInt(clip.Clip.length * FramesPerSecond);
                    var previous = new Quaternion[bones.Count];
                    var shown = new bool[bones.Count];
                    for (int f = 0; f <= frames; f++)
                    {
                        clip.Clip.SampleAnimation(model, Mathf.Min(f / FramesPerSecond, clip.Clip.length));
                        for (int b = 0; b < bones.Count; b++)
                        {
                            bool shows = bones[b].lossyScale.x > VisibleScale;
                            if (f > 0 && shows && shown[b])
                            {
                                Assert.Less(Quaternion.Angle(previous[b], bones[b].localRotation), maxDegreesPerFrame, $"{artName} {clip.Clip.name}: {bones[b].name} jumps at frame {f}");
                            }

                            previous[b] = bones[b].localRotation;
                            shown[b] = shows;
                        }
                    }
                }

                Assert.AreEqual(clipNames.Length, checkedClips, $"{artName}: not every clip is in the art");
            }
            finally
            {
                Object.DestroyImmediate(model);
            }
        }
    }
}
