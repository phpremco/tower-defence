using Game.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The "HUD margin" option: how far every HUD and menu keeps from the screen edges, for a TV that crops its picture.
    /// Starts at the platform's title-safe default; a choice is saved and moves every screen's frame at once.
    /// </summary>
    public sealed class HudMarginControl
    {
        private const int StepPercent = 1;

        private readonly ProgressService _progress;
        private Text _label;

        public HudMarginControl(ProgressService progress)
        {
            _progress = progress;
        }

        public Slider Build(RectTransform column)
        {
            float percent = UiKit.SafeMarginPercent;
            Slider slider = UiKit.CreateSlider(column, Label(Mathf.RoundToInt(percent)), percent / SafeFrame.MaxMarginPercent, OnChanged);
            _label = slider.GetComponentInChildren<Text>();
            return slider;
        }

        private void OnChanged(float value)
        {
            int percent = Mathf.RoundToInt(value * SafeFrame.MaxMarginPercent / StepPercent) * StepPercent;
            UiKit.SafeMarginPercent = percent;
            _progress?.SetHudMargin(percent);
            _label.text = Label(percent);
        }

        private static string Label(int percent) => $"HUD margin   {percent}%";
    }
}
