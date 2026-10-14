using Game.Core;
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
        private const float PanelHeight = 640f;
        private const float ButtonGap = 20f;
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color PanelTint = new Color(0.14f, 0.22f, 0.4f, 0.97f);

        private LevelContext _context;
        private TimeController _time;
        private IInputProvider _input;
        private GameObject _root;
        private GameObject _mainPage;
        private GameObject _confirmPage;
        private Button _resume;
        private Button _keepPlaying;
        private bool _wasOpen;

        public void Initialize(LevelContext context, RectTransform safeRoot, TimeController time, IInputProvider input)
        {
            _context = context;
            _time = time;
            _input = input;
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
            dim.raycastTarget = true;
            _root = dim.gameObject;

            Image panel = UiKit.CreatePanel(dim.rectTransform, "PausePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, PanelTint);
            panel.rectTransform.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            _mainPage = CreatePage(panel.rectTransform, "Paused");
            RectTransform main = _mainPage.GetComponent<RectTransform>();
            _resume = AddButton(main, "Resume", () => _time.TogglePause());
            AddButton(main, "Restart level", Restart);
            AddButton(main, "Quit to map", ShowConfirm);

            _confirmPage = CreatePage(panel.rectTransform, "Quit to the map?");
            RectTransform confirm = _confirmPage.GetComponent<RectTransform>();
            Text warning = UiKit.CreateText(confirm, "Progress in this level will be lost.", 30, TextAnchor.MiddleCenter);
            warning.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
            AddButton(confirm, "Yes, quit", QuitToMap);
            _keepPlaying = AddButton(confirm, "No, keep playing", ShowMain);
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

            if (open && !_wasOpen)
            {
                ShowMain();
            }

            _wasOpen = open;
        }

        private void ShowMain()
        {
            _confirmPage.SetActive(false);
            _mainPage.SetActive(true);
            UiKit.Select(_resume);
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
            if (!_root.activeSelf)
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
