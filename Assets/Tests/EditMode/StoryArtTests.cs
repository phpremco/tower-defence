using Game.Core;
using Game.Hero;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// The story's imported models as data (import stage 3b): the hero wears Mercer with every clip his view plays, the
    /// upper-body clips marked and masked to a bone the rig has, his weapons in grip space and the sockets they ride in;
    /// every speaker but the hero has a bust; the cutscene cast wears the machines and drones with the clips and sockets
    /// the sets use; and the base carries its timelapse order for the cutscene.
    /// </summary>
    public sealed class StoryArtTests
    {
        private const string DataFolder = "Assets/_Project/Data";

        [Test]
        public void Hero_WearsMercer_WithHisClipsSocketsAndWeapons()
        {
            int heroes = 0;
            foreach (HeroDefinition hero in Load<HeroDefinition>())
            {
                heroes++;
                HeroArtDefinition art = hero.Art;
                Assert.IsNotNull(art, $"{hero.name}: no imported look");
                ModelArtDefinition body = art.Body;
                Assert.IsNotNull(body, $"{hero.name}: no body");
                ModelClipRole[] roles =
                {
                    ModelClipRole.Idle, ModelClipRole.Walk, ModelClipRole.Move, ModelClipRole.Shoot, ModelClipRole.Reload, ModelClipRole.Slash,
                    ModelClipRole.Ability, ModelClipRole.Repair, ModelClipRole.Hit, ModelClipRole.Down, ModelClipRole.Revive,
                };
                foreach (ModelClipRole role in roles)
                {
                    Assert.GreaterOrEqual(body.IndexOf(role), 0, $"{body.name}: no {role} clip");
                }

                foreach (ModelClipRole role in new[] { ModelClipRole.Shoot, ModelClipRole.Reload, ModelClipRole.Slash })
                {
                    Assert.IsTrue(body.Clips[body.IndexOf(role)].UpperBody, $"{body.name}: {role} does not play on the upper body");
                }

                Assert.Greater(body.Clips[body.IndexOf(ModelClipRole.Move)].DistancePerLoop, 0f, "the run has no stride length");
                Assert.Greater(body.Clips[body.IndexOf(ModelClipRole.Down)].EventSeconds, 0f, "the fall has no ground moment");
                WithStill(body, model =>
                {
                    Assert.IsNotNull(model.Find(body.UpperBodyBone), $"no upper-body bone {body.UpperBodyBone}");
                    foreach (string socket in new[] { art.HandSocket, art.HolsterSocket, art.BackSocket, art.PackBone })
                    {
                        Assert.IsNotNull(model.Find(socket), $"no socket {socket}");
                    }

                    foreach (string bone in art.AimBones)
                    {
                        Assert.IsNotNull(model.Find(bone), $"no aim bone {bone}");
                    }

                    // The back socket (the hilt on the pack) sits behind the figure, so the model faces its owner's +Z.
                    Assert.Less(model.Find(art.BackSocket).position.z, -0.05f, "Mercer does not face +Z");
                });

                foreach (ModelArtDefinition weapon in new[] { art.Pistol, art.SwordHilt, art.SwordBlade })
                {
                    Assert.IsNotNull(weapon, $"{hero.name}: a weapon is missing");
                    Assert.AreEqual(0f, weapon.Yaw, $"{weapon.name}: a weapon is modelled in grip space and stays unturned");
                }

                // Round 51: the imported pistol's barrel runs along -Z of its socket; the muzzle lies out along the art's barrel axis.
                Assert.Greater(Vector3.Dot(art.Muzzle, art.BarrelAxis), 0f, "the muzzle lies out along the barrel");
                Assert.IsNotNull(art.Bust, $"{hero.name}: no portrait bust");
            }

            Assert.Greater(heroes, 0, "no hero definitions");
        }

        [Test]
        public void Speakers_HaveImportedBusts_ExceptTheHeroWhoseBustFollowsHisArt()
        {
            int speakers = 0;
            foreach (SpeakerDefinition speaker in Load<SpeakerDefinition>())
            {
                speakers++;
                if (speaker.Portrait == PortraitModel.Hero)
                {
                    Assert.IsNull(speaker.PortraitArt, $"{speaker.name}: the hero's bust comes from the hero's art");
                    continue;
                }

                PortraitArtDefinition bust = speaker.PortraitArt;
                Assert.IsNotNull(bust, $"{speaker.name}: no imported bust");
                Assert.IsNotNull(bust.Model, $"{bust.name}: no model");
                Assert.Greater(bust.Zoom, 0f);
            }

            Assert.Greater(speakers, 0, "no speakers");
        }

        [Test]
        public void CutsceneCast_WearsTheMachinesAndDrones_WithTheirClipsAndSockets()
        {
            int casts = 0;
            foreach (CutsceneCast cast in Load<CutsceneCast>())
            {
                casts++;
                Check(cast.CapsuleArt, new[] { ModelClipRole.Land, ModelClipRole.Open }, new[] { "Exit" }, "_Shield");
                Check(cast.DrillArt, new[] { ModelClipRole.Deploy, ModelClipRole.Idle }, new[] { "String", "BitTip" }, null);
                Check(cast.MothershipArt, new[] { ModelClipRole.Idle }, new[] { "Bay", "Nozzle_1" }, null);
                Check(cast.BeaconArt, new[] { ModelClipRole.Deploy, ModelClipRole.Idle }, new[] { "Lamp" }, null);
                Check(cast.BuilderArt, new[] { ModelClipRole.Move, ModelClipRole.Work }, new[] { "Rotor_1", "Weld_L", "Weld_R" }, null);
                Check(cast.HaulerArt, new[] { ModelClipRole.Move }, new[] { "Rotor_1", "Cargo" }, "_Cargo");
                Check(cast.ConsoleArt, new ModelClipRole[0], new[] { "Radar" }, "_Screen_L");
            }

            Assert.Greater(casts, 0, "no cutscene cast");
        }

        [Test]
        public void BaseArt_ListsItsTimelapseModules_AsMeshesOfTheModel()
        {
            int bases = 0;
            foreach (VisualTheme theme in Load<VisualTheme>())
            {
                BaseArtDefinition art = theme.BaseArt;
                if (art == null)
                {
                    continue;
                }

                bases++;
                Assert.IsNotEmpty(art.TimelapseOrder, $"{art.name}: no timelapse order");
                WithStill(art.Model, model =>
                {
                    foreach (string module in art.TimelapseOrder)
                    {
                        Assert.Greater(model.RenderersEndingWith(module).Length, 0, $"{art.name}: no module mesh {module}");
                    }
                });
            }

            Assert.Greater(bases, 0, "no theme wears the base model");
        }

        private static void Check(ModelArtDefinition art, ModelClipRole[] roles, string[] sockets, string mesh)
        {
            Assert.IsNotNull(art, "a cutscene model is missing");
            foreach (ModelClipRole role in roles)
            {
                Assert.GreaterOrEqual(art.IndexOf(role), 0, $"{art.name}: no {role} clip");
            }

            WithStill(art, model =>
            {
                foreach (string socket in sockets)
                {
                    Assert.IsNotNull(model.Find(socket), $"{art.name}: no socket {socket}");
                }

                if (mesh != null)
                {
                    Assert.Greater(model.RenderersEndingWith(mesh).Length, 0, $"{art.name}: no mesh {mesh}");
                }
            });
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

        private static System.Collections.Generic.IEnumerable<T> Load<T>() where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { DataFolder }))
            {
                yield return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }
    }
}
