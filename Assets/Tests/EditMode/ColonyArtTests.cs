using Game.Core;
using Game.Drones;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Our own side's imported models as data (import stage 3a): the gate wears a model per level with its clips, loose
    /// panels and wreck; every level's base has its modules, overlay and wreck; the resource node has its three stages;
    /// each drone and path object wears its model with the clips and sockets its view uses; the models face their owner's
    /// forward; and our colours follow the models (curator decision 2026-11-04).
    /// </summary>
    public sealed class ColonyArtTests
    {
        private const string DataFolder = "Assets/_Project/Data";

        [Test]
        public void Gate_WearsAModelPerLevel_WithItsClipsPanelsAndWreck()
        {
            int gates = 0;
            foreach (TowerDefinition tower in Load<TowerDefinition>())
            {
                if (!tower.BlocksGroundEnemies)
                {
                    continue;
                }

                gates++;
                GateArtDefinition art = tower.GateArt;
                Assert.IsNotNull(art, $"{tower.name}: no gate model");
                Assert.IsNotNull(art.Wreck, $"{tower.name}: no wreck");
                Assert.IsNotEmpty(art.LooseParts, $"{tower.name}: no panels fall off when damaged");
                for (int level = 0; level <= tower.MaxLevelIndex; level++)
                {
                    ModelArtDefinition look = art.ArtFor(level);
                    Assert.IsNotNull(look, $"{tower.name}: no model for level {level + 1}");
                    foreach (ModelClipRole role in new[] { ModelClipRole.Hit, ModelClipRole.Repair, ModelClipRole.Build, ModelClipRole.Death })
                    {
                        Assert.GreaterOrEqual(look.IndexOf(role), 0, $"{look.name}: no {role} clip");
                    }

                    if (level > 0)
                    {
                        Assert.AreNotSame(art.ArtFor(level - 1), look, $"{tower.name}: level {level + 1} looks like the level before");
                    }

                    WithStill(look, model =>
                    {
                        Assert.Greater(model.RenderersEndingWith("_Damage").Length, 0, $"{look.name}: no damage overlay");
                        foreach (string part in art.LooseParts)
                        {
                            Assert.IsNotNull(model.Find(part), $"{look.name}: no panel bone {part}");
                        }

                        // The gate's front faces its +Z (towards the base); the hero stands on the enemy side.
                        Assert.Less(model.Find("HeroPost").position.z, 0f, $"{look.name}: the hero post is on the base side");
                    });
                }
            }

            Assert.Greater(gates, 0, "no gate in the data");
        }

        [Test]
        public void EveryLevel_HasTheBase_AndTheResourceNode()
        {
            foreach (VisualTheme theme in Load<VisualTheme>("/Levels"))
            {
                BaseArtDefinition art = theme.BaseArt;
                Assert.IsNotNull(art, $"{theme.name}: no base model");
                Assert.IsNotNull(art.Damage, $"{theme.name}: no base damage overlay");
                Assert.IsNotNull(art.Wreck, $"{theme.name}: no base wreck");
                WithStill(art.Model, model =>
                {
                    Assert.Greater(model.RenderersEndingWith(art.TurningModule).Length, 0, $"no {art.TurningModule} module");
                    Assert.Greater(model.RenderersEndingWith(art.PulsingModule).Length, 0, $"no {art.PulsingModule} module");
                    Assert.IsNotNull(model.Find("Light_Core"), "no core light socket");
                    // The base turns its back to the road end (LevelVisualBuilder faces it with -along), so its door is +Z.
                    Assert.Greater(model.Find("HeroPost").position.z, 0f, "the base's door faces away from the road");
                });

                Assert.IsNotNull(theme.ResourceNodeArt, $"{theme.name}: no resource node model");
                WithStill(theme.ResourceNodeArt, model =>
                {
                    foreach (string stage in new[] { "_Full", "_Half", "_Empty" })
                    {
                        Assert.Greater(model.RenderersEndingWith(stage).Length, 0, $"the resource node has no {stage} stage");
                    }
                });
            }
        }

        [Test]
        public void EveryDrone_WearsItsModel_WithCyclesAndSockets()
        {
            int drones = 0;
            foreach (DroneDefinition drone in Load<DroneDefinition>())
            {
                drones++;
                ModelArtDefinition art = drone.Art;
                Assert.IsNotNull(art, $"{drone.name}: no model");
                foreach (ModelClipRole role in new[] { ModelClipRole.Idle, ModelClipRole.Move, ModelClipRole.Work })
                {
                    Assert.GreaterOrEqual(art.IndexOf(role), 0, $"{drone.name}: no {role} cycle");
                }

                var parent = new GameObject("DroneStill");
                try
                {
                    DroneArtRig rig = DroneArtRig.CreateStill(parent.transform, art);
                    Assert.Greater(rig.Rotors.Length, 0, $"{drone.name}: no rotors");
                    switch (drone.Kind)
                    {
                        case DroneKind.Attack:
                            Assert.AreEqual(2, rig.Muzzles.Length, "the attack drone has two guns");
                            Assert.IsNotNull(rig.Thruster, "the attack drone has no thruster");
                            // The guns point where the drone flies.
                            Assert.Greater(rig.Muzzles[0].position.z, 0f, "the attack drone flies backwards");
                            break;
                        default:
                            Assert.IsNotNull(rig.Tool, $"{drone.name}: no beam or drill socket");
                            break;
                    }

                    if (drone.Kind == DroneKind.Harvest)
                    {
                        Assert.Greater(rig.Cargo.Length, 0, "the harvest drone has no cargo pod");
                    }
                }
                finally
                {
                    Object.DestroyImmediate(parent);
                }
            }

            Assert.AreEqual(3, drones, "one drone per job");
        }

        [Test]
        public void EveryPathObject_WearsItsModel()
        {
            foreach (PathObjectDefinition pathObject in Load<PathObjectDefinition>())
            {
                ModelArtDefinition art = pathObject.Art;
                Assert.IsNotNull(art, $"{pathObject.name}: no model");
                Assert.IsTrue(art.Material.IsKeywordEnabled("_MASKMAP"), $"{pathObject.name}: the material ignores the mask");
                switch (pathObject.Look)
                {
                    case PathObjectLook.Mine:
                    case PathObjectLook.Chrono:
                        Assert.GreaterOrEqual(art.IndexOf(ModelClipRole.Deploy), 0, $"{pathObject.name}: does not set itself up");
                        Assert.GreaterOrEqual(art.IndexOf(ModelClipRole.Idle), 0, $"{pathObject.name}: no idle cycle");
                        break;
                    case PathObjectLook.GroundDrone:
                        Assert.GreaterOrEqual(art.IndexOf(ModelClipRole.Move), 0, "the ground drone does not drive");
                        break;
                    case PathObjectLook.LavaBall:
                        WithStill(art, model => Assert.IsNotNull(model.Find("Ball"), "the lava ball has no ball to roll"));
                        break;
                }
            }
        }

        [Test]
        public void OurColours_FollowTheModels()
        {
            foreach (PathObjectDefinition pathObject in Load<PathObjectDefinition>())
            {
                if (pathObject.Look == PathObjectLook.Mine)
                {
                    Assert.Greater(pathObject.Color.g, 0.6f, "the mine is red in the data; its model's ring is amber");
                }
            }

            foreach (DroneDefinition drone in Load<DroneDefinition>())
            {
                if (drone.Kind == DroneKind.Attack)
                {
                    Assert.Greater(drone.Color.b, drone.Color.r, "the attack drone is warm in the data; its model glows azure");
                }
            }
        }

        private static void WithStill(ModelArtDefinition art, System.Action<ArtModel> check)
        {
            var parent = new GameObject("Still");
            try
            {
                check(ArtModel.CreateStill(parent.transform, art));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static System.Collections.Generic.IEnumerable<T> Load<T>(string folder = "") where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { DataFolder + folder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
    }
}
