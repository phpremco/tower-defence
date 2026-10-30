using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One line of the story and the scene it is spoken over.</summary>
    [Serializable]
    public struct StoryBeat
    {
        [TextArea(2, 4)] public string Text;
        public StoryScene Scene;

        /// <summary>How long the beat stays on screen, in seconds.</summary>
        [Min(1f)] public float Seconds;
    }
}
