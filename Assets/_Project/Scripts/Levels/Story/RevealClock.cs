using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The typewriter: how many characters of a line show after a given time. Pure, so the reveal is tested without a
    /// view, and fed with unscaled time so it runs while the game clock is held.
    /// </summary>
    public sealed class RevealClock
    {
        private readonly float _charactersPerSecond;
        private float _elapsed;
        private bool _finished;

        public RevealClock(float charactersPerSecond)
        {
            _charactersPerSecond = Mathf.Max(charactersPerSecond, Mathf.Epsilon);
        }

        public int Total { get; private set; }

        // Finishing is a flag rather than a jump of the elapsed time, so float rounding can never leave one character hidden.
        public int Visible => _finished ? Total : Mathf.Min(Total, Mathf.FloorToInt(_elapsed * _charactersPerSecond));

        public bool IsComplete => Visible >= Total;

        /// <summary>Starts a new line with this many characters, none of them shown.</summary>
        public void Restart(int total)
        {
            Total = Mathf.Max(0, total);
            _elapsed = 0f;
            _finished = false;
        }

        public void Advance(float seconds)
        {
            if (seconds > 0f)
            {
                _elapsed += seconds;
            }
        }

        /// <summary>Shows the whole line at once, as when the player presses confirm mid-line.</summary>
        public void Finish() => _finished = true;
    }
}
