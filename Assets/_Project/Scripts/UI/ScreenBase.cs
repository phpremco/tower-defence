using Game.Core;
using Game.Economy;
using Game.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Base for the menu scenes. Sends the player back to Boot when the services are missing
    /// (for example when a menu scene is played directly in the editor).
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        private Button _firstSelected;

        protected ProgressService Progress { get; private set; }

        protected IPlatformSettings Platform { get; private set; }

        protected abstract void Build(RectTransform root);

        protected void SetFirstSelected(Button button) => _firstSelected = button;

        protected static void Go(string sceneName) => SceneManager.LoadScene(sceneName);

        private void Start()
        {
            if (!Services.TryGet(out ProgressService progress) || !Services.TryGet(out IPlatformSettings platform))
            {
                SceneManager.LoadScene(SceneNames.Boot);
                return;
            }

            Progress = progress;
            Platform = platform;
            Time.timeScale = 1f;

            UiKit.EnsureCamera();
            UiKit.EnsureEventSystem();
            RectTransform root = UiKit.CreateRoot(transform);
            Build(root);
            UiKit.Select(_firstSelected);
        }
    }
}
