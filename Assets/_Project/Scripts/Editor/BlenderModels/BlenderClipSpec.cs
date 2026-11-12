using Game.Core;

namespace Game.Editor
{
    /// <summary>A clip of one Blender model that needs more than its name says: an event frame, a power, a socket.</summary>
    public readonly struct BlenderClipSpec
    {
        public BlenderClipSpec(string clip, ModelClipRole role, int eventFrame = 0, string trigger = "", string socket = "", float weight = 1f, int sustainFrom = 0, int sustainTo = 0, string socketPoint = "")
        {
            SocketPoint = socketPoint;
            SustainFrom = sustainFrom;
            SustainTo = sustainTo;
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

        /// <summary>
        /// The report key of a point (Blender metres, Z up, rest pose) where the moment really leaves, off the socket bone (the
        /// Sealed One's throat ahead of its head bone), or empty for the bone itself.
        /// </summary>
        public string SocketPoint { get; }
        public float Weight { get; }

        /// <summary>For a lasting power: the frames (at 30 fps) that loop while it goes on, or zero for a hold on the event frame.</summary>
        public int SustainFrom { get; }
        public int SustainTo { get; }
    }
}
