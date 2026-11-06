using System;
using System.Collections.Generic;
using Game.Bosses;
using Game.Enemies;
using Game.Hero;
using Game.Levels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Game.UI
{
    /// <summary>
    /// Mercer meets the continent bosses (Docs/BOSS_INTROS.md). When a boss has come out of its entrance for the first
    /// time ever, a short beat plays: the game slows to a share of its speed, the camera glides to the boss, Mercer
    /// recoils (Startled) and says his line over his head while his visor cut-in slides in, then ORA and Dr. Voss answer
    /// in a compact box at the right edge (where the cut-in was) that types fast and moves on by itself; the Codex notes
    /// the new entry. Any button (keyboard, mouse or gamepad) ends it early, and it never runs while the game is paused, held by a story box or
    /// ended. Later meetings only get a bark; during the fight Mercer calls out the boss's signature moments, under a
    /// cooldown. Reads the boss brains and the intro data; drives views and the clock only.
    /// </summary>
    public sealed class BossIntroDirector : MonoBehaviour
    {
        // Two barks keep at least this many game seconds apart, whichever boss they are about.
        private const float BarkGapSeconds = 6f;
        private const float BarkSeconds = 2.6f;
        private const float ToastSeconds = 3.2f;
        // The camera frames the boss and the hero together while they are this close, else the boss alone.
        private const float FrameBothDistance = 16f;
        private static readonly string[] OutStates = { "Roar", "Road", "Surfacing", "Bursting" };

        private readonly List<Watch> _watches = new List<Watch>(2);
        private readonly BarkCooldown _cooldown = new BarkCooldown(BarkGapSeconds);
        private LevelContext _context;
        private TimeController _time;
        private StoryDefinition _story;
        private CameraPanZoom _pan;
        private HeroSpeechBubble _bubble;
        private BossCutIn _cutIn;
        private CodexToast _toast;
        private DialoguePlayer _answers;
        private BossAnswerBox _box;
        private Beat _beat;
        private IDisposable _skipListener;
        private bool _skip;

        private sealed class Watch
        {
            public ContinentBossBrain Brain;
            public Enemy Enemy;
            public BossIntroDefinition Intro;
            public bool Met;
            public readonly int[] Counts = new int[MomentCount];
        }

        private sealed class Beat
        {
            public Watch Watch;
            public float Age;
            public bool Answering;
            public float LineHeld;
        }

        private static readonly int MomentCount = Enum.GetValues(typeof(BossMoment)).Length;

        /// <summary>True while a first-meeting beat plays (slow motion, cut-in, answers).</summary>
        public bool InBeat => _beat != null;

        /// <summary>How many beats and barks have played in this level, for tests and probes.</summary>
        public int Beats { get; private set; }

        public int Barks { get; private set; }

        /// <summary>The line over the hero's head now, or empty.</summary>
        public string ShownLine => _bubble != null ? _bubble.Line : string.Empty;

        /// <summary>The answer box, for tests and probes.</summary>
        public BossAnswerBox AnswerBox => _box;

        public void Initialize(LevelContext context, TimeController time, StoryDefinition story, RectTransform safe, RectTransform worldLabels, Camera camera, HudKeepOut keepOut, PortraitProvider portraits)
        {
            _context = context;
            _time = time;
            _story = story;
            _pan = FindAnyObjectByType<CameraPanZoom>();
            _bubble = gameObject.AddComponent<HeroSpeechBubble>();
            _bubble.Initialize(worldLabels, camera, keepOut);
            _cutIn = gameObject.AddComponent<BossCutIn>();
            _cutIn.Initialize(safe);
            keepOut.Add(_cutIn.Panel);
            _toast = gameObject.AddComponent<CodexToast>();
            _toast.Initialize(safe);
            keepOut.Add(_toast.Panel);
            _box = gameObject.AddComponent<BossAnswerBox>();
            _answers = new DialoguePlayer(null);
            _box.Initialize(safe, portraits);
            keepOut.Add(_box.Box);
            _answers.Presenter = _box;
            context.EnemySpawned.Subscribe(OnSpawned);
        }

        private void OnDestroy()
        {
            _context?.EnemySpawned.Unsubscribe(OnSpawned);
            EndBeat();
        }

        private void OnSpawned(Enemy enemy)
        {
            if (!_context.BossIntros || enemy.Definition.Boss == null || _story == null)
            {
                return;
            }

            BossIntroDefinition intro = _story.IntroFor(enemy.Definition);
            var brain = enemy.GetComponent<ContinentBossBrain>();
            if (intro == null || brain == null)
            {
                return;
            }

            _watches.RemoveAll(w => w.Brain == brain);
            _watches.Add(new Watch { Brain = brain, Enemy = enemy, Intro = intro });
        }

        private bool FieldIsLive => BossIntroRules.FieldIsLive(
            _context.Paused,
            _context.Held || _context.InputBlocked || _context.BriefingPending || _context.ReconPending,
            false,
            _context.Outcome);

        private void LateUpdate()
        {
            if (_beat != null)
            {
                UpdateBeat(Time.unscaledDeltaTime);
                return;
            }

            for (int i = _watches.Count - 1; i >= 0; i--)
            {
                Watch watch = _watches[i];
                if (watch.Brain == null || !watch.Enemy.IsAlive)
                {
                    _watches.RemoveAt(i);
                    continue;
                }

                if (!watch.Met)
                {
                    TryMeet(watch);
                }
                else
                {
                    CallOutMoments(watch);
                }
            }
        }

        // Once the boss is out of its entrance (visible, roaring or on its way), the meeting plays as soon as the field is live.
        private void TryMeet(Watch watch)
        {
            if (watch.Brain.Hidden || Array.IndexOf(OutStates, watch.Brain.State) < 0 || !FieldIsLive)
            {
                return;
            }

            watch.Met = true;
            for (int m = 0; m < MomentCount; m++)
            {
                watch.Counts[m] = BossMoments.Count(watch.Brain.Record, (BossMoment)m);
            }

            _cooldown.Hold(watch.Brain, Time.time);
            if (BossIntroRules.MeetingFor(!watch.Brain.FirstMeeting) == BossMeeting.First)
            {
                StartBeat(watch);
            }
            else
            {
                Say(BossIntroRules.Pick(watch.Intro.RepeatBarks, UnityEngine.Random.value));
            }
        }

        // A signature moment the boss just showed (its count went up) draws its line, cooldown and field allowing.
        private void CallOutMoments(Watch watch)
        {
            BossRecord record = watch.Brain.Record;
            for (int m = 0; m < MomentCount; m++)
            {
                int count = BossMoments.Count(record, (BossMoment)m);
                if (count <= watch.Counts[m])
                {
                    continue;
                }

                watch.Counts[m] = count;
                string line = watch.Intro.BarkFor((BossMoment)m);
                if (line != null && FieldIsLive && _cooldown.TryBark(watch.Brain, watch.Intro.BarkCooldownSeconds, Time.time))
                {
                    Say(line);
                }
            }
        }

        private void Say(string line)
        {
            Commander hero = _context.Hero;
            if (hero == null || hero.IsDown || string.IsNullOrEmpty(line))
            {
                return;
            }

            hero.CallOut();
            _bubble.Say(hero.transform, line, BarkSeconds);
            Barks++;
        }

        private void StartBeat(Watch watch)
        {
            BossIntroDefinition intro = watch.Intro;
            Enemy boss = watch.Enemy;
            Commander hero = _context.Hero;
            _beat = new Beat { Watch = watch };
            Beats++;
            _time.SlowDown(this, intro.SlowFactor);
            _skip = false;
            _skipListener = InputSystem.onAnyButtonPress.CallOnce(_ => _skip = true);
            _toast.Show(boss.Definition.DisplayName, ToastSeconds);
            Vector3 focus = boss.Position;
            if (hero != null && !hero.IsDown)
            {
                if (Vector3.Distance(hero.transform.position, boss.Position) < FrameBothDistance)
                {
                    focus = Vector3.Lerp(boss.Position, hero.transform.position, 0.5f);
                }

                hero.Startle(boss.Position);
                _bubble.Say(hero.transform, intro.HeroLine, intro.FocusSeconds + intro.HeroLineSeconds);
                HeroArtView art = hero.GetComponentInChildren<HeroArtView>();
                if (art != null)
                {
                    _cutIn.Show(art, boss.transform, intro.CutInTint);
                }
            }

            _pan?.GlideTo(focus);
        }

        // The hero's line first, then the answers type out and move on by themselves; any button, a pause or the end of
        // the level ends it, and it never runs past its time.
        private void UpdateBeat(float deltaTime)
        {
            BossIntroDefinition intro = _beat.Watch.Intro;
            _beat.Age += deltaTime;
            bool over = _skip || !FieldIsLive || _beat.Age >= intro.MaxBeatSeconds;
            if (!_beat.Answering && !over && _beat.Age >= intro.FocusSeconds + intro.HeroLineSeconds)
            {
                _beat.Answering = true;
                _cutIn.Hide();
                if (intro.Answer != null)
                {
                    _answers.Enqueue(intro.Answer);
                    _answers.TryStart();
                }
            }

            if (_beat.Answering && !over)
            {
                over = !_answers.IsPlaying || AdvanceAnswer(intro, deltaTime);
            }

            if (over)
            {
                EndBeat();
            }
        }

        // Types the line at the intro's speed and moves on once it has held; true when the last line is done.
        private bool AdvanceAnswer(BossIntroDefinition intro, float deltaTime)
        {
            _answers.Tick(deltaTime * intro.TypeSpeed / DialoguePlayer.DefaultCharactersPerSecond);
            if (!_answers.LineComplete)
            {
                return false;
            }

            _beat.LineHeld += deltaTime;
            if (_beat.LineHeld < intro.LineHoldSeconds)
            {
                return false;
            }

            _beat.LineHeld = 0f;
            _answers.Advance();
            return !_answers.IsPlaying;
        }

        private void EndBeat()
        {
            if (_beat == null)
            {
                return;
            }

            _beat = null;
            _skipListener?.Dispose();
            _skipListener = null;
            if (_time != null)
            {
                _time.EndSlowDown(this);
            }

            _answers.Clear();
            _cutIn.Hide();
            _bubble.Hide();
        }
    }
}
