using System;
using Game.Platform;

namespace Game.Levels
{
    /// <summary>
    /// Keyboard and gamepad for a dialogue: confirm advances, cancel skips the conversation, the
    /// Codex button opens a scanned creature in the Codex. Mouse clicks are left to
    /// the buttons on the box, so one click never advances twice.
    /// </summary>
    public sealed class DialogueInput : IDisposable
    {
        private readonly DialoguePlayer _player;
        private readonly IInputProvider _input;
        private readonly Func<bool> _isFrozen;

        /// <param name="isFrozen">True while another layer owns the input, such as the pause menu.</param>
        public DialogueInput(DialoguePlayer player, IInputProvider input, Func<bool> isFrozen)
        {
            _player = player;
            _input = input;
            _isFrozen = isFrozen;
            _input.Confirmed += OnConfirmed;
            _input.Cancelled += OnCancelled;
            _input.CodexPressed += OnCodexPressed;
        }

        public void Dispose()
        {
            _input.Confirmed -= OnConfirmed;
            _input.Cancelled -= OnCancelled;
            _input.CodexPressed -= OnCodexPressed;
        }

        private bool Accepts => _player.IsPlaying && !_input.FromPointer && !_isFrozen();

        private void OnConfirmed()
        {
            if (Accepts)
            {
                _player.Advance();
            }
        }

        private void OnCancelled()
        {
            if (Accepts)
            {
                _player.SkipConversation();
            }
        }

        // Y or C on a scan: close it and open the creature's Codex page.
        private void OnCodexPressed()
        {
            if (Accepts)
            {
                _player.RequestCodex();
            }
        }
    }
}
