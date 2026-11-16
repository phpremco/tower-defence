namespace Game.Platform
{
    /// <summary>
    /// The PC platform: the "High (PC)" quality profile, or the player's "Low" choice under Options, which takes every
    /// quality switch of the low profile (<see cref="SwitchPlatformSettings"/>) and keeps the PC's own window, quit and
    /// margin behaviour.
    /// </summary>
    public sealed class PcPlatformSettings : IPlatformSettings
    {
        private static readonly IPlatformSettings LowProfile = new SwitchPlatformSettings();

        private readonly bool _low;

        public PcPlatformSettings(bool lowQuality = false)
        {
            _low = lowQuality;
        }

        /// <summary>True when the player picked the low profile.</summary>
        public bool IsLowQuality => _low;

        public string ProfileName => _low ? "Low" : "High (PC)";

        public bool BloomEnabled => !_low || LowProfile.BloomEnabled;

        public float BloomScale => _low ? LowProfile.BloomScale : 1f;

        public bool HighQualityBloom => !_low;

        public bool FogEnabled => !_low;

        public bool DepthOfFieldEnabled => !_low;

        public float ParticleScale => _low ? LowProfile.ParticleScale : 1f;

        public bool DistortionEnabled => !_low;

        public bool LowDetailModels => _low;

        public float LodBias => _low ? LowProfile.LodBias : 1f;

        public bool CanQuit => true;

        // A monitor shows every pixel; the player can still add a margin under Options.
        public float SafeMarginPercent => 0f;

        public PadLayout PadLayout => PadLayout.Xbox;

        public bool HasDisplayOptions => true;
    }
}
