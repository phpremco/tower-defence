using System;

namespace Game.Enemies
{
    /// <summary>
    /// The brood one level boss sends out along its walk (round 88b). Every call grants a budget of health: the escort's
    /// worth over a whole walk, spread over the walk's seconds and weighted by how far down its road the boss is, so the
    /// calls grow as it nears the base. The budget buys whole creatures at the health they walk in with; what is left
    /// carries over to the next call. A share of it buys plated creatures when the whole walk can pay for at least one.
    /// Pure arithmetic: the boss's powers drive it in play, the lab and the tests drive it on paper.
    /// </summary>
    public sealed class EscortBrood
    {
        private EscortSettings _settings;
        private float _fullWorth;
        private float _walkSeconds;
        private float _lightCost;
        private float _heavyCost;
        private float _lightBudget;
        private float _heavyBudget;
        private float _granted;

        /// <summary>True once <see cref="Begin"/> gave it a boss with a walk and a creature to send.</summary>
        public bool Active { get; private set; }

        /// <summary>True when a share of the brood walks in plated.</summary>
        public bool SendsHeavy { get; private set; }

        /// <summary>Health of the creatures sent so far.</summary>
        public float Sent { get; private set; }

        /// <summary>Health the escort is worth over an unhindered walk from spawn to the base.</summary>
        public float FullWorth => _fullWorth;

        /// <param name="bossBar">The boss's body plus armour as it spawned.</param>
        /// <param name="walkSeconds">Seconds of its walk from spawn to the base at its own speed, nothing in its way.</param>
        /// <param name="lightCost">Body plus armour of one escort creature as it walks in.</param>
        /// <param name="heavyCost">The same of one plated escort; zero or less when there is none to send.</param>
        public void Begin(EscortSettings settings, float bossBar, float walkSeconds, float lightCost, float heavyCost)
        {
            _settings = settings;
            _fullWorth = Math.Max(0f, settings.Worth * bossBar);
            _walkSeconds = walkSeconds;
            _lightCost = lightCost;
            _heavyCost = heavyCost;
            _lightBudget = 0f;
            _heavyBudget = 0f;
            _granted = 0f;
            Sent = 0f;
            Active = _fullWorth > 0f && walkSeconds > 0f && lightCost > 0f;
            // An escort that could not pay for one plated creature over the whole walk sends none, and loses no health to the saving.
            SendsHeavy = Active && heavyCost > 0f && _fullWorth * settings.HeavyShare >= heavyCost;
        }

        public void Stop()
        {
            Active = false;
            SendsHeavy = false;
        }

        /// <summary>
        /// One call: grants the budget of the <paramref name="seconds"/> since the last call at the boss's
        /// <paramref name="progress"/> down its road (0 at its spawn, 1 at the base) and says how many creatures it buys.
        /// </summary>
        public void Cast(float seconds, float progress, out int light, out int heavy)
        {
            light = 0;
            heavy = 0;
            if (!Active)
            {
                return;
            }

            float grant = _fullWorth * Weight(_settings, progress) * seconds / _walkSeconds;
            grant = Math.Max(0f, Math.Min(grant, _fullWorth * _settings.MostOfWorth - _granted));
            _granted += grant;
            float heavyPart = SendsHeavy ? grant * _settings.HeavyShare : 0f;
            _heavyBudget += heavyPart;
            _lightBudget += grant - heavyPart;
            if (SendsHeavy)
            {
                heavy = (int)Math.Floor(_heavyBudget / _heavyCost);
                _heavyBudget -= heavy * _heavyCost;
            }

            light = (int)Math.Floor(_lightBudget / _lightCost);
            _lightBudget -= light * _lightCost;
            Sent += light * _lightCost + heavy * _heavyCost;
        }

        /// <summary>
        /// How fast the brood comes at a point of the walk against its mean rate: a straight line from the start weight to
        /// the end weight, scaled so that a whole unhindered walk sends exactly the worth.
        /// </summary>
        public static float Weight(EscortSettings settings, float progress)
        {
            float mean = (settings.StartWeight + settings.EndWeight) * 0.5f;
            if (mean <= 0f)
            {
                return 1f;
            }

            float p = Math.Max(0f, Math.Min(1f, progress));
            return (settings.StartWeight + (settings.EndWeight - settings.StartWeight) * p) / mean;
        }

        /// <summary>Share of the full walk's worth an unhindered boss has sent by the time it is this far down its road.</summary>
        public static float ShareBy(EscortSettings settings, float progress)
        {
            float mean = (settings.StartWeight + settings.EndWeight) * 0.5f;
            if (mean <= 0f)
            {
                return Math.Max(0f, Math.Min(1f, progress));
            }

            float p = Math.Max(0f, Math.Min(1f, progress));
            return (settings.StartWeight * p + (settings.EndWeight - settings.StartWeight) * p * p * 0.5f) / mean;
        }
    }
}
