using Game.Core;
using Game.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// The four volume sliders (master, music, effects, ambience) that the options panel and the pause menu share.
    /// The mix hears every change at once and the settings keep it in whole percents (written when the screen closes).
    /// </summary>
    public sealed class VolumeControls
    {
        private const int Max = 100;
        private const int SnapPercent = 5;

        private readonly SettingsService _settings;
        private readonly AudioMix _mix;
        private readonly Slider[] _sliders = new Slider[4];
        private readonly Text[] _labels = new Text[4];

        public VolumeControls(SettingsService settings, AudioMix mix)
        {
            _settings = settings;
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
            _settings?.SetVolumes(values[0], values[1], values[2], values[3]);
            _mix.Set(values[0] / (float)Max, values[1] / (float)Max, values[2] / (float)Max, values[3] / (float)Max);
            AudioListener.volume = _mix.Master;
            _labels[group].text = Label(group, values[group]);
        }

        private int[] Values()
        {
            SettingsData data = _settings != null ? _settings.Data : new SettingsData();
            return new[] { data.MasterVolumePercent, data.MusicVolumePercent, data.EffectsVolumePercent, data.AmbienceVolumePercent };
        }

        private static string Label(int group, int percent)
        {
            switch (group)
            {
                case 0: return Loc.Format("options.volume.master", percent);
                case 1: return Loc.Format("options.volume.music", percent);
                case 2: return Loc.Format("options.volume.effects", percent);
                default: return Loc.Format("options.volume.ambience", percent);
            }
        }
    }
}
