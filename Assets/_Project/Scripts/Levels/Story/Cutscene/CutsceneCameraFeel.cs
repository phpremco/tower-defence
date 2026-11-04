using System;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// How the cutscene camera moves beyond its keys, as data: a slow drift like a soft camera mount (zero holds it
    /// still) and how hard an impact cue may kick it. Remco wants no sustained shake, so the kick is capped small and
    /// the drift stays far below one cycle per second.
    /// </summary>
    [Serializable]
    public struct CutsceneCameraFeel
    {
        /// <summary>Drift in world units per square root of the distance to the look-at point; zero turns it off.</summary>
        [Min(0f)] public float Drift;

        /// <summary>Cycles of the drift per second.</summary>
        [Range(0f, 0.5f)] public float DriftHertz;

        /// <summary>World units of a kick at strength one.</summary>
        [Range(0f, 2f)] public float KickScale;

        /// <summary>The strongest kick a cue may give, 0..1.</summary>
        [Range(0f, 1f)] public float MaxKick;

        /// <summary>The agreed feel: a barely felt drift and one small kick at most.</summary>
        public static CutsceneCameraFeel Calm => new CutsceneCameraFeel { Drift = 0.006f, DriftHertz = 0.07f, KickScale = 0.9f, MaxKick = 0.3f };
    }
}
