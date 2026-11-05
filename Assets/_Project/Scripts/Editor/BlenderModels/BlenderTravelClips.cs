using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// The facts of a boss's fixed-pivot mechanic clips from the <c>round28</c> block of its Blender report (ArtSource README,
    /// round 28): how far the body travels by the last frame, where the clip meets the world ahead of the pivot (the hole,
    /// the bank, the ledge face) and the earlier burst before a throw. Clips the block does not list have none.
    /// </summary>
    public static class BlenderTravelClips
    {
        public readonly struct Facts
        {
            public Facts(Vector2 endOffset, float anchor, float cueFrame, float distance, float eventFrame, float[] momentFrames = null, float[] burden = null)
            {
                MomentFrames = momentFrames ?? System.Array.Empty<float>();
                Burden = burden ?? System.Array.Empty<float>();
                EventFrame = eventFrame;
                EndOffset = endOffset;
                Anchor = anchor;
                CueFrame = cueFrame;
                Distance = distance;
            }

            /// <summary>Model units forward (x) and up (y) the body has moved by the last frame.</summary>
            public Vector2 EndOffset { get; }

            /// <summary>Model units ahead of the pivot where the clip meets the world.</summary>
            public float Anchor { get; }

            /// <summary>Frame of the burst before the main moment (a throw), or zero.</summary>
            public float CueFrame { get; }

            /// <summary>Model units one loop of an in-place cycle covers (a hunting stride), or zero.</summary>
            public float Distance { get; }

            /// <summary>Frame of the clip's main moment from the report (the impact, the cast, the last egg), or zero.</summary>
            public float EventFrame { get; }

            /// <summary>Frames of each heavy push (a gate crossing's <c>weight_frames</c>), or empty.</summary>
            public float[] MomentFrames { get; }

            /// <summary>The structure burden per frame (a gate crossing's <c>gate_burden</c>), or empty.</summary>
            public float[] Burden { get; }
        }

        private static readonly string[] AnchorKeys = { "hole_forward_m", "bank_forward_m", "exit_forward_m", "edge_forward_m", "target_forward_m", "gate_forward_m", "base_forward_m" };

        private static readonly string[] EventKeys = { "impact_frame", "cast_frame", "all_off_frame" };

        // The main moment the report names for a clip: the impact, the cast, the last plate off, or the last egg.
        private static float EventOf(Dictionary<string, object> fields)
        {
            foreach (string key in EventKeys)
            {
                object value = BlenderJson.Field(fields, key);
                if (value != null)
                {
                    return BlenderJson.Number(value);
                }
            }

            // A series of moments (eggs laid, pods fired): the last is the main one.
            List<object> series = Series(fields);
            return series.Count > 0 ? BlenderJson.Number(series[series.Count - 1]) : 0f;
        }

        private static readonly string[] SeriesKeys = { "egg_frames", "shot_frames", "weight_frames" };

        private static List<object> Series(Dictionary<string, object> fields)
        {
            foreach (string key in SeriesKeys)
            {
                List<object> values = BlenderJson.Array(BlenderJson.Field(fields, key));
                if (values.Count > 0)
                {
                    return values;
                }
            }

            return new List<object>();
        }

        private static float[] Numbers(object array)
        {
            List<object> values = BlenderJson.Array(array);
            var numbers = new float[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                numbers[i] = BlenderJson.Number(values[i]);
            }

            return numbers;
        }

        private static float FirstOf(object array)
        {
            List<object> values = BlenderJson.Array(array);
            return values.Count > 0 ? BlenderJson.Number(values[0]) : 0f;
        }

        public static Dictionary<string, Facts> Read(string reportPath)
        {
            var facts = new Dictionary<string, Facts>();
            if (!File.Exists(reportPath))
            {
                return facts;
            }

            Dictionary<string, object> root = BlenderJson.Object(BlenderJson.Parse(File.ReadAllText(reportPath)));
            Dictionary<string, object> clips = BlenderJson.Object(BlenderJson.Field(BlenderJson.Object(BlenderJson.Field(root, "round28")), "clips"));
            if (clips == null)
            {
                return facts;
            }

            foreach (KeyValuePair<string, object> clip in clips)
            {
                Dictionary<string, object> fields = BlenderJson.Object(clip.Value);
                var end = new Vector2(BlenderJson.Number(BlenderJson.Field(fields, "end_offset_forward_m")), BlenderJson.Number(BlenderJson.Field(fields, "end_offset_up_m")));
                float anchor = 0f;
                foreach (string key in AnchorKeys)
                {
                    object value = BlenderJson.Field(fields, key);
                    if (value != null)
                    {
                        anchor = BlenderJson.Number(value);
                        break;
                    }
                }

                // A throw has its burst first: that is the cue; every other clip has only its main moment.
                float cue = BlenderJson.Field(fields, "throw_frame") != null ? BlenderJson.Number(BlenderJson.Field(fields, "burst_frame")) : FirstOf(Series(fields));
                facts[clip.Key] = new Facts(end, anchor, cue, BlenderJson.Number(BlenderJson.Field(fields, "distance_per_loop_m")), EventOf(fields), Numbers(BlenderJson.Field(fields, "weight_frames")), Numbers(BlenderJson.Field(fields, "gate_burden")));
            }

            return facts;
        }
    }
}
