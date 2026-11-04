using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Playtest 2026-11-04: a rock stuck through the base of C1_01. Every campaign level is dressed as in play (the
    /// procedural props, the imported decor, scatter, cliffs and silhouettes) and no piece of decoration may reach
    /// into the footprint of the base, a road gate or the colony rig.
    /// </summary>
    public sealed class DecorClearanceTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

        [Test]
        public void NoDecor_ReachesIntoTheBaseOrAGate_OnAnyCampaignLevel()
        {
            var failures = new List<string>();
            foreach (LevelDefinition level in Levels())
            {
                var root = new GameObject($"DecorProbe_{level.name}");
                try
                {
                    List<Bounds> decor = Dress(level, root.transform);
                    Assert.IsNotEmpty(decor, $"{level.name}: no decoration was built at all");
                    CheckFootprints(level, decor, failures);
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void Clearance_KeepsTheBaseAndEveryGateFree_OnEveryCampaignLevel()
        {
            foreach (LevelDefinition level in Levels())
            {
                var clearance = new StructureClearance(level);
                Assert.IsTrue(clearance.Blocks(LevelVisualBuilder.BasePosition(level), 0f), $"{level.name}: the base stands outside its own clearance");
                foreach (BlockadeSpot gate in level.Blockades)
                {
                    Assert.IsTrue(clearance.Blocks(level.Road.CentreOf(gate.Cell), 0f), $"{level.name}: gate {gate.Cell} has no clearance");
                }
            }
        }

        // The same builders the level visuals run, with the instancer logging every decoration piece it is given.
        private static List<Bounds> Dress(LevelDefinition level, Transform root)
        {
            var decor = new List<Bounds>();
            VisualTheme theme = level.Theme;
            var field = new TerrainField(level, new RoadShape(level));
            EnvironmentInstancer instancer = theme.Environment != null ? EnvironmentInstancer.Create(root) : null;
            if (instancer != null)
            {
                instancer.DecorLog = decor;
            }

            PropScatterBuilder.Build(level, theme, field, root, instancer);
            if (instancer != null)
            {
                EnvironmentKitBuilder.Build(level, theme.Environment, field, instancer, false);
            }

            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                decor.Add(renderer.bounds);
            }

            return decor;
        }

        // The footprints without the theme's margin: decor may come close, it may not reach in.
        private static void CheckFootprints(LevelDefinition level, List<Bounds> decor, List<string> failures)
        {
            float margin = level.Theme.DecorClearance;
            foreach (StructureClearance.Zone zone in new StructureClearance(level).Zones)
            {
                if (zone.Name != "Base" && zone.Name != "Colony rig" && !zone.Name.StartsWith("Gate"))
                {
                    continue;
                }

                float radius = zone.Radius - margin;
                foreach (Bounds bounds in decor)
                {
                    if (Touches(bounds, zone.Centre, radius))
                    {
                        failures.Add($"{level.name}: decor at {bounds.center} (size {bounds.size}) reaches into the {zone.Name} footprint (radius {radius:0.00} at {zone.Centre})");
                    }
                }
            }
        }

        // Flat test: the box's footprint on the ground against the circle.
        private static bool Touches(Bounds bounds, Vector3 centre, float radius)
        {
            float x = Mathf.Clamp(centre.x, bounds.min.x, bounds.max.x);
            float z = Mathf.Clamp(centre.z, bounds.min.z, bounds.max.z);
            float dx = centre.x - x;
            float dz = centre.z - z;
            return dx * dx + dz * dz < radius * radius;
        }

        private static List<LevelDefinition> Levels()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            var levels = new List<LevelDefinition>();
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level != null)
                {
                    levels.Add(node.Level);
                }
            }

            Assert.IsNotEmpty(levels, "no campaign level");
            return levels;
        }
    }
}
