using System.Collections.Generic;
using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Platform;
using Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private const float MaxVolumePercent = 100f;

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private CampaignCatalog _campaign;
        [SerializeField] private UpgradeCatalog _upgrades;
        [SerializeField] private CodexCatalog _codex;
        [SerializeField] private UnlockTable _unlockTable;
        [SerializeField] private PlaytestSettings _playtestSettings;
        [Tooltip("Use the Low (Switch) quality profile: softer bloom, no mist or depth of field, half the particles.")]
        [SerializeField] private bool _lowQuality;
        [Tooltip("The tilt-shift full-screen pass on the PC renderer; switched off by profiles without depth of field.")]
        [SerializeField] private UnityEngine.Rendering.Universal.FullScreenPassRendererFeature _tiltShift;
        [SerializeField] private UiSkin _uiSkin;
        [SerializeField] private SfxLibrary _sfxLibrary;
        [SerializeField] private Game.Combat.VfxLibrary _vfxLibrary;

        // Everything in a level is built from runtime materials, which a player build cannot see when it strips
        // shader variants. These saved materials carry the keyword combinations the game needs, so they survive.
        [SerializeField] private Material[] _shaderKeepers = new Material[0];

        private static IInputProvider _input;

        /// <summary>The materials that keep the runtime shader variants alive in a build.</summary>
        public IReadOnlyList<Material> ShaderKeepers => _shaderKeepers;

        private void Awake()
        {
            // Boot is entered again after a restart, so release the previous input first.
            _input?.Dispose();
            Services.Clear();

            _input = new InputSystemProvider(_inputActions);
            var save = new FileSaveService(System.IO.Path.Combine(Application.persistentDataPath, SaveFolderName));
            var progress = new ProgressService(save, _unlockTable.StartingTowerIds);

            Services.Register(_input);
            Services.Register<ISaveService>(save);
            Services.Register<IAchievementService>(new NullAchievementService());
            Services.Register<IPlatformSettings>(_lowQuality ? new SwitchPlatformSettings() : new PcPlatformSettings());
            if (_tiltShift != null)
            {
                _tiltShift.SetActive(Services.Get<IPlatformSettings>().DepthOfFieldEnabled);
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
            }

            UiKit.Skin = _uiSkin;

            // The listener carries the master level; the players multiply their own group in from the mix.
            var mix = new AudioMix();
            ProgressData data = progress.Data;
            mix.Set(data.MasterVolumePercent / MaxVolumePercent, data.MusicVolumePercent / MaxVolumePercent, data.EffectsVolumePercent / MaxVolumePercent, data.AmbienceVolumePercent / MaxVolumePercent);
            Services.Register(mix);
            AudioListener.volume = mix.Master;
        }

        // The first launch opens with the story; every later one goes straight to the menu.
        private void Start()
        {
            bool introSeen = Services.Get<ProgressService>().Data.IntroSeen;
            SceneManager.LoadScene(introSeen ? SceneNames.MainMenu : SceneNames.Intro);
        }
    }
}

