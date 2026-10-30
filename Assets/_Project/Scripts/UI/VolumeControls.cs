using Game.Core;
using Game.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The four volume sliders (master, music, effects, ambience) that the options panel and the pause menu share.
    /// The mix hears every change at once and the progress file keeps it in whole percents.
    /// </summary>
    public sealed class VolumeControls
    {
        private const int Max = 100;
        private const int SnapPercent = 5;

        private readonly ProgressService _progress;
        private readonly AudioMix _mix;
        private readonly Slider[] _sliders = new Slider[4];
        private readonly Text[] _labels = new Text[4];
        private static readonly string[] Names = { "Master", "Music", "Effects", "Ambience" };

        public VolumeControls(ProgressService progress, AudioMix mix)
        {
            _progress = progress;
            _mix = mix;
        }

        /// <summary>The first slider, so a caller can select it for the gamepad.</summary>
        public Selectable First => _sliders[0];

        public void Build(RectTransform column)
        {
            int[] values = Values();
            for (int i = 0; i < _sliders.Length; i++)
            {
                int group = i;
                _sliders[i] = UiKit.CreateSlider(column, Label(group, values[group]), values[group] / (float)Max, value => Apply(group, value));
                _labels[i] = _sliders[i].GetComponentInChildren<Text>();
            }
        }

        // The slider moves in tenths from the gamepad and freely from the mouse; the saved value snaps to whole steps.
        private void Apply(int group, float value)
        {
            int[] values = Values();
            values[group] = Mathf.Clamp(Mathf.RoundToInt(value * Max / SnapPercent) * SnapPercent, 0, Max);
            _progress.SetVolumes(values[0], values[1], values[2], values[3]);
            _mix.Set(values[0] / (float)Max, values[1] / (float)Max, values[2] / (float)Max, values[3] / (float)Max);
            AudioListener.volume = _mix.Master;
            _labels[group].text = Label(group, values[group]);
        }

        private int[] Values()
        {
            ProgressData data = _progress.Data;
            return new[] { data.MasterVolumePercent, data.MusicVolumePercent, data.EffectsVolumePercent, data.AmbienceVolumePercent };
        }

        private static string Label(int group, int percent) => $"{Names[group]} volume   {percent}%";
    }
}
