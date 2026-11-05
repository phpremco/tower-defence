namespace Game.Bosses
{
    /// <summary>
    /// A boss's ice armour (Docs/BOSSES.md, Frost Warden): while it holds only a share of each hit lands (resistance, never
    /// immunity); fire cracks it and lets double damage through for a few seconds; in the exposed phase it is gone for good
    /// and every hit lands in full. Pure logic: it hands out the damage factor, the brain applies it.
    /// </summary>
    public sealed class IceArmour
    {
        public enum Stage
        {
            Armoured = 0,
            Cracked = 1,
            Exposed = 2,
        }

        private readonly float _soak;
        private readonly float _crackedFactor;
        private readonly float _crackSeconds;
        private float _crackLeft;

        public IceArmour(float soak, float crackedFactor, float crackSeconds)
        {
            _soak = soak;
            _crackedFactor = crackedFactor;
            _crackSeconds = crackSeconds;
        }

        public Stage Current { get; private set; }

        /// <summary>The share of each hit that lands now.</summary>
        public float DamageFactor => Current == Stage.Armoured ? _soak : Current == Stage.Cracked ? _crackedFactor : 1f;

        /// <summary>Seconds the crack still lets damage through.</summary>
        public float CrackLeft => Current == Stage.Cracked ? _crackLeft : 0f;

        public void Reset()
        {
            Current = Stage.Armoured;
            _crackLeft = 0f;
        }

        /// <summary>Fire hit it: the armour cracks (or the crack lasts its full time again). False once it is exposed.</summary>
        public bool Burn()
        {
            if (Current == Stage.Exposed)
            {
                return false;
            }

            bool fresh = Current == Stage.Armoured;
            Current = Stage.Cracked;
            _crackLeft = _crackSeconds;
            return fresh;
        }

        /// <summary>The armour breaks off for good (its exposed phase). False when it was already gone.</summary>
        public bool Break()
        {
            if (Current == Stage.Exposed)
            {
                return false;
            }

            Current = Stage.Exposed;
            _crackLeft = 0f;
            return true;
        }

        /// <summary>Time passes; returns true when the crack closed again this step.</summary>
        public bool Step(float deltaTime)
        {
            if (Current != Stage.Cracked)
            {
                return false;
            }

            _crackLeft -= deltaTime;
            if (_crackLeft > 0f)
            {
                return false;
            }

            Current = Stage.Armoured;
            _crackLeft = 0f;
            return true;
        }
    }
}
