using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Main menu: Start, Options and (on platforms that allow it) Quit, on a dim panel along the left so the living
    /// scene behind it stays in view.
    /// </summary>
    public sealed class MainMenuScreen : ScreenBase
    {
        private const string Title = "Tower Defence";
        private const string Subtitle = "The planet does not want us here.";
        private const float PanelWidth = 660f;
        private const float Margin = 90f;
        private const float TitleTop = -80f;
        private const float SubtitleTop = -175f;
        private const float ColumnWidth = 480f;
        private const float ColumnDrop = -70f;
        private const int SubtitleSize = 26;
        private static readonly Color PanelTint = new Color(0.02f, 0.03f, 0.06f, 0.66f);
        private static readonly Color SubtitleColor = new Color(0.75f, 0.85f, 1f, 0.85f);

        private GameObject _menu;
        private Button _optionsButton;

        // The diorama behind the menu replaces the flat starfield.
        protected override bool Starfield => false;

        protected override void Build(RectTransform root)
        {
            // Everything of the menu hangs under one object, so the options panel can take the whole screen.
            _menu = new GameObject("Menu", typeof(RectTransform));
            _menu.transform.SetParent(root, false);
            var menu = _menu.GetComponent<RectTransform>();
            menu.anchorMin = Vector2.zero;
            menu.anchorMax = Vector2.one;
            menu.offsetMin = Vector2.zero;
            menu.offsetMax = Vector2.zero;
            root = menu;

            UiKit.CreatePanel(root, "MenuPanel", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-40f, -40f), new Vector2(PanelWidth, 40f), PanelTint);

            UiKit.CreateFixedText(root, Title, UiKit.TitleSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Margin, TitleTop), new Vector2(1000f, 110f));
            Text subtitle = UiKit.CreateFixedText(root, Subtitle, SubtitleSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Margin + 4f, SubtitleTop), new Vector2(1000f, 50f));
            subtitle.color = SubtitleColor;

            RectTransform column = UiKit.CreateColumn(root, 24f);
            column.anchorMin = new Vector2(0f, 0.5f);
            column.anchorMax = new Vector2(0f, 0.5f);
            column.pivot = new Vector2(0f, 0.5f);
            column.anchoredPosition = new Vector2(Margin, ColumnDrop);
            column.sizeDelta = new Vector2(ColumnWidth, 0f);

            Button start = UiKit.CreateButton(column, "Start", () => Go(SceneNames.WorldMap));
            _optionsButton = UiKit.CreateButton(column, "Options", OpenOptions);
            if (Platform.CanQuit)
            {
                UiKit.CreateButton(column, "Quit", Quit);
            }

            SetFirstSelected(start);
        }

        /// <summary>Called by the options panel when it closes.</summary>
        public void ShowMenu()
        {
            _menu.SetActive(true);
            UiKit.Select(_optionsButton);
        }

        private void OpenOptions()
        {
            _menu.SetActive(false);
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
