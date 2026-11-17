using System;
using System.Collections.Generic;

namespace Game.Balance
{
    /// <summary>
    /// The clock of a wave's groups (round 79): when a group starts and ends on the script's clock, cutting a stream into
    /// clusters with breathers between them, and the seconds of a wave in which something is really coming. Pure.
    /// </summary>
    public static class WaveTimeline
    {
        /// <summary>Seconds after the wave's start the group's first creature appears, on the script's clock (no difficulty tempo).</summary>
        public static float Start(GroupSpec group) => group.Delay + group.Stagger;

        public static float End(GroupSpec group) => Start(group) + Math.Max(0, group.Count - 1) * group.Interval;

        /// <summary>The last spawn of the groups, zero for none.</summary>
        public static float Span(IReadOnlyList<GroupSpec> groups)
        {
            float span = 0f;
            foreach (GroupSpec group in groups)
            {
                span = Math.Max(span, End(group));
            }

            return span;
        }

        /// <summary>
        /// How many clusters a stream of <paramref name="span"/> seconds is cut into: beats of about
        /// <paramref name="beatSeconds"/>, none shorter than <paramref name="minShare"/> of one.
        /// </summary>
        public static int Beats(float span, float beatSeconds, float minShare, int maxBeats)
        {
            if (beatSeconds <= 0f || span <= 0f)
            {
                return 1;
            }

            int beats = Math.Max(1, Math.Min(maxBeats, (int)Math.Round(span / beatSeconds)));
            while (beats > 1 && span / beats < beatSeconds * minShare)
            {
                beats--;
            }

            return beats;
        }

        /// <summary>
        /// Cuts the groups into <paramref name="beats"/> clusters at equal moments of their span and leaves exactly
        /// <paramref name="pause"/> seconds of silence between the last creature of a cluster and the first of the next (a
        /// silence the groups already had there is not added to). Every creature keeps its place in its cluster; a group that
        /// runs across a cut is split. A cluster's groups are re-based on the cluster's start (a fixed delay; what lies inside
        /// the cluster shrinks with the difficulty's tempo, the breather does not). Returns the start of every cluster.
        /// </summary>
        public static float[] Spread(List<GroupSpec> groups, int beats, float pause)
        {
            float span = Span(groups);
            beats = Math.Max(1, beats);
            var starts = new float[beats];
            if (beats == 1 || span <= 0f)
            {
                return starts;
            }

            float length = span / beats;
            var beatOf = new List<int>(groups.Count + beats);
            var timeOf = new List<float>(groups.Count + beats);
            var cut = new List<GroupSpec>(groups.Count + beats);
            var first = new float[beats];
            var last = new float[beats];
            for (int b = 0; b < beats; b++)
            {
                first[b] = float.MaxValue;
                last[b] = float.MinValue;
            }

            foreach (GroupSpec group in groups)
            {
                int taken = 0;
                while (taken < group.Count)
                {
                    float time = Start(group) + taken * group.Interval;
                    int beat = Math.Min(beats - 1, (int)(time / length));
                    // The creatures of this group that still fall inside the beat (the last beat takes the rest).
                    int count = beat == beats - 1 || group.Interval <= 0f
                        ? group.Count - taken
                        : Math.Min(group.Count - taken, Math.Max(1, (int)Math.Ceiling(((beat + 1) * length - time) / group.Interval)));
                    GroupSpec piece = group;
                    piece.Count = count;
                    cut.Add(piece);
                    beatOf.Add(beat);
                    timeOf.Add(time);
                    first[beat] = Math.Min(first[beat], time);
                    last[beat] = Math.Max(last[beat], time + (count - 1) * group.Interval);
                    taken += count;
                }
            }

            // Each cluster starts a breather after the last creature of the one before; a beat nothing fell into takes no time.
            float clock = 0f;
            bool any = false;
            for (int b = 0; b < beats; b++)
            {
                if (first[b] == float.MaxValue)
                {
                    starts[b] = clock;
                    continue;
                }

                starts[b] = any ? clock + pause : first[b];
                clock = starts[b] + last[b] - first[b];
                any = true;
            }

            groups.Clear();
            for (int i = 0; i < cut.Count; i++)
            {
                GroupSpec piece = cut[i];
                piece.Delay = starts[beatOf[i]];
                piece.Stagger = Math.Max(0f, timeOf[i] - first[beatOf[i]]);
                groups.Add(piece);
            }

            return starts;
        }

        /// <summary>Moves every group later by <paramref name="seconds"/> (a fixed delay).</summary>
        public static void Shift(List<GroupSpec> groups, float seconds)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                GroupSpec group = groups[i];
                group.Delay += seconds;
                groups[i] = group;
            }
        }

        /// <summary>
        /// The seconds of the given spawn stretches (start, end) in which something is coming: their whole span less the
        /// silence of every pause beyond <paramref name="credit"/> seconds. The stretches are sorted in place.
        /// </summary>
        public static float ActiveSeconds(List<(float Start, float End)> stretches, float credit)
        {
            if (stretches.Count == 0)
            {
                return 0f;
            }

            stretches.Sort((a, b) => a.Start.CompareTo(b.Start));
            float reached = stretches[0].End;
            float idle = 0f;
            foreach ((float start, float end) in stretches)
            {
                idle += Math.Max(0f, start - reached - credit);
                reached = Math.Max(reached, end);
            }

            return Math.Max(0f, reached - idle);
        }
    }
}
