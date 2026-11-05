using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The player's camera preferences: how fast the view pans (a factor on the default speed) and whether a mouse at
    /// the screen edge pans at all. Set from the options, read by the level camera, kept in the progress file.
    /// </summary>
    public sealed class CameraControls
    {
        public const float MinSpeedScale = 0.5f;
        public const float MaxSpeedScale = 2.5f;

        public float SpeedScale { get; private set; } = 1f;

        public bool EdgeScrolling { get; private set; } = true;

        public event Action Changed;

        public void Set(float speedScale, bool edgeScrolling)
        {
            SpeedScale = Mathf.Clamp(speedScale, MinSpeedScale, MaxSpeedScale);
            EdgeScrolling = edgeScrolling;
            Changed?.Invoke();
        }
    }
}
