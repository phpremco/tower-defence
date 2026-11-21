using System.Collections.Generic;
using Game.Balance;
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
        private WaveClock _clock;
        private WaveCaller _caller;
        private bool _fastForwardAllowed;
        private ProgressService _progress;
        private int _firstRaid = FirstRaidRule.None;
        private int _raidWarningWave = FirstRaidRule.None;

        public void Initialize(LevelContext context, WaveDefinition[] waves, EnemySpawner spawner, IInputProvider input, ProgressService progress, WaveCallDefinition callRules, bool fastForwardAllowed, System.Random random = null)
        {
            _random = random ?? new System.Random();
            _context = context;
            _waves = waves;
            _spawner = spawner;
            _input = input;
            _fastForwardAllowed = fastForwardAllowed;
            _clock = new WaveClock(StartDelays(waves));
            _firstRaid = FirstRaidRule.FirstRaid(waves);
            _raidWarningWave = FirstRaidRule.WarningWave(_firstRaid, BalanceRules.Default.FirstRaidWarningWaves);
            _caller = new WaveCaller(callRules);
            _progress = progress;

            _context.NextWaveCountdown = _clock.Countdown;
            _context.NextWave = waves.Length > 0 ? waves[0] : null;
            _origins = CreateOrigins(context, waves);
            _context.Origins = _origins;
            PublishRoutes(0);
            _context.BaseHealth.Died += OnBaseDied;
            _input.CallWave += OnCallWave;
            _context.CallWaveRequested.Subscribe(OnCallWave);
            _context.FastForwardWaveRequested.Subscribe(OnFastForward);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.CallWave -= OnCallWave;
            }

            _context?.CallWaveRequested.Unsubscribe(OnCallWave);
            _context?.FastForwardWaveRequested.Unsubscribe(OnFastForward);
        }

        private void Update()
        {
            if (_context == null || _context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            _spawner.Step(Time.deltaTime);
            _origins?.Advance(Time.deltaTime);
            _caller.Tick(Time.deltaTime);
            if (_scheduler != null)
            {
                SpawnDueEnemies(Time.deltaTime);
            }
            else if (_clock.Tick(Time.deltaTime))
            {
                StartNextWave();
            }
            else
            {
                _context.NextWaveCountdown = _clock.Countdown;
            }

            if (_clock.IsCleared(_spawner.Alive))
            {
                Finish(LevelOutcome.Won);
                return;
            }

            _context.WaveCallBlock = _caller.Check(WaveCallSituation.Of(_context), _clock);
        }

        private static float[] StartDelays(WaveDefinition[] waves)
        {
            var delays = new float[waves.Length];
            for (int i = 0; i < waves.Length; i++)
            {
                delays[i] = waves[i].StartDelay;
            }

            return delays;
        }

        /// <summary>How many waves the player called early in this level, and the coins those calls paid (for probes and tests).</summary>
        public int EarlyCalls => _caller != null ? _caller.Calls : 0;

        public int EarlyBonusPaid => _caller != null ? _caller.BonusPaid : 0;

        /// <summary>
        /// Why the level has not ended yet, for a stalled run (round 25): whether the wave is still spawning, the nest spawns
        /// held for an origin, and every creature still counted alive with its state.
        /// </summary>
        public string SpawnDiagnostic()
        {
            var text = new System.Text.StringBuilder();
            text.Append(_scheduler == null ? "no wave spawning" : _scheduler.IsFinished ? "wave spawned" : "wave still spawning");
            text.Append(", held ").Append(_held.Count).Append(", alive ").Append(_spawner.Alive).Append(" (living ").Append(_spawner.Living.Count).Append(')');
            foreach (Enemy enemy in _spawner.Living)
            {
                text.Append(System.FormattableString.Invariant($"; {enemy.Definition.name} {(enemy.IsAlive ? "alive" : "dead")} hp {enemy.Health.Current:0}/{enemy.Health.Max:0} at {enemy.Position.x:0.0},{enemy.Position.z:0.0} progress {enemy.PathProgress:0.0}{(enemy.IsDirected ? " directed" : string.Empty)}"));
            }

            return text.ToString();
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
                _clock.EndSpawning();
                _context.NextWaveCountdown = _clock.Countdown;
                PublishRoutes(_clock.Started);
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

            return _origins.Wave(_clock.Started - 1).NestOf(request.Group, request.Index);
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
            int index = _clock.BeginWave();
            WaveDefinition wave = _waves[index];
            _scheduler = new WaveScheduler(wave, _context.Difficulty, _random);
            _origins?.BeginWave(index, _scheduler);
            _spawner.WaveHealthScale = wave.HealthMultiplier;
            _spawner.WaveBossHealthScale = wave.BossHealthScale;
            PublishRoutes(index);
            _context.WaveNumber = _clock.Started;
            _context.NextWave = _clock.AllStarted ? null : _waves[_clock.Started];
            _context.NextWaveCountdown = 0f;
            // Waves overlap: a wave without a boss that starts while an earlier wave's boss still lives keeps that boss on the HUD.
            EnemyDefinition boss = BossOf(wave);
            _context.ComingBoss = boss != null || _context.BossesAlive == 0 ? boss : _context.ComingBoss;
            _context.BossWave = boss != null || (_context.BossesAlive > 0 && _context.ComingBoss != null);
            _context.Changed.Raise();
            _context.WaveStarted.Raise();
            if (boss != null)
            {
                _context.BossWaveStarted.Raise();
            }

            WarnOfTheFirstRaid(index);
        }

        // The level's first air raid is announced a few waves ahead (FirstRaidRule); the coins for the air defence are in the start money (StartMoneyRule).
        private void WarnOfTheFirstRaid(int index)
        {
            if (index != _raidWarningWave)
            {
                return;
            }

            _context.AirRaidWarned.Raise(new AirRaidWarning(_firstRaid - index));
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

        // The player's call (round 73): the next wave starts now, on top of whatever is still on the field, and the seconds the
        // countdown had left are paid out. Only the countdown between waves can be cut short, so a wave is never cut off.
        private void OnCallWave()
        {
            if (_context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            WaveCallBlock block = _caller.Check(WaveCallSituation.Of(_context), _clock);
            _context.WaveCallBlock = block;
            if (block != WaveCallBlock.None)
            {
                return;
            }

            int bonus = _caller.Accept(_clock.Countdown);
            if (bonus > 0)
            {
                _context.Wallet.Add(bonus);
            }

            StartNextWave();
            _context.WaveCallBlock = _caller.Check(WaveCallSituation.Of(_context), _clock);
            _context.WaveCalledEarly.Raise(bonus);
        }

        // Developer fast-forward for tests and tools: the rest of the running wave comes out at once and the next wave starts.
        private void OnFastForward()
        {
            if (!_fastForwardAllowed || _context.Outcome != LevelOutcome.Running || _context.InputBlocked)
            {
                return;
            }

            if (_scheduler != null)
            {
                SpawnDueEnemies(float.MaxValue, true);
            }

            if (!_clock.AllStarted)
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
            _context.WaveCallBlock = WaveCallBlock.LevelOver;
            _context.Result = RewardCalculator.Compute(_context.Level, _context.Difficulty, _progress, outcome == LevelOutcome.Won, _context.BaseHealth.Fraction);
            if (!_context.Practice)
            {
                RewardCalculator.Apply(_context.Result, _context.Level, _context.Difficulty, _progress);
            }
            _context.Changed.Raise();
        }
    }
}

