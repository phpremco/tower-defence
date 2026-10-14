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
    /// Entry point in the Boot scene: registers the platform services, then loads the generic Level scene.
    /// This is the only place that picks concrete platform implementations.
    /// </summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        private const string SaveFolderName = "Saves";

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private LevelDefinition _startLevel;
        [SerializeField] private DifficultyProfile _startDifficulty;
        [SerializeField] private UnlockTable _unlockTable;
        [SerializeField] private PlaytestSettings _playtestSettings;

        private static IInputProvider _input;

        private void Awake()
        {
            // The Bootstrapper is recreated each time the game restarts from Boot, so release the previous input first.
            _input?.Dispose();
            Services.Clear();

            _input = new InputSystemProvider(_inputActions);
            Services.Register(_input);
            Services.Register<ISaveService>(new FileSaveService(System.IO.Path.Combine(Application.persistentDataPath, SaveFolderName)));
            Services.Register<IAchievementService>(new NullAchievementService());
            Services.Register<IPlatformSettings>(new PcPlatformSettings());
            Services.Register(new UnlockState(_unlockTable.StartingTowerIds));
            Services.Register(_playtestSettings);
            Services.Register(new LevelSession(_startLevel, _startDifficulty));
        }

        private void Start()
        {
            SceneManager.LoadScene(SceneNames.Level);
        }
    }
}


