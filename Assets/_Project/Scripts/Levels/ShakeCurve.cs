using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The feel of a camera kick, shared by the level camera and the cutscene camera: a smooth noise wobble in the
    /// screen plane whose strength dies out on its own. Pure, so both cameras shake the same way from one source.
    /// </summary>
    public static class ShakeCurve
    {
        public const float Amplitude = 0.18f;
        // Fast enough that even a full-strength kick is over in about half a second: a kick, never a lasting shake.
        private const float Decay = 8f;

        /// <summary>The cutscene camera's kicks die out slower, as they did before the level's kick was shortened (the
        /// touchdown should still be felt); its feel data carries it.</summary>
        public const float CutsceneDecay = 5.5f;
        private const float Frequency = 26f;

        /// <summary>Below this the shake counts as over.</summary>
        public const float Floor = 0.01f;

        /// <summary>The strength left after <paramref name="deltaTime"/> seconds.</summary>
        public static float Fade(float strength, float deltaTime) => Fade(strength, deltaTime, Decay);

        /// <summary>The strength left after <paramref name="deltaTime"/> seconds at a decay of its own.</summary>
        public static float Fade(float strength, float deltaTime, float decay) => strength * Mathf.Exp(-decay * deltaTime);

        /// <summary>The offset along the camera's right (x) and up (y) axes, in world units at strength one.</summary>
        public static Vector2 Offset(float time, float seed)
        {
            float t = time * Frequency;
            float x = Mathf.PerlinNoise(t, seed) * 2f - 1f;
            float y = Mathf.PerlinNoise(seed, t) * 2f - 1f;
            return new Vector2(x, y) * Amplitude;
        }
    }
}
