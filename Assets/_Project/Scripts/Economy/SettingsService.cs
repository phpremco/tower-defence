using System;
using Game.Platform;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// Owns the player's options. Setters change the values in memory and raise <see cref="Changed"/> at once, so a
    /// slider is heard and seen while it moves; the file is written only by <see cref="Flush"/>, which the options
    /// screens call when they close (consoles limit how often a game may write).
    /// </summary>
    public sealed class SettingsService
    {
        public const string SaveKey = "settings";

        /// <summary>Saves before the settings split kept the options in the progress file under this key.</summary>
        public const string LegacyProgressKey = "progress";

        private readonly ISaveService _save;
        private bool _dirty;

        public SettingsService(ISaveService save)
        {
            _save = save;
            if (_save.TryLoad(SaveKey, out SettingsData data) && data != null)
            {
                Data = data;
                Normalise(Data);
                return;
            }

            Data = SeedFromProgress(save);
            Normalise(Data);
            // Written at once: the next progress save drops the old option fields, so the seed must not wait for it.
            _save.Save(SaveKey, Data);
        }

        public SettingsData Data { get; }

        /// <summary>True while a change has not been written yet.</summary>
        public bool IsDirty => _dirty;

        public event Action Changed;

        /// <summary>Stores all four volume groups at once, in percent.</summary>
        public void SetVolumes(int master, int music, int effects, int ambience)
        {
            Data.MasterVolumePercent = master;
            Data.MusicVolumePercent = music;
            Data.EffectsVolumePercent = effects;
            Data.AmbienceVolumePercent = ambience;
            MarkChanged();
        }

        /// <summary>Stores the camera options: pan speed in percent of the default, and edge scrolling on or off.</summary>
        public void SetCameraControls(int speedPercent, bool edgeScrolling)
        {
            Data.CameraSpeedPercent = speedPercent;
            Data.EdgeScrollingOff = !edgeScrolling;
            MarkChanged();
        }

        /// <summary>Stores the radar options: shown or not, and its size in percent of the default.</summary>
        public void SetRadar(bool on, int sizePercent)
        {
            Data.RadarOff = !on;
            Data.RadarSizePercent = sizePercent;
            MarkChanged();
        }

        public void SetHudMargin(int percent)
        {
            Data.HudMarginPercent = percent;
            MarkChanged();
        }

        public void SetLanguage(string languageCode)
        {
            Data.LanguageCode = string.IsNullOrEmpty(languageCode) ? SettingsData.DefaultLanguage : languageCode;
            MarkChanged();
        }

        public void SetMuteInBackground(bool mute)
        {
            Data.MuteInBackground = mute;
            MarkChanged();
        }

        /// <summary>Writes the options when something changed since the last write; cheap to call on every close.</summary>
        public void Flush()
        {
            if (!_dirty)
            {
                return;
            }

            _save.Save(SaveKey, Data);
            _dirty = false;
        }

        private void MarkChanged()
        {
            _dirty = true;
            Changed?.Invoke();
        }

        // The option fields of the old progress file carry the same names as SettingsData, so reading that file as
        // SettingsData picks up exactly those values and leaves the new fields at their defaults.
        private static SettingsData SeedFromProgress(ISaveService save)
        {
            if (save.TryLoad(LegacyProgressKey, out SettingsData legacy) && legacy != null)
            {
                legacy.Version = SettingsData.CurrentVersion;
                Debug.Log("Settings moved from the progress file into their own file.");
                return legacy;
            }

            return new SettingsData();
        }

        // A progress file read as settings carries its own version number, and older or hand-edited files can lack
        // the language; both are put right here so the rest of the game never sees them.
        private static void Normalise(SettingsData data)
        {
            data.Version = SettingsData.CurrentVersion;
            if (string.IsNullOrEmpty(data.LanguageCode))
            {
                data.LanguageCode = SettingsData.DefaultLanguage;
            }
        }
    }
}
