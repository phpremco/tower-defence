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
            public Facts(Vector2 endOffset, float anchor, float cueFrame)
            {
                EndOffset = endOffset;
                Anchor = anchor;
                CueFrame = cueFrame;
            }

            /// <summary>Model units forward (x) and up (y) the body has moved by the last frame.</summary>
            public Vector2 EndOffset { get; }

            /// <summary>Model units ahead of the pivot where the clip meets the world.</summary>
            public float Anchor { get; }

            /// <summary>Frame of the burst before the main moment (a throw), or zero.</summary>
            public float CueFrame { get; }
        }

        private static readonly string[] AnchorKeys = { "hole_forward_m", "bank_forward_m", "exit_forward_m", "edge_forward_m" };

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
                float cue = BlenderJson.Field(fields, "throw_frame") != null ? BlenderJson.Number(BlenderJson.Field(fields, "burst_frame")) : 0f;
                facts[clip.Key] = new Facts(end, anchor, cue);
            }

            return facts;
        }
    }
}
