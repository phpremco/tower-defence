using UnityEngine;

namespace Game.UI
{
    /// <summary>Small timing helpers for set choreography, so every set reads the same way: share of a window, eases.</summary>
    public static class CutsceneMath
    {
        private const float BackOvershoot = 1.70158f;

        /// <summary>How far <paramref name="value"/> has come through the window from <paramref name="from"/> to <paramref name="to"/>, 0..1.</summary>
        public static float Span(float value, float from, float to) => to > from ? Mathf.Clamp01((value - from) / (to - from)) : (value >= to ? 1f : 0f);

        public static float Smooth(float t) => t * t * (3f - 2f * t);

        /// <summary>Overshoots a little past one and settles back: a part snapping into place.</summary>
        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * ((BackOvershoot + 1f) * t + BackOvershoot);
        }

        /// <summary>
        /// The heading of someone turning from <paramref name="fromYaw"/> to <paramref name="toYaw"/> (degrees), <paramref name="since"/>
        /// seconds after the turn began, the short way round at <paramref name="degreesPerSecond"/> on average, eased in and out,
        /// so a turn of any size takes its natural time and never snaps (round 39).
        /// </summary>
        public static float EasedTurn(float fromYaw, float toYaw, float since, float degreesPerSecond)
        {
            float delta = Mathf.DeltaAngle(fromYaw, toYaw);
            float seconds = TurnSeconds(delta, degreesPerSecond);
            return fromYaw + delta * Smooth(seconds > 0f ? Mathf.Clamp01(since / seconds) : 1f);
        }

        /// <summary>How long a turn of <paramref name="degrees"/> takes at <paramref name="degreesPerSecond"/> on average.</summary>
        public static float TurnSeconds(float degrees, float degreesPerSecond) => degreesPerSecond > 0f ? Mathf.Abs(degrees) / degreesPerSecond : 0f;

        /// <summary>
        /// Round 48: the share of a cross-fade <paramref name="since"/> seconds after it began, eased in and out over
        /// <paramref name="seconds"/>; 1 once it is over, or at once when it has no length.
        /// </summary>
        public static float Fade(float since, float seconds) => seconds > 0f ? Smooth(Mathf.Clamp01(since / seconds)) : 1f;

        /// <summary>
        /// Round 48: the moment of sample <paramref name="index"/> (0 to <paramref name="count"/>, both included) of a window of
        /// <paramref name="width"/> centred on <paramref name="centre"/>, for a heading or a clip weight averaged over the moments
        /// around now. The samples sit on a fixed grid (width / count apart) rather than moving with the moment, and
        /// <see cref="WindowWeight"/> fades them in and out at the window's edges, so the average changes smoothly from frame to
        /// frame: a step in the raw value becomes a ramp, worked out from the moment alone (a seek lands on the same value).
        /// </summary>
        public static float WindowAt(int index, int count, float centre, float width)
        {
            if (width <= 0f || count <= 0)
            {
                return centre;
            }

            float step = width / count;
            return (Mathf.Ceil((centre - width * 0.5f) / step) + index) * step;
        }

        /// <summary>
        /// Round 48: the weight of a sample at <paramref name="at"/> in that window: a triangle peaking at the centre and zero at
        /// the edges. Divide the weighted sum by the sum of the weights. Without a width only the centre counts.
        /// </summary>
        public static float WindowWeight(float at, float centre, float width)
        {
            if (width <= 0f)
            {
                return Mathf.Approximately(at, centre) ? 1f : 0f;
            }

            return Mathf.Max(0f, 1f - Mathf.Abs(at - centre) / (width * 0.5f));
        }

        /// <summary>A stable pseudo-random number in 0..1 for an index, so seeded scatter never changes between runs.</summary>
        public static float Hash(int index, int salt)
        {
            uint h = (uint)(index * 374761393 + salt * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }

        /// <summary>
        /// A jolt that hits fast and dies slowly (round 44: a flinch): 0 before <paramref name="since"/> reaches zero, eased up to 1
        /// over <paramref name="attack"/> seconds, then falling off with the time constant <paramref name="decay"/>.
        /// </summary>
        public static float Jolt(float since, float attack, float decay)
        {
            if (since <= 0f)
            {
                return 0f;
            }

            return since < attack ? Smooth(since / attack) : Mathf.Exp(-(since - attack) / Mathf.Max(decay, 0.0001f));
        }

        /// <summary>
        /// Round 57: how far a rotor has turned <paramref name="since"/> seconds after it starts, in seconds at full speed: its
        /// speed rises evenly from rest to full over <paramref name="ramp"/> seconds, so the turn eases in instead of jumping to
        /// full spin on the first frame. 0 before the start; after the ramp it runs on at full speed.
        /// </summary>
        public static float SpinUp(float since, float ramp)
        {
            if (since <= 0f)
            {
                return 0f;
            }

            if (ramp <= 0f || since >= ramp)
            {
                return since - Mathf.Max(0f, ramp) * 0.5f;
            }

            return since * since / (2f * ramp);
        }
    }
}
