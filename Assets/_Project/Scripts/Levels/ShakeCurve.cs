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
        private const float Frequency = 26f;

        /// <summary>Below this the shake counts as over.</summary>
        public const float Floor = 0.01f;

        /// <summary>The strength left after <paramref name="deltaTime"/> seconds.</summary>
        public static float Fade(float strength, float deltaTime) => strength * Mathf.Exp(-Decay * deltaTime);

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
