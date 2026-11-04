using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// A shot's own cinematography on top of its set's light: an exposure lift for the picture and a three-light rig the
    /// player turns with the camera (a key from the camera's upper left, a rim from behind the subject, a fill from the
    /// lower right). Zero intensities leave a light off, so a shot without lighting looks as its set lights it.
    /// </summary>
    [Serializable]
    public struct ShotLighting
    {
        [Tooltip("Post exposure in stops added for this shot.")]
        public float Exposure;
        [ColorUsage(false, true)] public Color KeyColour;
        [Min(0f)] public float KeyIntensity;
        [ColorUsage(false, true)] public Color RimColour;
        [Min(0f)] public float RimIntensity;
        [ColorUsage(false, true)] public Color FillColour;
        [Min(0f)] public float FillIntensity;

        [Tooltip("Hides the scattered decor between the camera and what it looks at (close shots of a character).")]
        public bool ClearView;

        public bool HasRig => KeyIntensity > 0f || RimIntensity > 0f || FillIntensity > 0f;
    }
}
