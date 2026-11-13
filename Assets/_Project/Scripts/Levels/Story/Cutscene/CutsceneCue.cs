using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Something that happens once at a moment of a shot: a sound or a camera kick. Plain fields read by kind.</summary>
    [Serializable]
    public struct CutsceneCue
    {
        /// <summary>Seconds into the shot.</summary>
        [Min(0f)] public float At;

        public CueKind Kind;

        /// <summary>Sound: which sound of the library.</summary>
        public CutsceneSound Sound;

        /// <summary>Sound: an optional clip that replaces the library sound.</summary>
        public AudioClip Clip;

        /// <summary>Sound: volume. Shake: how hard, one is the hardest hit.</summary>
        [Range(0f, 1f)] public float Strength;

        /// <summary>
        /// Sound: seconds the sound takes to swell from silence to its volume (round 59: the salvo's quieter launches start soft);
        /// zero starts it at full volume.
        /// </summary>
        [Min(0f)] public float FadeIn;
    }
}
