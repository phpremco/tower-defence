namespace Game.Platform
{
    /// <summary>Per-platform quality switches: "High (PC)" uses every effect, "Low (Switch)" uses fewer.</summary>
    public interface IPlatformSettings
    {
        string ProfileName { get; }

        bool BloomEnabled { get; }

        bool FogEnabled { get; }

        /// <summary>Multiplier for particle counts, 1 is full.</summary>
        float ParticleScale { get; }

        bool CanQuit { get; }
    }
}
