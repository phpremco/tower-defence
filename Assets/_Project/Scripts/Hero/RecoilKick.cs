using UnityEngine;

namespace Game.Hero
{
    /// <summary>
    /// Round 57: the shape of one shot's kick, laid on top of a held aim: nothing on the shot frame itself (the barrel lies on
    /// the target as the bolt leaves), held mostly back while the shot's bolt is still lit (so the bolt never bends away from
    /// the barrel at the muzzle), up to its peak, then dying away. The curve and its slope are continuous, so several shots in
    /// a row add up into one smooth motion instead of snapping the pose back to the aim.
    /// </summary>
    public static class RecoilKick
    {
        // Past this many peak times a kick is spent (well under a thousandth of its peak); also keeps an unfired slot (infinite
        // age) at zero.
        private const float SpentPeaks = 10f;

        /// <summary>
        /// The kick <paramref name="age"/> seconds after a shot, 0 to about 1: <c>s² e^(2(1 - s))</c> with s = age / peak (0 at
        /// the shot with no slope, 1 at <paramref name="peakSeconds"/>, falling off smoothly after), times the square of a
        /// smooth ramp over <paramref name="holdSeconds"/> (the bolt's life), which keeps it small while the bolt is lit.
        /// 0 before the shot and once spent.
        /// </summary>
        public static float At(float age, float peakSeconds, float holdSeconds)
        {
            if (age <= 0f || peakSeconds <= 0f || age >= peakSeconds * SpentPeaks + holdSeconds)
            {
                return 0f;
            }

            float share = age / peakSeconds;
            float released = holdSeconds > 0f ? Smooth(Mathf.Clamp01(age / holdSeconds)) : 1f;
            return share * share * Mathf.Exp(2f * (1f - share)) * released * released;
        }

        /// <summary>Slots for the last <paramref name="count"/> shot times, none fired yet.</summary>
        public static float[] NoShots(int count)
        {
            var times = new float[count];
            for (int i = 0; i < count; i++)
            {
                times[i] = float.NegativeInfinity;
            }

            return times;
        }

        /// <summary>The summed kick of every shot fired at <paramref name="shotTimes"/>, at <paramref name="now"/>.</summary>
        public static float Sum(float[] shotTimes, float now, float peakSeconds, float holdSeconds)
        {
            float sum = 0f;
            foreach (float shot in shotTimes)
            {
                sum += At(now - shot, peakSeconds, holdSeconds);
            }

            return sum;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
