namespace Game.Hero
{
    /// <summary>The cooldown of one ability slot, as plain C# so it can be tested without a scene.</summary>
    public sealed class HeroAbilityState
    {
        public HeroAbilityState(HeroAbilityDefinition definition)
        {
            Definition = definition;
        }

        public HeroAbilityDefinition Definition { get; }

        /// <summary>Seconds until the ability can be used again.</summary>
        public float Remaining { get; private set; }

        public bool Ready => Remaining <= 0f;

        /// <summary>One when the ability can be used, growing from zero while it cools down.</summary>
        public float ReadyFraction => Definition.Cooldown > 0f ? 1f - Clamp01(Remaining / Definition.Cooldown) : 1f;

        /// <summary>Whole seconds left, as the HUD shows them; zero when ready.</summary>
        public int SecondsLeft => (int)System.Math.Ceiling(Remaining);

        /// <summary>
        /// Counts the cooldown down; true only when the shown seconds changed (or the slot became ready), so the HUD
        /// rebuilds its label once a second instead of every frame.
        /// </summary>
        public bool Tick(float seconds)
        {
            if (Remaining <= 0f)
            {
                return false;
            }

            int shown = SecondsLeft;
            Remaining = Remaining - seconds > 0f ? Remaining - seconds : 0f;
            return SecondsLeft != shown;
        }

        /// <summary>Starts the cooldown after a use.</summary>
        public void Begin() => Remaining = Definition.Cooldown;

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
