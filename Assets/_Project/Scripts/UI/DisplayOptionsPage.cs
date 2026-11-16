using System.Collections.Generic;
using Game.Core;
using Game.Economy;
using Game.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The "Display and graphics" page of the options: on PC the window mode and resolution (picked with sliders, put
    /// on screen with Apply and kept only after the "keep these settings?" prompt), v-sync, the frame-rate cap and the
    /// High or Low quality profile; on every platform the screen-shake strength. Values live in the settings file and
    /// are written when the options close.
    /// </summary>
    public sealed class DisplayOptionsPage
    {
        private const int ShakeStepPercent = 10;
        private const int MaxShakePercent = 100;

        private static readonly DisplayMode[] Modes = { DisplayMode.Fullscreen, DisplayMode.Borderless, DisplayMode.Windowed };

        private readonly SettingsService _settings;
        private readonly IPlatformSettings _platform;
        private readonly QualityProfileSwitch _quality;
        private RectTransform _root;
        private List<Vector2Int> _sizes;
        private Slider _mode;
        private Slider _resolution;
        private Text _modeLabel;
        private Text _resolutionLabel;
        private Text _vSyncLabel;
        private Slider _cap;
        private Text _capLabel;
        private Text _qualityLabel;
        private Text _shakeLabel;
        private Button _apply;
        private DisplayConfirmPrompt _prompt;

        public DisplayOptionsPage(SettingsService settings, IPlatformSettings platform, QualityProfileSwitch quality)
        {
            _settings = settings;
            _platform = platform;
            _quality = quality;
        }

        /// <summary>The first row, for the gamepad focus when the page opens.</summary>
        public Selectable First { get; private set; }

        /// <summary>True while the keep-or-revert prompt is up; B then answers it.</summary>
        public bool PromptOpen => _prompt != null;

        public void RevertPrompt() => _prompt?.Revert();

        /// <summary>Builds the rows into <paramref name="column"/>; <paramref name="promptRoot"/> hosts the keep prompt.</summary>
        public void Build(RectTransform column, RectTransform promptRoot, UnityEngine.Events.UnityAction back)
        {
            _root = promptRoot;
            bool pc = _platform != null && _platform.HasDisplayOptions && _settings != null;
            if (pc)
            {
                BuildWindowRows(column);
                BuildFrameRows(column);
            }

            Slider shake = UiKit.CreateSlider(column, ShakeLabel(ShakePercent()), ShakePercent() / (float)MaxShakePercent, OnShake);
            _shakeLabel = shake.GetComponentInChildren<Text>();
            First = First != null ? First : shake;
            UiKit.CreateButton(column, Loc.Get("common.back"), back);
        }

        private void BuildWindowRows(RectTransform column)
        {
            SettingsData data = _settings.Data;
            _sizes = DisplayOptions.Resolutions(Screen.resolutions);
            var desktop = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
            if (_sizes.Count == 0)
            {
                _sizes.Add(desktop);
            }

            Vector2Int chosen = data.ResolutionWidth > 0 ? new Vector2Int(data.ResolutionWidth, data.ResolutionHeight) : desktop;
            _mode = Stepped(column, ModeLabel((DisplayMode)data.DisplayMode), System.Array.IndexOf(Modes, (DisplayMode)data.DisplayMode), Modes.Length, OnModeMoved);
            _modeLabel = _mode.GetComponentInChildren<Text>();
            int index = Mathf.Max(0, DisplayOptions.IndexOf(_sizes, chosen));
            _resolution = Stepped(column, ResolutionLabel(_sizes[index]), index, _sizes.Count, OnResolutionMoved);
            _resolutionLabel = _resolution.GetComponentInChildren<Text>();
            _apply = UiKit.CreateButton(column, Loc.Get("display.apply"), ApplyWindow);
            First = _mode;
        }

        private void BuildFrameRows(RectTransform column)
        {
            SettingsData data = _settings.Data;
            Button vSync = UiKit.CreateButton(column, VSyncLabel(data.VSync), OnVSyncToggled);
            _vSyncLabel = vSync.GetComponentInChildren<Text>();
            _cap = Stepped(column, CapLabel(data.FrameRateCap, data.VSync), DisplayOptions.CapIndex(data.FrameRateCap), DisplayOptions.FrameRateCaps.Length, OnCapMoved);
            _capLabel = _cap.GetComponentInChildren<Text>();
            if (_quality != null)
            {
                Button quality = UiKit.CreateButton(column, QualityLabel(_quality.Low), OnQualityToggled);
                _qualityLabel = quality.GetComponentInChildren<Text>();
            }
        }

        // A slider of whole steps: left and right on the pad move one step, the mouse drags between them.
        private static Slider Stepped(RectTransform column, string label, int index, int count, UnityEngine.Events.UnityAction<float> moved)
        {
            Slider slider = UiKit.CreateSlider(column, label, 0f, NoChange);
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(1, count - 1);
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(Mathf.Clamp(index, 0, count - 1));
            slider.interactable = count > 1;
            slider.onValueChanged.AddListener(moved);
            return slider;
        }

        // The stepped sliders get their real listener once their range is set, so building them reports nothing.
        private static void NoChange(float value)
        {
        }

        private void OnModeMoved(float value) => _modeLabel.text = ModeLabel(PendingMode());

        private void OnResolutionMoved(float value) => _resolutionLabel.text = ResolutionLabel(PendingSize());

        private DisplayMode PendingMode() => Modes[Mathf.Clamp(Mathf.RoundToInt(_mode.value), 0, Modes.Length - 1)];

        private Vector2Int PendingSize() => _sizes[Mathf.Clamp(Mathf.RoundToInt(_resolution.value), 0, _sizes.Count - 1)];

        // The new mode goes on screen at once; the settings only take it when the player keeps it.
        private void ApplyWindow()
        {
            SettingsData data = _settings.Data;
            var oldMode = (DisplayMode)data.DisplayMode;
            int oldWidth = data.ResolutionWidth;
            int oldHeight = data.ResolutionHeight;
            DisplayMode mode = PendingMode();
            Vector2Int size = PendingSize();
            if (mode == oldMode && size.x == oldWidth && size.y == oldHeight)
            {
                return;
            }

            DisplayOptions.ApplyWindow(mode, size.x, size.y);
            _prompt = DisplayConfirmPrompt.Open(_root,
                () => Keep(mode, size),
                () => Revert(oldMode, oldWidth, oldHeight));
        }

        private void Keep(DisplayMode mode, Vector2Int size)
        {
            _settings.SetDisplay(mode, size.x, size.y);
            _settings.Flush();
            _prompt = null;
            UiKit.Select(_apply);
        }

        private void Revert(DisplayMode mode, int width, int height)
        {
            DisplayOptions.ApplyWindow(mode, width, height);
            _mode.value = System.Array.IndexOf(Modes, mode);
            var desktop = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
            _resolution.value = Mathf.Max(0, DisplayOptions.IndexOf(_sizes, width > 0 ? new Vector2Int(width, height) : desktop));
            _prompt = null;
            UiKit.Select(_apply);
        }

        private void OnVSyncToggled()
        {
            SettingsData data = _settings.Data;
            _settings.SetFrameRate(!data.VSync, data.FrameRateCap);
            DisplayOptions.ApplyFrameRate(data.VSync, data.FrameRateCap);
            _vSyncLabel.text = VSyncLabel(data.VSync);
            _capLabel.text = CapLabel(data.FrameRateCap, data.VSync);
        }

        private void OnCapMoved(float value)
        {
            SettingsData data = _settings.Data;
            int cap = DisplayOptions.FrameRateCaps[Mathf.Clamp(Mathf.RoundToInt(value), 0, DisplayOptions.FrameRateCaps.Length - 1)];
            _settings.SetFrameRate(data.VSync, cap);
            DisplayOptions.ApplyFrameRate(data.VSync, cap);
            _capLabel.text = CapLabel(cap, data.VSync);
        }

        // Applied at once for everything global (models, LOD, tilt-shift); the next screen or level is built with it.
        private void OnQualityToggled()
        {
            bool low = !_quality.Low;
            _quality.Set(low);
            _settings.SetLowQuality(low);
            _qualityLabel.text = QualityLabel(low);
        }

        private void OnShake(float value)
        {
            int percent = Mathf.RoundToInt(value * MaxShakePercent / ShakeStepPercent) * ShakeStepPercent;
            _settings?.SetScreenShake(percent);
            _shakeLabel.text = ShakeLabel(percent);
        }

        private int ShakePercent() => _settings != null ? _settings.Data.ScreenShakePercent : MaxShakePercent;

        private static string ModeLabel(DisplayMode mode)
        {
            string name;
            switch (mode)
            {
                case DisplayMode.Fullscreen: name = Loc.Get("display.mode_fullscreen"); break;
                case DisplayMode.Windowed: name = Loc.Get("display.mode_windowed"); break;
                default: name = Loc.Get("display.mode_borderless"); break;
            }

            return Loc.Format("display.mode", name);
        }

        private static string ResolutionLabel(Vector2Int size) => Loc.Format("display.resolution", size.x, size.y);

        private static string VSyncLabel(bool on) => on ? Loc.Get("display.vsync_on") : Loc.Get("display.vsync_off");

        // With v-sync on the monitor sets the pace and Unity ignores the cap, so the label says so.
        private static string CapLabel(int cap, bool vSync)
        {
            string value = cap > 0 ? Loc.Format("display.frame_cap_fps", cap) : Loc.Get("display.frame_cap_none");
            return vSync ? Loc.Format("display.frame_cap_vsync", value) : Loc.Format("display.frame_cap", value);
        }

        private static string QualityLabel(bool low) => low ? Loc.Get("display.quality_low") : Loc.Get("display.quality_high");

        private static string ShakeLabel(int percent) => Loc.Format("display.shake", percent);
    }
}
