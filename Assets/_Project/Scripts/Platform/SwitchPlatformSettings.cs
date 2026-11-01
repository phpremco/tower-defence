namespace Game.Platform
{
    /// <summary>The "Low (Switch)" quality profile: softer bloom, no mist or depth of field, half the particles, no quit.</summary>
    public sealed class SwitchPlatformSettings : IPlatformSettings
    {
        public string ProfileName => "Low (Switch)";

        public bool BloomEnabled => true;

        public float BloomScale => 0.6f;

        public bool FogEnabled => false;

        public bool DepthOfFieldEnabled => false;

        public float ParticleScale => 0.5f;

        public bool CanQuit => false;
    }
}
