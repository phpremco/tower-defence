using System.Collections.Generic;

namespace Game.Levels
{
    /// <summary>
    /// When the hero may call out again (Docs/BOSS_INTROS.md: "with a cooldown so they never spam"): each boss waits its
    /// own cooldown after its last bark, and any two barks keep a short gap between them, whoever they are about. Times
    /// are game seconds; the caller passes the clock in, so it is pure and testable.
    /// </summary>
    public sealed class BarkCooldown
    {
        private readonly Dictionary<object, float> _last = new Dictionary<object, float>();
        private readonly float _gap;
        private float _lastAny = float.NegativeInfinity;

        /// <param name="gap">Seconds between any two barks.</param>
        public BarkCooldown(float gap)
        {
            _gap = gap;
        }

        /// <summary>True when a bark about <paramref name="source"/> may play now; it is then counted as played.</summary>
        public bool TryBark(object source, float cooldown, float now)
        {
            if (!Ready(source, cooldown, now))
            {
                return false;
            }

            _last[source] = now;
            _lastAny = now;
            return true;
        }

        /// <summary>True when a bark about <paramref name="source"/> could play now (nothing is counted).</summary>
        public bool Ready(object source, float cooldown, float now)
        {
            if (now - _lastAny < _gap)
            {
                return false;
            }

            return !_last.TryGetValue(source, out float last) || now - last >= cooldown;
        }

        /// <summary>Starts a source's cooldown without a bark (its intro just played, so its first bark waits).</summary>
        public void Hold(object source, float now)
        {
            _last[source] = now;
            _lastAny = now;
        }
    }
}
