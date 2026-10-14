using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Settings for the fixed orthographic camera of a level. The camera zooms to fit the whole field on screen.</summary>
    [Serializable]
    public struct CameraSettings
    {
        [Range(10f, 80f)] public float PitchDegrees;
        [Range(-180f, 180f)] public float YawDegrees;

        /// <summary>Extra space around the field as a fraction of its size, so the field never touches the screen edge.</summary>
        [Range(0f, 0.3f)] public float FitMargin;

        public static CameraSettings Default => new CameraSettings
        {
            PitchDegrees = 55f,
            YawDegrees = 0f,
            FitMargin = 0.04f,
        };
    }
}
