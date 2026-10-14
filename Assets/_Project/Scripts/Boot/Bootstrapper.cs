using Game.Core;
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

        private IInputProvider _input;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Services.Clear();
            _input = new InputSystemProvider(_inputActions);
            Services.Register<IInputProvider>(_input);
            Services.Register<ISaveService>(new FileSaveService(System.IO.Path.Combine(Application.persistentDataPath, SaveFolderName)));
            Services.Register<IAchievementService>(new NullAchievementService());
            Services.Register<IPlatformSettings>(new PcPlatformSettings());
            Services.Register(new LevelSession(_startLevel));
        }

        private void Start()
        {
            SceneManager.LoadScene(SceneNames.Level);
        }

        private void OnDestroy()
        {
            _input?.Dispose();
        }
    }
}
