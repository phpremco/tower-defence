using UnityEngine;

namespace Game.Levels
{
    /// <summary>Small deterministic noise helpers for building the landscape: the same level always looks the same.</summary>
    public static class TerrainNoise
    {
        private const float Lacunarity = 2.03f;
        private const float Gain = 0.5f;

        /// <summary>Smooth fractal noise in 0..1.</summary>
        public static float Fbm(float x, float z, int octaves, float seed = 0f)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x + seed + i * 31.7f, z - seed * 0.7f + i * 17.3f) * amplitude;
                total += amplitude;
                x *= Lacunarity;
                z *= Lacunarity;
                amplitude *= Gain;
            }

            return sum / total;
        }

        /// <summary>Thin bright ridges in 0..1, used for veins and cracks.</summary>
        public static float Ridge(float x, float z, float seed = 0f) =>
            1f - Mathf.Abs(Mathf.PerlinNoise(x + seed, z - seed * 0.5f) * 2f - 1f);

        /// <summary>Stable pseudo-random number in 0..1 for an integer lattice point.</summary>
        public static float Hash(int x, int z, int salt = 0)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + z * 668265263 + salt * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xffffff) / (float)0x1000000;
            }
        }

        public static float SmoothStep(float edge0, float edge1, float value)
        {
            float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
