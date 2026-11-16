namespace Game.Levels
{
    /// <summary>
    /// The rules of calling the next wave early (round 73): when a call is accepted, the cooldown between calls, and the bonus
    /// for the seconds the countdown still had. Plain logic; the level controller asks it and starts the wave.
    /// </summary>
    public sealed class WaveCaller
    {
        private readonly WaveCallDefinition _rules;

        public WaveCaller(WaveCallDefinition rules)
        {
            _rules = rules;
        }

        /// <summary>Game seconds until the next call is accepted.</summary>
        public float CooldownLeft { get; private set; }

        /// <summary>How many waves the player has called early in this level.</summary>
        public int Calls { get; private set; }

        /// <summary>The coins those calls have paid in all.</summary>
        public int BonusPaid { get; private set; }

        public void Tick(float deltaTime)
        {
            if (CooldownLeft > 0f)
            {
                CooldownLeft = deltaTime >= CooldownLeft ? 0f : CooldownLeft - deltaTime;
            }
        }

        /// <summary>Why a call is refused in this situation, or <see cref="WaveCallBlock.None"/>; checked in the order a player would read them.</summary>
        public WaveCallBlock Check(in WaveCallSituation situation, WaveClock clock)
        {
            if (!situation.Running)
            {
                return WaveCallBlock.LevelOver;
            }

            if (situation.NotDeployed)
            {
                return WaveCallBlock.NotDeployed;
            }

            if (situation.InputBlocked)
            {
                return WaveCallBlock.InputBlocked;
            }

            if (situation.BeatPlaying)
            {
                return WaveCallBlock.BeatPlaying;
            }

            if (clock.AllStarted)
            {
                return WaveCallBlock.NoWaveLeft;
            }

            if (clock.Spawning)
            {
                return WaveCallBlock.WaveSpawning;
            }

            if (clock.Started == 0 && !_rules.AllowBeforeFirstWave)
            {
                return WaveCallBlock.BeforeFirstWave;
            }

            return CooldownLeft > 0f ? WaveCallBlock.Cooldown : WaveCallBlock.None;
        }

        /// <summary>A call was accepted with this much countdown left: starts the cooldown and returns the bonus to pay.</summary>
        public int Accept(float secondsSaved)
        {
            CooldownLeft = _rules.CooldownSeconds;
            int bonus = _rules.BonusFor(secondsSaved);
            Calls++;
            BonusPaid += bonus;
            return bonus;
        }
    }
}
