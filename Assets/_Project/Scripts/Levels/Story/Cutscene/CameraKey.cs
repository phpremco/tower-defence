using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// One pose of the cutscene camera at a moment of its shot. Position and look-at point are written relative to
    /// the key's <see cref="Anchor"/>; the camera runs a smooth curve through the positions and eases between keys.
    /// </summary>
    [Serializable]
    public struct CameraKey
    {
        public const float DefaultFieldOfView = 40f;

        /// <summary>When in the shot, from 0 (start) to 1 (the end of the camera move).</summary>
        [Range(0f, 1f)] public float At;

        public CameraAnchor Anchor;
        public Vector3 Position;
        public Vector3 LookAt;
        [Range(1f, 120f)] public float FieldOfView;

        /// <summary>How the move from the previous key into this one speeds up and slows down.</summary>
        public CameraEase Ease;

        public CameraKeyMode Mode;
    }
}
