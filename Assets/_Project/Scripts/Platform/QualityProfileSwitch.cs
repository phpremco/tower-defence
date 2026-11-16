using System;

namespace Game.Platform
{
    /// <summary>
    /// The player's quality profile choice (High or Low) on platforms that offer one. The boot code hands in how a
    /// profile is applied (register the platform settings, LOD bias, the tilt-shift pass); the options screen only
    /// flips it. Screens built afterwards read the new profile; the next level is built with it entirely.
    /// </summary>
    public sealed class QualityProfileSwitch
    {
        private readonly Action<bool> _apply;

        public QualityProfileSwitch(bool low, Action<bool> apply)
        {
            Low = low;
            _apply = apply;
        }

        public bool Low { get; private set; }

        public event Action Changed;

        public void Set(bool low)
        {
            if (low == Low)
            {
                return;
            }

            Low = low;
            _apply?.Invoke(low);
            Changed?.Invoke();
        }
    }
}
