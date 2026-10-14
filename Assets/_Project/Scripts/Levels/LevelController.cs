using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using Game.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Levels
{
    /// <summary>Runs the wave timeline and decides win or loss. Restarts the game from Boot once the level has ended.</summary>
    public sealed class LevelController : MonoBehaviour
    {
        private readonly List<EnemyDefinition> _due = new List<EnemyDefinition>();

        private LevelContext _context;
        private IInputProvider _input;
        private WaveDefinition[] _waves;
        private EnemySpawner _spawner;
        private WaveScheduler _scheduler;
        private int _nextWaveIndex;
        private bool _skipAllowed;

        public void Initialize(LevelContext context, WaveDefinition[] waves, EnemySpawner spawner, IInputProvider input, bool skipAllowed)
        {
            _context = context;
            _waves = waves;
            _spawner = spawner;
            _input = input;
            _skipAllowed = skipAllowed;

            _context.NextWaveCountdown = waves[0].StartDelay;
            _context.BaseHealth.Died += OnBaseDied;
            _input.SkipWave += OnSkipWave;
            _input.Confirmed += OnConfirmed;
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.SkipWave -= OnSkipWave;
                _input.Confirmed -= OnConfirmed;
            }
        }

        private void Update()
        {
            if (_context == null || _context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            if (_scheduler != null)
            {
                SpawnDueEnemies(Time.deltaTime);
            }
            else if (_nextWaveIndex < _waves.Length)
            {
                _context.NextWaveCountdown -= Time.deltaTime;
                if (_context.NextWaveCountdown <= 0f)
                {
                    StartNextWave();
                }
            }

            if (_scheduler == null && _nextWaveIndex >= _waves.Length && _spawner.Alive == 0)
            {
                Finish(LevelOutcome.Won);
            }
        }

        private void SpawnDueEnemies(float deltaTime)
        {
            _due.Clear();
            _scheduler.Advance(deltaTime, _due);
            for (int i = 0; i < _due.Count; i++)
            {
                _spawner.Spawn(_due[i]);
            }

            if (_scheduler.IsFinished)
            {
                _scheduler = null;
                _context.NextWaveCountdown = _nextWaveIndex < _waves.Length ? _waves[_nextWaveIndex].StartDelay : 0f;
                _context.Changed.Raise();
            }
        }

        private void StartNextWave()
        {
            _scheduler = new WaveScheduler(_waves[_nextWaveIndex], _context.Difficulty);
            _nextWaveIndex++;
            _context.WaveNumber = _nextWaveIndex;
            _context.NextWaveCountdown = 0f;
            _context.Changed.Raise();
        }

        private void OnSkipWave()
        {
            if (!_skipAllowed || _context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            if (_scheduler != null)
            {
                SpawnDueEnemies(float.MaxValue);
            }

            if (_nextWaveIndex < _waves.Length)
            {
                StartNextWave();
            }
        }

        private void OnBaseDied() => Finish(LevelOutcome.Lost);

        private void Finish(LevelOutcome outcome)
        {
            if (_context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            _context.Outcome = outcome;
            _context.Changed.Raise();
        }

        private void OnConfirmed()
        {
            if (_context.Outcome != LevelOutcome.Running)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneNames.Boot);
            }
        }
    }
}
