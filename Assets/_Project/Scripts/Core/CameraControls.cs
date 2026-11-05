using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The player's camera preferences: how fast the view pans (a factor on the default speed) and whether a mouse at
    /// the screen edge pans at all, and the HUD radar (on or off, and its size). Set from the options, read by the level
    /// camera and the HUD, kept in the progress file.
    /// </summary>
    public sealed class CameraControls
    {
        public const float MinSpeedScale = 0.5f;
        public const float MaxSpeedScale = 2.5f;
        public const float MinRadarScale = 0.7f;
        public const float MaxRadarScale = 1.5f;

        public float SpeedScale { get; private set; } = 1f;

        public bool EdgeScrolling { get; private set; } = true;

        /// <summary>Whether the HUD shows the radar of the level.</summary>
        public bool RadarOn { get; private set; } = true;

        /// <summary>The radar's size as a factor on its default size.</summary>
        public float RadarScale { get; private set; } = 1f;

        public event Action Changed;

        public void Set(float speedScale, bool edgeScrolling)
        {
            SpeedScale = Mathf.Clamp(speedScale, MinSpeedScale, MaxSpeedScale);
            EdgeScrolling = edgeScrolling;
            Changed?.Invoke();
        }

        public void SetRadar(bool on, float scale)
        {
            RadarOn = on;
            RadarScale = Mathf.Clamp(scale, MinRadarScale, MaxRadarScale);
            Changed?.Invoke();
        }
    }
}
