using System.Collections.Generic;
using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.Platform;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Options panel over the main menu: the four volume sliders, mute in the background, camera speed, edge
    /// scrolling, radar, HUD margin and language (kept in the settings file, written when the panel closes), the
    /// controls, the sound test and a replay of the opening cutscene.</summary>
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
        private Button _muteButton;
        private bool _storyOpen;
        private IInputProvider _input;
        private SettingsService _settings;
        private ILocalizationService _localization;

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

        // Built when the page opens, not at class load, so the texts come from the language the player chose.
        private static string[,] ControlRows()
        {
            return new[,]
            {
                { Loc.Get("controls.cursor"), Loc.Get("controls.cursor_pad"), Loc.Get("controls.cursor_keys") },
                { Loc.Get("controls.camera"), Loc.Get("controls.camera_pad"), Loc.Get("controls.camera_keys") },
                { Loc.Get("controls.zoom"), Loc.Get("controls.zoom_pad"), Loc.Get("controls.zoom_keys") },
                { Loc.Get("controls.overview"), Loc.Get("controls.overview_pad"), Loc.Get("controls.overview_keys") },
                { Loc.Get("controls.radar"), Loc.Get("controls.radar_pad"), Loc.Get("controls.radar_keys") },
                { Loc.Get("controls.confirm"), Loc.Get("controls.confirm_pad"), Loc.Get("controls.confirm_keys") },
                { Loc.Get("controls.cancel"), Loc.Get("controls.cancel_pad"), Loc.Get("controls.cancel_keys") },
                { Loc.Get("controls.switch_tower"), Loc.Get("controls.switch_tower_pad"), Loc.Get("controls.switch_tower_keys") },
                { Loc.Get("controls.tower_icons"), Loc.Get("controls.tower_icons_pad"), Loc.Get("controls.tower_icons_keys") },
                { Loc.Get("controls.send_hero"), Loc.Get("controls.send_hero_pad"), Loc.Get("controls.send_hero_keys") },
                { Loc.Get("controls.hero_post"), Loc.Get("controls.hero_post_pad"), Loc.Get("controls.hero_post_keys") },
                { Loc.Get("controls.hero_abilities"), Loc.Get("controls.hero_abilities_pad"), Loc.Get("controls.hero_abilities_keys") },
                { Loc.Get("controls.drones"), Loc.Get("controls.drones_pad"), Loc.Get("controls.drones_keys") },
                { Loc.Get("controls.codex"), Loc.Get("controls.codex_pad"), Loc.Get("controls.codex_keys") },
                { Loc.Get("controls.speed"), Loc.Get("controls.speed_pad"), Loc.Get("controls.speed_keys") },
                { Loc.Get("controls.pause"), Loc.Get("controls.pause_pad"), Loc.Get("controls.pause_keys") },
                { Loc.Get("controls.skip_wave"), Loc.Get("controls.skip_wave_pad"), Loc.Get("controls.skip_wave_keys") },
            };
        }

        private static string[] ControlHeaders()
        {
            return new[] { Loc.Get("controls.header_action"), Loc.Get("controls.header_pad"), Loc.Get("controls.header_keys") };
        }

        public void Open(MainMenuScreen owner)
        {
            _owner = owner;
            Services.TryGet(out _settings);
            Services.TryGet(out _localization);
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
            CoverScreen.Add(rect);

            _title = UiKit.CreateFixedText(root, Loc.Get("options.title"), UiKit.TitleSize, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, TitleTop), new Vector2(1200f, TitleHeight));
            _column = UiKit.CreateColumn(root, 24f);
            ColumnFit.Attach(_column, -TitleTop + TitleHeight + TitleGap, BottomMargin);
            var volumes = new VolumeControls(_settings, mix);
            volumes.Build(_column);
            _muteButton = UiKit.CreateButton(_column, MuteLabel(), OnMuteToggled);
            Services.TryGet(out CameraControls cameraControls);
            new CameraOptionControls(_settings, cameraControls ?? new CameraControls()).Build(_column);
            new HudMarginControl(_settings).Build(_column);
            UiKit.CreateButton(_column, LanguageLabel(), OnLanguagePressed);
            _controlsButton = UiKit.CreateButton(_column, Loc.Get("options.controls"), ShowControls);
            _soundButton = UiKit.CreateButton(_column, Loc.Get("options.sound_test"), ShowSoundBoard);
            if (OpeningCutscene() != null)
            {
                UiKit.CreateButton(_column, Loc.Get("options.replay_intro"), ReplayIntro);
            }

            // Developer tools only when developer mode is on (editor or development build; never in a release build).
            if (Services.TryGet(out PlaytestSettings playtest) && playtest.DeveloperModeOn)
            {
                _storyButton = UiKit.CreateButton(_column, Loc.Get("options.story_viewer"), ShowStoryViewer);
            }

            UiKit.CreateButton(_column, Loc.Get("common.back"), Close);
            UiKit.Select(volumes.First);

            BuildControlsPage(root);

            if (Services.TryGet(out _input))
            {
                _input.Cancelled += OnCancelled;
            }
        }

        private void OnDestroy()
        {
            _settings?.Flush();
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
            string[,] controlRows = ControlRows();
            string[] controlHeaders = ControlHeaders();
            string gold = ColorUtility.ToHtmlStringRGB(HeaderColour);
            int rows = controlRows.GetLength(0);
            for (int column = 0; column < controlHeaders.Length; column++)
            {
                // The header line in gold through rich text, so it stays a line of the same text as the rows below it.
                var lines = new System.Text.StringBuilder();
                lines.Append("<color=#").Append(gold).Append('>').Append(controlHeaders[column]).Append("</color>");
                for (int row = 0; row < rows; row++)
                {
                    lines.Append('\n').Append(string.Format(controlRows[row, column], hero));
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

            Button back = UiKit.CreateButton(page, Loc.Get("common.back"), HideControls);
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
            if (Services.TryGet(out SettingsService settings))
            {
                settings.Flush();
            }

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

        // Mute in the background: on by default; the switch takes effect the next time the window loses focus.
        private void OnMuteToggled()
        {
            if (_settings == null)
            {
                return;
            }

            _settings.SetMuteInBackground(!_settings.Data.MuteInBackground);
            UiKit.SetLabel(_muteButton, MuteLabel());
        }

        private string MuteLabel()
        {
            bool mute = _settings == null || _settings.Data.MuteInBackground;
            return mute ? Loc.Get("options.mute_background_on") : Loc.Get("options.mute_background_off");
        }

        // The language button steps through every language that has a table. All screens build their text when they
        // open, so a new language reloads the main menu instead of refreshing each label in place.
        private void OnLanguagePressed()
        {
            IReadOnlyList<LanguageInfo> languages = _localization != null ? _localization.Languages : null;
            if (languages == null || languages.Count < 2)
            {
                return;
            }

            int next = (IndexOf(languages, _localization.CurrentLanguage) + 1) % languages.Count;
            string code = languages[next].Code;
            if (!_localization.SetLanguage(code))
            {
                return;
            }

            _settings?.SetLanguage(code);
            _settings?.Flush();
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private string LanguageLabel()
        {
            string current = Loc.Get("language.name");
            return Loc.Format("options.language", current);
        }

        private static int IndexOf(IReadOnlyList<LanguageInfo> languages, string code)
        {
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i].Code == code)
                {
                    return i;
                }
            }

            return 0;
        }

        private void Close()
        {
            // Sliders and switches change the settings in memory; closing the screen writes them once.
            _settings?.Flush();
            Destroy(_panel);
            _owner.ShowMenu();
            Destroy(this);
        }
    }
}
