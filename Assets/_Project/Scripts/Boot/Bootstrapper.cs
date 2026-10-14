using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Platform;
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
        [SerializeField] private UnlockTable _unlockTable;
        [SerializeField] private PlaytestSettings _playtestSettings;

        private static IInputProvider _input;

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
            Services.Register<IPlatformSettings>(new PcPlatformSettings());
            Services.Register(progress);
            Services.Register(_campaign);
            Services.Register(_upgrades);
            Services.Register(_playtestSettings);

            AudioListener.volume = progress.Data.MasterVolumePercent / MaxVolumePercent;
        }

        private void Start()
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
