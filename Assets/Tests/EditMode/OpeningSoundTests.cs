using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 42: the opening cutscene sounds with Remco's approved intro set (Audio/Approved/Intro). Every approved file is in
    /// the cutscene's bank, no slot is empty, no music plays, and the held layers fade
    /// by the shot clock alone.
    /// </summary>
    public sealed class OpeningSoundTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string IntroFolder = "Assets/_Project/Audio/Approved/Intro";
        private const float Near = 0.0001f;

        // The files that arrived last (2026-11-11): their slots must exist and be filled.
        private static readonly CutsceneSound[] LateFiles =
        {
            CutsceneSound.CapsuleRattle, CutsceneSound.HatchOpen, CutsceneSound.DroneLand, CutsceneSound.EyesOpenWet,
            CutsceneSound.StageSeparation, CutsceneSound.DroneHum, CutsceneSound.DrillStart,
        };

        // Sounds that play without a cue or layer of their own: the sets' loops (drill, drone fans) and Spencer's acting beats
        // (FieldSet), plus two takes kept for an ear check: the dry gasp, and the radio call (the dialogue box's opener
        // plays when Voss starts, round 43).
        private static readonly CutsceneSound[] PlayedElsewhere =
        {
            CutsceneSound.DrillRun, CutsceneSound.DroneHum, CutsceneSound.Gasp, CutsceneSound.PistolDraw, CutsceneSound.PistolCharge, CutsceneSound.GaspDry,
            CutsceneSound.RadioOpen,
            // Round 44: his footsteps follow his clips' footfalls (FieldSet), the quake rumble follows the shaking (CutscenePlayer),
            // the burrows sound their own closing (FieldSwarm), the builders their work (FieldBase).
            CutsceneSound.StepMetal, CutsceneSound.StepGravel, CutsceneSound.StepStone, CutsceneSound.QuakeLoop, CutsceneSound.BurrowClose,
            CutsceneSound.BuildHammer, CutsceneSound.BuildHammerLoop, CutsceneSound.BuildDrill, CutsceneSound.BuildDrillLoop,
        };

        // Round 44: slots whose files are still being made; they stay silent (no fallback) until the files land.
        private static readonly CutsceneSound[] PendingFiles = { CutsceneSound.BurrowClose };

        private CutsceneDefinition _cutscene;
        private CutsceneSoundBank _bank;

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "run Tools/Generate/Scenes and Starter Data");
            _cutscene = catalog.Story.OpeningCutscene;
            Assert.IsNotNull(_cutscene, "the story names its opening cutscene");
            _bank = _cutscene.Sounds;
            Assert.IsNotNull(_bank, "the opening has no sound bank: run Tools/Generate/Opening Cutscene Sounds");
        }

        [Test]
        public void TheBank_Validates()
        {
            var errors = new List<string>();
            Assert.IsTrue(_bank.Validate(errors), string.Join("\n", errors));
        }

        [Test]
        public void EveryApprovedIntroFile_IsInTheBank()
        {
            var inBank = new HashSet<AudioClip>();
            foreach (CutsceneSoundSlot slot in _bank.Slots)
            {
                for (int i = 0; i < (slot.Clips != null ? slot.Clips.Count : 0); i++)
                {
                    inBank.Add(slot.Clips[i]);
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { IntroFolder });
            Assert.Greater(guids.Length, 0, "no approved intro files");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Assert.IsTrue(inBank.Contains(AssetDatabase.LoadAssetAtPath<AudioClip>(path)), $"{path} is not in the opening's sound bank");
            }
        }

        [Test]
        public void TheLateFiles_HaveFilledSlots()
        {
            foreach (CutsceneSound sound in LateFiles)
            {
                Assert.GreaterOrEqual(_bank.IndexOf(sound), 0, $"no slot for {sound}");
            }
        }

        [Test]
        public void NoSlot_IsEmpty()
        {
            foreach (CutsceneSoundSlot slot in _bank.Slots)
            {
                if (System.Array.IndexOf(PendingFiles, slot.Sound) >= 0)
                {
                    Assert.AreEqual(CutsceneSound.None, slot.Fallback, $"{slot.Sound} waits for its files in silence, without a stand-in");
                    continue;
                }

                Assert.IsFalse(slot.IsEmpty, $"{slot.Sound} has no file: run Tools/Generate/Opening Cutscene Sounds");
            }
        }

        [Test]
        public void TheOpening_PlaysNoMusic()
        {
            foreach (CutsceneMusic mood in new[] { CutsceneMusic.Wonder, CutsceneMusic.Tension, CutsceneMusic.Attack })
            {
                Assert.IsFalse(_bank.TryGetMusic(mood, out _), $"a {mood} track is set: Remco wants no intro music yet");
            }

            Assert.Greater(_bank.Music.Count, 0, "the music hook is missing from the bank");
            foreach (CutsceneMusicSlot slot in _bank.Music)
            {
                Assert.IsTrue(slot.IsEmpty, $"the {slot.Mood} track is not empty");
            }
        }

        [Test]
        public void EverySlot_IsPlayedSomewhere()
        {
            var used = new HashSet<CutsceneSound>(PlayedElsewhere);
            foreach (CutsceneShot shot in _cutscene.Shots)
            {
                foreach (CutsceneCue cue in shot.Cues)
                {
                    used.Add(cue.Sound);
                }

                foreach (CutsceneLayer layer in shot.Layers ?? new CutsceneLayer[0])
                {
                    used.Add(layer.Sound);
                }
            }

            foreach (CutsceneSoundSlot slot in _bank.Slots)
            {
                Assert.IsTrue(used.Contains(slot.Sound), $"{slot.Sound} is in the bank but never played");
            }
        }

        [Test]
        public void EveryLayer_HasASlot_AndTheWindSitsUnderTheBed()
        {
            float wind = 0f;
            float bed = 0f;
            foreach (CutsceneShot shot in _cutscene.Shots)
            {
                foreach (CutsceneLayer layer in shot.Layers ?? new CutsceneLayer[0])
                {
                    int slot = _bank.IndexOf(layer.Sound);
                    Assert.GreaterOrEqual(slot, 0, $"the {layer.Sound} layer has no slot in the bank");
                    // What reaches the mix: the layer's level times the slot's measured gain.
                    float level = layer.Volume * _bank.Slots[slot].Clips.Gain;
                    if (layer.Sound == CutsceneSound.WindPlain)
                    {
                        wind = Mathf.Max(wind, level);
                    }
                    else if (layer.Sound == CutsceneSound.OrbitBed)
                    {
                        bed = Mathf.Max(bed, level);
                    }
                }
            }

            Assert.Greater(wind, 0f, "no wind over the plain");
            Assert.Less(wind, bed * 0.5f, "Remco: the wind is loud, it must sit well under the space bed");
        }

        [Test]
        public void TheSpaceBed_CarriesThePrologue()
        {
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(HasLayer(_cutscene.Shots[i], CutsceneSound.OrbitBed), $"shot {i + 1} has no space bed");
            }
        }

        [Test]
        public void ALayer_FadesInHoldsAndFadesOut_ByTheClock()
        {
            var layer = new CutsceneLayer { Sound = CutsceneSound.BoosterBurn, From = 2f, To = 6f, Volume = 0.5f, FadeIn = 1f, FadeOut = 2f };
            Assert.AreEqual(0f, layer.Envelope(1.9f), Near);
            Assert.AreEqual(0.5f, layer.Envelope(2.5f), Near);
            Assert.AreEqual(1f, layer.Envelope(3.5f), Near);
            Assert.AreEqual(0.5f, layer.Envelope(5f), Near);
            Assert.AreEqual(0f, layer.Envelope(6f), Near);

            var hold = new CutsceneLayer { Sound = CutsceneSound.OrbitBed, From = 0f, Volume = 0.5f, FadeIn = 2f };
            Assert.AreEqual(1f, hold.Envelope(500f), Near, "a layer without an end holds to the end of the shot");
        }

        [Test]
        public void ALayer_WithBrokenTimes_IsReported()
        {
            var errors = new List<string>();
            new CutsceneLayer { Sound = CutsceneSound.WindPlain, From = 3f, To = 2f, Volume = 0.1f }.Validate(1, errors);
            new CutsceneLayer { Sound = CutsceneSound.None, From = 0f, Volume = 0.1f }.Validate(1, errors);
            new CutsceneLayer { Sound = CutsceneSound.WindPlain, From = 0f, To = 1f, Volume = 0.1f, FadeIn = 1f, FadeOut = 1f }.Validate(1, errors);
            Assert.AreEqual(3, errors.Count, string.Join("\n", errors));
        }

        private static bool HasLayer(CutsceneShot shot, CutsceneSound sound)
        {
            foreach (CutsceneLayer layer in shot.Layers ?? new CutsceneLayer[0])
            {
                if (layer.Sound == sound)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
