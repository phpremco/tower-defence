using System;
using System.Collections.Generic;

namespace Game.Levels
{
    /// <summary>
    /// Plays queued dialogues line by line on an unscaled clock and tells a presenter what to show. It knows nothing
    /// of levels or time holds, so the level, the intro cutscene and the world map can all drive one.
    /// </summary>
    public sealed class DialoguePlayer
    {
        /// <summary>Typewriter speed when the caller does not choose one.</summary>
        public const float DefaultCharactersPerSecond = 45f;

        private readonly Queue<DialogueDefinition> _queue = new Queue<DialogueDefinition>();
        private readonly RevealClock _clock;
        private readonly Action<string> _markSeen;
        private int _lastVisible;
        private bool _wasBusy;

        /// <param name="markSeen">Called with the dialogue id when a conversation ends or is skipped; persists it.</param>
        public DialoguePlayer(Action<string> markSeen, float charactersPerSecond = DefaultCharactersPerSecond)
        {
            _markSeen = markSeen;
            _clock = new RevealClock(charactersPerSecond);
        }

        /// <summary>Raised whenever <see cref="IsBusy"/> flips.</summary>
        public event Action BusyChanged;

        /// <summary>Draws the dialogue. Set it before anything plays; a level only queues story while one is set.</summary>
        public IDialoguePresenter Presenter { get; set; }

        /// <summary>Test hook: every tick finishes the current line or moves to the next, so a run never waits for a player.</summary>
        public bool AutoAdvance { get; set; }

        public bool IsPlaying => Current != null;

        /// <summary>True while a conversation plays or waits in the queue.</summary>
        public bool IsBusy => IsPlaying || _queue.Count > 0;

        public int QueuedCount => _queue.Count;

        public DialogueDefinition Current { get; private set; }

        public int LineIndex { get; private set; }

        public int VisibleCharacters => _clock.Visible;

        public bool LineComplete => _clock.IsComplete;

        public void Enqueue(DialogueDefinition dialogue)
        {
            if (dialogue == null || dialogue.Lines.Count == 0)
            {
                return;
            }

            _queue.Enqueue(dialogue);
            NotifyBusy();
        }

        /// <summary>Starts the next queued conversation when nothing plays. The caller decides when the moment is right.</summary>
        public bool TryStart()
        {
            if (IsPlaying || _queue.Count == 0)
            {
                return false;
            }

            Begin(_queue.Dequeue());
            return true;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            if (AutoAdvance)
            {
                Advance();
                return;
            }

            _clock.Advance(unscaledDeltaTime);
            ReportReveal();
        }

        /// <summary>Confirm: shows the rest of the line, or goes to the next line once the line is complete.</summary>
        public void Advance()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (!_clock.IsComplete)
            {
                _clock.Finish();
                ReportReveal();
                return;
            }

            if (LineIndex + 1 < Current.Lines.Count)
            {
                ShowLine(LineIndex + 1);
                return;
            }

            EndConversation(true);
        }

        /// <summary>Skip: ends the current conversation as seen. Conversations still queued play after it.</summary>
        public void SkipConversation()
        {
            if (IsPlaying)
            {
                EndConversation(true);
            }
        }

        /// <summary>Drops everything without marking it seen, as when the level is lost.</summary>
        public void Clear()
        {
            _queue.Clear();
            if (IsPlaying)
            {
                EndConversation(false);
            }

            NotifyBusy();
        }

        private void Begin(DialogueDefinition dialogue)
        {
            Current = dialogue;
            Presenter?.Open(dialogue);
            ShowLine(0);
            NotifyBusy();
        }

        private void ShowLine(int index)
        {
            LineIndex = index;
            DialogueLine line = Current.Lines[index];
            _clock.Restart(line.CharacterCount);
            _lastVisible = -1;
            Presenter?.ShowLine(line, index);
            ReportReveal();
        }

        private void ReportReveal()
        {
            int visible = _clock.Visible;
            if (visible == _lastVisible)
            {
                return;
            }

            _lastVisible = visible;
            Presenter?.Reveal(visible, _clock.IsComplete);
        }

        private void EndConversation(bool seen)
        {
            DialogueDefinition finished = Current;
            Current = null;
            LineIndex = 0;
            if (seen)
            {
                _markSeen?.Invoke(finished.Id);
            }

            // Queued conversations follow on in the same box, so the level stays held between them.
            if (seen && _queue.Count > 0)
            {
                Begin(_queue.Dequeue());
                return;
            }

            Presenter?.Close();
            NotifyBusy();
        }

        private void NotifyBusy()
        {
            bool busy = IsBusy;
            if (busy == _wasBusy)
            {
                return;
            }

            _wasBusy = busy;
            BusyChanged?.Invoke();
        }
    }
}
