using UnityEngine;

namespace Game.Core
{
    /// <summary>Small generated textures shared by the effects: a soft ring, a ragged puff, a beam profile, a round glow and tileable noise.</summary>
    public static class SpriteKit
    {
        private const int Size = 128;
        private const float Tau = Mathf.PI * 2f;
        // Share of the half-width the beam's white core fills, and how quickly its glow falls off towards the edge.
        private const float BeamCoreWidth = 0.3f;
        private const float BeamGlowFalloff = 1.6f;
        // Share of the beam profile's length over which it fades in at its start and out at its end (round 51: no square ends).
        private const float BeamEndFade = 0.03f;
        // A flared beam's ends sit under glows: its profile starts at this strength and reaches full over a much shorter share,
        // so its cut end does not show through the glow and no dark gap opens between the glow and the beam.
        private const float FlaredEdgeStrength = 0.5f;
        private const float FlaredEndFade = 0.015f;
        // The flare's white-hot point and its glow as shares of the card's half size; the glow is gone before the card's edge.
        private const float GlowCoreRadius = 0.18f;
        private const float GlowRadius = 0.42f;
        private const float GlowEdgeStart = 0.7f;

        private static Texture2D _ring;
        private static Texture2D _puff;
        private static Texture2D _beam;
        private static Texture2D _flaredBeam;
        private static Texture2D _glow;
        private static Texture2D _noise;

        /// <summary>A soft ring with a transparent centre and edge, for ripples and shock waves seen from above.</summary>
        public static Texture2D Ring()
        {
            if (_ring != null)
            {
                return _ring;
            }

            const float radius = 0.42f;
            const float width = 0.05f;
            _ring = Make("Ring", TextureWrapMode.Clamp, (u, v) =>
            {
                float d = Mathf.Abs(Mathf.Sqrt(u * u + v * v) - radius);
                float a = Mathf.Clamp01(1f - d / width);
                return new Color(1f, 1f, 1f, a * a);
            });
            return _ring;
        }

        /// <summary>A ragged puff of vapour: a soft disc broken up by noise, so smoke and steam have body instead of a blur.</summary>
        public static Texture2D Puff()
        {
            if (_puff != null)
            {
                return _puff;
            }

            _puff = Make("Puff", TextureWrapMode.Clamp, (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v) * 2f;
                float x = (u + 0.5f) * Size;
                float y = (v + 0.5f) * Size;
                float noise = Mathf.PerlinNoise(x * 0.07f + 3.1f, y * 0.07f + 7.7f) * 0.7f + Mathf.PerlinNoise(x * 0.16f, y * 0.16f) * 0.3f;
                float edge = Mathf.Clamp01(1f - (r - 0.25f) / 0.7f);
                return new Color(1f, 1f, 1f, edge * edge * Mathf.Clamp01(noise * 1.6f - 0.2f));
            });
            return _puff;
        }

        /// <summary>
        /// The cross-section of an energy beam for lines and trails (U along the line, V across it): a narrow white-hot core
        /// in red, a wider soft glow in green and the same glow as coverage, so a line fades out at its edges instead of
        /// ending in a hard flat strip. Along the line (U) the whole profile fades in over the first and out over the last
        /// few hundredths, so a line stretched once never starts or ends with a square edge.
        /// </summary>
        public static Texture2D Beam() => _beam != null ? _beam : _beam = BeamProfile("Beam", true);

        /// <summary>
        /// The same cross-section for a beam whose ends sit under glows (<see cref="LineKit.UseFlaredBeam"/>): it starts at half
        /// strength and is full after a very short stretch, as a full fade would open a dark gap between the glow and the beam.
        /// </summary>
        public static Texture2D FlaredBeam() => _flaredBeam != null ? _flaredBeam : _flaredBeam = BeamProfile("FlaredBeam", false);

        private static Texture2D BeamProfile(string name, bool fadeEnds)
        {
            return Make(name, TextureWrapMode.Clamp, (u, v) =>
            {
                float across = Mathf.Abs(v) * 2f;
                float core = Mathf.Exp(-(across * across) / (BeamCoreWidth * BeamCoreWidth));
                float along = u + 0.5f;
                float floor = fadeEnds ? 0f : FlaredEdgeStrength;
                float fade = fadeEnds ? BeamEndFade : FlaredEndFade;
                float ends = Mathf.Lerp(floor, 1f, Mathf.SmoothStep(0f, 1f, along / fade) * Mathf.SmoothStep(0f, 1f, (1f - along) / fade));
                float glow = Mathf.Pow(Mathf.Clamp01(1f - across), BeamGlowFalloff) * ends;
                return new Color(core * ends, glow, 0f, glow);
            });
        }

        /// <summary>
        /// A round glow for the source and impact of a beam (<see cref="LineKit.Flare"/>): a white-hot point in red, a wider
        /// soft glow in green and as coverage. Both are Gaussian and the glow is eased to nothing before the card's edge, so
        /// the card never shows a rim or a disc.
        /// </summary>
        public static Texture2D Glow()
        {
            if (_glow != null)
            {
                return _glow;
            }

            _glow = Make("Glow", TextureWrapMode.Clamp, (u, v) =>
            {
                float r = Mathf.Sqrt(u * u + v * v) * 2f;
                float core = Mathf.Exp(-(r * r) / (GlowCoreRadius * GlowCoreRadius));
                float edge = 1f - Mathf.SmoothStep(0f, 1f, (r - GlowEdgeStart) / (1f - GlowEdgeStart));
                float glow = Mathf.Exp(-(r * r) / (GlowRadius * GlowRadius)) * edge;
                return new Color(core * edge, glow, 0f, glow);
            });
            return _glow;
        }

        /// <summary>Seamless cloudy noise from summed sines, for shaders that churn a surface.</summary>
        public static Texture2D Noise()
        {
            if (_noise != null)
            {
                return _noise;
            }

            _noise = Make("Noise", TextureWrapMode.Repeat, (u, v) =>
            {
                float w = Mathf.Sin((3f * u + 2f * v) * Tau) + Mathf.Sin((1f * u - 4f * v) * Tau + 1.7f) * 0.8f
                          + Mathf.Sin((5f * u + 1f * v) * Tau + 0.6f) * 0.5f + Mathf.Sin((2f * u + 6f * v) * Tau + 2.9f) * 0.4f;
                float value = Mathf.Clamp01(0.5f + w * 0.18f);
                return new Color(value, value, value, 1f);
            });
            return _noise;
        }

        // Shader samples u and v centred on zero, from -0.5 to 0.5.
        private static Texture2D Make(string name, TextureWrapMode wrap, System.Func<float, float, Color> shade)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = shade((x + 0.5f) / Size - 0.5f, (y + 0.5f) / Size - 0.5f);
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true, false) { name = name, wrapMode = wrap };
            texture.SetPixels32(pixels);
            texture.Apply(true);
            return texture;
        }
    }
}
