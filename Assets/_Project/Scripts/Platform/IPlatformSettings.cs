namespace Game.Platform
{
    /// <summary>Per-platform quality switches: "High (PC)" uses every effect, "Low (Switch)" uses fewer.</summary>
    public interface IPlatformSettings
    {
        string ProfileName { get; }

        bool BloomEnabled { get; }

        /// <summary>Multiplier on the theme's bloom intensity, 1 is full.</summary>
        float BloomScale { get; }

        /// <summary>The finer, costlier bloom filter; the low profile uses the cheaper one.</summary>
        bool HighQualityBloom { get; }

        bool FogEnabled { get; }

        /// <summary>The tilt-shift depth of field that keeps the band around the focus sharp and softens the rest.</summary>
        bool DepthOfFieldEnabled { get; }

        /// <summary>Multiplier for particle counts, 1 is full.</summary>
        float ParticleScale { get; }

        bool CanQuit { get; }
    }
}
