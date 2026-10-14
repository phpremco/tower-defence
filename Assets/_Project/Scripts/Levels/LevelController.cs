using System.Collections.Generic;
using Game.Core;
using Game.Economy;
using Game.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Levels
{
    /// <summary>Runs the wave timeline, decides win or loss and hands out the reward. After the level, confirm leads to the upgrade screen (win) or the map (loss).</summary>
    public sealed class LevelController : MonoBehaviour
    {
        private readonly List<SpawnRequest> _due = new List<SpawnRequest>();

        private LevelContext _context;
        private IInputProvider _input;
        private WaveDefinition[] _waves;
        private EnemySpawner _spawner;
        private WaveScheduler _scheduler;
        private int _nextWaveIndex;
        private bool _skipAllowed;
        private ProgressService _progress;

        public void Initialize(LevelContext context, WaveDefinition[] waves, EnemySpawner spawner, IInputProvider input, ProgressService progress, bool skipAllowed)
        {
            _context = context;
            _waves = waves;
            _spawner = spawner;
            _input = input;
            _skipAllowed = skipAllowed;
            _progress = progress;

            _context.NextWaveCountdown = waves[0].StartDelay;
            PublishRoutes(0);
            _context.BaseHealth.Died += OnBaseDied;
            _input.SkipWave += OnSkipWave;
            _input.Confirmed += OnConfirmed;
            _context.SkipWaveRequested.Subscribe(OnSkipWave);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.SkipWave -= OnSkipWave;
                _input.Confirmed -= OnConfirmed;
            }

            _context?.SkipWaveRequested.Unsubscribe(OnSkipWave);
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
                _spawner.Spawn(_due[i].Enemy, _due[i].PathIndex);
            }

            if (_scheduler.IsFinished)
            {
                _scheduler = null;
                _context.NextWaveCountdown = _nextWaveIndex < _waves.Length ? _waves[_nextWaveIndex].StartDelay : 0f;
                PublishRoutes(_nextWaveIndex);
                _context.Changed.Raise();
            }
        }

        private void StartNextWave()
        {
            _scheduler = new WaveScheduler(_waves[_nextWaveIndex], _context.Difficulty);
            PublishRoutes(_nextWaveIndex);
            _nextWaveIndex++;
            _context.WaveNumber = _nextWaveIndex;
            _context.NextWaveCountdown = 0f;
            _context.Changed.Raise();
        }

        // Tells the view which flying routes the given wave uses. Beyond the last wave nothing is shown.
        private void PublishRoutes(int waveIndex)
        {
            _context.ActiveRoutes.Clear();
            if (waveIndex < _waves.Length)
            {
                foreach (SpawnGroup group in _waves[waveIndex].Groups)
                {
                    if (group.PathIndex >= 0 && group.PathIndex < _context.Level.Paths.Length && _context.Level.Paths[group.PathIndex].Flying)
                    {
                        _context.ActiveRoutes.Add(group.PathIndex);
                    }
                }
            }

            _context.RoutesChanged.Raise();
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
            _context.Result = RewardCalculator.Compute(_context.Level, _context.Difficulty, _progress, outcome == LevelOutcome.Won);
            RewardCalculator.Apply(_context.Result, _context.Level, _context.Difficulty, _progress);
            _context.Changed.Raise();
        }

        private void OnConfirmed()
        {
            if (_context.Outcome != LevelOutcome.Running)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(_context.Outcome == LevelOutcome.Won ? SceneNames.Base : SceneNames.WorldMap);
            }
        }
    }
}




