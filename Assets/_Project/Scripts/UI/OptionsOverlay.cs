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
    /// display and graphics page, the controls page with rebinding, the sound test and a replay of the opening
    /// cutscene. The same panel opens from the pause menu inside a level, without the rows that leave it.</summary>
    public sealed class OptionsOverlay : MonoBehaviour
    {
        // In front of the HUD, the pause menu and the dialogue box when opened inside a level.
        private const int LevelSortingOrder = 100;

        private System.Action _closed;
        private GameObject _panel;
        private ControlsPage _controls;
        private RectTransform _displayColumn;
        private DisplayOptionsPage _display;
        private Button _displayButton;
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
        private const float ColumnSpacing = 24f;

        /// <summary>Opens over the main menu; closing hands the focus back to it.</summary>
        public void Open(MainMenuScreen owner)
        {
            Build(owner.ShowMenu, false);
        }

        /// <summary>
        /// Opens over the pause menu of a running level, which stays paused underneath. The rows that leave the level
        /// (language, which reloads the main menu, the sound test, the intro replay and the story viewer) are left out.
        /// </summary>
        public void OpenInLevel(System.Action closed)
        {
            Build(closed, true);
        }

        /// <summary>True while a key or button is being rebound: the owner leaves every press to the options.</summary>
        public bool Busy => _controls != null && _controls.Busy;

        /// <summary>Closes the options as Back would (the settings are written once).</summary>
        public void Close()
        {
            // Sliders and switches change the settings in memory; closing the screen writes them once.
            _settings?.Flush();
            Destroy(_panel);
            System.Action closed = _closed;
            _closed = null;
            Destroy(this);
            closed?.Invoke();
        }

        private void Build(System.Action closed, bool inLevel)
        {
            _closed = closed;
            Services.TryGet(out _settings);
            Services.TryGet(out _localization);
            Services.TryGet(out AudioMix mix);

            RectTransform root = UiKit.CreateRoot(transform, !inLevel);
            _panel = root.parent.gameObject;
            if (inLevel)
            {
                _panel.GetComponent<Canvas>().sortingOrder = LevelSortingOrder;
            }

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
            _column = UiKit.CreateColumn(root, ColumnSpacing);
            ColumnFit.Attach(_column, -TitleTop + TitleHeight + TitleGap, BottomMargin);
            var volumes = new VolumeControls(_settings, mix);
            volumes.Build(_column);
            _muteButton = UiKit.CreateButton(_column, MuteLabel(), OnMuteToggled);
            Services.TryGet(out CameraControls cameraControls);
            new CameraOptionControls(_settings, cameraControls ?? new CameraControls()).Build(_column);
            new HudMarginControl(_settings).Build(_column);
            if (!inLevel)
            {
                UiKit.CreateButton(_column, LanguageLabel(), OnLanguagePressed);
            }

            _displayButton = UiKit.CreateButton(_column, Loc.Get("options.display"), ShowDisplay);
            _controlsButton = UiKit.CreateButton(_column, Loc.Get("options.controls"), ShowControls);
            if (!inLevel)
            {
                AddMenuOnlyRows();
            }

            UiKit.CreateButton(_column, Loc.Get("common.back"), Close);
            UiKit.Select(volumes.First);

            _controls = ControlsPage.Create(root, HideControls);
            BuildDisplayPage(root);

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

        // B or Esc steps back one page: the controls, the display page or the sound test to the options, the options to
        // the main menu. A pending display change is reverted first; a key being rebound keeps Esc for itself. The story
        // viewer is a developer tool with its own buttons and keeps B for itself.
        private void OnCancelled()
        {
            if (_storyOpen || (_controls != null && _controls.Busy))
            {
                return;
            }

            if (_soundBoard != null)
            {
                _soundBoard.Close();
            }
            else if (_display != null && _display.PromptOpen)
            {
                _display.RevertPrompt();
            }
            else if (_controls != null && _controls.gameObject.activeSelf)
            {
                HideControls();
            }
            else if (_displayColumn != null && _displayColumn.gameObject.activeSelf)
            {
                HideDisplay();
            }
            else
            {
                Close();
            }
        }

        // The display page is a column of its own under the same title; it shrinks to fit like the main column.
        private void BuildDisplayPage(RectTransform root)
        {
            Services.TryGet(out IPlatformSettings platform);
            Services.TryGet(out QualityProfileSwitch quality);
            _displayColumn = UiKit.CreateColumn(root, ColumnSpacing);
            ColumnFit.Attach(_displayColumn, -TitleTop + TitleHeight + TitleGap, BottomMargin);
            _display = new DisplayOptionsPage(_settings, platform, quality);
            _display.Build(_displayColumn, root, HideDisplay);
            _displayColumn.gameObject.SetActive(false);
        }

        private void ShowDisplay()
        {
            _column.gameObject.SetActive(false);
            _displayColumn.gameObject.SetActive(true);
            _title.text = Loc.Get("display.title");
            UiKit.Select(_display.First);
        }

        private void HideDisplay()
        {
            _displayColumn.gameObject.SetActive(false);
            _column.gameObject.SetActive(true);
            _title.text = Loc.Get("options.title");
            UiKit.Select(_displayButton);
        }

        private void ShowControls()
        {
            _column.gameObject.SetActive(false);
            _controls.gameObject.SetActive(true);
            _title.text = Loc.Get("controls.title");
            UiKit.Select(_controls.First);
        }

        private void HideControls()
        {
            _controls.gameObject.SetActive(false);
            _column.gameObject.SetActive(true);
            _title.text = Loc.Get("options.title");
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

        // The sound test, the intro replay and the developer tools belong to the main menu only.
        private void AddMenuOnlyRows()
        {
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
        }
    }
}
