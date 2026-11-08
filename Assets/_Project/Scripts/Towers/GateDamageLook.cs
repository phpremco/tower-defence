namespace Game.Towers
{
    /// <summary>
    /// Whether a gate shows its damaged look (overlay, fallen panels, stuttering field), decided in one place with a band
    /// between going in and coming out. A repair drone heals a gate a little every frame while the creatures at it hit it
    /// in chunks, so its health hovers around one line; a single threshold made the look flip between broken and whole
    /// on every hit and every heal tick (round 23c). The look turns damaged below <see cref="DamagedBelow"/> and turns
    /// whole again at or above <see cref="RecoverAt"/>, or at once when one repair (the hero's, a bought one) lifts it past
    /// the line by at least the band's width: that is a deliberate fix, not a trickle. A wreck is never "damaged" (it
    /// has its own look) and a rebuilt gate starts whole. Plain class so the rule is tested without a scene.
    /// </summary>
    public sealed class GateDamageLook
    {
        private float _last = 1f;

        public GateDamageLook(float damagedBelow, float recoverMargin)
        {
            DamagedBelow = damagedBelow;
            RecoverMargin = System.Math.Max(0f, recoverMargin);
            RecoverAt = System.Math.Min(1f, damagedBelow + RecoverMargin);
        }

        public float DamagedBelow { get; }

        public float RecoverMargin { get; }

        /// <summary>Health share from which a damaged gate looks whole again.</summary>
        public float RecoverAt { get; }

        public bool IsDamaged { get; private set; }

        /// <summary>Takes the gate's health share and whether it is a wreck; true when the look changed.</summary>
        public bool Update(float fraction, bool wrecked)
        {
            bool oneRepair = fraction - _last >= RecoverMargin && fraction >= DamagedBelow;
            _last = fraction;
            bool damaged = !wrecked && (IsDamaged ? fraction < RecoverAt && !oneRepair : fraction < DamagedBelow);
            if (damaged == IsDamaged)
            {
                return false;
            }

            IsDamaged = damaged;
            return true;
        }
    }
}
