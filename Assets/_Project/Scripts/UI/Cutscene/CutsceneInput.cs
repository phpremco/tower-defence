using System;
using Game.Levels;
using Game.Platform;

namespace Game.UI
{
    /// <summary>
    /// Keyboard and gamepad during a cutscene. Confirm reads dialogue and nothing else. While a dialogue box is open,
    /// Cancel belongs to it (skip the conversation); with no box open, Cancel and Start arm the whole-cutscene skip;
    /// while a box is open Start does nothing. One press never does both. Mouse clicks are left to the buttons on screen.
    /// </summary>
    public sealed class CutsceneInput : IDisposable
    {
        private readonly IInputProvider _input;
        private readonly DialoguePlayer _dialogue;
        private readonly Action _skipPressed;

        /// <param name="skipPressed">A press towards skipping the whole cutscene: the first arms, a second confirms.</param>
        public CutsceneInput(IInputProvider input, DialoguePlayer dialogue, Action skipPressed)
        {
            _input = input;
            _dialogue = dialogue;
            _skipPressed = skipPressed;
            _input.Confirmed += OnConfirmed;
            _input.Cancelled += OnCancelled;
            _input.PauseToggled += OnPause;
        }

        public void Dispose()
        {
            _input.Confirmed -= OnConfirmed;
            _input.Cancelled -= OnCancelled;
            _input.PauseToggled -= OnPause;
        }

        private void OnConfirmed()
        {
            if (!_input.FromPointer && _dialogue.IsPlaying)
            {
                _dialogue.Advance();
            }
        }

        private void OnCancelled()
        {
            if (_input.FromPointer)
            {
                return;
            }

            if (_dialogue.IsPlaying)
            {
                _dialogue.SkipConversation();
                return;
            }

            _skipPressed?.Invoke();
        }

        // Start is a skip press only between conversations, so a box can never be skipped past with the whole scene.
        private void OnPause()
        {
            if (!_dialogue.IsPlaying)
            {
                _skipPressed?.Invoke();
            }
        }
    }
}
