using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Options panel over the main menu: the four volume sliders and the camera speed and edge scrolling
    /// (stored in the saved progress), the controls, the sound test and a replay of the opening cutscene.</summary>
    public sealed class OptionsOverlay : MonoBehaviour
    {
        private MainMenuScreen _owner;
        private GameObject _panel;
        private GameObject _controlsPanel;
        private RectTransform _column;
        private Button _controlsButton;
        private Button _soundButton;
        private Button _storyButton;
        private Text _title;
        private SoundBoardOverlay _soundBoard;
        private bool _storyOpen;
        private IInputProvider _input;

        // The title sits at the top of the safe area and the column hangs a clear gap under it; the column shrinks to
        // fit above the bottom margin when the screen is short, so the title never runs into the first slider.
        private const float TitleTop = -36f;
        private const float TitleHeight = 100f;
        private const float TitleGap = 28f;
        private const float BottomMargin = 40f;

        // The controls page: a table of three columns (what, gamepad, keyboard and mouse), each column one text so the
        // rows line up; "{0}" is the commander's name. The columns are placed by their left edge from the screen centre.
        private const int ControlsSize = 30;
        private const float ControlsTop = -150f;
        private const float ControlsHeight = 760f;
        private const float BackBottom = 60f;
        private static readonly float[] ColumnLeft = { -880f, -320f, 300f };
        private static readonly float[] ColumnWidth = { 540f, 600f, 580f };
        private static readonly Color HeaderColour = new Color(1f, 0.85f, 0.4f);
        private static readonly Color ActionColour = new Color(0.75f, 0.85f, 1f);

        private static readonly string[,] ControlRows =
        {
            { "Move the cursor", "Left stick / D-pad", "Mouse" },
            { "Move the camera", "Right stick", "WASD / arrow keys" },
            { "Zoom", "L3 / R3", "- / +   Mouse wheel" },
            { "Overview (hold)", "View (hold), A: go there", "Tab (hold), click: go there" },
            { "Radar (top right)", "-", "Click: send the camera there" },
            { "Confirm", "A", "Enter   Left click" },
            { "Cancel / back", "B", "Esc   Right click" },
            { "Switch tower or street weapon", "LB / RB", "Q / E" },
            { "Tower icons", "A on a tower, stick, A", "Click a tower, or U / R / X" },
            { "Send {0} to a blockade", "Its third icon", "X on the blockade" },
            { "{0}: next post", "X", "H" },
            { "{0}: shockwave / repair", "LT / RT", "F / G" },
            { "Drones", "A on the base doorstep", "Click a card (bottom right)" },
            { "Codex", "Start, then Codex", "Book icon (top right)" },
            { "Game speed", "Y", "Space" },
            { "Pause", "Start", "P" },
            { "Skip wave (test)", "View (tap)", "N" },
        };

        private static readonly string[] ControlHeaders = { "Action", "Gamepad", "Keyboard and mouse" };

        public void Open(MainMenuScreen owner)
        {
            _owner = owner;
            Services.TryGet(out ProgressService progress);
            Services.TryGet(out AudioMix mix);

            RectTransform root = UiKit.CreateRoot(transform);
            _panel = root.parent.gameObject;
            var background = new GameObject("Dim", typeof(RectTransform)).AddComponent<Image>();
            background.transform.SetParent(root, false);
            background.color = new Color(0.04f, 0.05f, 0.08f, 0.9f);
            RectTransform rect = background.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _title = UiKit.CreateFixedText(root, "Options", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, TitleTop), new Vector2(1200f, TitleHeight));
            _column = UiKit.CreateColumn(root, 24f);
            ColumnFit.Attach(_column, -TitleTop + TitleHeight + TitleGap, BottomMargin);
            var volumes = new VolumeControls(progress, mix);
            volumes.Build(_column);
            Services.TryGet(out CameraControls cameraControls);
            new CameraOptionControls(progress, cameraControls ?? new CameraControls()).Build(_column);
            _controlsButton = UiKit.CreateButton(_column, "Controls", ShowControls);
            _soundButton = UiKit.CreateButton(_column, "Sound test", ShowSoundBoard);
            if (OpeningCutscene() != null)
            {
                UiKit.CreateButton(_column, "Replay intro", ReplayIntro);
            }

            // Developer tools only when developer mode is on (editor or development build; never in a release build).
            if (Services.TryGet(out PlaytestSettings playtest) && playtest.DeveloperModeOn)
            {
                _storyButton = UiKit.CreateButton(_column, "Story viewer (dev)", ShowStoryViewer);
            }

            UiKit.CreateButton(_column, "Back", Close);
            UiKit.Select(volumes.First);

            BuildControlsPage(root);

            if (Services.TryGet(out _input))
            {
                _input.Cancelled += OnCancelled;
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Cancelled -= OnCancelled;
            }
        }

        // B or Esc steps back one page: the controls or the sound test to the options, the options to the main menu.
        // The story viewer is a developer tool with its own buttons and keeps B for itself.
        private void OnCancelled()
        {
            if (_storyOpen)
            {
                return;
            }

            if (_soundBoard != null)
            {
                _soundBoard.Close();
            }
            else if (_controlsPanel.activeSelf)
            {
                HideControls();
            }
            else
            {
                Close();
            }
        }

        // The panel covers the whole safe area (a bare RectTransform would be a 100 x 100 box in the middle, which put the
        // Back button half-way up the screen and the table off its left edge).
        private void BuildControlsPage(RectTransform root)
        {
            _controlsPanel = new GameObject("Controls", typeof(RectTransform));
            RectTransform page = _controlsPanel.GetComponent<RectTransform>();
            page.SetParent(root, false);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = Vector2.zero;
            page.offsetMax = Vector2.zero;

            string hero = HeroNaming.Campaign();
            string gold = ColorUtility.ToHtmlStringRGB(HeaderColour);
            int rows = ControlRows.GetLength(0);
            for (int column = 0; column < ControlHeaders.Length; column++)
            {
                // The header line in gold through rich text, so it stays a line of the same text as the rows below it.
                var lines = new System.Text.StringBuilder();
                lines.Append("<color=#").Append(gold).Append('>').Append(ControlHeaders[column]).Append("</color>");
                for (int row = 0; row < rows; row++)
                {
                    lines.Append('\n').Append(string.Format(ControlRows[row, column], hero));
                }

                float centre = ColumnLeft[column] + ColumnWidth[column] * 0.5f;
                Text text = UiKit.CreateFixedText(page, lines.ToString(), ControlsSize, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(centre, ControlsTop), new Vector2(ColumnWidth[column], ControlsHeight));
                // One size for all three columns, or a column that shrank to fit would no longer line up with the others.
                text.resizeTextForBestFit = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.supportRichText = true;
                text.color = column == 0 ? ActionColour : Color.white;
            }

            Button back = UiKit.CreateButton(page, "Back", HideControls);
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, BackBottom);
            backRect.sizeDelta = new Vector2(UiKit.ButtonWidth, UiKit.ButtonHeight);
            _controlsPanel.SetActive(false);
        }

        private void ShowControls()
        {
            _column.gameObject.SetActive(false);
            _controlsPanel.SetActive(true);
            UiKit.Select(_controlsPanel.GetComponentInChildren<Button>());
        }

        private void HideControls()
        {
            _controlsPanel.SetActive(false);
            _column.gameObject.SetActive(true);
            UiKit.Select(_controlsButton);
        }

        private void ShowSoundBoard()
        {
            // The board draws its own title; this one would show through its dim.
            _column.gameObject.SetActive(false);
            _title.gameObject.SetActive(false);
            _soundBoard = gameObject.AddComponent<SoundBoardOverlay>();
            _soundBoard.Open(this);
        }

        /// <summary>The sound board closes itself and hands the options column back.</summary>
        public void ReturnFromSoundBoard()
        {
            _soundBoard = null;
            _title.gameObject.SetActive(true);
            _column.gameObject.SetActive(true);
            UiKit.Select(_soundButton);
        }

        private void ShowStoryViewer()
        {
            _column.gameObject.SetActive(false);
            _title.gameObject.SetActive(false);
            _storyOpen = true;
            gameObject.AddComponent<StoryViewerOverlay>().Open(this);
        }

        /// <summary>The story viewer closes itself and hands the options column back.</summary>
        public void ReturnFromStoryViewer()
        {
            _storyOpen = false;
            _title.gameObject.SetActive(true);
            _column.gameObject.SetActive(true);
            UiKit.Select(_storyButton);
        }

        // The opening cutscene plays again from the start and returns to the main menu; it never touches the save.
        private static void ReplayIntro()
        {
            CutsceneDefinition cutscene = OpeningCutscene();
            if (cutscene == null)
            {
                return;
            }

            Services.Register(new CutsceneRequest(cutscene, CutsceneHandoff.ReturnScene, SceneNames.MainMenu));
            SceneManager.LoadScene(SceneNames.Intro);
        }

        private static CutsceneDefinition OpeningCutscene()
        {
            return Services.TryGet(out CampaignCatalog catalog) && catalog.Story != null ? catalog.Story.OpeningCutscene : null;
        }

        private void Close()
        {
            Destroy(_panel);
            _owner.ShowMenu();
            Destroy(this);
        }
    }
}
