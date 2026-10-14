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

        public void Initialize(LevelContext context, IInputProvider input)
        {
            _context = context;
            _input = input;
            _input.SpeedToggled += OnSpeedToggled;
            _input.PauseToggled += OnPauseToggled;
            Apply();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.SpeedToggled -= OnSpeedToggled;
                _input.PauseToggled -= OnPauseToggled;
            }

            Time.timeScale = 1f;
        }

        private void OnSpeedToggled()
        {
            _speedIndex = (_speedIndex + 1) % _speedSteps.Length;
            Apply();
        }

        private void OnPauseToggled()
        {
            _paused = !_paused;
            Apply();
        }

        private void Apply()
        {
            Time.timeScale = _paused ? 0f : _speedSteps[_speedIndex];
            _context.Changed.Raise();
        }

        public float CurrentSpeed => _paused ? 0f : _speedSteps[_speedIndex];
    }
}
