using Game.Core;
using Game.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The camera options the options panel and the pause menu share: a "Camera speed" slider (a factor on the default
    /// pan speed, for keys, stick and screen edge alike) and an edge-scrolling switch. The level camera hears every
    /// change at once; the progress file keeps the speed in whole percents.
    /// </summary>
    public sealed class CameraOptionControls
    {
        private const float PercentScale = 100f;
        private const int SnapPercent = 10;

        private readonly ProgressService _progress;
        private readonly CameraControls _controls;
        private Slider _slider;
        private Text _speedLabel;
        private Text _edgeLabel;

        public CameraOptionControls(ProgressService progress, CameraControls controls)
        {
            _progress = progress;
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

        private void Store(float scale, bool edgeScrolling)
        {
            _controls.Set(scale, edgeScrolling);
            _progress?.SetCameraControls(Percent(_controls.SpeedScale), _controls.EdgeScrolling);
        }

        private static float ToSlider(float scale)
        {
            return Mathf.InverseLerp(CameraControls.MinSpeedScale, CameraControls.MaxSpeedScale, scale);
        }

        private static int Percent(float scale) => Mathf.RoundToInt(scale * PercentScale);

        private static string SpeedLabel(int percent) => $"Camera speed   {percent}%";

        private static string EdgeLabel(bool on) => on ? "Edge scrolling   On" : "Edge scrolling   Off";
    }
}
