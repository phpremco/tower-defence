using Game.Core;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>What just left a barrel: where from and what kind of shot, so audio and effects can react in character.</summary>
    public readonly struct ShotInfo
    {
        public ShotInfo(Vector3 position, float splashRadius, TargetLayers targets, bool slows, ProjectileStyle style)
        {
            Position = position;
            SplashRadius = splashRadius;
            Targets = targets;
            Slows = slows;
            Style = style;
        }

        public Vector3 Position { get; }

        /// <summary>Zero for a shot that hits one target.</summary>
        public float SplashRadius { get; }

        public TargetLayers Targets { get; }

        /// <summary>True when the shot chills its targets.</summary>
        public bool Slows { get; }

        public ProjectileStyle Style { get; }
    }
}
