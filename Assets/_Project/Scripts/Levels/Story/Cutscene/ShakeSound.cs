using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Round 44 (Remco: "I see things shaking, but there is no rumbling noise"): the level of the quake rumble under a
    /// cutscene's shaking. Whatever shakes hardest this frame sets it: the camera's impact kick, the set's rumble of the lens
    /// or the ground and props trembling; the shot's <see cref="CutsceneShot.ShakeRumble"/> sets how loud it may get.
    /// </summary>
    public static class ShakeSound
    {
        /// <summary>A kick this strong (the strongest impact cue of the opening) gives the full rumble.</summary>
        public const float FullKick = 0.25f;

        /// <summary>The rumble's share 0..1 before the slot's gain and the mix.</summary>
        public static float Level(float kick, float rumble, float tremble, float shotVolume)
        {
            float shake = Mathf.Max(Mathf.Clamp01(kick / FullKick), Mathf.Max(Mathf.Clamp01(rumble), Mathf.Clamp01(tremble)));
            return shake * Mathf.Clamp01(shotVolume);
        }
    }
}
