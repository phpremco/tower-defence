using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Main menu: Start, Options and (on platforms that allow it) Quit.</summary>
    public sealed class MainMenuScreen : ScreenBase
    {
        private const string Title = "Tower Defence";

        private RectTransform _column;
        private Button _optionsButton;

        protected override void Build(RectTransform root)
        {
            UiKit.CreateFixedText(root, Title, UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1200f, 120f));

            _column = UiKit.CreateColumn(root, 24f);
            Button start = UiKit.CreateButton(_column, "Start", () => Go(SceneNames.WorldMap));
            _optionsButton = UiKit.CreateButton(_column, "Options", OpenOptions);
            if (Platform.CanQuit)
            {
                UiKit.CreateButton(_column, "Quit", Quit);
            }

            SetFirstSelected(start);
        }

        /// <summary>Called by the options panel when it closes.</summary>
        public void ShowMenu()
        {
            _column.gameObject.SetActive(true);
            UiKit.Select(_optionsButton);
        }

        private void OpenOptions()
        {
            _column.gameObject.SetActive(false);
            gameObject.AddComponent<OptionsOverlay>().Open(this);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

