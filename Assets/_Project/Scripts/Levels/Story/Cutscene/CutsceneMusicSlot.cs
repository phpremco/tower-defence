using System;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>The track a cutscene plays for a music mood, and how loud; an empty track keeps the mood silent.</summary>
    [Serializable]
    public struct CutsceneMusicSlot
    {
        public CutsceneMusic Mood;
        public SoundSet Track;
        [Range(0f, 1f)] public float Volume;

        public bool IsEmpty => Track == null || Track.IsEmpty;
    }
}
