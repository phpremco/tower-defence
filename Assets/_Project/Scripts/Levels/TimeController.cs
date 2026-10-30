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
        private bool _paused;
        private bool _held;

        public float CurrentSpeed => _paused || _held ? 0f : _speedSteps[_speedIndex];

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
            _speedIndex = (_speedIndex + 1) % _speedSteps.Length;
            Apply();
        }

        public void TogglePause()
        {
            _paused = !_paused;
            Apply();
        }

        /// <summary>Freezes time without opening the pause menu, while a panel such as the briefing is up.</summary>
        public void Hold(bool held)
        {
            _held = held;
            Apply();
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

        private void Apply()
        {
            Time.timeScale = _paused || _held ? 0f : _speedSteps[_speedIndex];
            _context.Paused = _paused;
            _context.Changed.Raise();
        }
    }
}
