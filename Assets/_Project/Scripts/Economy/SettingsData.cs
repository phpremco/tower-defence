using System;

namespace Game.Economy
{
    /// <summary>
    /// The player's options, saved per machine in their own file (<c>settings.json</c>), apart from the progress: a new
    /// game keeps them, and a cloud save of the progress does not carry one machine's options to another. The audio,
    /// camera, radar and margin fields keep the names they had in <see cref="ProgressData"/>, so an older progress file
    /// read as this type yields its values (see <see cref="SettingsService"/>).
    /// </summary>
    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentVersion = 1;
        public const string DefaultLanguage = "en";

        public int Version = CurrentVersion;
        public int MasterVolumePercent = 100;
        public int MusicVolumePercent = 70;
        public int EffectsVolumePercent = 100;
        public int AmbienceVolumePercent = 80;

        /// <summary>The camera pan speed as a percent of the default.</summary>
        public int CameraSpeedPercent = 100;

        /// <summary>Off by choice: false (the default) keeps edge scrolling on.</summary>
        public bool EdgeScrollingOff;

        /// <summary>Off by choice: false (the default) shows the HUD radar.</summary>
        public bool RadarOff;

        /// <summary>The radar size as a percent of the default.</summary>
        public int RadarSizePercent = 100;

        /// <summary>
        /// The HUD margin on every screen edge as a percent of the screen; -1 (the default) keeps the platform's own
        /// title-safe margin.
        /// </summary>
        public int HudMarginPercent = -1;

        /// <summary>The language code of the string tables the game shows (see Docs/LOCALISATION.md).</summary>
        public string LanguageCode = DefaultLanguage;

        /// <summary>Silences the game while its window is in the background; on by default.</summary>
        public bool MuteInBackground = true;
    }
}
