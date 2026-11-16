using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Draws button glyphs into pixels: discs, pills, key caps, sticks, the d-pad and a mouse, with letters from the
    /// <see cref="StrokeFont"/> and the PlayStation shapes as strokes. Every edge is a signed distance turned into
    /// coverage, so the glyphs stay smooth at any size the UI scales them to. Pure apart from the texture wrapper.
    /// </summary>
    public static class GlyphPainter
    {
        /// <summary>The pixel height glyph textures are drawn at; the UI scales them down to the prompt size.</summary>
        public const int DefaultHeight = 80;

        // Proportions of the glyph height.
        private const float Pad = 0.04f;
        private const float RimInset = 0.035f;
        private const float RimWidth = 0.035f;
        private const float RoundText = 0.42f;
        private const float SmallText = 0.3f;
        private const float KeyText = 0.36f;
        private const float StrokeOfUnit = 0.8f;
        private const float ShoulderWidth = 1.55f;
        private const float ShoulderHeight = 0.62f;
        private const float TriggerWidth = 1.2f;
        private const float TriggerHeight = 0.86f;
        private const float TextMargin = 0.5f;
        private const float MaxWidth = 4.5f;
        private const float KeyRadius = 0.16f;
        private const float KeyLip = 0.1f;
        private const float StickInner = 0.31f;
        private const float SymbolSize = 0.18f;
        private const float SymbolStroke = 0.07f;
        private const float MouseWidth = 0.6f;
        private const float MouseHeight = 0.9f;
        private const float DpadArm = 0.32f;
        private const float RimLight = 0.4f;
        private const float KeyShade = 0.62f;

        /// <summary>The pixel width a glyph gets at a height: round glyphs are square, pills and key caps fit their name.</summary>
        public static int WidthFor(GlyphSpec spec, int height)
        {
            float text = TextWidth(spec, height);
            switch (spec.Shape)
            {
                case GlyphShape.Shoulder: return Mathf.CeilToInt(Mathf.Clamp(Mathf.Max(ShoulderWidth * height, text + TextMargin * height), height, MaxWidth * height));
                case GlyphShape.Trigger: return Mathf.CeilToInt(Mathf.Clamp(Mathf.Max(TriggerWidth * height, text + TextMargin * height), height, MaxWidth * height));
                case GlyphShape.KeyCap: return Mathf.CeilToInt(Mathf.Clamp(text + TextMargin * height, height, MaxWidth * height));
                case GlyphShape.Mouse: return Mathf.CeilToInt(height * (MouseWidth + Pad * 2f));
                default: return height;
            }
        }

        /// <summary>The glyph as a texture, bilinear and clamped, read-only once drawn.</summary>
        public static Texture2D PaintTexture(GlyphSpec spec, int height = DefaultHeight)
        {
            Color32[] pixels = Paint(spec, height, out int width);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Glyph " + spec.Text,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        /// <summary>The glyph's pixels, bottom row first (Texture2D order), transparent around the shape.</summary>
        public static Color32[] Paint(GlyphSpec spec, int height, out int width)
        {
            width = WidthFor(spec, height);
            var canvas = new Canvas(width, height);
            float h = height;
            var centre = new Vector2(width * 0.5f, h * 0.5f);
            Color fill = spec.Fill;
            Color ink = spec.Ink;
            Color rim = Color.Lerp(fill, Color.white, RimLight);

            switch (spec.Shape)
            {
                case GlyphShape.Round:
                case GlyphShape.Menu:
                case GlyphShape.View:
                    DrawDisc(canvas, centre, h * (0.5f - Pad), fill, rim, h);
                    DrawInside(canvas, spec, centre, h, ink, RoundText);
                    break;
                case GlyphShape.Shoulder:
                    DrawPill(canvas, centre, new Vector2(width - h * Pad * 2f, h * ShoulderHeight), h * ShoulderHeight * 0.5f, fill, rim, h);
                    DrawText(canvas, spec.Text, centre, h * SmallText, width - h * TextMargin, ink);
                    break;
                case GlyphShape.Trigger:
                    DrawPill(canvas, centre, new Vector2(width - h * Pad * 2f, h * TriggerHeight), h * 0.28f, fill, rim, h);
                    DrawText(canvas, spec.Text, centre, h * SmallText, width - h * TextMargin, ink);
                    break;
                case GlyphShape.KeyCap:
                    DrawKeyCap(canvas, spec, width, h, fill, ink);
                    break;
                case GlyphShape.Stick:
                    DrawDisc(canvas, centre, h * (0.5f - Pad), fill, rim, h);
                    canvas.Fill(p => Circle(p, centre, h * StickInner), rim);
                    DrawText(canvas, spec.Text, centre, h * SmallText, h * StickInner * 1.8f, ink);
                    break;
                case GlyphShape.Dpad:
                    DrawDpad(canvas, centre, h, fill, ink, spec.Symbol);
                    break;
                case GlyphShape.Mouse:
                    DrawMouse(canvas, centre, h, fill, ink, spec.Symbol);
                    break;
            }

            return canvas.ToPixels();
        }

        private static float TextWidth(GlyphSpec spec, int height)
        {
            if (string.IsNullOrEmpty(spec.Text))
            {
                return 0f;
            }

            float size = spec.Shape == GlyphShape.KeyCap ? KeyText : SmallText;
            return StrokeFont.Width(spec.Text) * height * size / StrokeFont.CellHeight;
        }

        private static void DrawDisc(Canvas canvas, Vector2 centre, float radius, Color fill, Color rim, float h)
        {
            canvas.Fill(p => Circle(p, centre, radius), fill);
            float ring = radius - h * RimInset;
            canvas.Fill(p => Mathf.Abs(Circle(p, centre, ring)) - h * RimWidth * 0.5f, rim);
        }

        private static void DrawPill(Canvas canvas, Vector2 centre, Vector2 size, float radius, Color fill, Color rim, float h)
        {
            Vector2 half = size * 0.5f;
            canvas.Fill(p => RoundBox(p, centre, half, radius), fill);
            float inset = h * RimInset;
            Vector2 inner = half - new Vector2(inset, inset);
            canvas.Fill(p => Mathf.Abs(RoundBox(p, centre, inner, Mathf.Max(0f, radius - inset))) - h * RimWidth * 0.5f, rim);
        }

        // The face sits on a darker lip, so the key reads as raised like a real key cap.
        private static void DrawKeyCap(Canvas canvas, GlyphSpec spec, int width, float h, Color fill, Color ink)
        {
            float pad = h * Pad;
            var bodyCentre = new Vector2(width * 0.5f, h * 0.5f);
            var bodyHalf = new Vector2(width * 0.5f - pad, h * 0.5f - pad);
            canvas.Fill(p => RoundBox(p, bodyCentre, bodyHalf, h * KeyRadius), Color.Lerp(Color.black, fill, KeyShade));
            float lip = h * KeyLip;
            var faceCentre = new Vector2(bodyCentre.x, bodyCentre.y - lip * 0.5f);
            var faceHalf = new Vector2(bodyHalf.x - pad, bodyHalf.y - lip * 0.5f - pad);
            canvas.Fill(p => RoundBox(p, faceCentre, faceHalf, h * KeyRadius * 0.8f), fill);
            DrawText(canvas, spec.Text, faceCentre, h * KeyText, faceHalf.x * 2f - h * 0.2f, ink);
        }

        private static void DrawInside(Canvas canvas, GlyphSpec spec, Vector2 centre, float h, Color ink, float textSize)
        {
            float s = h * SymbolSize;
            float stroke = h * SymbolStroke;
            var segments = new List<Segment>();
            switch (spec.Shape)
            {
                case GlyphShape.Menu:
                    for (int line = -1; line <= 1; line++)
                    {
                        float y = centre.y + line * h * 0.12f;
                        segments.Add(new Segment(new Vector2(centre.x - s, y), new Vector2(centre.x + s, y)));
                    }

                    canvas.Strokes(segments, stroke * 0.85f, ink);
                    return;
                case GlyphShape.View:
                    AddBox(segments, centre + new Vector2(-h * 0.05f, -h * 0.05f), new Vector2(s * 0.8f, s * 0.65f));
                    AddBox(segments, centre + new Vector2(h * 0.07f, h * 0.07f), new Vector2(s * 0.8f, s * 0.65f));
                    canvas.Strokes(segments, stroke * 0.7f, ink);
                    return;
            }

            switch (spec.Symbol)
            {
                case GlyphSymbol.Cross:
                    segments.Add(new Segment(centre + new Vector2(-s, -s), centre + new Vector2(s, s)));
                    segments.Add(new Segment(centre + new Vector2(s, -s), centre + new Vector2(-s, s)));
                    canvas.Strokes(segments, stroke, ink);
                    return;
                case GlyphSymbol.Circle:
                    canvas.Fill(p => Mathf.Abs(Circle(p, centre, s * 1.05f)) - stroke * 0.5f, ink);
                    return;
                case GlyphSymbol.Square:
                    AddBox(segments, centre, new Vector2(s * 0.95f, s * 0.95f));
                    canvas.Strokes(segments, stroke, ink);
                    return;
                case GlyphSymbol.Triangle:
                    var top = new Vector2(centre.x, centre.y - s * 1.15f);
                    var right = new Vector2(centre.x + s * 1.15f, centre.y + s * 0.85f);
                    var left = new Vector2(centre.x - s * 1.15f, centre.y + s * 0.85f);
                    segments.Add(new Segment(top, right));
                    segments.Add(new Segment(right, left));
                    segments.Add(new Segment(left, top));
                    canvas.Strokes(segments, stroke, ink);
                    return;
            }

            DrawText(canvas, spec.Text, centre, h * (spec.Text.Length > 1 ? SmallText : textSize), h * 0.7f, ink);
        }

        private static void DrawDpad(Canvas canvas, Vector2 centre, float h, Color fill, Color ink, GlyphSymbol lit)
        {
            float arm = h * DpadArm * 0.5f;
            float reach = h * (0.5f - Pad);
            canvas.Fill(p => Mathf.Min(RoundBox(p, centre, new Vector2(arm, reach), arm * 0.4f), RoundBox(p, centre, new Vector2(reach, arm), arm * 0.4f)), fill);
            Vector2 direction;
            switch (lit)
            {
                case GlyphSymbol.Up: direction = Vector2.down; break;
                case GlyphSymbol.Down: direction = Vector2.up; break;
                case GlyphSymbol.Left: direction = Vector2.left; break;
                case GlyphSymbol.Right: direction = Vector2.right; break;
                default: return;
            }

            // Pixel rows run downwards, so the up arm sits at smaller y.
            Vector2 armCentre = centre + direction * (reach * 0.62f);
            Vector2 half = direction.x != 0f ? new Vector2(reach * 0.36f, arm * 0.8f) : new Vector2(arm * 0.8f, reach * 0.36f);
            canvas.Fill(p => RoundBox(p, armCentre, half, arm * 0.3f), ink);
        }

        private static void DrawMouse(Canvas canvas, Vector2 centre, float h, Color fill, Color ink, GlyphSymbol lit)
        {
            var half = new Vector2(h * MouseWidth * 0.5f, h * MouseHeight * 0.5f);
            float radius = half.x * 0.95f;
            canvas.Fill(p => RoundBox(p, centre, half, radius), fill);
            float split = centre.y - half.y * 0.15f;
            float lineWidth = h * 0.025f;
            if (lit == GlyphSymbol.MouseLeft || lit == GlyphSymbol.MouseRight)
            {
                bool left = lit == GlyphSymbol.MouseLeft;
                canvas.Fill(p => Mathf.Max(Mathf.Max(RoundBox(p, centre, half, radius), p.y - split), left ? p.x - centre.x : centre.x - p.x), ink);
            }

            var dark = Color.Lerp(fill, Color.black, 0.55f);
            var lines = new List<Segment>
            {
                new Segment(new Vector2(centre.x - half.x, split), new Vector2(centre.x + half.x, split)),
                new Segment(new Vector2(centre.x, centre.y - half.y), new Vector2(centre.x, split)),
            };
            canvas.Strokes(lines, lineWidth, dark);
            Color wheel = lit == GlyphSymbol.MouseMiddle || lit == GlyphSymbol.MouseWheel ? ink : dark;
            var wheelCentre = new Vector2(centre.x, centre.y - half.y * 0.5f);
            canvas.Fill(p => RoundBox(p, wheelCentre, new Vector2(h * 0.045f, h * 0.09f), h * 0.04f), wheel);
        }

        private static void DrawText(Canvas canvas, string text, Vector2 centre, float height, float maxWidth, Color ink)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            float unit = height / StrokeFont.CellHeight;
            float units = StrokeFont.Width(text);
            if (units * unit > maxWidth && units > 0f)
            {
                unit = maxWidth / units;
            }

            var segments = new List<Segment>();
            float x = centre.x - units * unit * 0.5f;
            float top = centre.y - StrokeFont.CellHeight * unit * 0.5f;
            foreach (char c in text)
            {
                foreach (Vector2[] line in StrokeFont.Strokes(c))
                {
                    for (int i = 0; i + 1 < line.Length; i++)
                    {
                        segments.Add(new Segment(new Vector2(x + line[i].x * unit, top + line[i].y * unit), new Vector2(x + line[i + 1].x * unit, top + line[i + 1].y * unit)));
                    }

                    if (line.Length == 1)
                    {
                        Vector2 dot = new Vector2(x + line[0].x * unit, top + line[0].y * unit);
                        segments.Add(new Segment(dot, dot));
                    }
                }

                x += ((c == ' ' ? StrokeFont.SpaceWidth : StrokeFont.CellWidth) + StrokeFont.Spacing) * unit;
            }

            canvas.Strokes(segments, Mathf.Max(1.5f, unit * StrokeOfUnit), ink);
        }

        private static void AddBox(List<Segment> segments, Vector2 centre, Vector2 half)
        {
            var a = centre + new Vector2(-half.x, -half.y);
            var b = centre + new Vector2(half.x, -half.y);
            var c = centre + new Vector2(half.x, half.y);
            var d = centre + new Vector2(-half.x, half.y);
            segments.Add(new Segment(a, b));
            segments.Add(new Segment(b, c));
            segments.Add(new Segment(c, d));
            segments.Add(new Segment(d, a));
        }

        private static float Circle(Vector2 p, Vector2 centre, float radius) => (p - centre).magnitude - radius;

        private static float RoundBox(Vector2 p, Vector2 centre, Vector2 half, float radius)
        {
            radius = Mathf.Min(radius, Mathf.Min(half.x, half.y));
            Vector2 q = new Vector2(Mathf.Abs(p.x - centre.x), Mathf.Abs(p.y - centre.y)) - half + new Vector2(radius, radius);
            Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        private readonly struct Segment
        {
            public Segment(Vector2 a, Vector2 b)
            {
                A = a;
                B = b;
            }

            public Vector2 A { get; }

            public Vector2 B { get; }

            public float Distance(Vector2 p)
            {
                Vector2 ab = B - A;
                float length = ab.sqrMagnitude;
                float t = length > 0f ? Mathf.Clamp01(Vector2.Dot(p - A, ab) / length) : 0f;
                return (p - (A + ab * t)).magnitude;
            }
        }

        // Float layers composited source-over; x right, y down while drawing, flipped into texture order at the end.
        private sealed class Canvas
        {
            private readonly int _width;
            private readonly int _height;
            private readonly Color[] _pixels;

            public Canvas(int width, int height)
            {
                _width = width;
                _height = height;
                _pixels = new Color[width * height];
            }

            /// <summary>Fills where the signed distance is below zero, with a one-pixel soft edge.</summary>
            public void Fill(System.Func<Vector2, float> distance, Color colour)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        float coverage = Mathf.Clamp01(0.5f - distance(new Vector2(x + 0.5f, y + 0.5f)));
                        if (coverage > 0f)
                        {
                            Blend(x, y, colour, coverage);
                        }
                    }
                }
            }

            /// <summary>Draws lines of one width; overlaps do not darken because the nearest line decides each pixel.</summary>
            public void Strokes(List<Segment> segments, float width, Color colour)
            {
                if (segments.Count == 0)
                {
                    return;
                }

                float half = width * 0.5f;
                Vector2 min = segments[0].A;
                Vector2 max = segments[0].A;
                foreach (Segment segment in segments)
                {
                    min = Vector2.Min(min, Vector2.Min(segment.A, segment.B));
                    max = Vector2.Max(max, Vector2.Max(segment.A, segment.B));
                }

                int x0 = Mathf.Max(0, Mathf.FloorToInt(min.x - half - 1f));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(min.y - half - 1f));
                int x1 = Mathf.Min(_width - 1, Mathf.CeilToInt(max.x + half + 1f));
                int y1 = Mathf.Min(_height - 1, Mathf.CeilToInt(max.y + half + 1f));
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        float nearest = float.MaxValue;
                        foreach (Segment segment in segments)
                        {
                            nearest = Mathf.Min(nearest, segment.Distance(p));
                        }

                        float coverage = Mathf.Clamp01(half + 0.5f - nearest);
                        if (coverage > 0f)
                        {
                            Blend(x, y, colour, coverage);
                        }
                    }
                }
            }

            public Color32[] ToPixels()
            {
                var result = new Color32[_pixels.Length];
                for (int y = 0; y < _height; y++)
                {
                    int target = (_height - 1 - y) * _width;
                    int source = y * _width;
                    for (int x = 0; x < _width; x++)
                    {
                        result[target + x] = _pixels[source + x];
                    }
                }

                return result;
            }

            private void Blend(int x, int y, Color colour, float coverage)
            {
                int index = y * _width + x;
                Color under = _pixels[index];
                float alpha = colour.a * coverage;
                float outAlpha = alpha + under.a * (1f - alpha);
                if (outAlpha <= 0f)
                {
                    return;
                }

                Color mixed = (colour * alpha + under * under.a * (1f - alpha)) / outAlpha;
                mixed.a = outAlpha;
                _pixels[index] = mixed;
            }
        }
    }
}
