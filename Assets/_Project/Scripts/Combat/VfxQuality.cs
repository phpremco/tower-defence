namespace Game.Combat
{
    /// <summary>
    /// How much of the effects a profile can afford: the share of particles, the half-size textures, the distortion
    /// passes (they need the opaque texture only the high profile renders) and the point lights blasts throw.
    /// </summary>
    public readonly struct VfxQuality
    {
        public VfxQuality(float particleScale, bool lowTextures, bool distortion)
        {
            ParticleScale = particleScale;
            LowTextures = lowTextures;
            Distortion = distortion;
        }

        public static VfxQuality High => new VfxQuality(1f, false, true);

        public float ParticleScale { get; }

        public bool LowTextures { get; }

        public bool Distortion { get; }

        /// <summary>Lights cost more than particles, so a reduced particle budget goes without them.</summary>
        public bool Lights => ParticleScale >= 1f;
    }
}
