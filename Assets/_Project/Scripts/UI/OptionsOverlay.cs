using Game.Core;
using Game.Economy;
using Game.Levels;
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

        // The title sits at the top of the safe area and the column hangs a clear gap under it; the column shrinks to
        // fit above the bottom margin when the screen is short, so the title never runs into the first slider.
        private const float TitleTop = -36f;
        private const float TitleHeight = 100f;
        private const float TitleGap = 28f;
        private const float BottomMargin = 40f;

        private const string ControlsText =
            "Move the cursor      Left stick / D-pad    Mouse\n" +
            "Move the camera      Right stick    WASD / arrow keys\n" +
            "Zoom                 L3 / R3    - / +    Mouse wheel\n" +
            "Overview (hold)      View (hold)    Tab   (click or A: go there on release)\n" +
            "Radar                Top right: click to send the camera there\n" +
            "Confirm              A    Enter    Left click\n" +
            "Cancel               B    Esc    Right click\n" +
            "Switch tower         LB / RB    Q / E\n" +
            "Tower icons          Stick / D-pad to pick, A to use, B to close\n" +
            "Upgrade / Repair / Sell   U / R / X   (X on a blockade sends the hero)\n" +
            "Hero: next post / shockwave / repair post   X button / LT / RT    H / F / G\n" +
            "Drones               Panel bottom right: click a card, or A on the base doorstep, then the stick\n" +
            "Game speed           Y    Space\n" +
            "Pause                Start    P\n" +
            "Skip wave (test)     View (tap)    N";

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

            UiKit.CreateFixedText(root, "Options", UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, TitleTop), new Vector2(1200f, TitleHeight));
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

            _controlsPanel = new GameObject("Controls", typeof(RectTransform));
            _controlsPanel.transform.SetParent(root, false);
            Text text = UiKit.CreateFixedText(_controlsPanel.GetComponent<RectTransform>(), ControlsText, UiKit.BodySize, TextAnchor.UpperLeft, new Vector2(0.5f, 0.5f), new Vector2(-560f, 300f), new Vector2(1200f, 600f));
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Button back = UiKit.CreateButton(_controlsPanel.GetComponent<RectTransform>(), "Back", HideControls);
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 60f);
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
            _column.gameObject.SetActive(false);
            gameObject.AddComponent<SoundBoardOverlay>().Open(this);
        }

        /// <summary>The sound board closes itself and hands the options column back.</summary>
        public void ReturnFromSoundBoard()
        {
            _column.gameObject.SetActive(true);
            UiKit.Select(_soundButton);
        }

        private void ShowStoryViewer()
        {
            _column.gameObject.SetActive(false);
            gameObject.AddComponent<StoryViewerOverlay>().Open(this);
        }

        /// <summary>The story viewer closes itself and hands the options column back.</summary>
        public void ReturnFromStoryViewer()
        {
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
