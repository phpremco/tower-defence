using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The pause menu: resume, restart or quit to the map. Quitting asks "are you sure" first because the progress
    /// of the level is lost. It opens whenever the game is paused and the level is still running.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        private const float PanelWidth = 760f;
        // The panel keeps this far from the safe area edges and shrinks as a whole on a window narrower than it.
        private const float SafeMargin = 24f;
        // Header plus five buttons (Resume, Codex, Options, Restart, Quit) with their gaps and the page margins.
        private const float PanelHeight = 800f;
        private const float ButtonGap = 20f;
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color PanelTint = new Color(0.14f, 0.22f, 0.4f, 0.97f);

        private LevelContext _context;
        private TimeController _time;
        private IInputProvider _input;
        private GameObject _root;
        private GameObject _panel;
        private GameObject _mainPage;
        private GameObject _confirmPage;
        private Button _optionsButton;
        private OptionsOverlay _options;
        private Button _resume;
        private Button _keepPlaying;
        private Button _codexButton;
        private CodexView _codex;
        private GameObject _lastFocus;
        private bool _wasOpen;
        private bool _codexWasOpen;
        private bool _codexFromMenu;
        private int _optionsClosedFrame = -1;

        /// <param name="codex">The level codex, or null when the level has none; the pause menu is how a gamepad reaches it.</param>
        public void Initialize(LevelContext context, RectTransform safeRoot, TimeController time, IInputProvider input, CodexView codex)
        {
            _context = context;
            _time = time;
            _input = input;
            _codex = codex != null && codex.Available ? codex : null;
            Build(safeRoot);
            _context.Changed.Subscribe(Refresh);
            _input.Cancelled += OnCancelled;
            Refresh();
        }

        private void OnDestroy()
        {
            _context?.Changed.Unsubscribe(Refresh);
            FlushSettings();
            if (_input != null)
            {
                _input.Cancelled -= OnCancelled;
            }
        }

        private void Build(RectTransform safeRoot)
        {
            // A full-screen dim that also blocks clicks on the field underneath.
            Image dim = UiKit.CreatePanel(safeRoot, "PauseDim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Dim);
            CoverScreen.Add(dim.rectTransform);
            dim.raycastTarget = true;
            _root = dim.gameObject;

            Image panel = UiKit.CreatePanel(dim.rectTransform, "PausePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, PanelTint);
            panel.rectTransform.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            FitInsideParent.Add(panel.rectTransform, SafeMargin);
            _panel = panel.gameObject;

            _mainPage = CreatePage(panel.rectTransform, Loc.Get("pause.title"));
            RectTransform main = _mainPage.GetComponent<RectTransform>();
            _resume = AddButton(main, Loc.Get("pause.resume"), () => _time.TogglePause());
            if (_codex != null)
            {
                _codexButton = AddButton(main, Loc.Get("pause.codex"), OpenCodex);
            }

            _optionsButton = AddButton(main, Loc.Get("pause.options"), OpenOptions);
            AddButton(main, Loc.Get("pause.restart"), Restart);
            AddButton(main, Loc.Get("pause.quit_to_map"), ShowConfirm);

            _confirmPage = CreatePage(panel.rectTransform, Loc.Get("pause.quit_question"));
            RectTransform confirm = _confirmPage.GetComponent<RectTransform>();
            Text warning = UiKit.CreateText(confirm, Loc.Get("pause.quit_warning"), 30, TextAnchor.MiddleCenter);
            UiKit.WrapInBox(warning);
            warning.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            AddButton(confirm, Loc.Get("pause.quit_yes"), QuitToMap);
            _keepPlaying = AddButton(confirm, Loc.Get("pause.quit_no"), ShowMain);
            _confirmPage.SetActive(false);

            _root.SetActive(false);
        }

        private static GameObject CreatePage(RectTransform parent, string title)
        {
            var page = new GameObject(title, typeof(RectTransform));
            var rect = page.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(60f, 40f);
            rect.offsetMax = new Vector2(-60f, -40f);

            var layout = page.AddComponent<VerticalLayoutGroup>();
            layout.spacing = ButtonGap;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;

            Text header = UiKit.CreateText(rect, title, 56, TextAnchor.MiddleCenter);
            UiKit.WrapInBox(header);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;
            return page;
        }

        private static Button AddButton(RectTransform parent, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = UiKit.CreateButton(parent, label, action);
            button.GetComponent<LayoutElement>().preferredHeight = UiKit.ButtonHeight;
            return button;
        }

        private void Refresh()
        {
            bool open = _context.Paused && _context.Outcome == LevelOutcome.Running;
            // Resumed (Start) or ended while the options lay on top: they close with the menu and write the settings.
            if (!open && _options != null)
            {
                _options.Close();
            }

            _root.SetActive(open);

            if (_codexButton != null)
            {
                // As the HUD book: not while a story scene owns the screen.
                _codexButton.interactable = !_context.StoryBusy;
            }

            if (open && !_wasOpen)
            {
                ShowMain();
            }

            _wasOpen = open;
        }

        // True while the book lies over the menu, and in the frame it closed (its B press must not also close the menu).
        private bool CodexOwnsInput => _codex != null && (_codex.IsOpen || _codex.ClosedThisFrame);

        // The options take every press while open, and in the frame their Back closed them.
        private bool OptionsOwnInput => _options != null || _optionsClosedFrame == Time.frameCount;

        // A mouse click on the dim clears the focus; give it back so the gamepad can still move through the menu. While the
        // book lies on top the focus is the book's; when it closes, the focus comes back to the button that opened it.
        private void Update()
        {
            bool codexOpen = _codex != null && _codex.IsOpen;
            // The book's page and the options are partly see-through: the menu's buttons would show through them, so they
            // step aside meanwhile.
            bool covered = codexOpen || _options != null;
            if (_panel.activeSelf == covered)
            {
                _panel.SetActive(!covered);
            }

            if (_codexWasOpen && !codexOpen && _root.activeSelf)
            {
                UiKit.Select(_codexFromMenu && _codexButton != null ? _codexButton : _resume);
                _lastFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            }

            if (!codexOpen)
            {
                _codexFromMenu = false;
            }

            _codexWasOpen = codexOpen;
            if (_root.activeSelf && !covered)
            {
                UiKit.KeepFocus(ref _lastFocus);
            }
        }

        // Remco 2026-11-10: the book opens over the menu and the game stays paused under it, so closing it (B) lands back in
        // the pause menu rather than in a running fight. The book's own hold changes nothing while the pause stands.
        private void OpenCodex()
        {
            _codexFromMenu = true;
            _codex.Open();
        }

        // The full options (sound, camera, HUD, display, controls) over the paused level; the clock stays stopped under them.
        private void OpenOptions()
        {
            _options = gameObject.AddComponent<OptionsOverlay>();
            _options.OpenInLevel(OnOptionsClosed);
        }

        private void OnOptionsClosed()
        {
            _options = null;
            _optionsClosedFrame = Time.frameCount;
            if (_root.activeSelf)
            {
                _panel.SetActive(true);
                UiKit.Select(_optionsButton);
                _lastFocus = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            }
        }

        private static void FlushSettings()
        {
            if (Services.TryGet(out SettingsService settings))
            {
                settings.Flush();
            }
        }

        private void ShowMain()
        {
            _confirmPage.SetActive(false);
            _mainPage.SetActive(true);
            // A pause pressed while the book is open lies under it: the focus stays in the book until it closes.
            if (_codex == null || !_codex.IsOpen)
            {
                UiKit.Select(_resume);
            }
        }

        private void ShowConfirm()
        {
            _mainPage.SetActive(false);
            _confirmPage.SetActive(true);
            UiKit.Select(_keepPlaying);
        }

        // Back or Escape steps out of the quit question first, then closes the menu.
        private void OnCancelled()
        {
            if (!_root.activeSelf || CodexOwnsInput || OptionsOwnInput)
            {
                return;
            }

            if (_confirmPage.activeSelf)
            {
                ShowMain();
            }
            else
            {
                _time.TogglePause();
            }
        }

        // The clock stays stopped while the loading screen fades in over the pause menu; the loader starts it again.
        private static void Restart() => LevelLoader.Reload();

        private static void QuitToMap()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.WorldMap);
        }
    }
}
