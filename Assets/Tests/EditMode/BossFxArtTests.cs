using Game.Combat;
using Game.Core;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The round-29 boss art as data (round 5 boss look): the effect library carries the boss FX art; every ground mark wears
    /// the boss decal shader with both maps, every flipbook a sheet; the ruin gate has its three clips, the seal crystals
    /// their three variants, the ice block is see-through; both gate parts have a drained emission map; the colony rig has
    /// its wreck; the dampened overlay exists; the re-exported bosses carry the clips the round added.
    /// </summary>
    public sealed class BossFxArtTests
    {
        private const string LibraryPath = "Assets/_Project/Data/Vfx/VfxLibrary.asset";
        private const string RigPath = "Assets/_Project/Data/Art/Environment/ColonyRig_CrystalPlains.asset";

        private static BossFxArt Art()
        {
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
            Assert.IsNotNull(library, "no effect library");
            Assert.IsNotNull(library.BossFx, "the effect library carries no boss FX art");
            return library.BossFx;
        }

        [Test]
        public void EveryMark_WearsTheDecalShaderWithBothMaps()
        {
            BossFxArt art = Art();
            foreach (BossFxArt.Mark mark in System.Enum.GetValues(typeof(BossFxArt.Mark)))
            {
                Material material = art.Material(mark);
                Assert.IsNotNull(material, $"{mark}: no material");
                Assert.AreEqual("Game/BossDecal", material.shader.name, mark.ToString());
                Assert.IsNotNull(material.GetTexture("_BaseMap"), $"{mark}: no albedo");
                Assert.IsNotNull(material.GetTexture("_GlowMap"), $"{mark}: no glow pack");
                Assert.Greater(art.Width(mark), 0f, mark.ToString());
                Assert.Greater(art.TileLength(mark), 0f, mark.ToString());
            }
        }

        [Test]
        public void EverySheet_HasAFlipbookMaterial()
        {
            BossFxArt art = Art();
            foreach (BossFxArt.Sheet sheet in System.Enum.GetValues(typeof(BossFxArt.Sheet)))
            {
                Material material = art.Material(sheet);
                Assert.IsNotNull(material, $"{sheet}: no material");
                Assert.IsNotNull(material.GetTexture("_MainTex"), $"{sheet}: no sheet");
                Assert.Greater(art.FramesPerSecond(sheet), 0f, sheet.ToString());
            }
        }

        [Test]
        public void Props_HaveTheirClipsVariantsAndLook()
        {
            BossFxArt art = Art();
            Assert.IsNotNull(art.RuinGate, "no ruin gate");
            Assert.GreaterOrEqual(art.RuinGate.IndexOf(ModelClipRole.Closed), 0, "ruin gate: no Closed_Idle");
            Assert.GreaterOrEqual(art.RuinGate.IndexOf(ModelClipRole.Open), 0, "ruin gate: no Open");
            Assert.GreaterOrEqual(art.RuinGate.IndexOf(ModelClipRole.Idle), 0, "ruin gate: no Opened_Idle");
            Assert.IsNotNull(art.SealCrystals, "no seal crystals");
            Assert.AreEqual(3, art.SealCrystalParts.Length);
            Assert.IsNotNull(art.IceBlock, "no ice block");
            Assert.AreEqual((float)UnityEngine.Rendering.BlendMode.SrcAlpha, art.IceBlock.Material.GetFloat("_SrcBlend"), "the ice block is not see-through");
            Assert.IsNotNull(art.CrackSlabs, "no crack slabs");
            Assert.AreEqual(4, art.CrackSlabParts.Length);
            Assert.IsNotNull(art.Dampened, "no dampened overlay");
        }

        [Test]
        public void DrillWreck_CarriesItsCollapse_AsDeathWithTheImpactMoment()
        {
            var rig = AssetDatabase.LoadAssetAtPath<ColonyRigDefinition>(RigPath);
            int index = rig.Wreck.IndexOf(ModelClipRole.Death);
            Assert.GreaterOrEqual(index, 0, "the wreck has no collapse clip");
            Assert.Greater(rig.Wreck.Clips[index].EventSeconds, 0f, "the collapse has no impact moment");
        }

        [Test]
        public void GateParts_HaveDrainedMaps_AndTheRigItsWreck()
        {
            BossFxArt art = Art();
            foreach (string lit in new[] { "Gate_Emission", "Tower_GateGun_Emission" })
            {
                var texture = new Texture2D(1, 1) { name = lit };
                Assert.IsNotNull(art.DrainedFor(texture), $"{lit}: no drained map");
                Object.DestroyImmediate(texture);
            }

            var rig = AssetDatabase.LoadAssetAtPath<ColonyRigDefinition>(RigPath);
            Assert.IsNotNull(rig, "no colony rig");
            Assert.IsNotNull(rig.Wreck, "the colony rig has no wreck");
        }

        [Test]
        public void ReexportedBosses_CarryTheRoundsClips()
        {
            var sealedOne = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>("Assets/_Project/Data/Art/SealedOne.asset");
            Assert.GreaterOrEqual(sealedOne.IndexOf(ModelClipRole.OnBaseLoop), 0, "Sealed One: no OnBase_Loop");
            foreach (string shard in new[] { "ShardRunner", "ShardCrawler" })
            {
                var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"Assets/_Project/Data/Art/{shard}.asset");
                Assert.GreaterOrEqual(art.IndexOf(ModelClipRole.ClimbUp), 0, $"{shard}: no Climb_Up");
                Assert.GreaterOrEqual(art.IndexOf(ModelClipRole.ClimbDown), 0, $"{shard}: no Climb_Down");
            }
        }
    }
}
