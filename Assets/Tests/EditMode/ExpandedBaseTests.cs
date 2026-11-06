using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 14: the expanded colony base (Blender round 37) as data and on every level: its parts for the radar, beacon,
    /// shuttle and drone bays are there with a lid per dock, its footprint and places are measured from the model, no tower
    /// can be built under it, its guns stand on its own mounts off the road, its docks lie inside it, and no gate, mine or
    /// nest of any level (campaign and arenas) lies under it.
    /// </summary>
    public sealed class ExpandedBaseTests
    {
        private const string BaseArtPath = "Assets/_Project/Data/Art/Colony/BaseArt.asset";
        private const string LevelFolder = "Assets/_Project/Data/Levels";

        // The old base reached 2.13 m round its middle; round 37's reaches 3.66 m.
        private const float MinimumHalfWidth = 2.5f;
        private const int Docks = 4;
        private const int Mounts = 2;

        [Test]
        public void BaseArt_HasItsAnimatedParts_AndALidPerDock()
        {
            BaseArtDefinition art = Art();
            var parent = new GameObject("BaseStill");
            try
            {
                ArtModel model = ArtModel.CreateStill(parent.transform, art.Model);
                Assert.Greater(model.RenderersEndingWith(art.TurningModule).Length, 0, "no radar dish");
                Assert.Greater(model.RenderersEndingWith(art.BeaconModule).Length, 0, "no beacon");
                Assert.Greater(model.RenderersEndingWith(art.ShuttleModule).Length, 0, "no shuttle");
                Assert.IsNotNull(model.Find(art.ExhaustSocket), "no shuttle exhaust socket");
                Transform[] docks = model.Numbered(art.DockSocketPrefix);
                Assert.AreEqual(Docks, docks.Length, "the bays have not four docks");
                for (int i = 0; i < docks.Length; i++)
                {
                    Assert.Greater(model.RenderersEndingWith($"{art.BayDoorPrefix}_{i + 1}").Length, 0, $"dock {i + 1} has no lid");
                }

                // Animated parts come upright: turning them about their own Y turns them about world up.
                foreach (Renderer dish in model.RenderersEndingWith(art.TurningModule))
                {
                    Assert.Greater(Vector3.Dot(dish.transform.up, Vector3.up), 0.999f, $"{dish.name} is not upright");
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }

            CollectionAssert.Contains(art.TimelapseOrder, art.ShuttleModule, "the timelapse misses the shuttle");
        }

        [Test]
        public void BaseArt_FootprintAndPlaces_AreMeasuredFromTheModel()
        {
            BaseArtDefinition art = Art();
            Assert.Greater(art.FootprintMax.x, MinimumHalfWidth, "the footprint is the old base's");
            Assert.Less(art.FootprintMin.x, -MinimumHalfWidth, "the footprint is the old base's");
            Assert.AreEqual(Mounts, art.TurretMounts.Length, "the base guns have no mounts");
            Assert.AreEqual(Docks, art.DockPoints.Length, "the drone docks were not measured");
            foreach (Vector3 dock in art.DockPoints)
            {
                Assert.IsTrue(art.Covers(dock, Vector3.zero, Quaternion.identity, 0f), $"dock {dock} lies outside the base");
            }
        }

        [Test]
        public void NoTower_CanBeBuiltUnderTheBase_AndItsGunsStandOffTheRoad()
        {
            foreach (LevelDefinition level in Levels())
            {
                BaseArtDefinition art = level.Theme.BaseArt;
                Assert.IsNotNull(art, $"{level.name}: no base art");
                var grid = new BuildGrid(level);
                GridMap map = level.Grid;
                Vector3 centre = LevelVisualBuilder.BasePosition(level);
                Quaternion facing = LevelVisualBuilder.BaseFacing(level);
                for (int x = 0; x < level.GridSize.x; x++)
                {
                    for (int y = 0; y < level.GridSize.y; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (art.Covers(map.CellToWorld(cell), centre, facing, 0f))
                        {
                            Assert.IsFalse(grid.CanBuild(cell), $"{level.name}: a tower can be built at {cell} under the base");
                        }
                    }
                }

                foreach (Vector3 mount in art.TurretMounts)
                {
                    Vector2Int cell = map.WorldToCell(BaseArtDefinition.ToWorld(mount, centre, facing));
                    Assert.IsFalse(level.Road.IsRoad(cell), $"{level.name}: a base gun stands on the road at {cell}");
                }
            }
        }

        [Test]
        public void EveryLevel_PassesTheBaseFootprintCheck()
        {
            var errors = new List<string>();
            var failures = new List<string>();
            foreach (LevelDefinition level in Levels())
            {
                LevelValidator.ValidateLayout(level, errors);
                foreach (string error in errors)
                {
                    if (error.Contains("under the base"))
                    {
                        failures.Add($"{level.name}: {error}");
                    }
                }
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        private static BaseArtDefinition Art()
        {
            var art = AssetDatabase.LoadAssetAtPath<BaseArtDefinition>(BaseArtPath);
            Assert.IsNotNull(art, "no base art: run Tools/PlayTest/Scripts/ApplyBase2.cs or the generator");
            return art;
        }

        // The campaign levels and the boss arenas: every level asset with a road.
        private static List<LevelDefinition> Levels()
        {
            var levels = new List<LevelDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (level != null && level.MainRoad.Length > 0 && level.Theme != null)
                {
                    levels.Add(level);
                }
            }

            Assert.IsNotEmpty(levels, "no level");
            return levels;
        }
    }
}
