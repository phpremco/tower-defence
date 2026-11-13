using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Round 44 (Remco: "I see things shaking, but there is no rumbling noise"): the level of the quake rumble under a
    /// cutscene's shaking. Whatever shakes hardest this frame sets it: the camera's impact kick, the set's rumble of the lens
    /// or the ground and props trembling; the shot's <see cref="CutsceneShot.ShakeRumble"/> sets how loud it may get.
    /// Round 60 (Remco: the rumble must be present for as long as the quake lasts): a quake shot also holds the rumble at its
    /// <see cref="CutsceneShot.QuakeHold"/>, so the trembling's flicker never dips it, and the hold glides across cuts.
    /// </summary>
    public static class ShakeSound
    {
        /// <summary>A kick this strong (the strongest impact cue of the opening) gives the full rumble.</summary>
        public const float FullKick = 0.25f;

        /// <summary>How long the hold takes to glide from the previous shot's level to this shot's, eased at both ends.</summary>
        public const float HoldGlideSeconds = 2f;

        /// <summary>The rumble's share 0..1 before the slot's gain and the mix.</summary>
        public static float Level(float kick, float rumble, float tremble, float shotVolume)
        {
            float shake = Mathf.Max(Mathf.Clamp01(kick / FullKick), Mathf.Max(Mathf.Clamp01(rumble), Mathf.Clamp01(tremble)));
            return shake * Mathf.Clamp01(shotVolume);
        }

        /// <summary>The share the quake holds <paramref name="time"/> seconds into a shot: from the previous shot's hold to this one's.</summary>
        public static float Hold(float previous, float current, float time)
        {
            float t = Mathf.Clamp01(time / HoldGlideSeconds);
            return Mathf.Lerp(Mathf.Clamp01(previous), Mathf.Clamp01(current), t * t * (3f - 2f * t));
        }

        /// <summary>The rumble's share: the shake's level, never below the quake's hold.</summary>
        public static float Level(float kick, float rumble, float tremble, float shotVolume, float hold) => Mathf.Max(Level(kick, rumble, tremble, shotVolume), Mathf.Clamp01(hold));
    }
}
