using Game.Platform;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Game speed and pause. Speed steps are data on this component, not magic numbers in code.</summary>
    public sealed class TimeController : MonoBehaviour
    {
        [SerializeField] private float[] _speedSteps = { 1f, 2f, 3f };

        private IInputProvider _input;
        private LevelContext _context;
        private int _speedIndex;
        private readonly TimeHolds _holds = new TimeHolds();
        private bool _paused;
        private object _slowOwner;
        private float _slow = 1f;

        public float CurrentSpeed => _paused || _holds.IsHeld ? 0f : _speedSteps[_speedIndex] * _slow;

        /// <summary>True while a short slow-motion beat (a boss intro) runs.</summary>
        public bool IsSlowed => _slowOwner != null;

        /// <summary>
        /// Slows the game to a share of its speed for a short beat (it never pauses, so the moment stays live). One beat at
        /// a time: a second owner replaces the first.
        /// </summary>
        public void SlowDown(object owner, float factor)
        {
            _slowOwner = owner;
            _slow = Mathf.Clamp(factor, 0.05f, 1f);
            Apply();
        }

        /// <summary>Ends the slow motion the owner started; another owner's beat is left alone.</summary>
        public void EndSlowDown(object owner)
        {
            if (_slowOwner != owner)
            {
                return;
            }

            _slowOwner = null;
            _slow = 1f;
            Apply();
        }

        public void Initialize(LevelContext context, IInputProvider input)
        {
            _context = context;
            _input = input;
            _input.SpeedToggled += CycleSpeed;
            _input.PauseToggled += TogglePause;
            Apply();
        }

        public void CycleSpeed()
        {
            if (_context.InputBlocked || Ended)
            {
                return;
            }

            _speedIndex = (_speedIndex + 1) % _speedSteps.Length;
            Apply();
        }

        public void TogglePause()
        {
            // After the outcome there is no pause menu to resume from, so a pause would freeze the ending for good.
            if (Ended)
            {
                return;
            }

            _paused = !_paused;
            Apply();
        }

        /// <summary>
        /// Freezes time without opening the pause menu while a panel such as the briefing is up. Each panel holds
        /// under its own owner, so several can be open at once and time resumes only after the last one releases.
        /// </summary>
        public void Hold(object owner)
        {
            if (_holds.Acquire(owner))
            {
                Apply();
            }
        }

        public void Release(object owner)
        {
            if (_holds.Release(owner))
            {
                Apply();
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.SpeedToggled -= CycleSpeed;
                _input.PauseToggled -= TogglePause;
            }

            Time.timeScale = 1f;
        }

        // Once the level has ended the result screen owns the clock: holds still come and go (so the input gate
        // opens again after a victory dialogue), but the time scale is no longer written.
        private bool Ended => _context.Outcome != LevelOutcome.Running;

        private void Apply()
        {
            if (!Ended)
            {
                Time.timeScale = CurrentSpeed;
            }

            _context.Paused = _paused;
            _context.Held = _holds.IsHeld;
            _context.Changed.Raise();
        }
    }
}
