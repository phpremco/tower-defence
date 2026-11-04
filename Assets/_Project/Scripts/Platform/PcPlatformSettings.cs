namespace Game.Platform
{
    /// <summary>The "High (PC)" quality profile.</summary>
    public sealed class PcPlatformSettings : IPlatformSettings
    {
        public string ProfileName => "High (PC)";

        public bool BloomEnabled => true;

        public float BloomScale => 1f;

        public bool HighQualityBloom => true;

        public bool FogEnabled => true;

        public bool DepthOfFieldEnabled => true;

        public float ParticleScale => 1f;

        public bool DistortionEnabled => true;

        public bool LowDetailModels => false;

        public float LodBias => 1f;

        public bool CanQuit => true;
    }
}
