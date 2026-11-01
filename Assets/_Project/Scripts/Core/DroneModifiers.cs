namespace Game.Core
{
    /// <summary>
    /// Bonuses from the permanent upgrades of one drone type, applied when it is launched. "Output" is the job's
    /// yield (repair per second, money per trip or damage per shot); "tempo" is how fast the job is done.
    /// </summary>
    public readonly struct DroneModifiers
    {
        public DroneModifiers(float speedMultiplier, float healthMultiplier, float outputMultiplier, float tempoMultiplier, int extraCount)
        {
            SpeedMultiplier = speedMultiplier;
            HealthMultiplier = healthMultiplier;
            OutputMultiplier = outputMultiplier;
            TempoMultiplier = tempoMultiplier;
            ExtraCount = extraCount;
        }

        public static DroneModifiers None => new DroneModifiers(1f, 1f, 1f, 1f, 0);

        public float SpeedMultiplier { get; }
        public float HealthMultiplier { get; }
        public float OutputMultiplier { get; }
        public float TempoMultiplier { get; }

        /// <summary>How many more of this drone may fly at once.</summary>
        public int ExtraCount { get; }
    }
}
