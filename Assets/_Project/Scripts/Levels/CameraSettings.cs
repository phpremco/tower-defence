using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Settings for the fixed orthographic camera of a level.</summary>
    [Serializable]
    public struct CameraSettings
    {
        [Range(10f, 80f)] public float PitchDegrees;
        [Range(-180f, 180f)] public float YawDegrees;
        [Min(1f)] public float OrthographicSize;

        public static CameraSettings Default => new CameraSettings
        {
            PitchDegrees = 55f,
            YawDegrees = 0f,
            OrthographicSize = 7f,
        };
    }
}
