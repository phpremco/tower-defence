namespace Game.Platform
{
    /// <summary>The "Low (Switch)" quality profile: softer and cheaper bloom, no mist or depth of field, half the particles, no quit.</summary>
    public sealed class SwitchPlatformSettings : IPlatformSettings
    {
        public string ProfileName => "Low (Switch)";

        public bool BloomEnabled => true;

        public float BloomScale => 0.6f;

        public bool HighQualityBloom => false;

        public bool FogEnabled => false;

        public bool DepthOfFieldEnabled => false;

        public float ParticleScale => 0.5f;

        public bool DistortionEnabled => false;

        public bool LowDetailModels => true;

        public float LodBias => 0.6f;

        public bool CanQuit => false;

        // Docked on a TV the edges may be cut off: keep the HUD inside the usual title-safe frame.
        public float SafeMarginPercent => 5f;

        public PadLayout PadLayout => PadLayout.Nintendo;
    }
}
