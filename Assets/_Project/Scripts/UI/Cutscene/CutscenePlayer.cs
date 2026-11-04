using System;
using System.Collections.Generic;
using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// Plays a cutscene: drives the <see cref="CutsceneSequencer"/> on unscaled time, builds each set the first time
    /// one of its shots comes up (behind black), shows it while its shots play, moves the camera along the shot's
    /// keys, speaks the dialogue in the game's dialogue box and frames the picture (bars, fades, title, radar). The
    /// end and a confirmed skip do the same thing: the intro counts as seen, the picture fades to black and the
    /// handoff loads the first level (or returns to the menu on a replay).
    /// </summary>
    public sealed class CutscenePlayer : MonoBehaviour
    {
        private const float VolumeFadeSeconds = 0.5f;
        private const float FinishFadeSeconds = 0.9f;
        private const float TitleStartShare = 0.3f;
        private const float NearClip = 0.1f;
        private const float FarClip = 600f;
        // The dialogue box sits low and a little smaller, just above the bottom bar, so the picture stays free.
        private const float DialogueGap = 6f;
        private const float DialogueScale = 0.86f;
        // A seek runs particles forward at most this far, which covers every effect and keeps the jump cheap.
        private const float MaxParticleSeekSeconds = 8f;
        private const uint ParticleSeed = 6907u;

        private readonly Dictionary<CutsceneSet, ICutsceneSet> _sets = new Dictionary<CutsceneSet, ICutsceneSet>();
        private readonly Dictionary<CutsceneSet, GameObject> _roots = new Dictionary<CutsceneSet, GameObject>();
        private readonly Dictionary<CutsceneSet, Volume> _volumes = new Dictionary<CutsceneSet, Volume>();
        private readonly List<CameraKey> _keys = new List<CameraKey>();
        private readonly SkipConfirm _skip = new SkipConfirm();

        private CutsceneRequest _request;
        private CutsceneContext _context;
        private ProgressService _progress;
        private CutsceneSequencer _sequencer;
        private DialoguePlayer _dialogue;
        private CutsceneInput _input;
        private CutsceneOverlay _overlay;
        private DialogueView _dialogueView;
        private CutsceneCamera _camera;
        private CutsceneAudio _audio;
        private ICutsceneSet _current;
        private CutsceneSet _currentSet;
        private float _finishFade = -1f;
        private bool _handedOff;

        public CutsceneSequencer Sequencer => _sequencer;

        public DialoguePlayer Dialogue => _dialogue;

        public CutsceneOverlay Overlay => _overlay;

        /// <summary>The set of the current shot, for probes.</summary>
        public ICutsceneSet CurrentSet => _current;

        /// <summary>True from the end (or a confirmed skip) until the next scene loads.</summary>
        public bool IsFinishing => _finishFade >= 0f;

        public void Initialize(CutsceneRequest request, CampaignCatalog catalog, Camera camera, Func<VisualTheme, Volume> createVolume, IPlatformSettings platform, IInputProvider input, ProgressService progress, AudioMix mix, SfxLibrary library)
        {
            _request = request;
            _progress = progress;
            CutsceneDefinition cutscene = request.Cutscene;
            // The cast's Blender sprites dress every glow, trail and crack the sets build from here on.
            CutsceneFxSet.Active = cutscene.Cast != null ? cutscene.Cast.Fx : null;

            camera.orthographic = false;
            camera.nearClipPlane = NearClip;
            camera.farClipPlane = FarClip;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            _camera = new CutsceneCamera(camera, cutscene.CameraFeel);

            _audio = new GameObject("CutsceneAudio").AddComponent<CutsceneAudio>();
            _audio.transform.SetParent(transform, false);
            _audio.Initialize(library, mix, cutscene.Cast);

            _overlay = gameObject.AddComponent<CutsceneOverlay>();
            _overlay.Build(OnSkipPressed);

            _dialogue = new DialoguePlayer(null) { AutoAdvance = request.AutoAdvance };
            _dialogueView = gameObject.AddComponent<DialogueView>();
            var portraits = new PortraitProvider(cutscene.Cast != null ? cutscene.Cast.Hero : null);
            _dialogueView.Initialize(_overlay.DialogueRoot, _dialogue, portraits);
            _dialogue.Presenter = _dialogueView;
            Prewarm(cutscene, portraits);
            _input = new CutsceneInput(input, _dialogue, OnSkipPressed);

            _context = new CutsceneContext(cutscene, cutscene.FieldLevel(catalog), catalog, camera, createVolume, platform.ParticleScale, platform.FogEnabled, _audio);
            _sequencer = new CutsceneSequencer(cutscene.Shots, StartDialogue, () => _dialogue.IsBusy) { Speed = request.Speed };
            _sequencer.ShotStarted += OnShotStarted;
            _sequencer.CueFired += OnCueFired;
            _sequencer.Finished += OnFinished;
            _overlay.SetFade(1f);
            _sequencer.Begin();
            PoseCurrent(0f);
        }

        /// <summary>
        /// Jumps to a moment of a shot (for screenshots and tests): builds its set, skips the dialogue and poses the
        /// picture. Everything that would otherwise ease in over real time (volume blend, dialogue fade, camera
        /// breathing, particles) is put straight into the state of that moment, so the same seek gives the same frame.
        /// </summary>
        public void Seek(int shot, float progress)
        {
            _dialogue.Clear();
            _dialogueView.HideNow();
            _finishFade = -1f;
            _sequencer.Seek(shot, progress);
            _camera.ResetClock(_sequencer.LocalTime);
            SnapVolumes();
            SimulateParticles(_sequencer.LocalTime);
            PoseCurrent(0f);
            _overlay.SetFade(0f);
        }

        private void OnDestroy()
        {
            if (_request != null && _request.Cutscene.Cast != null && CutsceneFxSet.Active == _request.Cutscene.Cast.Fx)
            {
                CutsceneFxSet.Active = null;
            }

            _input?.Dispose();
            if (_sequencer != null)
            {
                _sequencer.ShotStarted -= OnShotStarted;
                _sequencer.CueFired -= OnCueFired;
                _sequencer.Finished -= OnFinished;
            }
        }

        private void Update()
        {
            if (_sequencer == null || _handedOff)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            if (IsFinishing)
            {
                UpdateFinish(delta);
                return;
            }

            _sequencer.Tick(delta);
            _dialogue.Tick(delta);
            if (!_sequencer.IsFinished)
            {
                PoseCurrent(delta * _sequencer.Speed);
            }

            _overlay.SetSkipArmed(_skip.ArmedShare(Time.unscaledTime));
            _dialogueView.SetPlacement(_overlay.BarHeight + DialogueGap, DialogueScale);
            FadeVolumes(delta);
        }

        private void PoseCurrent(float delta)
        {
            if (_current == null || _sequencer.IsFinished)
            {
                return;
            }

            CutsceneShot shot = _sequencer.Current;
            float time = _sequencer.LocalTime;
            float progress = _sequencer.Progress;
            _current.Tick(time, progress);
            _camera.Apply(CameraPath.Sample(_keys, progress), delta);
            _overlay.SetFade(ShotFade(shot, time));
            _overlay.SetTitle(shot.TitleCard, TitleAlpha(shot, time));
            _overlay.Radar.Tick(time, progress);
        }

        private float ShotFade(CutsceneShot shot, float time)
        {
            float fade = 0f;
            if (shot.FadeIn > 0f)
            {
                fade = Mathf.Max(fade, 1f - time / shot.FadeIn);
            }

            float end = _sequencer.ShotEndTime;
            if (shot.FadeOut > 0f && !float.IsInfinity(end))
            {
                fade = Mathf.Max(fade, 1f - (end - time) / shot.FadeOut);
            }

            return Mathf.Clamp01(fade);
        }

        private static float TitleAlpha(CutsceneShot shot, float time)
        {
            if (string.IsNullOrEmpty(shot.TitleCard))
            {
                return 0f;
            }

            float start = shot.CameraLength * TitleStartShare;
            return Mathf.Clamp01((time - start) / CutsceneOverlay.TitleSeconds);
        }

        private void OnShotStarted(int index)
        {
            CutsceneShot shot = _request.Cutscene.Shots[index];
            ICutsceneSet set = SetFor(shot.Set);
            if (_current != null && _current != set)
            {
                _current.Exit();
                _roots[_currentSet].SetActive(false);
            }

            _roots[shot.Set].SetActive(true);
            _current = set;
            _currentSet = shot.Set;
            _context.Camera.backgroundColor = set.Theme != null ? set.Theme.BackgroundColor : Color.black;
            set.Enter(shot.Action);
            ResolveKeys(shot, set);
            _audio.SetMusic(shot.Music);
            _overlay.SetLetterbox(shot.Letterbox);
            _overlay.Radar.Show(shot.Overlay == CutsceneOverlayKind.Radar);
        }

        private void OnCueFired(int shot, CutsceneCue cue)
        {
            switch (cue.Kind)
            {
                case CueKind.Sound:
                    _audio.Play(cue);
                    break;
                case CueKind.Shake:
                    _camera.Shake(cue.Strength);
                    break;
            }
        }

        private void StartDialogue(DialogueDefinition dialogue)
        {
            _dialogue.Enqueue(dialogue);
            _dialogue.TryStart();
        }

        // The sets are built the first time a shot needs them; the shot before fades to black, so the build hides.
        private ICutsceneSet SetFor(CutsceneSet kind)
        {
            if (_sets.TryGetValue(kind, out ICutsceneSet set))
            {
                return set;
            }

            set = CutsceneSets.Create(kind);
            var root = new GameObject($"Set_{kind}");
            root.transform.SetParent(transform, false);
            set.Build(root.transform, _context);
            _sets[kind] = set;
            _roots[kind] = root;
            if (set.Theme != null && _context.CreateVolume != null)
            {
                Volume volume = _context.CreateVolume(set.Theme);
                volume.weight = _volumes.Count == 0 ? 1f : 0f;
                _volumes[kind] = volume;
            }

            return set;
        }

        // Keys are written relative to anchors of the set; they are resolved once per shot into world space.
        private void ResolveKeys(CutsceneShot shot, ICutsceneSet set)
        {
            _keys.Clear();
            if (shot.Camera == null)
            {
                return;
            }

            foreach (CameraKey key in shot.Camera)
            {
                CameraKey world = key;
                if (key.Mode == CameraKeyMode.MatchLevelRig && _context.FieldLevel != null)
                {
                    CameraSample pose = LevelPlayPose.Pose(_context.FieldLevel, _context.Camera.aspect, key.FieldOfView > 0f ? key.FieldOfView : CameraKey.DefaultFieldOfView);
                    world.Position = pose.Position;
                    world.LookAt = pose.LookAt;
                    world.FieldOfView = pose.FieldOfView;
                }
                else if (key.Anchor != CameraAnchor.World && set.TryGetAnchor(key.Anchor, out Vector3 origin, out Quaternion facing))
                {
                    world.Position = origin + facing * key.Position;
                    world.LookAt = origin + facing * key.LookAt;
                }

                world.Anchor = CameraAnchor.World;
                _keys.Add(world);
            }
        }

        private void FadeVolumes(float delta)
        {
            float step = delta / VolumeFadeSeconds;
            foreach (KeyValuePair<CutsceneSet, Volume> pair in _volumes)
            {
                float target = pair.Key == _currentSet ? 1f : 0f;
                pair.Value.weight = Mathf.MoveTowards(pair.Value.weight, target, step);
            }
        }

        private void SnapVolumes()
        {
            foreach (KeyValuePair<CutsceneSet, Volume> pair in _volumes)
            {
                pair.Value.weight = pair.Key == _currentSet ? 1f : 0f;
            }
        }

        // Particles run on their own clock; after a jump they are restarted with a fixed seed and run forward to the
        // shot time, so smoke and sparks look the same on every seek. Seeks are rare, so the search allocates.
        private void SimulateParticles(float time)
        {
            if (!_roots.TryGetValue(_currentSet, out GameObject root))
            {
                return;
            }

            float seconds = Mathf.Min(time, MaxParticleSeekSeconds);
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];
                // A system the set keeps stopped (it emits on a beat) only loses what it had in flight.
                if (!system.isPlaying)
                {
                    system.Clear(false);
                    continue;
                }

                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                system.useAutoRandomSeed = false;
                system.randomSeed = (uint)(ParticleSeed + i);
                system.Simulate(seconds, false, true, true);
                system.Play(false);
            }
        }

        private void OnSkipPressed()
        {
            if (IsFinishing)
            {
                return;
            }

            if (_skip.Press(Time.unscaledTime))
            {
                _sequencer.Skip();
            }
        }

        // The end and a confirmed skip land here alike: remember the intro, fade out, then hand over.
        private void OnFinished()
        {
            _progress?.MarkIntroSeen();
            _dialogue.Clear();
            _finishFade = _overlay.FadeAlpha;
            _overlay.SetTitle(null, 0f);
            _overlay.SetSkipArmed(0f);
            _audio.SetMusic(CutsceneMusic.Silence);
        }

        private void UpdateFinish(float delta)
        {
            _finishFade = Mathf.MoveTowards(_finishFade, 1f, delta * _sequencer.Speed / FinishFadeSeconds);
            _overlay.SetFade(_finishFade);
            if (_finishFade < 1f)
            {
                return;
            }

            _handedOff = true;
            HandOff();
        }

        private void HandOff()
        {
            CutsceneDefinition cutscene = _request.Cutscene;
            if (_request.Handoff == CutsceneHandoff.ReturnScene)
            {
                SceneManager.LoadScene(_request.ReturnScene);
                return;
            }

            LevelDefinition level = CutsceneDefinition.FirstCampaignLevel(_context.Catalog);
            DifficultyProfile difficulty = cutscene.HandoffDifficulty != null ? cutscene.HandoffDifficulty : FirstDifficulty();
            if (level == null || difficulty == null)
            {
                Debug.LogError("The cutscene cannot hand over: the campaign has no first level or difficulty.", this);
                SceneManager.LoadScene(SceneNames.MainMenu);
                return;
            }

            Services.Register(new LevelSession(level, difficulty, true));
            SceneManager.LoadScene(SceneNames.Level);
        }

        private DifficultyProfile FirstDifficulty() => _context.Catalog != null && _context.Catalog.Difficulties.Count > 0 ? _context.Catalog.Difficulties[0] : null;

        private static void Prewarm(CutsceneDefinition cutscene, PortraitProvider portraits)
        {
            var speakers = new HashSet<SpeakerDefinition>();
            foreach (CutsceneShot shot in cutscene.Shots)
            {
                if (shot.Dialogue == null)
                {
                    continue;
                }

                foreach (DialogueLine line in shot.Dialogue.Lines)
                {
                    if (line.Speaker != null && speakers.Add(line.Speaker))
                    {
                        portraits.Get(line.Speaker, IgnorePicture);
                    }
                }
            }
        }

        private static void IgnorePicture(Sprite _)
        {
        }
    }
}
