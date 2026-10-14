namespace Game.Platform
{
    /// <summary>The "High (PC)" quality profile.</summary>
    public sealed class PcPlatformSettings : IPlatformSettings
    {
        public string ProfileName => "High (PC)";

        public bool BloomEnabled => true;

        public bool FogEnabled => true;

        public float ParticleScale => 1f;

        public bool CanQuit => true;
    }
}
