using Game.Core;

namespace Game.Editor
{
    /// <summary>A clip of one Blender model that needs more than its name says: an event frame, a power, a socket.</summary>
    public readonly struct BlenderClipSpec
    {
        public BlenderClipSpec(string clip, ModelClipRole role, int eventFrame = 0, string trigger = "", string socket = "", float weight = 1f)
        {
            Clip = clip;
            Role = role;
            EventFrame = eventFrame;
            Trigger = trigger;
            Socket = socket;
            Weight = weight;
        }

        public string Clip { get; }
        public ModelClipRole Role { get; }

        /// <summary>The frame (at the export's 30 fps) of the clip's moment: the hit, the release, the shield up.</summary>
        public int EventFrame { get; }

        /// <summary>For a power: the power kind it shows, by its enum name.</summary>
        public string Trigger { get; }
        public string Socket { get; }
        public float Weight { get; }
    }
}
