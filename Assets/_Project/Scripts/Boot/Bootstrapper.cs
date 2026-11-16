using System.Collections.Generic;
using Game.Core;
using Game.Drones;
using Game.Economy;
using Game.Levels;
using Game.Platform;
using Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Boot
{
    /// <summary>
    /// Entry point in the Boot scene: registers the platform services and the game data, loads the saved progress,
    /// then opens the main menu. This is the only place that picks concrete platform implementations.
    /// </summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        private const string SaveFolderName = "Saves";
        private const string SessionObjectName = "Session";
        private const float MaxVolumePercent = 100f;
        private const float PercentScale = 100f;

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private CampaignCatalog _campaign;
        [SerializeField] private UpgradeCatalog _upgrades;
        [SerializeField] private CodexCatalog _codex;
        [SerializeField] private UnlockTable _unlockTable;
        [SerializeField] private PlaytestSettings _playtestSettings;
        [Tooltip("Use the Low (Switch) quality profile: softer bloom, no mist or depth of field, half the particles.")]
        [SerializeField] private bool _lowQuality;
        [Tooltip("The tilt-shift full-screen pass on the PC renderer; switched off by profiles without depth of field.")]
        [SerializeField] private FullScreenPassRendererFeature _tiltShift;
        [SerializeField] private UiSkin _uiSkin;
        [SerializeField] private SfxLibrary _sfxLibrary;
        [SerializeField] private Game.Combat.VfxLibrary _vfxLibrary;

        // Everything in a level is built from runtime materials, which a player build cannot see when it strips
        // shader variants. These saved materials carry the keyword combinations the game needs, so they survive.
        [SerializeField] private Material[] _shaderKeepers = new Material[0];

        private static IInputProvider _input;
        private static GameObject _session;

        /// <summary>The materials that keep the runtime shader variants alive in a build.</summary>
        public IReadOnlyList<Material> ShaderKeepers => _shaderKeepers;

        private void Awake()
        {
            // Boot is entered again after a restart, so release the previous input first.
            _input?.Dispose();
            Services.Clear();

            var save = new FileSaveService(System.IO.Path.Combine(Application.persistentDataPath, SaveFolderName));
            // Settings first: on the first launch after the split they are read out of the old progress file.
            var settings = new SettingsService(save);
            SettingsData data = settings.Data;

            // The Boot checkbox simulates the console (fixed Low profile); on PC the player picks High or Low under Options.
            IPlatformSettings platform = _lowQuality ? new SwitchPlatformSettings() : new PcPlatformSettings(data.LowQuality);
            var input = new InputSystemProvider(_inputActions, _uiSkin.PadGlyphs, platform.PadLayout, _uiSkin.KeyGlyphs);
            input.LoadOverrides(data.BindingOverrides);
            _input = input;
            var progress = new ProgressService(save, _unlockTable.StartingTowerIds);
            var localization = new StringTableService(data.LanguageCode);

            Services.Register(_input);
            Services.Register<IInputBindings>(input);
            Services.Register<ISaveService>(save);
            Services.Register(settings);
            Services.Register<ILocalizationService>(localization);
            Services.Register<IAchievementService>(new NullAchievementService());
            ApplyQuality(platform, _tiltShift);
            if (platform.HasDisplayOptions)
            {
                FullScreenPassRendererFeature tiltShift = _tiltShift;
                Services.Register(new QualityProfileSwitch(data.LowQuality, low => ApplyQuality(new PcPlatformSettings(low), tiltShift)));
                ApplyDisplay(data);
            }

            Services.Register(progress);
            Services.Register(_campaign);
            Services.Register(_upgrades);
            if (_codex != null)
            {
                Services.Register(_codex);
            }
            Services.Register(_playtestSettings);
            Services.Register(_uiSkin);
            if (_sfxLibrary != null)
            {
                Services.Register(_sfxLibrary);
            }

            if (_vfxLibrary != null)
            {
                Services.Register(_vfxLibrary);
                if (_vfxLibrary.Markers != null)
                {
                    Services.Register(_vfxLibrary.Markers);
                }

                if (_vfxLibrary.BossFx != null)
                {
                    Services.Register(_vfxLibrary.BossFx);
                }
            }

            UiKit.Skin = _uiSkin;
            UiKit.SafeMarginPercent = SafeFrame.Effective(settings.Data.HudMarginPercent, platform.SafeMarginPercent);
            MoneyPopup.Font = _uiSkin.Font;

            // The listener carries the master level; the players multiply their own group in from the mix.
            var mix = new AudioMix();
            mix.Set(data.MasterVolumePercent / MaxVolumePercent, data.MusicVolumePercent / MaxVolumePercent, data.EffectsVolumePercent / MaxVolumePercent, data.AmbienceVolumePercent / MaxVolumePercent);
            Services.Register(mix);
            AudioListener.volume = mix.Master;

            var cameraControls = new CameraControls();
            cameraControls.Set(data.CameraSpeedPercent / PercentScale, !data.EdgeScrollingOff);
            cameraControls.SetRadar(!data.RadarOff, data.RadarSizePercent / PercentScale);
            Services.Register(cameraControls);
            EnsureSessionObject();
        }

        // One object for the whole session (it survives scene loads): mute in the background, the settings write on quit,
        // and the controller banner. Boot is entered again after a restart, so an existing one is rebound, not doubled.
        // A quality profile is the platform settings every screen and level reads, the model detail, the LOD bias and
        // the tilt-shift pass; the options screen calls this again through the QualityProfileSwitch.
        private static void ApplyQuality(IPlatformSettings platform, FullScreenPassRendererFeature tiltShift)
        {
            Services.Register(platform);
            Services.Register(new ModelQuality(platform.LowDetailModels));
            QualitySettings.lodBias = platform.LodBias;
            if (tiltShift != null)
            {
                tiltShift.SetActive(platform.DepthOfFieldEnabled);
            }
        }

        // The window is only set when the player chose something else than the build's own start (borderless at the
        // desktop size), so Alt+Enter and a first launch behave as before.
        private static void ApplyDisplay(SettingsData data)
        {
            var mode = (DisplayMode)data.DisplayMode;
            if (mode != DisplayMode.Borderless || data.ResolutionWidth > 0)
            {
                DisplayOptions.ApplyWindow(mode, data.ResolutionWidth, data.ResolutionHeight);
            }

            DisplayOptions.ApplyFrameRate(data.VSync, data.FrameRateCap);
        }

        private static void EnsureSessionObject()
        {
            if (_session == null)
            {
                _session = new GameObject(SessionObjectName);
                DontDestroyOnLoad(_session);
                _session.AddComponent<AppFocusAudio>();
                _session.AddComponent<ControllerNotice>();
            }

            _session.GetComponent<ControllerNotice>().Bind(_input);
        }

        // The first launch opens with the cutscene and drops into the first level; every later one goes to the menu.
        // Quitting before the cutscene ends leaves the intro unseen, so it plays again on the next launch.
        private void Start()
        {
            bool introSeen = Services.Get<ProgressService>().Data.IntroSeen;
            CutsceneDefinition opening = _campaign.Story != null ? _campaign.Story.OpeningCutscene : null;
            if (!introSeen && opening != null)
            {
                Services.Register(CutsceneRequest.FirstLaunch(opening));
            }

            SceneManager.LoadScene(introSeen ? SceneNames.MainMenu : SceneNames.Intro);
        }
    }
}

