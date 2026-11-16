namespace Game.Levels
{
    /// <summary>The state of the level a wave call is judged against (<see cref="WaveCaller.Check"/>).</summary>
    public readonly struct WaveCallSituation
    {
        public WaveCallSituation(bool running, bool inputBlocked, bool notDeployed, bool beatPlaying)
        {
            Running = running;
            InputBlocked = inputBlocked;
            NotDeployed = notDeployed;
            BeatPlaying = beatPlaying;
        }

        public bool Running { get; }

        public bool InputBlocked { get; }

        public bool NotDeployed { get; }

        public bool BeatPlaying { get; }

        /// <summary>Reads the situation off the level's shared state.</summary>
        public static WaveCallSituation Of(LevelContext context) => new WaveCallSituation(
            context.Outcome == LevelOutcome.Running,
            context.InputBlocked,
            context.BriefingPending || context.ReconPending,
            context.WaveCallHeld);
    }
}
