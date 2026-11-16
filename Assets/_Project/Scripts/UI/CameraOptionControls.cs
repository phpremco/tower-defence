using Game.Core;
using Game.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The camera options the options panel and the pause menu share: a "Camera speed" slider (a factor on the default
    /// pan speed, for keys, stick and screen edge alike), an edge-scrolling switch, and the HUD radar (a switch and a size
    /// slider). The level camera and the HUD hear every change at once; the settings keep speed and size in percents.
    /// </summary>
    public sealed class CameraOptionControls
    {
        private const float PercentScale = 100f;
        private const int SnapPercent = 10;

        private readonly SettingsService _settings;
        private readonly CameraControls _controls;
        private Slider _slider;
        private Text _speedLabel;
        private Text _edgeLabel;
        private Slider _radarSize;
        private Text _radarSizeLabel;
        private Text _radarLabel;

        public CameraOptionControls(SettingsService settings, CameraControls controls)
        {
            _settings = settings;
            _controls = controls;
        }

        /// <summary>The speed slider, so a caller can select it for the gamepad.</summary>
        public Selectable First => _slider;

        public void Build(RectTransform column)
        {
            int percent = Percent(_controls.SpeedScale);
            _slider = UiKit.CreateSlider(column, SpeedLabel(percent), ToSlider(_controls.SpeedScale), OnSpeed);
            _speedLabel = _slider.GetComponentInChildren<Text>();
            Button edge = UiKit.CreateButton(column, EdgeLabel(_controls.EdgeScrolling), OnEdgeToggled);
            _edgeLabel = edge.GetComponentInChildren<Text>();
            Button radar = UiKit.CreateButton(column, RadarLabel(_controls.RadarOn), OnRadarToggled);
            _radarLabel = radar.GetComponentInChildren<Text>();
            _radarSize = UiKit.CreateSlider(column, RadarSizeLabel(Percent(_controls.RadarScale)), ToRadarSlider(_controls.RadarScale), OnRadarSize);
            _radarSizeLabel = _radarSize.GetComponentInChildren<Text>();
        }

        private void OnSpeed(float value)
        {
            float scale = Mathf.Lerp(CameraControls.MinSpeedScale, CameraControls.MaxSpeedScale, value);
            int percent = Mathf.RoundToInt(scale * PercentScale / SnapPercent) * SnapPercent;
            Store(percent / PercentScale, _controls.EdgeScrolling);
            _speedLabel.text = SpeedLabel(Percent(_controls.SpeedScale));
        }

        private void OnEdgeToggled()
        {
            Store(_controls.SpeedScale, !_controls.EdgeScrolling);
            _edgeLabel.text = EdgeLabel(_controls.EdgeScrolling);
        }

        private void OnRadarToggled()
        {
            StoreRadar(!_controls.RadarOn, _controls.RadarScale);
            _radarLabel.text = RadarLabel(_controls.RadarOn);
        }

        private void OnRadarSize(float value)
        {
            float scale = Mathf.Lerp(CameraControls.MinRadarScale, CameraControls.MaxRadarScale, value);
            int percent = Mathf.RoundToInt(scale * PercentScale / SnapPercent) * SnapPercent;
            StoreRadar(_controls.RadarOn, percent / PercentScale);
            _radarSizeLabel.text = RadarSizeLabel(Percent(_controls.RadarScale));
        }

        private void StoreRadar(bool on, float scale)
        {
            _controls.SetRadar(on, scale);
            _settings?.SetRadar(_controls.RadarOn, Percent(_controls.RadarScale));
        }

        private void Store(float scale, bool edgeScrolling)
        {
            _controls.Set(scale, edgeScrolling);
            _settings?.SetCameraControls(Percent(_controls.SpeedScale), _controls.EdgeScrolling);
        }

        private static float ToSlider(float scale)
        {
            return Mathf.InverseLerp(CameraControls.MinSpeedScale, CameraControls.MaxSpeedScale, scale);
        }

        private static int Percent(float scale) => Mathf.RoundToInt(scale * PercentScale);

        private static string SpeedLabel(int percent) => Loc.Format("options.camera_speed", percent);

        private static string EdgeLabel(bool on) => on ? Loc.Get("options.edge_scrolling_on") : Loc.Get("options.edge_scrolling_off");

        private static float ToRadarSlider(float scale) => Mathf.InverseLerp(CameraControls.MinRadarScale, CameraControls.MaxRadarScale, scale);

        private static string RadarLabel(bool on) => on ? Loc.Get("options.radar_on") : Loc.Get("options.radar_off");

        private static string RadarSizeLabel(int percent) => Loc.Format("options.radar_size", percent);
    }
}
