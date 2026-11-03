using System.Collections.Generic;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class CutsceneSequencerTests
    {
        private readonly List<int> _started = new List<int>();
        private readonly List<float> _cues = new List<float>();
        private readonly List<DialogueDefinition> _dialogues = new List<DialogueDefinition>();
        private bool _busy;
        private int _finished;

        [SetUp]
        public void SetUp()
        {
            _started.Clear();
            _cues.Clear();
            _dialogues.Clear();
            _busy = false;
            _finished = 0;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (DialogueDefinition dialogue in _dialogues)
            {
                Object.DestroyImmediate(dialogue);
            }
        }

        [Test]
        public void FixedShots_EndAtExactTimes()
        {
            CutsceneSequencer sequencer = Create(Fixed(2f), Fixed(3f));

            sequencer.Tick(1.99f);
            Assert.AreEqual(0, sequencer.ShotIndex);
            sequencer.Tick(0.02f);
            Assert.AreEqual(1, sequencer.ShotIndex);
            Assert.AreEqual(0.01f, sequencer.LocalTime, 0.0001f, "the time past the end carries into the next shot");
            sequencer.Tick(2.98f);
            Assert.IsFalse(sequencer.IsFinished);
            sequencer.Tick(0.02f);
            Assert.IsTrue(sequencer.IsFinished);
            Assert.AreEqual(1, _finished);
            CollectionAssert.AreEqual(new[] { 0, 1 }, _started);
        }

        [Test]
        public void AfterDialogue_HoldsUntilTheDialogueIsDonePlusSeconds()
        {
            DialogueDefinition dialogue = Dialogue();
            var shot = new CutsceneShot { Timing = ShotTiming.AfterDialogue, Seconds = 2f, Dialogue = dialogue, DialogueDelay = 1f, Camera = new CameraKey[0] };
            CutsceneSequencer sequencer = Create(shot, Fixed(1f));

            sequencer.Tick(0.5f);
            Assert.IsFalse(_busy, "no dialogue before its delay");
            sequencer.Tick(0.6f);
            Assert.IsTrue(_busy, "the dialogue opened at its delay");
            sequencer.Tick(30f);
            Assert.AreEqual(0, sequencer.ShotIndex, "the shot waits while the box is open");
            Assert.IsTrue(sequencer.WaitingForDialogue);

            _busy = false;
            sequencer.Tick(1.9f);
            Assert.AreEqual(0, sequencer.ShotIndex, "still within the seconds after the dialogue");
            sequencer.Tick(0.2f);
            Assert.AreEqual(1, sequencer.ShotIndex);
        }

        [Test]
        public void FixedShotWithDialogue_WaitsForTheReader()
        {
            DialogueDefinition dialogue = Dialogue();
            var shot = new CutsceneShot { Timing = ShotTiming.Fixed, Seconds = 3f, Dialogue = dialogue, DialogueDelay = 1f, Camera = new CameraKey[0] };
            CutsceneSequencer sequencer = Create(shot, Fixed(1f));

            sequencer.Tick(10f);
            Assert.AreEqual(0, sequencer.ShotIndex, "an open box holds a fixed shot past its length");
            _busy = false;
            sequencer.Tick(0.01f);
            Assert.AreEqual(1, sequencer.ShotIndex);
        }

        [Test]
        public void Cues_FireOnceInOrder_OnABigStep()
        {
            var shot = Fixed(5f);
            shot.Cues = new[] { Cue(3f), Cue(1f), Cue(2f), Cue(4.5f) };
            CutsceneSequencer sequencer = Create(shot, Fixed(1f));

            sequencer.Tick(3.5f);
            CollectionAssert.AreEqual(new[] { 1f, 2f, 3f }, _cues);
            sequencer.Tick(10f);
            CollectionAssert.AreEqual(new[] { 1f, 2f, 3f, 4.5f }, _cues, "each cue fires once, in time order");
            Assert.IsTrue(sequencer.IsFinished);
        }

        [Test]
        public void Skip_FinishesOnce()
        {
            CutsceneSequencer sequencer = Create(Fixed(5f), Fixed(5f));
            sequencer.Tick(1f);

            sequencer.Skip();
            sequencer.Skip();
            sequencer.Tick(20f);

            Assert.IsTrue(sequencer.IsFinished);
            Assert.AreEqual(1, _finished);
        }

        [Test]
        public void Speed_ScalesTime()
        {
            CutsceneSequencer sequencer = Create(Fixed(8f));
            sequencer.Speed = 4f;

            sequencer.Tick(1f);

            Assert.AreEqual(4f, sequencer.LocalTime, 0.0001f);
            Assert.AreEqual(0.5f, sequencer.Progress, 0.0001f);
        }

        [Test]
        public void Seek_SkipsEarlierCuesAndTheDialogueHold()
        {
            DialogueDefinition dialogue = Dialogue();
            var held = new CutsceneShot { Timing = ShotTiming.AfterDialogue, Seconds = 1f, CameraSeconds = 4f, Dialogue = dialogue, Camera = new CameraKey[0], Cues = new[] { Cue(0.5f), Cue(3f) } };
            CutsceneSequencer sequencer = Create(Fixed(2f), held);

            sequencer.Seek(1, 0.5f);

            Assert.AreEqual(1, sequencer.ShotIndex);
            Assert.AreEqual(2f, sequencer.LocalTime, 0.0001f);
            Assert.IsFalse(sequencer.WaitingForDialogue);
            CollectionAssert.IsEmpty(_cues, "cues before the sought moment do not fire");
            sequencer.Tick(1.5f);
            CollectionAssert.AreEqual(new[] { 3f }, _cues);
        }

        [Test]
        public void SkipConfirm_NeedsASecondPressInsideTheWindow()
        {
            var skip = new SkipConfirm();

            Assert.IsFalse(skip.Press(10f), "the first press only arms");
            Assert.IsTrue(skip.IsArmed(11f));
            Assert.IsFalse(skip.IsArmed(10f + SkipConfirm.WindowSeconds + 0.1f), "the hint goes away");
            Assert.IsFalse(skip.Press(13f), "a late press arms again");
            Assert.IsTrue(skip.Press(14f), "a press inside the window confirms");
        }

        private CutsceneSequencer Create(params CutsceneShot[] shots)
        {
            var sequencer = new CutsceneSequencer(shots, OnDialogue, () => _busy);
            sequencer.ShotStarted += index => _started.Add(index);
            sequencer.CueFired += (index, cue) => _cues.Add(cue.At);
            sequencer.Finished += () => _finished++;
            sequencer.Begin();
            return sequencer;
        }

        private void OnDialogue(DialogueDefinition dialogue) => _busy = true;

        private DialogueDefinition Dialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
            _dialogues.Add(dialogue);
            return dialogue;
        }

        private static CutsceneShot Fixed(float seconds) => new CutsceneShot { Timing = ShotTiming.Fixed, Seconds = seconds, Camera = new CameraKey[0] };

        private static CutsceneCue Cue(float at) => new CutsceneCue { At = at, Kind = CueKind.Shake, Strength = 0.5f };
    }
}
