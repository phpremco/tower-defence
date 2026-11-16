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
        // 2 (round 70): display, frame rate, quality, screen shake and key bindings.
        public const int CurrentVersion = 2;
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

        /// <summary>The window mode as a <c>Game.Platform.DisplayMode</c> number; borderless full screen by default.</summary>
        public int DisplayMode = DefaultDisplayMode;

        /// <summary>The chosen screen size; 0 by 0 (the default) keeps the desktop resolution.</summary>
        public int ResolutionWidth;

        public int ResolutionHeight;

        /// <summary>Waits for the monitor between frames; on by default.</summary>
        public bool VSync = true;

        /// <summary>Frames per second at most when v-sync is off; 0 is no cap.</summary>
        public int FrameRateCap;

        /// <summary>The player's quality profile on PC: false is "High (PC)", true is "Low".</summary>
        public bool LowQuality;

        /// <summary>How hard camera kicks are, from 0 (none) to 100 (as designed).</summary>
        public int ScreenShakePercent = 100;

        /// <summary>Rebound keys and buttons as the Input System's override JSON; empty keeps the default bindings.</summary>
        public string BindingOverrides = string.Empty;

        // Borderless full screen: what the build has always started in (BuildScript).
        private const int DefaultDisplayMode = 1;
    }
}
