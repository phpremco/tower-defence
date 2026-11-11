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

        // Round 41: the rig stood on the rim's slope with its deck hanging over the downhill side; the ground under its
        // footprint is levelled now, so every point of it lies at one height.
        [Test]
        public void TheGroundUnderEveryColonyRig_IsLevel()
        {
            const float Tolerance = 0.02f;
            const int Rings = 4;
            const int Spokes = 16;
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            int checkedLevels = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                LevelDefinition level = node.Level;
                ColonyRigDefinition rig = level != null && level.Theme.Environment != null ? level.Theme.Environment.ColonyRig : null;
                var clearance = rig != null ? new StructureClearance(level) : null;
                if (clearance == null || !clearance.HasColonyRig)
                {
                    continue;
                }

                checkedLevels++;
                var field = new TerrainField(level, new RoadShape(level));
                Vector3 at = clearance.ColonyRigPosition;
                float centre = field.HeightAt(at.x, at.z);
                // The bilinear sample between grid points blends in a sliver of the slope at the rim of the pad.
                float reach = rig.FootprintRadius - field.Step * 1.5f;
                for (int ring = 1; ring <= Rings; ring++)
                {
                    for (int spoke = 0; spoke < Spokes; spoke++)
                    {
                        float angle = spoke * Mathf.PI * 2f / Spokes;
                        float radius = reach * ring / Rings;
                        float x = at.x + Mathf.Cos(angle) * radius;
                        float z = at.z + Mathf.Sin(angle) * radius;
                        Assert.AreEqual(centre, field.HeightAt(x, z), Tolerance, $"{level.name}: the ground under the rig is not level at ({x:0.0}, {z:0.0})");
                    }
                }
            }

            Assert.Greater(checkedLevels, 0, "no level wears a colony rig: run Tools/Generate/Colony Rig");
        }
    }
}
