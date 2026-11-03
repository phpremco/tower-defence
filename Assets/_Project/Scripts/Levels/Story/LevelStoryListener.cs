using System.Collections.Generic;
using Game.Economy;
using Game.Enemies;
using Game.PathObjects;
using Game.Platform;
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
        private bool[] _fired;
        private bool _levelStartRaised;
        private bool _levelEndHandled;
        private bool _bossThisFrame;
        private bool _holding;
        private int _lastPausedFrame = NeverPaused;

        public DialoguePlayer Player => _player;

        public void Initialize(LevelContext context, TimeController time, IInputProvider input, ProgressService progress, bool storyEnabled)
        {
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
            _context.Changed.Unsubscribe(OnChanged);
            _context.WaveStarted.Unsubscribe(OnWaveStarted);
            _context.EnemySpawned.Unsubscribe(OnEnemySpawned);
            _context.EnemyKilled.Unsubscribe(OnEnemyKilled);
            _context.PathObjectSpawned.Unsubscribe(OnPathObjectSpawned);
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
            }

            if (enemy.Definition.IsBoss)
            {
                int before = _pending.Count;
                Select(StoryEvent.BossSpawn(enemy.Definition), _pending);
                _bossThisFrame |= _pending.Count > before;
            }
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
