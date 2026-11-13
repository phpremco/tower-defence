using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 60, Remco's notes after his first full playthrough: the mothership's bay doors are shut while it arrives and open
    /// only over the salvo; the quake rumble holds through the whole quake; the drill is heard only where it is seen; every
    /// drone card says its job; no spawn prop covers another from the play camera; the swarm species die quietly.
    /// </summary>
    public sealed class Round60Tests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string ShipPath = "Assets/_Project/Art/Models/Blender/Cutscene_Mothership/Cutscene_Mothership.fbx";
        private const string LevelFolder = "Assets/_Project/Data/Levels";
        private const string DroneFolder = "Assets/_Project/Data/Drones";
        private const string VoiceFolder = "Assets/_Project/Data/Audio/Voices";
        private const float Frame = 1f / 60f;
        // A door may turn at most this share of its swing in one frame at 60 fps (a smooth swing, never a pop).
        private const float MaxSwingPerFrame = 0.04f;
        // Shut doors meet in the bay's middle: each door's far edge lies this close to the keel line (metres, model space).
        private const float MeetTolerance = 0.06f;

        private static CutsceneDefinition Opening() => AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath).Story.OpeningCutscene;

        private static CutsceneShot ShotOf(CutsceneAction action, out int index)
        {
            CutsceneDefinition cutscene = Opening();
            for (index = 0; index < cutscene.Shots.Count; index++)
            {
                if (cutscene.Shots[index].Action == action)
                {
                    return cutscene.Shots[index];
                }
            }

            Assert.Fail($"the opening has no {action} shot");
            return default;
        }

        [Test]
        public void TheBayDoors_AreShutOnArrival_OpenForEveryRelease_AndShutAgain()
        {
            CutsceneCast cast = Opening().Cast;
            CutsceneShot launch = ShotOf(CutsceneAction.CapsulesFall, out _);
            float first = CapsuleDrop.ReleaseAt(0, cast.ReleaseGap);
            float last = CapsuleDrop.ReleaseAt(cast.CapsuleCount - 1, cast.ReleaseGap);
            Assert.AreEqual(0f, MothershipDoors.Opening(0f, first, last), "the doors must be shut as the launch shot opens (the arrival holds them shut)");
            for (int i = 0; i < cast.CapsuleCount; i++)
            {
                float release = CapsuleDrop.ReleaseAt(i, cast.ReleaseGap);
                Assert.IsTrue(MothershipDoors.FullyOpen(release, first, last), $"capsule {i + 1} leaves at {release:0.00} s through doors that are not fully open");
            }

            Assert.AreEqual(0f, MothershipDoors.Opening(launch.Seconds, first, last), "the doors stay open to the end of the shot");
            float previous = 0f;
            for (float t = 0f; t <= launch.Seconds; t += Frame)
            {
                float open = CutsceneMath.Smooth(MothershipDoors.Opening(t, first, last));
                Assert.Less(Mathf.Abs(open - previous), MaxSwingPerFrame, $"the doors jump at {t:0.000} s");
                previous = open;
            }
        }

        [Test]
        public void TheMainEngines_BurnWhileTheShipMoves_AndEaseOutAsItHolds()
        {
            Assert.Greater(OrbitSet.MainEngineThrottle(OrbitSet.ArrivalSeconds * 0.25f), 0.3f, "the engines burn while the ship comes in fast");
            Assert.AreEqual(0f, OrbitSet.MainEngineThrottle(OrbitSet.ArrivalSeconds), 1e-6f, "the engines are out once the ship holds");
            float previous = OrbitSet.MainEngineThrottle(0f);
            for (float t = Frame; t <= OrbitSet.ArrivalSeconds; t += Frame)
            {
                float throttle = OrbitSet.MainEngineThrottle(t);
                Assert.Less(Mathf.Abs(throttle - previous), MaxSwingPerFrame, $"the engines jump at {t:0.000} s");
                previous = throttle;
            }
        }

        [Test]
        public void TheImportedShip_HasTwoDoorsLiftedOffItsHull_ThatMeetWhenShut()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShipPath);
            GameObject ship = Object.Instantiate(prefab);
            try
            {
                var skin = ship.GetComponentInChildren<SkinnedMeshRenderer>(true);
                int before = skin.sharedMesh.triangles.Length;
                MothershipDoors doors = MothershipDoors.Build(ship.transform);
                Assert.IsNotNull(doors, "no doors found on the imported mothership (is the mesh readable?)");
                Assert.AreEqual(2, doors.Count);
                Assert.Less(skin.sharedMesh.triangles.Length, before, "the open doors were not taken off the hull");
                doors.Pose(0f);
                foreach (MeshFilter door in ship.GetComponentsInChildren<MeshFilter>(true))
                {
                    // The door's own space is the hull mesh's space about the hinge; its far edge must reach the keel line.
                    Transform frame = door.transform.parent.parent;
                    float nearest = float.MaxValue;
                    foreach (Vector3 vertex in door.sharedMesh.vertices)
                    {
                        Vector3 inHull = frame.InverseTransformPoint(door.transform.TransformPoint(vertex));
                        nearest = Mathf.Min(nearest, Mathf.Abs(inHull.x));
                    }

                    Assert.Less(nearest, MeetTolerance, $"{door.transform.parent.name}: a shut door does not reach the middle of the bay");
                }
            }
            finally
            {
                // The stripped hull and the doors are meshes of their own, made for this test.
                foreach (SkinnedMeshRenderer skin in ship.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (!AssetDatabase.Contains(skin.sharedMesh))
                    {
                        Object.DestroyImmediate(skin.sharedMesh);
                    }
                }

                foreach (MeshFilter door in ship.GetComponentsInChildren<MeshFilter>(true))
                {
                    Object.DestroyImmediate(door.sharedMesh);
                }

                Object.DestroyImmediate(ship);
            }
        }

        [Test]
        public void TheQuake_HoldsItsRumble_FromTheCaveToTheBurrows_AndGlidesInAndOut()
        {
            CutsceneDefinition cutscene = Opening();
            ShotOf(CutsceneAction.CrystalsTremble, out int from);
            ShotOf(CutsceneAction.SwarmPours, out int to);
            Assert.AreEqual(0f, cutscene.Shots[from - 1].QuakeHold, "the quake starts in the cave");
            for (int i = from; i <= to; i++)
            {
                Assert.GreaterOrEqual(cutscene.Shots[i].QuakeHold, 0.5f, $"shot {i + 1} ({cutscene.Shots[i].Action}) lets the rumble dip mid-quake");
                Assert.GreaterOrEqual(ShakeSound.Level(0f, 0f, 0f, cutscene.Shots[i].ShakeRumble, cutscene.Shots[i].QuakeHold), cutscene.Shots[i].QuakeHold);
            }

            Assert.AreEqual(0f, cutscene.Shots[to + 1].QuakeHold, "the quake ends with the burrows");
            Assert.AreEqual(0f, ShakeSound.Hold(0f, 0.8f, 0f), "the rumble glides in from silence");
            Assert.AreEqual(0.4f, ShakeSound.Hold(0f, 0.8f, ShakeSound.HoldGlideSeconds * 0.5f), 1e-4f);
            Assert.AreEqual(0.8f, ShakeSound.Hold(0.8f, 0f, 0f), 1e-6f, "a cut keeps the rumble where it was");
            Assert.AreEqual(0f, ShakeSound.Hold(0.8f, 0f, ShakeSound.HoldGlideSeconds), 1e-6f, "and it glides out after the quake");
        }

        [Test]
        public void TheDrill_IsHeardOnlyInThePictureAndNearTheLens()
        {
            Assert.AreEqual(1f, FieldDrill.Presence(new Vector3(0.5f, 0.5f, 10f), 10f), 1e-4f, "framed and near: full");
            Assert.AreEqual(0f, FieldDrill.Presence(new Vector3(0.5f, 0.5f, -5f), 5f), "behind the lens: silent");
            Assert.AreEqual(0f, FieldDrill.Presence(new Vector3(1.4f, 0.5f, 10f), 10f), "well off the side of the picture: silent");
            Assert.AreEqual(0f, FieldDrill.Presence(new Vector3(0.5f, 0.5f, 60f), 60f), "far away: silent");
            Assert.AreEqual(0.5f, FieldDrill.Presence(new Vector3(1f, 0.5f, 10f), 10f), 1e-4f, "on the frame's edge: half");
        }

        [Test]
        public void EveryDroneCard_SaysItsJob()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:DroneDefinition", new[] { DroneFolder }))
            {
                var drone = AssetDatabase.LoadAssetAtPath<DroneDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsFalse(string.IsNullOrWhiteSpace(drone.Role), $"{drone.name} has no role");
                Assert.LessOrEqual(drone.Role.Length, DroneDefinition.MaxRoleLength, $"{drone.name}'s role does not fit its card");
                Assert.IsFalse(string.IsNullOrWhiteSpace(drone.Summary), $"{drone.name} has no summary");
            }
        }

        [Test]
        public void NoSpawnProp_CoversAnother_FromThePlayCamera()
        {
            int checkedLevels = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }))
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                List<LevelValidator.SpawnProp> props = LevelValidator.SpawnProps(level);
                for (int a = 0; a < props.Count; a++)
                {
                    for (int b = a + 1; b < props.Count; b++)
                    {
                        float apart = Vector2.Distance(props[a].Seen, props[b].Seen);
                        Assert.GreaterOrEqual(apart, props[a].Radius + props[b].Radius, $"{level.name}: {props[a].Name} covers {props[b].Name}");
                    }
                }

                checkedLevels += props.Count > 0 ? 1 : 0;
            }

            Assert.Greater(checkedLevels, 5, "the campaign's levels have living spawn props");
        }

        [Test]
        public void APodHoveringBesideARoadMouth_ShowsOverIt()
        {
            // Shard Ridge before round 60: the south pod 1.6 m up over (-19.5, -10.5), the second road's mound at (-19.5, -9).
            Vector3 view = Quaternion.Euler(44f, 0f, 0f) * Vector3.forward;
            Vector2 pod = LevelValidator.SeenOnGround(new Vector3(-19.5f, 1.6f, -10.5f), view);
            Vector2 mound = LevelValidator.SeenOnGround(new Vector3(-19.5f, 0f, -9f), view);
            Assert.Less(Vector2.Distance(pod, mound), 0.5f, "the camera shows the pod right over the mound");
        }

        [Test]
        public void TheSwarmSpecies_CryQuietly_AndNotOnEveryDeath()
        {
            foreach (string species in new[] { "Skitter", "FireLarva", "RotSporePod" })
            {
                var voice = AssetDatabase.LoadAssetAtPath<CreatureVoice>($"{VoiceFolder}/Voice_{species}.asset");
                Assert.LessOrEqual(voice.DeathVolume, 0.5f, $"{species} dies too loud for a swarm");
                Assert.GreaterOrEqual(voice.DeathInterval, 0.3f, $"{species} may cry on every death of a swarm");
            }

            var single = AssetDatabase.LoadAssetAtPath<CreatureVoice>($"{VoiceFolder}/Voice_Bulwark.asset");
            Assert.AreEqual(1f, single.DeathVolume, "a lone heavy keeps its full cry");
        }
    }
}
