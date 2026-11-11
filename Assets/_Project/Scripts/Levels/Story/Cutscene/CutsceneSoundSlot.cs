using System;
using Game.Core;

namespace Game.Levels
{
    /// <summary>
    /// One sound of a cutscene's own bank: its recorded variants (one picked at random per play), whether it loops, the
    /// mix group it follows and what plays while it has no file yet. Plain fields, filled by the generator.
    /// </summary>
    [Serializable]
    public struct CutsceneSoundSlot
    {
        public CutsceneSound Sound;
        public SoundSet Clips;

        /// <summary>A bed or a held sound: plays on a source of its own that loops (a one-shot held as a layer plays once).</summary>
        public bool Loop;

        public CutsceneSoundChannel Channel;

        /// <summary>The borrowed sound while the slot is empty; None stays silent.</summary>
        public CutsceneSound Fallback;

        public bool IsEmpty => Clips == null || Clips.IsEmpty;
    }
}
