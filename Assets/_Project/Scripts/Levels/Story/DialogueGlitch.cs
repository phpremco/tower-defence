using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// Seeded noise for glitching lines: which characters swap for a junk glyph and how far the box jitters on a
    /// given tick. Deterministic for a seed, index and tick, so a view can redraw without storing state and tests can
    /// pin the behaviour. The view chooses the tick rate and the pixel scale.
    /// </summary>
    public static class DialogueGlitch
    {
        private const string Glyphs = "#%&@$*!?/<>[]{}=+~";

        // Even a full-strength glitch keeps most of the line readable.
        private const float MaxScrambleShare = 0.35f;
        private const float HashToUnit = 1f / 16777216f;
        private const int UnitMask = 0xFFFFFF;

        /// <summary>True when character <paramref name="index"/> shows a junk glyph on this tick.</summary>
        public static bool IsScrambled(float intensity, int seed, int index, int tick)
        {
            return intensity > 0f && Unit(seed, index, tick) < Mathf.Clamp01(intensity) * MaxScrambleShare;
        }

        /// <summary>The junk glyph for this character on this tick.</summary>
        public static char Glyph(int seed, int index, int tick)
        {
            return Glyphs[(int)(Hash(seed, index ^ 0x5bd1e995, tick) % (uint)Glyphs.Length)];
        }

        /// <summary>Offset of the box on this tick, each axis within [-intensity, intensity]; the view scales it to pixels.</summary>
        public static Vector2 Jitter(float intensity, int seed, int tick)
        {
            float strength = Mathf.Clamp01(intensity);
            if (strength <= 0f)
            {
                return Vector2.zero;
            }

            float x = Unit(seed, -1, tick) * 2f - 1f;
            float y = Unit(seed, -2, tick) * 2f - 1f;
            return new Vector2(x, y) * strength;
        }

        private static float Unit(int seed, int index, int tick) => (Hash(seed, index, tick) & UnitMask) * HashToUnit;

        // A small integer mix (in the spirit of a PCG/xxHash finaliser): cheap, allocation-free and well spread.
        private static uint Hash(int seed, int index, int tick)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)index * 0x85EBCA77u;
                h = (h << 13) | (h >> 19);
                h ^= (uint)tick * 0xC2B2AE3Du;
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return h;
            }
        }
    }
}
