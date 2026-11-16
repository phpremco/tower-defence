using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace Game.UI
{
    /// <summary>
    /// Button glyphs inside TextMeshPro text (dialogue lines): one runtime sprite asset whose sheet holds every glyph
    /// drawn so far, so a line can say "press [A]" with the real button picture inline. A new glyph is painted once and
    /// appended; indices never move, so a tag written earlier stays valid. Texts that show a tag use <see cref="Asset"/>.
    /// </summary>
    public static class GlyphSpriteAtlas
    {
        // The glyph's height on the sheet; TextMeshPro scales it to the font's ascender, so this only sets sharpness.
        private const int GlyphHeight = 64;
        private const int SheetWidth = 1024;
        private const int Gap = 2;
        // The glyph hangs a little under the baseline, as a button picture sits in a line of capitals.
        private const float AboveBaseline = 0.82f;
        private const float AdvanceGap = 0.12f;
        private const string SpriteShader = "TextMeshPro/Sprite";

        private static readonly Dictionary<string, int> Indices = new Dictionary<string, int>();
        private static readonly List<Entry> Entries = new List<Entry>();
        private static TMP_SpriteAsset _asset;
        private static Texture2D _sheet;
        private static Material _material;

        /// <summary>The sprite asset to set on a TextMeshPro text that shows glyph tags; null before the first glyph.</summary>
        public static TMP_SpriteAsset Asset => _asset;

        /// <summary>The rich-text tag that draws the glyph inline (one character wide for the typewriter).</summary>
        public static string Tag(GlyphSpec spec) => "<sprite index=" + IndexOf(spec) + ">";

        /// <summary>The glyph's index in the sheet, painting and appending it on first use.</summary>
        public static int IndexOf(GlyphSpec spec)
        {
            // The editor can enter Play Mode without a domain reload: the glyph list survives, the asset and sheet do not.
            if (_asset == null && Entries.Count > 0)
            {
                Rebuild();
            }

            string key = spec.Key;
            if (Indices.TryGetValue(key, out int index))
            {
                return index;
            }

            Color32[] pixels = GlyphPainter.Paint(spec, GlyphHeight, out int width);
            index = Entries.Count;
            Entries.Add(new Entry(pixels, width));
            Indices[key] = index;
            Rebuild();
            return index;
        }

        private static void Rebuild()
        {
            List<RectInt> places = Pack(out int height);
            if (_sheet == null || _sheet.height != height)
            {
                if (_sheet != null)
                {
                    Object.Destroy(_sheet);
                }

                _sheet = new Texture2D(SheetWidth, height, TextureFormat.RGBA32, false)
                {
                    name = "Prompt glyph sheet",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }

            _sheet.SetPixels32(new Color32[SheetWidth * height]);
            for (int i = 0; i < Entries.Count; i++)
            {
                RectInt place = places[i];
                _sheet.SetPixels32(place.x, place.y, place.width, place.height, Entries[i].Pixels);
            }

            _sheet.Apply(false, false);
            BuildAsset(places);
        }

        // Shelves left to right, bottom to top; a glyph is at most a few heights wide, far under the sheet width.
        private static List<RectInt> Pack(out int height)
        {
            var places = new List<RectInt>(Entries.Count);
            int x = Gap;
            int y = Gap;
            foreach (Entry entry in Entries)
            {
                if (x + entry.Width + Gap > SheetWidth)
                {
                    x = Gap;
                    y += GlyphHeight + Gap;
                }

                places.Add(new RectInt(x, y, entry.Width, GlyphHeight));
                x += entry.Width + Gap;
            }

            height = Mathf.NextPowerOfTwo(y + GlyphHeight + Gap);
            return places;
        }

        private static void BuildAsset(List<RectInt> places)
        {
            if (_asset == null)
            {
                _asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                _asset.name = "Prompt glyphs";
                _asset.spriteInfoList = new List<TMP_Sprite>();
                Shader shader = Shader.Find(SpriteShader);
                // A player build may strip the TextMeshPro sprite shader; the canvas default draws a textured quad as well.
                _material = shader != null ? new Material(shader) : new Material(Canvas.GetDefaultCanvasMaterial());
            }

            _material.mainTexture = _sheet;
            _asset.spriteSheet = _sheet;
            _asset.spriteGlyphTable.Clear();
            _asset.spriteCharacterTable.Clear();
            for (int i = 0; i < places.Count; i++)
            {
                RectInt place = places[i];
                var metrics = new GlyphMetrics(place.width, place.height, 0f, place.height * AboveBaseline, place.width + place.height * AdvanceGap);
                var glyph = new TMP_SpriteGlyph((uint)i, metrics, new GlyphRect(place.x, place.y, place.width, place.height), 1f, 0);
                _asset.spriteGlyphTable.Add(glyph);
                _asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = "glyph" + i });
            }

            // The lookup refresh upgrades an asset that has a material but no version, which would empty these new
            // tables; an asset made in code has no version, so the material is attached after the refresh.
            _asset.material = null;
            _asset.UpdateLookupTables();
            _asset.material = _material;
        }

        private readonly struct Entry
        {
            public Entry(Color32[] pixels, int width)
            {
                Pixels = pixels;
                Width = width;
            }

            public Color32[] Pixels { get; }

            public int Width { get; }
        }
    }
}
