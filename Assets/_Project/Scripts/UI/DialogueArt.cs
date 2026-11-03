using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// The textures the dialogue box is drawn with, painted in code once and shared: a rounded panel and its glowing
    /// edge (both nine-sliced on the same geometry, so the light sits exactly on the panel's border), scanlines, a
    /// noise strip for redacted words, a soft radial glow, a bottom-up shade and the "next" arrow. All white, tinted
    /// by the view.
    /// </summary>
    public static class DialogueArt
    {
        private const int SliceSize = 64;
        private const float SliceInset = 18f;
        private const float CornerRadius = 10f;
        private const float EdgeWidth = 1.6f;
        private const float GlowReach = 16f;
        private const float InnerGlowReach = 6f;
        private const float InnerGlowStrength = 0.35f;
        private const int SliceBorder = 30;

        private const int ScanlineHeight = 4;
        private const int NoiseWidth = 128;
        private const int NoiseHeight = 16;
        private const int RadialSize = 64;
        private const int ShadeHeight = 64;
        private const int ArrowSize = 32;
        private const float ArrowMargin = 6f;

        private static Sprite _panel;
        private static Sprite _edge;
        private static Texture2D _scanlines;
        private static Texture2D _noise;
        private static Texture2D _radial;
        private static Texture2D _shade;
        private static Sprite _arrow;

        /// <summary>How far outside the panel the glow sprite reaches; the edge image is offset by this much.</summary>
        public static float EdgeOverhang => SliceInset;

        /// <summary>A filled rounded rectangle.</summary>
        public static Sprite Panel => _panel != null ? _panel : _panel = Slice(PanelAlpha, "DialoguePanel");

        /// <summary>A thin bright line on the panel's border with a soft glow outside and a faint one inside.</summary>
        public static Sprite Edge => _edge != null ? _edge : _edge = Slice(EdgeAlpha, "DialogueEdge");

        /// <summary>One dark line every four pixels; tile it vertically with a RawImage uvRect.</summary>
        public static Texture2D Scanlines
        {
            get
            {
                if (_scanlines == null)
                {
                    _scanlines = NewTexture(1, ScanlineHeight, TextureWrapMode.Repeat, FilterMode.Point);
                    _scanlines.SetPixels(new[] { new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0.55f), new Color(1f, 1f, 1f, 0.25f) });
                    _scanlines.Apply();
                }

                return _scanlines;
            }
        }

        /// <summary>Dark grey speckle for the redacted bars; shift its uvRect to make it crawl.</summary>
        public static Texture2D Noise
        {
            get
            {
                if (_noise == null)
                {
                    _noise = NewTexture(NoiseWidth, NoiseHeight, TextureWrapMode.Repeat, FilterMode.Point);
                    var random = new System.Random(31);
                    var pixels = new Color[NoiseWidth * NoiseHeight];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        float value = 0.0f + (float)random.NextDouble() * 0.16f;
                        pixels[i] = new Color(value, value, value * 1.15f, 1f);
                    }

                    _noise.SetPixels(pixels);
                    _noise.Apply();
                }

                return _noise;
            }
        }

        /// <summary>A round glow, bright in the middle and gone at the edge.</summary>
        public static Texture2D Radial
        {
            get
            {
                if (_radial == null)
                {
                    _radial = NewTexture(RadialSize, RadialSize, TextureWrapMode.Clamp, FilterMode.Bilinear);
                    var pixels = new Color[RadialSize * RadialSize];
                    float half = RadialSize * 0.5f;
                    for (int y = 0; y < RadialSize; y++)
                    {
                        for (int x = 0; x < RadialSize; x++)
                        {
                            float distance = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                            float alpha = Mathf.Clamp01(1f - distance);
                            pixels[y * RadialSize + x] = new Color(1f, 1f, 1f, alpha * alpha);
                        }
                    }

                    _radial.SetPixels(pixels);
                    _radial.Apply();
                }

                return _radial;
            }
        }

        /// <summary>Opaque at the bottom, clear at the top: darkens the field under the box without hiding it.</summary>
        public static Texture2D Shade
        {
            get
            {
                if (_shade == null)
                {
                    _shade = NewTexture(1, ShadeHeight, TextureWrapMode.Clamp, FilterMode.Bilinear);
                    var pixels = new Color[ShadeHeight];
                    for (int y = 0; y < ShadeHeight; y++)
                    {
                        float t = 1f - y / (ShadeHeight - 1f);
                        pixels[y] = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, t));
                    }

                    _shade.SetPixels(pixels);
                    _shade.Apply();
                }

                return _shade;
            }
        }

        /// <summary>A small solid triangle pointing down.</summary>
        public static Sprite Arrow
        {
            get
            {
                if (_arrow == null)
                {
                    Texture2D texture = NewTexture(ArrowSize, ArrowSize, TextureWrapMode.Clamp, FilterMode.Bilinear);
                    var pixels = new Color[ArrowSize * ArrowSize];
                    float half = ArrowSize * 0.5f;
                    for (int y = 0; y < ArrowSize; y++)
                    {
                        for (int x = 0; x < ArrowSize; x++)
                        {
                            // Apex at the bottom middle, flat side at the top; one pixel of soft edge.
                            float py = y + 0.5f;
                            float t = (py - ArrowMargin) / (ArrowSize - 2f * ArrowMargin);
                            float halfWidth = t * (half - ArrowMargin);
                            float inside = Mathf.Min(halfWidth - Mathf.Abs(x + 0.5f - half), Mathf.Min(ArrowSize - ArrowMargin - py, py - ArrowMargin));
                            pixels[y * ArrowSize + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(inside));
                        }
                    }

                    texture.SetPixels(pixels);
                    texture.Apply();
                    _arrow = Sprite.Create(texture, new Rect(0, 0, ArrowSize, ArrowSize), new Vector2(0.5f, 0.5f), ArrowSize);
                    _arrow.name = "DialogueArrow";
                }

                return _arrow;
            }
        }

        private delegate float AlphaAt(float signedDistance);

        // Signed distance to the rounded rectangle inset from the texture edge: negative inside.
        private static float RoundedDistance(float x, float y)
        {
            float half = SliceSize * 0.5f;
            float extent = half - SliceInset - CornerRadius;
            float dx = Mathf.Max(Mathf.Abs(x - half) - extent, 0f);
            float dy = Mathf.Max(Mathf.Abs(y - half) - extent, 0f);
            float outside = new Vector2(dx, dy).magnitude;
            float inside = Mathf.Min(Mathf.Max(Mathf.Abs(x - half) - extent, Mathf.Abs(y - half) - extent), 0f);
            return outside + inside - CornerRadius;
        }

        private static float PanelAlpha(float distance) => Mathf.Clamp01(0.5f - distance);

        private static float EdgeAlpha(float distance)
        {
            float line = Mathf.Clamp01(EdgeWidth - Mathf.Abs(distance));
            float outer = distance > 0f ? Mathf.Pow(Mathf.Clamp01(1f - distance / GlowReach), 2.2f) * 0.6f : 0f;
            float inner = distance < 0f ? Mathf.Pow(Mathf.Clamp01(1f + distance / InnerGlowReach), 2f) * InnerGlowStrength : 0f;
            return Mathf.Clamp01(Mathf.Max(line, outer + inner));
        }

        private static Sprite Slice(AlphaAt alphaAt, string name)
        {
            Texture2D texture = NewTexture(SliceSize, SliceSize, TextureWrapMode.Clamp, FilterMode.Bilinear);
            var pixels = new Color[SliceSize * SliceSize];
            for (int y = 0; y < SliceSize; y++)
            {
                for (int x = 0; x < SliceSize; x++)
                {
                    pixels[y * SliceSize + x] = new Color(1f, 1f, 1f, alphaAt(RoundedDistance(x + 0.5f, y + 0.5f)));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            var border = new Vector4(SliceBorder, SliceBorder, SliceBorder, SliceBorder);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, SliceSize, SliceSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }

        private static Texture2D NewTexture(int width, int height, TextureWrapMode wrap, FilterMode filter)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = filter, hideFlags = HideFlags.DontSave };
        }
    }
}
