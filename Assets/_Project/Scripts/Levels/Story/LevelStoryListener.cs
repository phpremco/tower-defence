using System.Collections.Generic;
using Game.Economy;
using Game.Enemies;
using Game.Hero;
using Game.PathObjects;
using Game.Platform;
using Game.Towers;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Turns level events into dialogues: matches them against the level's story triggers, queues what plays, holds
    /// the game clock while a conversation shows and releases it afterwards. Inactive until the level allows story
    /// and a presenter is set on <see cref="LevelContext.Dialogue"/>, so tools and simulations never wait on a box.
    /// </summary>
    public sealed class LevelStoryListener : MonoBehaviour
    {
        // Input events arrive before Update, so the press that closes the pause menu would also reach the dialogue.
        private const int PauseGraceFrames = 1;
        private const int NeverPaused = -PauseGraceFrames - 1;

        private readonly List<DialogueDefinition> _selected = new List<DialogueDefinition>();
        private readonly List<DialogueDefinition> _pendingWave = new List<DialogueDefinition>();
        private readonly List<DialogueDefinition> _pending = new List<DialogueDefinition>();
        private readonly HashSet<EnemyDefinition> _sighted = new HashSet<EnemyDefinition>();
        private readonly HashSet<PathObjectDefinition> _used = new HashSet<PathObjectDefinition>();

        private LevelContext _context;
        private TimeController _time;
        private ProgressService _progress;
        private DialoguePlayer _player;
        private DialogueInput _dialogueInput;
        private System.Func<string, bool> _isSeen;
        private StoryTrigger[] _triggers;
        private IReadOnlyList<ScanHint> _scanHints;
        private DialogueDefinition _rearNestWarning;
        private RearNestWatch _rearNests;
        private IReadOnlyList<GateTip> _gateTips;
        private IReadOnlyList<Tower> _gates;
        private GateTipGate[] _gateStates;
        // A tip is marked seen only when it ends, so one already queued in this level must not be queued again.
        private readonly HashSet<DialogueDefinition> _tipsQueued = new HashSet<DialogueDefinition>();
        private OriginDirector _origins;
        private bool[] _fired;
        private bool _levelStartRaised;
        private bool _levelEndHandled;
        private bool _bossThisFrame;
        private bool _holding;
        private int _lastPausedFrame = NeverPaused;

        public DialoguePlayer Player => _player;

        /// <param name="scanHints">Dr. Voss's creature scans (the campaign story); null plays none.</param>
        /// <param name="rearNestWarning">Voss's warning for a rift behind the defence gate (once per save); null plays none.</param>
        /// <param name="gateTips">Voss's gate tips (once per save each); null plays none.</param>
        /// <param name="gates">The level's gates in the order of its data, which the hero's posts follow.</param>
        public void Initialize(LevelContext context, TimeController time, IInputProvider input, ProgressService progress, bool storyEnabled, IReadOnlyList<ScanHint> scanHints = null, DialogueDefinition rearNestWarning = null, IReadOnlyList<GateTip> gateTips = null, IReadOnlyList<Tower> gates = null)
        {
            _scanHints = scanHints;
            _context = context;
            _time = time;
            _progress = progress;
            _triggers = context.Level.StoryTriggers;
            _fired = new bool[_triggers.Length];
            _isSeen = _progress.HasSeenStory;
            _player = new DialoguePlayer(_progress.MarkStorySeen);
            _player.BusyChanged += OnBusyChanged;
            _dialogueInput = new DialogueInput(_player, input, () => InputFrozen);
            context.Dialogue = _player;
            context.StoryEnabled = storyEnabled;

            // Subscribed before the HUD, so StoryBusy is set before the result screen reads it on the same event.
            _context.Changed.Subscribe(OnChanged);
            _context.WaveStarted.Subscribe(OnWaveStarted);
            _context.EnemySpawned.Subscribe(OnEnemySpawned);
            _context.EnemyKilled.Subscribe(OnEnemyKilled);
            _context.PathObjectSpawned.Subscribe(OnPathObjectSpawned);
            WatchRearNests(rearNestWarning);
            WatchGates(gateTips, gates);
        }

        // Only while the warning is still unseen in this save and the level has a rift behind its gate is the map read at all.
        private void WatchRearNests(DialogueDefinition warning)
        {
            if (warning == null || warning.Lines.Count == 0 || !_context.StoryEnabled || _isSeen(warning.Id) || _context.Origins == null)
            {
                return;
            }

            var watch = new RearNestWatch(RearNests.BehindGate(_context.Level));
            if (!watch.HasAny)
            {
                return;
            }

            _rearNestWarning = warning;
            _rearNests = watch;
            _origins = _context.Origins;
            _origins.Signalled += OnOriginSignalled;
        }

        // Only while a tip is still unseen in this save are the gates weighed, and then only when one is hit or the money changes.
        private void WatchGates(IReadOnlyList<GateTip> tips, IReadOnlyList<Tower> gates)
        {
            if (tips == null || gates == null || gates.Count == 0 || !_context.StoryEnabled || !AnyUnseen(tips))
            {
                return;
            }

            _gateTips = tips;
            _gates = gates;
            _gateStates = new GateTipGate[gates.Count];
            _context.StructureHit.Subscribe(OnStructureHit);
            _context.Wallet.Changed += OnMoneyChanged;
        }

        private bool AnyUnseen(IReadOnlyList<GateTip> tips)
        {
            for (int i = 0; i < tips.Count; i++)
            {
                if (tips[i].Dialogue != null && !_isSeen(tips[i].Dialogue.Id))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnStructureHit(Vector3 _) => WeighGateTips();

        private void OnMoneyChanged(int _) => WeighGateTips();

        private void WeighGateTips()
        {
            if (!Active || _context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            for (int i = 0; i < _gates.Count; i++)
            {
                Tower gate = _gates[i];
                bool affordable = gate.CanUpgrade && _context.Wallet.CanAfford(gate.NextUpgradeCost);
                _gateStates[i] = new GateTipGate(gate.HealthFraction, !gate.IsWrecked, affordable);
            }

            Commander hero = _context.Hero;
            int heroGate = hero != null && hero.PostIndex < _gates.Count ? hero.PostIndex : -1;
            bool heroReady = hero != null && !hero.IsDown && !hero.IsFlying;
            DialogueDefinition tip = GateTipRule.Pick(_gateTips, _gateStates, heroGate, heroReady, _isSeen, _pending);
            if (tip != null && _tipsQueued.Add(tip))
            {
                _pending.Add(tip);
            }
        }

        // Releasing the hold here would restart time after the level scene is gone; the time controller resets it.
        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _dialogueInput.Dispose();
            _player.BusyChanged -= OnBusyChanged;
            if (_origins != null)
            {
                _origins.Signalled -= OnOriginSignalled;
            }

            _context.Changed.Unsubscribe(OnChanged);
            _context.WaveStarted.Unsubscribe(OnWaveStarted);
            _context.EnemySpawned.Unsubscribe(OnEnemySpawned);
            _context.EnemyKilled.Unsubscribe(OnEnemyKilled);
            _context.PathObjectSpawned.Unsubscribe(OnPathObjectSpawned);
            if (_gates != null)
            {
                _context.StructureHit.Unsubscribe(OnStructureHit);
                _context.Wallet.Changed -= OnMoneyChanged;
            }
        }

        private bool Active => _context.StoryEnabled && _player.Presenter != null;

        private bool InputFrozen => _context.Paused || Time.frameCount - _lastPausedFrame <= PauseGraceFrames;

        private void Update()
        {
            if (_context == null)
            {
                return;
            }

            if (_context.Paused)
            {
                _lastPausedFrame = Time.frameCount;
                return;
            }

            _player.Tick(Time.unscaledDeltaTime);
        }

        // Late, so every event of the frame is in before the boss rule picks, and a confirm that ends a conversation
        // is handled by the whole frame's input while the game is still held.
        private void LateUpdate()
        {
            if (_context == null)
            {
                return;
            }

            RaiseLevelStart();
            FlushPending();
            UpdateHold();
            if (CanStart)
            {
                _player.TryStart();
                UpdateHold();
            }
        }

        private bool CanStart => !_player.IsPlaying && _player.QueuedCount > 0 && !_context.InputBlocked && !_context.BriefingPending && !_context.ReconPending;

        private void RaiseLevelStart()
        {
            if (_levelStartRaised || !Active || _context.BriefingPending || _context.ReconPending || _context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            _levelStartRaised = true;
            Select(StoryEvent.LevelStart(), _pending);
        }

        // Boss alarm rule: a boss entrance and a wave start in the same frame make one box, and the boss line wins.
        private void FlushPending()
        {
            if (!_bossThisFrame)
            {
                EnqueueAll(_pendingWave);
            }

            _pendingWave.Clear();
            EnqueueAll(_pending);
            _pending.Clear();
            _bossThisFrame = false;
        }

        private void EnqueueAll(List<DialogueDefinition> dialogues)
        {
            for (int i = 0; i < dialogues.Count; i++)
            {
                _player.Enqueue(dialogues[i]);
            }
        }

        private void UpdateHold()
        {
            bool shouldHold = _player.IsPlaying;
            if (shouldHold == _holding)
            {
                return;
            }

            // Once the level has ended the result screen owns the clock, so no new hold is taken; a hold from before
            // the end is still given back (the time controller then only opens the input gate, it leaves the clock).
            if (shouldHold && _context.Outcome != LevelOutcome.Running)
            {
                return;
            }

            _holding = shouldHold;
            if (_holding)
            {
                _time.Hold(this);
            }
            else
            {
                _time.Release(this);
            }
        }

        private void Select(StoryEvent evt, List<DialogueDefinition> into)
        {
            _selected.Clear();
            if (StoryTriggerSelector.Select(_triggers, evt, _isSeen, _fired, _selected) > 0)
            {
                into.AddRange(_selected);
            }
        }

        private void OnWaveStarted()
        {
            if (Active)
            {
                Select(StoryEvent.WaveStart(_context.WaveNumber), _pendingWave);
            }
        }

        private void OnEnemySpawned(Enemy enemy)
        {
            if (!Active || enemy.Definition == null)
            {
                return;
            }

            if (_sighted.Add(enemy.Definition))
            {
                Select(StoryEvent.FirstSighting(enemy.Definition), _pending);
                // The scan follows the level's own sighting lines, once per campaign rather than once per level.
                DialogueDefinition scan = SightingHints.Pick(_scanHints, enemy.Definition, _isSeen, _pending);
                if (scan != null)
                {
                    _pending.Add(scan);
                }
            }

            if (enemy.Definition.IsBoss)
            {
                int before = _pending.Count;
                Select(StoryEvent.BossSpawn(enemy.Definition), _pending);
                _bossThisFrame |= _pending.Count > before;
            }
        }

        // The rift starts to open a few seconds before its first creature: Voss speaks then, and the HUD points at the rift.
        private void OnOriginSignalled(int origin, OriginSignal signal)
        {
            if (signal != OriginSignal.StateChanged || !Active || _context.Outcome != LevelOutcome.Running || !_rearNests.TryFire(origin, _origins.StateOf(origin)))
            {
                return;
            }

            if (!_isSeen(_rearNestWarning.Id) && !_pending.Contains(_rearNestWarning))
            {
                _pending.Add(_rearNestWarning);
            }

            LevelDefinition level = _context.Level;
            _context.RearNestWarned.Raise(level.Grid.CellToWorld(level.Nests[origin].Cell));
        }

        private void OnPathObjectSpawned(PathObject pathObject)
        {
            if (Active && pathObject.Definition != null && _used.Add(pathObject.Definition))
            {
                Select(StoryEvent.FirstUse(pathObject.Definition), _pending);
            }
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            if (Active && enemy.Definition != null && enemy.Definition.IsBoss)
            {
                Select(StoryEvent.BossDefeated(enemy.Definition), _pending);
            }
        }

        private void OnChanged()
        {
            if (_levelEndHandled || _context.Outcome == LevelOutcome.Running)
            {
                return;
            }

            _levelEndHandled = true;
            _pendingWave.Clear();
            if (_context.Outcome != LevelOutcome.Won)
            {
                _pending.Clear();
                _player.Clear();
                return;
            }

            // Queued at once, inside the event that ends the level, so the result screen sees StoryBusy and waits.
            // What the last kill raised this frame (a boss defeat, a first sighting) still plays, before the victory.
            if (Active)
            {
                Select(StoryEvent.LevelWon(), _pending);
            }

            EnqueueAll(_pending);
            _pending.Clear();
        }

        private void OnBusyChanged()
        {
            _context.StoryBusy = _player.IsBusy;
            _context.Changed.Raise();
        }
    }
}
