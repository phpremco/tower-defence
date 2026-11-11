using System.Collections.Generic;
using Game.Core;
using Game.Editor;
using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 39, Remco's notes on the opening cutscene: Spencer turns at a person's pace (never a snap) and steps out of the
    /// capsule with a clip named in data, the brood stays until the gate dialogue is read, drones stand on the ground with
    /// every foot, and no brittle decoration (crystals) stands buried in a rock.
    /// </summary>
    public sealed class OpeningStagingTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const float TurnDegreesPerSecond = 240f;
        private const float Tolerance = 0.001f;

        private CampaignCatalog _catalog;
        private readonly List<Object> _made = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(_catalog, "run Tools/Generate/Scenes and Starter Data");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                if (made != null)
                {
                    Object.DestroyImmediate(made);
                }
            }

            _made.Clear();
        }

        [Test]
        public void ATurn_TakesThePersonsTime_TheShortWayRound_WithoutASnap()
        {
            float half = CutsceneMath.TurnSeconds(180f, TurnDegreesPerSecond);
            Assert.That(half, Is.InRange(0.6f, 0.9f), "a half turn should take 0.6-0.9 s");
            Assert.AreEqual(0f, CutsceneMath.EasedTurn(0f, 180f, 0f, TurnDegreesPerSecond), Tolerance);
            Assert.AreEqual(90f, CutsceneMath.EasedTurn(0f, 180f, half * 0.5f, TurnDegreesPerSecond), Tolerance);
            Assert.AreEqual(180f, CutsceneMath.EasedTurn(0f, 180f, half, TurnDegreesPerSecond), Tolerance);
            // From 350 to 10 degrees is a 20 degree turn through north, never 340 the long way.
            float step = CutsceneMath.EasedTurn(350f, 10f, 0.05f, TurnDegreesPerSecond);
            Assert.That(Mathf.DeltaAngle(350f, step), Is.InRange(0f, 20f));

            // No frame jumps more than a person turns: 60 frames a second, never more than twice the average rate.
            float last = 0f;
            for (float t = 0f; t <= half; t += 1f / 60f)
            {
                float yaw = CutsceneMath.EasedTurn(0f, 180f, t, TurnDegreesPerSecond);
                Assert.LessOrEqual(Mathf.Abs(yaw - last), TurnDegreesPerSecond * 2f / 60f + Tolerance);
                last = yaw;
            }
        }

        [Test]
        public void TheCapsuleExit_IsAClipNamedInData_TheWalkStandsInWithoutIt()
        {
            CutsceneCast cast = _catalog.Story.OpeningCutscene.Cast;
            Assert.AreEqual(ModelClipRole.CapsuleExit, cast.CapsuleExitClip, "the cast does not name the exit clip");
            Assert.IsTrue(BlenderStoryCatalog.Roles.TryGetValue("Capsule_Exit", out ModelClipRole role) && role == ModelClipRole.CapsuleExit, "the Blender take Capsule_Exit has no role, so its import would be dropped");
            Assume.That(cast.Hero != null && cast.Hero.Art != null && cast.Hero.Art.Body != null, "no imported commander");

            var parent = new GameObject("ExitTest");
            _made.Add(parent);
            var rig = new CutsceneHeroRig(parent.transform, cast.Hero);
            bool hasClip = cast.Hero.Art.Body.IndexOf(ModelClipRole.CapsuleExit) >= 0;
            // Until the Blender take arrives the length is zero and FieldSet walks him out instead.
            Assert.AreEqual(hasClip, rig.ClipLength(ModelClipRole.CapsuleExit) > 0f);
        }

        [Test]
        public void TheBrood_HoldsUntilTheGateDialogueIsRead_ThenRetreatsInTheHandoff()
        {
            CutsceneDefinition cutscene = _catalog.Story.OpeningCutscene;
            int handoff = IndexOf(cutscene, CutsceneAction.Handoff);
            CutsceneShot stand = cutscene.Shots[handoff - 1];
            Assert.AreEqual(CutsceneAction.GateStand, stand.Action, "the gate stand no longer leads into the handoff");
            Assert.IsNotNull(stand.Dialogue, "the gate stand has no dialogue to wait for");
            Assert.AreEqual(ShotTiming.AfterDialogue, stand.Timing);
            Assert.LessOrEqual(stand.Seconds, 1f, "the retreat should follow the click, not seconds after it");

            // Every other dialogue is read at once; the gate's stays open. However long, the handoff (the retreat) waits.
            CutsceneSequencer reading = null;
            reading = new CutsceneSequencer(cutscene.Shots, _ => { }, () => reading.ShotIndex == handoff - 1);
            reading.Begin();
            for (int i = 0; i < 3600 && !reading.IsFinished; i++)
            {
                reading.Tick(1f);
            }

            Assert.AreEqual(handoff - 1, reading.ShotIndex, "the handoff began while the gate dialogue was still open");

            // The swarm itself holds without a retreat time, at any clock, and runs only after one is set.
            LevelDefinition field = cutscene.FieldLevel(_catalog);
            var root = new GameObject("SwarmTest");
            _made.Add(root);
            // Built through Activator: the constructor's volume factory type lives in an assembly the tests do not reference.
            var context = (CutsceneContext)System.Activator.CreateInstance(typeof(CutsceneContext), cutscene, field, _catalog, null, null, 1f, false, null);
            var swarm = new FieldSwarm(root.transform, new FieldLayout(field), context);
            Assert.IsTrue(float.IsPositiveInfinity(swarm.RetreatClock));
            Assert.IsTrue(swarm.Holding(10000f), "the brood turned away without a retreat being called");
            swarm.SetRetreat(40f);
            Assert.IsTrue(swarm.Holding(40f), "they turned before the last roar");
            Assert.IsFalse(swarm.Holding(45f), "they never turned away after the roar");
            swarm.SetRetreat(float.PositiveInfinity);
            Assert.IsTrue(swarm.Holding(45f), "holding again did not take the retreat back");
        }

        [Test]
        public void AParkedDrone_StandsOnTheGround_WithEveryFoot_OnASlope()
        {
            CutsceneCast cast = _catalog.Story.OpeningCutscene.Cast;
            Assume.That(cast.HaulerArt != null || cast.Hauler != null, "no hauler in the cast");
            var holder = new GameObject("Hauler").transform;
            _made.Add(holder.gameObject);
            holder.localScale = Vector3.one * 1.6f;
            var drone = new CutsceneDrone(holder, cast.HaulerArt, cast.Hauler);
            Vector3[] feet = drone.Feet();
            Assert.Greater(feet.Length, 0, "the hauler has no feet to stand on");

            // A slope rising 0.3 m per metre along x and 0.1 along z, turned 37 degrees.
            float Ground(float x, float z) => 0.3f * x + 0.1f * z + 2f;
            Quaternion facing = Quaternion.Euler(0f, 37f, 0f);
            Vector3 rest = drone.RestOn(new Vector3(4f, 0f, -2f), facing, Ground);
            float lowest = float.PositiveInfinity;
            foreach (Vector3 foot in feet)
            {
                Vector3 world = rest + facing * Vector3.Scale(foot, holder.localScale);
                float clearance = world.y - Ground(world.x, world.z);
                Assert.GreaterOrEqual(clearance, -Tolerance, "a foot sinks into the slope");
                lowest = Mathf.Min(lowest, clearance);
            }

            Assert.AreEqual(0f, lowest, Tolerance, "the drone hovers over the slope instead of standing on it");
        }

        [Test]
        public void ACrystalBuriedInABoulder_IsLeftOut_OneBesideItStays()
        {
            var host = new GameObject("Instancer");
            _made.Add(host);
            EnvironmentInstancer instancer = EnvironmentInstancer.Create(host.transform);
            Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _made.Add(material);
            EnvironmentMesh boulder = new EnvironmentMesh("Boulder", cube, material, null, true, Matrix4x4.identity).WithContact(DecorContact.Climbable, default);
            EnvironmentMesh crystal = new EnvironmentMesh("Crystal", cube, material, null, true, Matrix4x4.identity).WithContact(DecorContact.Breakable, default);
            instancer.AddDecor(boulder, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * 2f), false);
            instancer.AddDecor(crystal, Matrix4x4.TRS(new Vector3(0.2f, 0.3f, 0f), Quaternion.identity, Vector3.one * 0.5f), false);
            var glow = new GameObject("Glow").transform;
            glow.SetParent(host.transform, false);
            instancer.AttachToLast(glow);
            instancer.AddDecor(crystal, Matrix4x4.TRS(new Vector3(3f, 0f, 0f), Quaternion.identity, Vector3.one * 0.5f), false);

            Assert.AreEqual(2, instancer.BreakableCount);
            Assert.AreEqual(1, instancer.LeaveOutBuriedBrittle(), "the buried crystal was not left out, or the free one went too");
            Assert.AreEqual(1, instancer.BreakableCount);
            Assert.IsTrue(glow == null, "the buried crystal's glow stayed behind");
            Assert.AreEqual(0, instancer.LeaveOutBuriedBrittle());
        }

        private static int IndexOf(CutsceneDefinition cutscene, CutsceneAction action)
        {
            for (int i = 0; i < cutscene.Shots.Count; i++)
            {
                if (cutscene.Shots[i].Action == action)
                {
                    return i;
                }
            }

            Assert.Fail($"the opening has no {action} shot");
            return -1;
        }
    }
}
