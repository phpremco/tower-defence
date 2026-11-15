using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The bar of the loading screen: it follows what the load reports, but smoothly. The shown value closes a set share
    /// of the gap to the reported one each second (and never less than a slow crawl, so short reports still move it), it
    /// never runs backwards, and it only reaches the end once the load said it was done. Pure, so the feel is tested.
    /// </summary>
    public sealed class LoadingProgress
    {
        private readonly float _catchUp;
        private readonly float _crawl;
        private readonly float _holdBack;
        private readonly float _finish;
        private float _reported;
        private bool _complete;

        /// <param name="catchUpPerSecond">Share of the gap to the reported value closed per second (an exponential ease).</param>
        /// <param name="crawlPerSecond">The least the bar moves per second while it is behind.</param>
        /// <param name="holdBack">The furthest the bar goes before the load is complete, just short of full.</param>
        /// <param name="finishPerSecond">The least the bar moves per second once the load is complete, so the player is not kept waiting on an ease.</param>
        public LoadingProgress(float catchUpPerSecond, float crawlPerSecond, float holdBack, float finishPerSecond)
        {
            _catchUp = Mathf.Max(0.01f, catchUpPerSecond);
            _crawl = Mathf.Max(0f, crawlPerSecond);
            _holdBack = Mathf.Clamp01(holdBack);
            _finish = Mathf.Max(_crawl, finishPerSecond);
        }

        /// <summary>What the bar shows, from 0 to 1.</summary>
        public float Shown { get; private set; }

        /// <summary>What the load last reported, before any easing.</summary>
        public float Reported => _reported;

        public bool IsComplete => _complete;

        /// <summary>True once the load is done and the bar has reached its end.</summary>
        public bool IsFull => _complete && Shown >= 1f;

        public void Report(float fraction) => _reported = Mathf.Max(_reported, Mathf.Clamp01(fraction));

        public void Complete()
        {
            _complete = true;
            _reported = 1f;
        }

        /// <summary>Moves the shown value for one frame of this many (unscaled) seconds.</summary>
        public void Tick(float seconds)
        {
            float goal = _complete ? 1f : Mathf.Min(_reported, _holdBack);
            if (Shown >= goal || seconds <= 0f)
            {
                return;
            }

            float eased = (goal - Shown) * (1f - Mathf.Exp(-_catchUp * seconds));
            Shown = Mathf.Min(goal, Shown + Mathf.Max(eased, (_complete ? _finish : _crawl) * seconds));
        }
    }
}
