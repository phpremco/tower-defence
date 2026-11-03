namespace Game.Core
{
    /// <summary>
    /// Keeps a repeated action (a shot, a strike, a bite) on its own rhythm whatever the frame length. Resetting a
    /// cooldown to the full interval after it ran out throws away the part of the frame that overshot it, so at a high
    /// game speed or a low frame rate every gun fires slower than its data says (at 20x speed a 0.2 s gun fired once a
    /// frame, every 0.5 s). The overshoot is kept instead, and an action whose interval is shorter than the frame may
    /// repeat a few times in one frame. Pure, so it is tested without a scene.
    /// </summary>
    public static class Cadence
    {
        /// <summary>Most repeats of one action in a single frame, so a long hitch never releases a burst.</summary>
        public const int MaxPerFrame = 4;

        /// <summary>True while the action is due again this frame (cooldown run out, fewer than the cap done).</summary>
        public static bool Due(float cooldown, int doneThisFrame) => cooldown <= 0f && doneThisFrame < MaxPerFrame;

        /// <summary>The cooldown after the action fired: the interval less what the frame overshot.</summary>
        public static float Rearm(float cooldown, float interval) => cooldown + interval;

        /// <summary>With nothing to act on (or the cap reached) no credit is banked for later.</summary>
        public static float Idle(float cooldown) => cooldown < 0f ? 0f : cooldown;
    }
}
