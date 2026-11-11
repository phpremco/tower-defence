using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A sound held over part of a shot (a bed, an engine, a breath): it fades in from <see cref="From"/> and out towards
    /// <see cref="To"/>. Its level is a function of the shot clock alone, so a seek lands on the same mix; a layer the next
    /// shot lists again carries on without a restart, any other one fades out.
    /// </summary>
    [Serializable]
    public struct CutsceneLayer
    {
        private const float Tolerance = 0.001f;

        public CutsceneSound Sound;

        /// <summary>Seconds into the shot when it starts to fade in.</summary>
        [Min(0f)] public float From;

        /// <summary>Seconds into the shot when it has faded out; zero holds it to the end of the shot.</summary>
        [Min(0f)] public float To;

        /// <summary>Its level before the slot's gain and the player's mix.</summary>
        [Range(0f, 1f)] public float Volume;

        [Min(0f)] public float FadeIn;
        [Min(0f)] public float FadeOut;

        public bool HoldsToEnd => To <= 0f;

        /// <summary>The share of <see cref="Volume"/> at a moment of the shot.</summary>
        public float Envelope(float time)
        {
            if (time < From || (!HoldsToEnd && time >= To))
            {
                return 0f;
            }

            float rise = FadeIn > 0f ? Mathf.Clamp01((time - From) / FadeIn) : 1f;
            float fall = !HoldsToEnd && FadeOut > 0f ? Mathf.Clamp01((To - time) / FadeOut) : 1f;
            return Mathf.Min(rise, fall);
        }

        /// <summary>Adds what is wrong with the layer to the list.</summary>
        public void Validate(int shotNumber, List<string> errors)
        {
            if (Sound == CutsceneSound.None)
            {
                errors.Add($"shot {shotNumber}: a sound layer without a sound.");
            }

            if (!HoldsToEnd && To <= From)
            {
                errors.Add($"shot {shotNumber}: the {Sound} layer ends before it starts.");
            }
            else if (!HoldsToEnd && FadeIn + FadeOut > To - From + Tolerance)
            {
                errors.Add($"shot {shotNumber}: the {Sound} layer's fades are longer than the layer.");
            }
        }
    }
}
