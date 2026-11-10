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
        // Header plus six buttons (Resume, Codex, Sound, Camera, Restart, Quit) with their gaps and the page margins.
        private const float PanelHeight = 920f;
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
        private GameObject _soundPage;
        private Button _soundBack;
        private GameObject _cameraPage;
        private Selectable _cameraFirst;
        private Button _resume;
        private Button _keepPlaying;
        private Button _codexButton;
        private CodexView _codex;
        private GameObject _lastFocus;
        private bool _wasOpen;
        private bool _codexWasOpen;
        private bool _codexFromMenu;

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

            _mainPage = CreatePage(panel.rectTransform, "Paused");
            RectTransform main = _mainPage.GetComponent<RectTransform>();
            _resume = AddButton(main, "Resume", () => _time.TogglePause());
            if (_codex != null)
            {
                _codexButton = AddButton(main, "Codex", OpenCodex);
            }

            AddButton(main, "Sound", ShowSound);
            AddButton(main, "Camera", ShowCamera);
            AddButton(main, "Restart level", Restart);
            AddButton(main, "Quit to map", ShowConfirm);

            _confirmPage = CreatePage(panel.rectTransform, "Quit to the map?");
            RectTransform confirm = _confirmPage.GetComponent<RectTransform>();
            Text warning = UiKit.CreateText(confirm, "Progress in this level will be lost.", 30, TextAnchor.MiddleCenter);
            UiKit.WrapInBox(warning);
            warning.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            AddButton(confirm, "Yes, quit", QuitToMap);
            _keepPlaying = AddButton(confirm, "No, keep playing", ShowMain);
            _confirmPage.SetActive(false);

            // The volume groups, so the mix can be fixed without leaving the level.
            _soundPage = CreatePage(panel.rectTransform, "Sound");
            RectTransform sound = _soundPage.GetComponent<RectTransform>();
            Services.TryGet(out ProgressService progress);
            Services.TryGet(out AudioMix mix);
            var volumes = new VolumeControls(progress, mix);
            volumes.Build(sound);
            foreach (LayoutElement element in sound.GetComponentsInChildren<LayoutElement>())
            {
                if (element.GetComponent<Button>() != null)
                {
                    element.preferredHeight = UiKit.ButtonHeight;
                }
            }

            _soundBack = AddButton(sound, "Back", ShowMain);
            _soundPage.SetActive(false);

            // Pan speed and edge scrolling, so a camera that feels slow can be fixed mid-fight.
            _cameraPage = CreatePage(panel.rectTransform, "Camera");
            RectTransform cameraRect = _cameraPage.GetComponent<RectTransform>();
            Services.TryGet(out CameraControls cameraControls);
            var cameraOptions = new CameraOptionControls(progress, cameraControls ?? new CameraControls());
            cameraOptions.Build(cameraRect);
            foreach (Button button in cameraRect.GetComponentsInChildren<Button>())
            {
                button.GetComponent<LayoutElement>().preferredHeight = UiKit.ButtonHeight;
            }

            _cameraFirst = cameraOptions.First;
            AddButton(cameraRect, "Back", ShowMain);
            _cameraPage.SetActive(false);

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

        // A mouse click on the dim clears the focus; give it back so the gamepad can still move through the menu. While the
        // book lies on top the focus is the book's; when it closes, the focus comes back to the button that opened it.
        private void Update()
        {
            bool codexOpen = _codex != null && _codex.IsOpen;
            // The book's page is partly see-through: the menu's buttons would show through it, so they step aside meanwhile.
            if (_panel.activeSelf == codexOpen)
            {
                _panel.SetActive(!codexOpen);
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
            if (_root.activeSelf && !codexOpen)
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

        private void ShowSound()
        {
            _mainPage.SetActive(false);
            _soundPage.SetActive(true);
            UiKit.Select(_soundBack);
        }

        private void ShowCamera()
        {
            _mainPage.SetActive(false);
            _cameraPage.SetActive(true);
            UiKit.Select(_cameraFirst);
        }

        private void ShowMain()
        {
            _confirmPage.SetActive(false);
            _soundPage.SetActive(false);
            _cameraPage.SetActive(false);
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
            if (!_root.activeSelf || CodexOwnsInput)
            {
                return;
            }

            if (_confirmPage.activeSelf || _soundPage.activeSelf || _cameraPage.activeSelf)
            {
                ShowMain();
            }
            else
            {
                _time.TogglePause();
            }
        }

        private static void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.Level);
        }

        private static void QuitToMap()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.WorldMap);
        }
    }
}
