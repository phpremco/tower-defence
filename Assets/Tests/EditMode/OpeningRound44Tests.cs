using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 44, Remco's notes on the opening: footsteps come from his feet, one launch after the mothership flies in, every
    /// shake has its rumble, no creature is heard that the shot does not show, and the burrow and base-building sounds have
    /// their slots.
    /// </summary>
    public sealed class OpeningRound44Tests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string HeroArtPath = "Assets/_Project/Data/Art/Story/MercerArt.asset";
        private const float Fps = 30f;
        // Capsule_Exit's own events (Mercer_report.json): he stands at frame 66 of 90 and only looks round after it.
        private const float ExitStandsAt = 66f / 30f;
        private const float Tolerance = 0.05f;

        private static readonly CutsceneSound[] Steps = { CutsceneSound.StepMetal, CutsceneSound.StepGravel, CutsceneSound.StepStone, CutsceneSound.HeroStep };
        private static readonly CutsceneSound[] CreatureVoices = { CutsceneSound.Growl, CutsceneSound.Scream, CutsceneSound.QueenRoar, CutsceneSound.HeavySteps, CutsceneSound.Emerge };

        // The shots whose sets shake something: the launch's booster, the landing's fall, the drill biting, the trembling cave,
        // the shivering ground, the bursting and closing burrows.
        private static readonly CutsceneAction[] Shaking =
        {
            CutsceneAction.CapsulesFall, CutsceneAction.PathfinderLands, CutsceneAction.DrillDown, CutsceneAction.CrystalsTremble,
            CutsceneAction.EyesOpen, CutsceneAction.GroundShivers, CutsceneAction.SwarmPours, CutsceneAction.Handoff,
        };

        private CutsceneDefinition _cutscene;

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            _cutscene = catalog.Story.OpeningCutscene;
        }

        [Test]
        public void Footfalls_AreFoundWhereAFootLandsAfterAStride_AndNotWhileStanding()
        {
            // A foot that swings 0.4 m forward over 10 frames and rests 10 frames, twice; the other stands still.
            var swinging = new List<Vector3>();
            var standing = new List<Vector3>();
            float z = 0f;
            for (int cycle = 0; cycle < 2; cycle++)
            {
                for (int f = 0; f < 10; f++)
                {
                    z += 0.04f;
                    swinging.Add(new Vector3(0f, 0.05f, z));
                    standing.Add(Vector3.zero);
                }

                for (int f = 0; f < 10; f++)
                {
                    swinging.Add(new Vector3(0f, 0f, z));
                    standing.Add(Vector3.zero);
                }
            }

            float[] steps = Footfalls.Detect(swinging, standing, Fps, false);
            Assert.AreEqual(2, steps.Length, "one footfall per landing");
            Assert.AreEqual(11f / Fps, steps[0], 1.5f / Fps);

            var shuffle = new List<Vector3>();
            for (int f = 0; f < 40; f++)
            {
                shuffle.Add(new Vector3(0.002f * Mathf.Sin(f), 0f, 0f));
            }

            Assert.AreEqual(0, Footfalls.Detect(shuffle, shuffle, Fps, true).Length, "a weight shift on the spot is no step");
        }

        [Test]
        public void Crossed_FindsAFootfall_InThePlayedSpan_AcrossALoopSeam()
        {
            float[] moments = { 0.1f, 0.6f };
            Assert.IsTrue(Footfalls.Crossed(moments, 1f, false, 0.05f, 0.15f));
            Assert.IsFalse(Footfalls.Crossed(moments, 1f, false, 0.15f, 0.5f));
            Assert.IsTrue(Footfalls.Crossed(moments, 1f, true, 2.95f, 3.12f), "the step after the seam of the third loop");
            Assert.IsFalse(Footfalls.Crossed(moments, 1f, true, 3.2f, 3.5f));
        }

        [Test]
        public void SpencersClips_CarryFootfalls_OnlyWhereHisFeetMove()
        {
            var hero = AssetDatabase.LoadAssetAtPath<Game.Hero.HeroArtDefinition>(HeroArtPath);
            Assert.IsNotNull(hero, "no hero art");
            ModelClip exit = Clip(hero.Body, ModelClipRole.CapsuleExit);
            Assert.GreaterOrEqual(exit.FootfallSeconds.Length, 2, "he climbs out and down the ramp without a step (Tools/Generate/Hero Footfalls)");
            foreach (float step in exit.FootfallSeconds)
            {
                Assert.Less(step, ExitStandsAt + Tolerance, "a step after he stands still at the ramp's foot");
            }

            Assert.AreEqual(0, Clip(hero.Body, ModelClipRole.Idle).FootfallSeconds.Length, "steps while he stands");
            Assert.AreEqual(0, Clip(hero.Body, ModelClipRole.AimHoldNervous).FootfallSeconds.Length, "steps while he holds the aim");
            ModelClip walk = Clip(hero.Body, ModelClipRole.Walk);
            Assert.AreEqual(2, walk.FootfallSeconds.Length, "a walk cycle has two steps");
            Assert.AreEqual(walk.Length * 0.5f, Mathf.Abs(walk.FootfallSeconds[1] - walk.FootfallSeconds[0]), walk.Length * 0.15f, "the walk's steps are half a cycle apart");
        }

        [Test]
        public void TheOpening_PlaysNoFootstepByTheClock()
        {
            foreach (CutsceneShot shot in _cutscene.Shots)
            {
                foreach (CutsceneCue cue in shot.Cues)
                {
                    Assert.IsFalse(System.Array.IndexOf(Steps, cue.Sound) >= 0, $"{shot.Action}: a footstep at {cue.At} s by the clock");
                }
            }
        }

        [Test]
        public void TheMothership_FliesIn_ThenLaunchesOnce()
        {
            int arrival = IndexOf(CutsceneAction.MothershipArrives);
            int launch = IndexOf(CutsceneAction.CapsulesFall);
            Assert.AreEqual(arrival + 1, launch, "the launch follows the fly-in");
            Assert.IsNotNull(_cutscene.Shots[arrival].Dialogue, "ORA speaks over the arrival");
            Assert.GreaterOrEqual(_cutscene.Shots[arrival].DialogueDelay, OrbitSet.ArrivalSeconds, "the text comes after the ship has settled");
            Assert.IsNull(_cutscene.Shots[launch].Dialogue, "no text over the launch");
            // Round 59 (the salvo): one launch of several capsules; the doors open and the spent booster fades once, every
            // capsule sounds its own release, ignition and separation once (OpeningSalvoTests checks each capsule).
            int capsules = _cutscene.Cast.CapsuleCount;
            CutsceneShot shot = _cutscene.Shots[launch];
            foreach (CutsceneSound sound in new[] { CutsceneSound.BayDoors, CutsceneSound.BoosterFade })
            {
                Assert.AreEqual(1, Count(shot, sound), $"{sound} must sound exactly once");
            }

            foreach (CutsceneSound sound in new[] { CutsceneSound.ClampsRelease, CutsceneSound.ColdGasPuff, CutsceneSound.BoosterIgnition, CutsceneSound.StageSeparation })
            {
                Assert.AreEqual(capsules, Count(shot, sound), $"{sound} must sound once per capsule");
            }

            Assert.AreEqual(CapsuleDrop.ReleaseAt(0, _cutscene.Cast.ReleaseGap), CueAt(shot, CutsceneSound.ClampsRelease), 0.001f, "the clamps let go as the first capsule leaves");
        }

        [Test]
        public void TheArrival_BrakesToAStop()
        {
            Vector3 hold = OrbitSet.ArrivingAt(OrbitSet.ArrivalSeconds);
            Assert.AreEqual(0f, Vector3.Distance(hold, OrbitSet.ArrivingAt(OrbitSet.ArrivalSeconds + 5f)), 0.0001f, "it holds after the arrival");
            float early = Vector3.Distance(OrbitSet.ArrivingAt(1f), OrbitSet.ArrivingAt(1.1f));
            float late = Vector3.Distance(OrbitSet.ArrivingAt(6f), OrbitSet.ArrivingAt(6.1f));
            Assert.Greater(early, late * 4f, "it slows down as it comes in");
            Assert.Greater(OrbitSet.Brake(1f), OrbitSet.Brake(6f), "the forward thrusters burn hardest while it is fast");
            Assert.AreEqual(0f, OrbitSet.Brake(OrbitSet.ArrivalSeconds), "the thrusters are off once it holds");
        }

        [Test]
        public void EveryShake_HasItsRumble()
        {
            for (int i = 0; i < _cutscene.Shots.Count; i++)
            {
                CutsceneShot shot = _cutscene.Shots[i];
                bool kicks = false;
                foreach (CutsceneCue cue in shot.Cues)
                {
                    kicks |= cue.Kind == CueKind.Shake;
                }

                if (kicks || System.Array.IndexOf(Shaking, shot.Action) >= 0)
                {
                    Assert.Greater(shot.ShakeRumble, 0f, $"shot {i + 1} ({shot.Action}) shakes in silence");
                }
            }

            Assert.GreaterOrEqual(_cutscene.Sounds.IndexOf(CutsceneSound.QuakeLoop), 0, "no quake rumble in the bank");
            Assert.AreEqual(0f, ShakeSound.Level(0f, 0f, 0f, 1f), "no shake, no rumble");
            Assert.AreEqual(0.5f, ShakeSound.Level(0f, 0f, 0.5f, 1f), 0.0001f, "the rumble follows the trembling");
            Assert.AreEqual(0.8f, ShakeSound.Level(ShakeSound.FullKick, 0.2f, 0f, 0.8f), 0.0001f, "a full kick gives the shot's full rumble");
        }

        [Test]
        public void NoCreature_IsHeard_ThatTheShotDoesNotShow()
        {
            foreach (CutsceneAction unseen in new[] { CutsceneAction.CrystalsTremble, CutsceneAction.EyesOpen, CutsceneAction.MercerReacts })
            {
                CutsceneShot shot = _cutscene.Shots[IndexOf(unseen)];
                foreach (CutsceneSound voice in CreatureVoices)
                {
                    Assert.AreEqual(0, Count(shot, voice), $"{unseen}: a {voice} with no creature in the picture");
                }
            }
        }

        [Test]
        public void TheReactionShot_CutsInOnTheQueensScream()
        {
            CutsceneShot swarm = _cutscene.Shots[IndexOf(CutsceneAction.SwarmPours)];
            float scream = CueAt(swarm, CutsceneSound.QueenRoar);
            Assert.Greater(swarm.Seconds, scream, "the scream must be heard on her");
            Assert.Less(swarm.Seconds - scream, 1.5f, "the cut to Spencer comes long after her scream: his startle would not answer it");
            Assert.AreEqual(IndexOf(CutsceneAction.SwarmPours) + 1, IndexOf(CutsceneAction.MercerReacts));
            Assert.Greater(_cutscene.Cast.FlinchFace.Length, 0, "no face for the startle");
        }

        [Test]
        public void TheBurrowAndBuildingSounds_HaveSlots_WithoutTheOldPloof()
        {
            CutsceneSoundBank bank = _cutscene.Sounds;
            int burrow = bank.IndexOf(CutsceneSound.BurrowClose);
            Assert.GreaterOrEqual(burrow, 0, "no slot for the burrows closing");
            Assert.AreEqual(CutsceneSound.None, bank.Slots[burrow].Fallback, "the burrow close falls back on the old ploof");
            Assert.AreEqual(0, Count(_cutscene.Shots[IndexOf(CutsceneAction.Handoff)], CutsceneSound.Emerge), "the ploof is still cued in the handoff");
            foreach (CutsceneSound sound in new[] { CutsceneSound.BuildHammer, CutsceneSound.BuildHammerLoop, CutsceneSound.BuildDrill, CutsceneSound.BuildDrillLoop })
            {
                int slot = bank.IndexOf(sound);
                Assert.GreaterOrEqual(slot, 0, $"no slot for {sound}");
                Assert.IsFalse(bank.Slots[slot].IsEmpty, $"{sound} has no file: run Tools/Generate/Opening Cutscene Sounds");
            }

            Assert.IsTrue(bank.Slots[bank.IndexOf(CutsceneSound.BuildHammerLoop)].Loop, "the hammering bed must loop");
        }

        private static ModelClip Clip(ModelArtDefinition art, ModelClipRole role)
        {
            int index = art.IndexOf(role);
            Assert.GreaterOrEqual(index, 0, $"Spencer has no {role} clip");
            return art.Clips[index];
        }

        private int IndexOf(CutsceneAction action)
        {
            for (int i = 0; i < _cutscene.Shots.Count; i++)
            {
                if (_cutscene.Shots[i].Action == action)
                {
                    return i;
                }
            }

            Assert.Fail($"the opening has no {action} shot");
            return -1;
        }

        private static int Count(CutsceneShot shot, CutsceneSound sound)
        {
            int count = 0;
            foreach (CutsceneCue cue in shot.Cues)
            {
                if (cue.Kind == CueKind.Sound && cue.Sound == sound)
                {
                    count++;
                }
            }

            return count;
        }

        private static float CueAt(CutsceneShot shot, CutsceneSound sound)
        {
            foreach (CutsceneCue cue in shot.Cues)
            {
                if (cue.Kind == CueKind.Sound && cue.Sound == sound)
                {
                    return cue.At;
                }
            }

            Assert.Fail($"no {sound} in {shot.Action}");
            return 0f;
        }
    }
}
