using System.Collections.Generic;
using Game.Core;
using Game.Enemies;
using Game.Economy;
using Game.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Levels
{
    /// <summary>Runs the wave timeline, decides win or loss and hands out the reward. What happens next is up to the result screen.</summary>
    public sealed class LevelController : MonoBehaviour
    {
        private readonly List<SpawnRequest> _due = new List<SpawnRequest>();

        // Nest spawns whose origin is not open yet: they come out as soon as the maw is up.
        private readonly List<SpawnRequest> _held = new List<SpawnRequest>();
        private System.Random _random;
        private OriginDirector _origins;

        private LevelContext _context;
        private IInputProvider _input;
        private WaveDefinition[] _waves;
        private EnemySpawner _spawner;
        private WaveScheduler _scheduler;
        private int _nextWaveIndex;
        private bool _skipAllowed;
        private ProgressService _progress;

        public void Initialize(LevelContext context, WaveDefinition[] waves, EnemySpawner spawner, IInputProvider input, ProgressService progress, bool skipAllowed, System.Random random = null)
        {
            _random = random ?? new System.Random();
            _context = context;
            _waves = waves;
            _spawner = spawner;
            _input = input;
            _skipAllowed = skipAllowed;
            _progress = progress;

            _context.NextWaveCountdown = waves[0].StartDelay;
            _origins = CreateOrigins(context, waves);
            _context.Origins = _origins;
            PublishRoutes(0);
            _context.BaseHealth.Died += OnBaseDied;
            _input.SkipWave += OnSkipWave;
            _context.SkipWaveRequested.Subscribe(OnSkipWave);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.SkipWave -= OnSkipWave;
            }

            _context?.SkipWaveRequested.Unsubscribe(OnSkipWave);
        }

        private void Update()
        {
            if (_context == null || _context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            _spawner.Step(Time.deltaTime);
            _origins?.Advance(Time.deltaTime);
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

        // A wave called early (force) sends every creature out at once, opening their origins on the spot.
        private void SpawnDueEnemies(float deltaTime, bool force = false)
        {
            _due.Clear();
            _due.AddRange(_held);
            _held.Clear();
            _scheduler.Advance(deltaTime, _due);
            for (int i = 0; i < _due.Count; i++)
            {
                SpawnRequest request = _due[i];
                int nest = NestOf(request);
                if (nest >= 0 && _origins != null)
                {
                    if (force)
                    {
                        _origins.ForceOpen(nest);
                    }
                    else if (!_origins.IsReady(nest))
                    {
                        _held.Add(request);
                        continue;
                    }
                }

                _spawner.Spawn(request.Enemy, request.PathIndex, request.Source, nest);
                if (nest >= 0)
                {
                    _origins?.NotifyEmerged(nest);
                }
            }

            if (_scheduler.IsFinished && _held.Count == 0)
            {
                _scheduler = null;
                _context.NextWaveCountdown = _nextWaveIndex < _waves.Length ? _waves[_nextWaveIndex].StartDelay : 0f;
                PublishRoutes(_nextWaveIndex);
                _context.Changed.Raise();
            }
        }

        // The nest a nest spawn comes out of: drawn when the level started, so its origin could open ahead of it.
        private int NestOf(SpawnRequest request)
        {
            if (request.Source != SpawnSource.Nest || _origins == null)
            {
                return NestAssignment.None;
            }

            return _origins.Wave(_nextWaveIndex - 1).NestOf(request.Group, request.Index);
        }

        // Every nest spawn of every wave gets its nest now, so the origins any wave uses are marked from the start.
        private OriginDirector CreateOrigins(LevelContext context, WaveDefinition[] waves)
        {
            SpawnOriginDefinition definition = context.Level.SpawnOrigins;
            if (definition == null)
            {
                return null;
            }

            var assignments = new NestAssignment[waves.Length];
            for (int w = 0; w < waves.Length; w++)
            {
                assignments[w] = NestAssignment.Draw(waves[w], context.Difficulty, context.Level.Nests, _random);
            }

            return new OriginDirector(context.Level.Nests.Length, assignments, definition.Timing);
        }

        private void StartNextWave()
        {
            _scheduler = new WaveScheduler(_waves[_nextWaveIndex], _context.Difficulty, _random);
            _origins?.BeginWave(_nextWaveIndex, _scheduler);
            _spawner.WaveHealthScale = _waves[_nextWaveIndex].HealthMultiplier;
            PublishRoutes(_nextWaveIndex);
            _nextWaveIndex++;
            _context.WaveNumber = _nextWaveIndex;
            _context.NextWaveCountdown = 0f;
            _context.ComingBoss = BossOf(_waves[_nextWaveIndex - 1]);
            _context.BossWave = _context.ComingBoss != null;
            _context.Changed.Raise();
            _context.WaveStarted.Raise();
            if (_context.BossWave)
            {
                _context.BossWaveStarted.Raise();
            }
        }

        private static EnemyDefinition BossOf(WaveDefinition wave)
        {
            foreach (SpawnGroup group in wave.Groups)
            {
                if (group.Enemy != null && group.Enemy.IsBoss)
                {
                    return group.Enemy;
                }
            }

            return null;
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
            if (!_skipAllowed || _context.Outcome != LevelOutcome.Running || _context.InputBlocked)
            {
                return;
            }

            if (_scheduler != null)
            {
                SpawnDueEnemies(float.MaxValue, true);
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
            _context.Result = RewardCalculator.Compute(_context.Level, _context.Difficulty, _progress, outcome == LevelOutcome.Won, _context.BaseHealth.Fraction);
            if (!_context.Practice)
            {
                RewardCalculator.Apply(_context.Result, _context.Level, _context.Difficulty, _progress);
            }
            _context.Changed.Raise();
        }
    }
}

