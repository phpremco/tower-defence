using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>One thing one speaker says, made of styled segments.</summary>
    [Serializable]
    public struct DialogueLine
    {
        /// <summary>Stable key of the line, for a string table later. Unique within its dialogue.</summary>
        public string Id;

        public SpeakerDefinition Speaker;
        public DialogueSegment[] Segments;

        /// <summary>How strongly the line and the portrait glitch; zero is clean. Lets the AI's decay escalate over the campaign.</summary>
        [Range(0f, 1f)] public float GlitchIntensity;

        /// <summary>Number of characters the typewriter reveals: every segment counts, redacted ones as bars.</summary>
        public int CharacterCount
        {
            get
            {
                int count = 0;
                if (Segments != null)
                {
                    for (int i = 0; i < Segments.Length; i++)
                    {
                        count += Segments[i].Length;
                    }
                }

                return count;
            }
        }
    }
}
