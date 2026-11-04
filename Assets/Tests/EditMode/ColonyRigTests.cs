using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The colony's drilling rig stands beside every Crystal Plains field: outside the grid (so off every road and build
    /// pad), on screen in the zoomed-out view, clear of every structure's ground, and kept free of decor.
    /// </summary>
    public sealed class ColonyRigTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

        [Test]
        public void EveryCampaignLevelWithARig_FindsItAFreeVisibleSpotBesideTheField()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            int checkedLevels = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                LevelDefinition level = node.Level;
                ColonyRigDefinition rig = level != null && level.Theme.Environment != null ? level.Theme.Environment.ColonyRig : null;
                if (rig == null)
                {
                    continue;
                }

                checkedLevels++;
                var clearance = new StructureClearance(level);
                Assert.IsTrue(clearance.HasColonyRig, $"{level.name}: the rig found no spot");
                Vector3 at = clearance.ColonyRigPosition;
                Assert.IsTrue(ColonyRigPlacement.OutsideField(at, level.WorldSize * 0.5f, rig.ClearRadius), $"{level.name}: the rig at {at} reaches into the field");
                Assert.IsTrue(FixedCameraRig.InView(level, at, ColonyRigPlacement.ReferenceAspect, 0f), $"{level.name}: the rig at {at} is off screen");
                Assert.IsTrue(FixedCameraRig.InView(level, at + Vector3.up * rig.Height, ColonyRigPlacement.ReferenceAspect, 0f), $"{level.name}: the rig's top is off screen");
                foreach (StructureClearance.Zone zone in clearance.Zones)
                {
                    if (zone.Name == "Colony rig")
                    {
                        continue;
                    }

                    float distance = Vector2.Distance(new Vector2(at.x, at.z), new Vector2(zone.Centre.x, zone.Centre.z));
                    Assert.GreaterOrEqual(distance, zone.Radius + rig.ClearRadius - 1e-3f, $"{level.name}: the rig overlaps {zone.Name}");
                }

                Assert.IsTrue(clearance.Blocks(at, 0f), $"{level.name}: decor is not kept off the rig");
            }

            Assert.Greater(checkedLevels, 0, "no level wears a colony rig: run Tools/Generate/Colony Rig");
        }
    }
}
