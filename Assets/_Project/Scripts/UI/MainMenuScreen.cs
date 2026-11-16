using Game.Core;
using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Main menu: Start, Codex, Options, Credits and (on platforms that allow it) Quit, on a dim panel along the left so the living
    /// scene behind it stays in view.
    /// </summary>
    public sealed class MainMenuScreen : ScreenBase
    {
        private const float PanelWidth = 660f;
        private const float Margin = 90f;
        private const float TitleTop = -80f;
        private const float SubtitleTop = -175f;
        private const float ColumnWidth = 480f;
        private const float ColumnDrop = -70f;
        private const int SubtitleSize = 26;
        private static readonly Color PanelTint = new Color(0.02f, 0.03f, 0.06f, 0.66f);
        private static readonly Color SubtitleColor = new Color(0.75f, 0.85f, 1f, 0.85f);
        private const int VersionSize = 22;
        private const float VersionMargin = 24f;
        private const float VersionWidth = 520f;
        private const float VersionHeight = 32f;
        private static readonly Color VersionColor = new Color(0.78f, 0.86f, 1f, 0.9f);

        private GameObject _menu;
        private Button _optionsButton;
        private Button _codexButton;
        private CodexView _codex;
        private Button _creditsButton;
        private CreditsView _credits;

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

            UiKit.CreateFixedText(root, Loc.Get("menu.title"), UiKit.TitleSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Margin, TitleTop), new Vector2(1000f, 110f));
            Text subtitle = UiKit.CreateFixedText(root, Loc.Get("menu.subtitle"), SubtitleSize, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(Margin + 4f, SubtitleTop), new Vector2(1000f, 50f));
            subtitle.color = SubtitleColor;

            RectTransform column = UiKit.CreateColumn(root, 24f);
            column.anchorMin = new Vector2(0f, 0.5f);
            column.anchorMax = new Vector2(0f, 0.5f);
            column.pivot = new Vector2(0f, 0.5f);
            column.anchoredPosition = new Vector2(Margin, ColumnDrop);
            column.sizeDelta = new Vector2(ColumnWidth, 0f);

            Button start = UiKit.CreateButton(column, Loc.Get("menu.start"), () => Go(SceneNames.WorldMap));
            if (HasCodex())
            {
                _codexButton = UiKit.CreateButton(column, Loc.Get("menu.codex"), OpenCodex);
            }

            _optionsButton = UiKit.CreateButton(column, Loc.Get("menu.options"), OpenOptions);
            if (Services.TryGet(out CreditsDefinition _))
            {
                _creditsButton = UiKit.CreateButton(column, Loc.Get("menu.credits"), OpenCredits);
            }

            if (Platform.CanQuit)
            {
                UiKit.CreateButton(column, Loc.Get("menu.quit"), Quit);
            }

            AddVersion(root);
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

        private static bool HasCodex() => Services.TryGet(out CodexCatalog catalog) && (catalog.Recipes.Count > 0 || catalog.Enemies.Count > 0);

        // The general codex: the same book as in a level, with every creature, on its own canvas over the diorama.
        private void OpenCodex()
        {
            if (_codex == null)
            {
                RectTransform root = UiKit.CreateRoot(transform, false);
                _codex = gameObject.AddComponent<CodexView>();
                Services.TryGet(out IInputProvider input);
                _codex.Initialize(root, input, OnCodexClosed);
            }

            _menu.SetActive(false);
            _codex.Open();
        }

        private void OnCodexClosed()
        {
            _menu.SetActive(true);
            UiKit.Select(_codexButton);
        }

        private void OpenCredits()
        {
            if (_credits == null)
            {
                RectTransform root = UiKit.CreateRoot(transform, false);
                _credits = gameObject.AddComponent<CreditsView>();
                Services.TryGet(out IInputProvider input);
                _credits.Initialize(root, Services.Get<CreditsDefinition>(), input, OnCreditsClosed);
            }

            _menu.SetActive(false);
            _credits.Open();
        }

        private void OnCreditsClosed()
        {
            _menu.SetActive(true);
            UiKit.Select(_creditsButton);
        }

        // Small in the bottom-right corner, so a screenshot or a bug report always says which build it came from.
        private static void AddVersion(RectTransform root)
        {
            Text version = UiKit.CreateFixedText(root, Loc.Format("menu.version", BuildVersion.Current), VersionSize, TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(-VersionMargin, VersionMargin), new Vector2(VersionWidth, VersionHeight));
            version.color = VersionColor;
            version.raycastTarget = false;
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
