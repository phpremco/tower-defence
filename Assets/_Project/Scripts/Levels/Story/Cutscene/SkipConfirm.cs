namespace Game.Levels
{
    /// <summary>
    /// Skipping a whole cutscene takes two presses: the first arms the skip and shows a hint, a second press within
    /// the window confirms it. A stray press never throws away the story. Pure, timed by the caller's clock.
    /// </summary>
    public sealed class SkipConfirm
    {
        public const float WindowSeconds = 2f;

        private float _armedAt = float.NegativeInfinity;

        /// <summary>True while a second press would skip.</summary>
        public bool IsArmed(float now) => now - _armedAt <= WindowSeconds;

        /// <summary>Returns true when this press confirms the skip; otherwise it arms the window.</summary>
        public bool Press(float now)
        {
            if (IsArmed(now))
            {
                _armedAt = float.NegativeInfinity;
                return true;
            }

            _armedAt = now;
            return false;
        }
    }
}
