using Game.Core;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The imported tower models as data (import stage 2): every tower wears a look for each level, level 3 shows path
    /// A by default with path B recorded beside it, every look has its materials and an idle cycle, the guns have a shot
    /// origin and a wreck with their own debris, and our colours follow the models (curator decision 2026-11-04).
    /// </summary>
    public sealed class TowerArtTests
    {
        private const string TowerFolder = "Assets/_Project/Data/Towers";

        [Test]
        public void EveryTower_WearsALookForEachLevel()
        {
            int modelled = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:TowerDefinition", new[] { TowerFolder }))
            {
                var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                TowerArtDefinition art = tower.Art;
                Assert.IsNotNull(art, $"{tower.name}: no imported look");
                modelled++;
                for (int level = 0; level <= tower.MaxLevelIndex; level++)
                {
                    ModelArtDefinition look = art.ArtFor(level);
                    Assert.IsNotNull(look, $"{tower.name}: no look for level {level + 1}");
                    Assert.IsNotNull(look.Model, $"{tower.name} level {level + 1}: no model");
                    Assert.IsTrue(look.Material.IsKeywordEnabled("_MASKMAP"), $"{tower.name}: the material ignores the mask");
                    Assert.IsFalse(look.LowMaterial.IsKeywordEnabled("_DETAIL2"), $"{tower.name}: the Low material keeps the second detail");
                    Assert.GreaterOrEqual(look.IndexOf(ModelClipRole.Idle), 0, $"{tower.name} level {level + 1}: no idle cycle");
                    if (level > 0)
                    {
                        Assert.AreNotSame(art.ArtFor(level - 1), look, $"{tower.name}: level {level + 1} looks like the level before");
                    }
                }

                if (!tower.BlocksGroundEnemies)
                {
                    Assert.IsNotNull(art.Wreck, $"{tower.name}: no wreck");
                    Assert.IsNotEmpty(art.Debris, $"{tower.name}: no debris");
                }
            }

            Assert.Greater(modelled, 10, "the Blender towers are not wired to the tower definitions");
        }

        [Test]
        public void LevelThree_ShowsPathA_AndKeepsPathB()
        {
            TowerArtDefinition gun = Load("PulseRifle").Art;
            Assert.AreEqual(TowerArtPath.A, gun.DefaultPath);
            StringAssert.EndsWith("_T3A", gun.ArtFor(2).name);
            StringAssert.EndsWith("_T3B", gun.ArtFor(2, TowerArtPath.B).name);
            Assert.AreSame(gun.ArtFor(1), gun.ArtFor(1, TowerArtPath.B), "levels 1 and 2 are shared by both paths");
            StringAssert.EndsWith("_T3", Load("Artillery").Art.ArtFor(2, TowerArtPath.B).name, "a hybrid has one level-3 look");
        }

        [Test]
        public void ShotOrigins_FollowTheRig()
        {
            Assert.AreEqual("Arc", Load("Tesla").Art.MuzzlePrefix, "lightning leaves from the arc prongs");
            Assert.AreEqual(TowerMuzzleChoice.Nearest, Load("Tesla").Art.MuzzleChoice);
            Assert.AreEqual(TowerAimMode.None, Load("Support").Art.Aim);
            Assert.AreEqual(TowerAimMode.YawOnly, Load("Rocket").Art.Aim, "the Flak keeps its modelled elevation");
            Assert.GreaterOrEqual(Load("Laser").Art.ArtFor(0).IndexOf(ModelClipRole.Charge), 0, "the laser winds up");
            Assert.GreaterOrEqual(Load("Laser").Art.ArtFor(0).IndexOf(ModelClipRole.Firing), 0, "the laser has its beam cycle");
            Assert.GreaterOrEqual(Load("PulseRifle").Art.ArtFor(1).IndexOf(ModelClipRole.Fire, "Muzzle_2"), 0, "the twin gun recoils per barrel");
        }

        // Curator decision 2026-11-04: our colours follow the models; warm is for the aliens.
        [Test]
        public void ColonyColours_FollowTheModels()
        {
            Color laser = Load("Laser").Color;
            Assert.Greater(laser.b, laser.r, "the laser beam is cyan like its lens");
            Color support = Load("Support").Color;
            Assert.Greater(support.r, support.g * 1.1f, "the support glows signal yellow");
            Color artillery = Load("Artillery").Color;
            Assert.Greater(artillery.g, 0.7f, "the artillery glows amber, not orange");
        }

        private static TowerDefinition Load(string id) => AssetDatabase.LoadAssetAtPath<TowerDefinition>($"{TowerFolder}/{id}.asset");
    }
}
