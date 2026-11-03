using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The clock of a cutscene: which shot plays, how far into it, which cues are due and when the shot may end.
    /// A shot with a dialogue waits for the player to read it; cues fire once each, in time order, even when one
    /// tick jumps over several. Pure C#: the caller passes the unscaled frame time (times <see cref="Speed"/>), so
    /// the game clock is never touched and the whole run can be tested without a frame.
    /// </summary>
    public sealed class CutsceneSequencer
    {
        // Every loop pass ends a shot, starts a dialogue or consumes the time left, so this only guards bad data.
        private const int MaxStepsPerShot = 4;

        private readonly IReadOnlyList<CutsceneShot> _shots;
        private readonly Action<DialogueDefinition> _startDialogue;
        private readonly Func<bool> _dialogueBusy;
        private readonly List<int> _cueOrder = new List<int>();
        private int _nextCue;
        private bool _dialogueStarted;
        private bool _dialogueDone;
        private float _dialogueDoneAt;

        /// <param name="startDialogue">Opens a shot's dialogue box.</param>
        /// <param name="dialogueBusy">True while that dialogue is still on screen.</param>
        public CutsceneSequencer(IReadOnlyList<CutsceneShot> shots, Action<DialogueDefinition> startDialogue, Func<bool> dialogueBusy)
        {
            _shots = shots ?? throw new ArgumentNullException(nameof(shots));
            _startDialogue = startDialogue;
            _dialogueBusy = dialogueBusy;
        }

        /// <summary>Raised with the shot index whenever a shot begins, also after a seek.</summary>
        public event Action<int> ShotStarted;

        /// <summary>Raised with the shot index and the cue when a cue is due.</summary>
        public event Action<int, CutsceneCue> CueFired;

        /// <summary>Raised once, when the last shot ends or the cutscene is skipped.</summary>
        public event Action Finished;

        /// <summary>How fast cutscene time runs against real time; tests run it faster.</summary>
        public float Speed { get; set; } = 1f;

        public int ShotIndex { get; private set; } = -1;

        /// <summary>Seconds into the current shot.</summary>
        public float LocalTime { get; private set; }

        public bool IsFinished { get; private set; }

        public int ShotCount => _shots.Count;

        public CutsceneShot Current => _shots[ShotIndex];

        /// <summary>How far the camera move of the current shot has come, 0..1.</summary>
        public float Progress => ShotIndex < 0 || IsFinished ? 1f : Mathf.Clamp01(LocalTime / Current.CameraLength);

        /// <summary>True while the shot's time is up but its dialogue is still being read.</summary>
        public bool WaitingForDialogue => ShotIndex >= 0 && !IsFinished && _dialogueStarted && !_dialogueDone;

        /// <summary>When the current shot will end, in its own seconds; infinite while its dialogue is still open.</summary>
        public float ShotEndTime => ShotIndex < 0 || IsFinished ? 0f : EndTime(Current);

        /// <summary>Starts the first shot.</summary>
        public void Begin()
        {
            if (_shots.Count == 0)
            {
                Finish();
                return;
            }

            StartShot(0, 0f);
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (ShotIndex < 0 || IsFinished)
            {
                return;
            }

            float remaining = Mathf.Max(0f, unscaledDeltaTime * Speed);
            int guard = (_shots.Count + 1) * MaxStepsPerShot;
            while (!IsFinished && guard-- > 0)
            {
                CutsceneShot shot = Current;
                if (_dialogueStarted && !_dialogueDone && !(_dialogueBusy?.Invoke() ?? false))
                {
                    _dialogueDone = true;
                    _dialogueDoneAt = LocalTime;
                }

                float end = EndTime(shot);
                float dialogueAt = shot.Dialogue != null && !_dialogueStarted ? shot.DialogueDelay : float.PositiveInfinity;
                float stop = Mathf.Max(LocalTime, Mathf.Min(LocalTime + remaining, Mathf.Min(end, dialogueAt)));
                FireCues(shot, stop);
                remaining = Mathf.Max(0f, remaining - (stop - LocalTime));
                LocalTime = Mathf.Max(LocalTime, stop);

                if (stop >= dialogueAt)
                {
                    _dialogueStarted = true;
                    _startDialogue?.Invoke(shot.Dialogue);
                    continue;
                }

                if (stop >= end)
                {
                    NextShot();
                    continue;
                }

                break;
            }
        }

        /// <summary>Ends the whole cutscene now. Does nothing once it has finished.</summary>
        public void Skip()
        {
            if (!IsFinished)
            {
                Finish();
            }
        }

        /// <summary>
        /// Jumps to a moment of a shot, for screenshots and tests. Cues before that moment count as fired without
        /// firing, and the shot's dialogue counts as read, so a seek never waits.
        /// </summary>
        public void Seek(int shot, float progress)
        {
            if (_shots.Count == 0)
            {
                return;
            }

            IsFinished = false;
            int index = Mathf.Clamp(shot, 0, _shots.Count - 1);
            StartShot(index, Mathf.Clamp01(progress) * _shots[index].CameraLength);
            _dialogueStarted = true;
            _dialogueDone = true;
            _dialogueDoneAt = LocalTime;
            while (_nextCue < _cueOrder.Count && _shots[index].Cues[_cueOrder[_nextCue]].At <= LocalTime)
            {
                _nextCue++;
            }
        }

        // Fixed shots end on time unless a dialogue is still open; AfterDialogue shots run on after their dialogue.
        private float EndTime(CutsceneShot shot)
        {
            bool hasDialogue = shot.Dialogue != null;
            if (hasDialogue && !_dialogueDone)
            {
                return float.PositiveInfinity;
            }

            if (shot.Timing == ShotTiming.AfterDialogue && hasDialogue)
            {
                return _dialogueDoneAt + shot.Seconds;
            }

            return Mathf.Max(shot.Seconds, hasDialogue ? _dialogueDoneAt : 0f);
        }

        private void FireCues(CutsceneShot shot, float upTo)
        {
            while (_nextCue < _cueOrder.Count)
            {
                CutsceneCue cue = shot.Cues[_cueOrder[_nextCue]];
                if (cue.At > upTo)
                {
                    return;
                }

                _nextCue++;
                CueFired?.Invoke(ShotIndex, cue);
            }
        }

        private void NextShot()
        {
            if (ShotIndex + 1 >= _shots.Count)
            {
                Finish();
                return;
            }

            StartShot(ShotIndex + 1, 0f);
        }

        private void StartShot(int index, float localTime)
        {
            ShotIndex = index;
            LocalTime = localTime;
            _dialogueStarted = false;
            _dialogueDone = false;
            _dialogueDoneAt = 0f;
            _nextCue = 0;
            SortCues(_shots[index].Cues);
            ShotStarted?.Invoke(index);
        }

        // Data may list cues in any order; they fire by time, and cues at the same time in the order written.
        private void SortCues(CutsceneCue[] cues)
        {
            _cueOrder.Clear();
            if (cues == null)
            {
                return;
            }

            for (int i = 0; i < cues.Length; i++)
            {
                int at = _cueOrder.Count;
                while (at > 0 && cues[_cueOrder[at - 1]].At > cues[i].At)
                {
                    at--;
                }

                _cueOrder.Insert(at, i);
            }
        }

        private void Finish()
        {
            IsFinished = true;
            Finished?.Invoke();
        }
    }
}
