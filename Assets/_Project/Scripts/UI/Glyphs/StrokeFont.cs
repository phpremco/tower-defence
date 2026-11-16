using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// A small line font for button glyphs: capitals, digits, the symbols key names use and four arrows, each a few
    /// strokes on a 4 by 6 grid (y down). Glyph letters are drawn from it in code, so prompts need no font atlas and
    /// look the same in every text system. Lower case is drawn as upper case.
    /// </summary>
    public static class StrokeFont
    {
        public const float CellWidth = 4f;
        public const float CellHeight = 6f;
        public const float Spacing = 1.6f;
        public const float SpaceWidth = 3f;
        public const char Missing = '?';

        // One entry per character: polylines split by '|', points "x,y" split by spaces.
        private static readonly Dictionary<char, string> Shapes = new Dictionary<char, string>
        {
            { 'A', "0,6 2,0 4,6|1,4 3,4" },
            { 'B', "0,3 3,3 4,4 4,5 3,6 0,6 0,0 3,0 4,1 4,2 3,3" },
            { 'C', "4,1 3,0 1,0 0,1 0,5 1,6 3,6 4,5" },
            { 'D', "0,0 0,6 2,6 4,4 4,2 2,0 0,0" },
            { 'E', "4,0 0,0 0,6 4,6|0,3 3,3" },
            { 'F', "4,0 0,0 0,6|0,3 3,3" },
            { 'G', "4,1 3,0 1,0 0,1 0,5 1,6 3,6 4,5 4,3 2,3" },
            { 'H', "0,0 0,6|4,0 4,6|0,3 4,3" },
            { 'I', "1,0 3,0|2,0 2,6|1,6 3,6" },
            { 'J', "4,0 4,5 3,6 1,6 0,5" },
            { 'K', "0,0 0,6|4,0 0,4|1,3 4,6" },
            { 'L', "0,0 0,6 4,6" },
            { 'M', "0,6 0,0 2,3 4,0 4,6" },
            { 'N', "0,6 0,0 4,6 4,0" },
            { 'O', "1,0 3,0 4,1 4,5 3,6 1,6 0,5 0,1 1,0" },
            { 'P', "0,6 0,0 3,0 4,1 4,2 3,3 0,3" },
            { 'Q', "1,0 3,0 4,1 4,5 3,6 1,6 0,5 0,1 1,0|2,4 4,6" },
            { 'R', "0,6 0,0 3,0 4,1 4,2 3,3 0,3|2,3 4,6" },
            { 'S', "4,1 3,0 1,0 0,1 0,2 1,3 3,3 4,4 4,5 3,6 1,6 0,5" },
            { 'T', "0,0 4,0|2,0 2,6" },
            { 'U', "0,0 0,5 1,6 3,6 4,5 4,0" },
            { 'V', "0,0 2,6 4,0" },
            { 'W', "0,0 1,6 2,3 3,6 4,0" },
            { 'X', "0,0 4,6|4,0 0,6" },
            { 'Y', "0,0 2,3 4,0|2,3 2,6" },
            { 'Z', "0,0 4,0 0,6 4,6" },
            { '0', "1,0 3,0 4,1 4,5 3,6 1,6 0,5 0,1 1,0|3.6,1 0.4,5" },
            { '1', "1,1 2,0 2,6|1,6 3,6" },
            { '2', "0,1 1,0 3,0 4,1 4,2 0,6 4,6" },
            { '3', "0,1 1,0 3,0 4,1 4,2 3,3 4,4 4,5 3,6 1,6 0,5|1.5,3 3,3" },
            { '4', "3,6 3,0 0,4 4,4" },
            { '5', "4,0 0,0 0,3 3,3 4,4 4,5 3,6 0,6" },
            { '6', "4,0 2,0 0,2 0,5 1,6 3,6 4,5 4,4 3,3 0,3" },
            { '7', "0,0 4,0 1,6" },
            { '8', "1,0 3,0 4,1 4,2 3,3 1,3 0,2 0,1 1,0|1,3 0,4 0,5 1,6 3,6 4,5 4,4 3,3" },
            { '9', "4,3 1,3 0,2 0,1 1,0 3,0 4,1 4,4 2,6 0,6" },
            { '+', "2,1 2,5|0,3 4,3" },
            { '-', "0,3 4,3" },
            { '=', "0,2 4,2|0,4 4,4" },
            { '/', "4,0 0,6" },
            { '\\', "0,0 4,6" },
            { '.', "2,5.5 2,6" },
            { ',', "2,5 1.4,6.4" },
            { ';', "2,1.5 2,2|2,5 1.4,6.4" },
            { ':', "2,1.5 2,2|2,5.5 2,6" },
            { '\'', "2,0 2,1.5" },
            { '"', "1,0 1,1.5|3,0 3,1.5" },
            { '`', "1,0 2,1" },
            { '[', "3,0 1,0 1,6 3,6" },
            { ']', "1,0 3,0 3,6 1,6" },
            { '<', "4,0 0,3 4,6" },
            { '>', "0,0 4,3 0,6" },
            { '^', "0,3 2,0 4,3" },
            { '?', "0,1 1,0 3,0 4,1 4,2 2,3.5 2,4.5|2,5.5 2,6" },
            { '#', "1,0 1,6|3,0 3,6|0,2 4,2|0,4 4,4" },
            { '*', "2,1 2,5|0.5,2 3.5,4|3.5,2 0.5,4" },
            { '!', "2,0 2,4|2,5.5 2,6" },
            { '(', "3,0 1.5,1.5 1.5,4.5 3,6" },
            { ')', "1,0 2.5,1.5 2.5,4.5 1,6" },
            { '_', "0,6 4,6" },
            { '~', "0,3.5 1,2.5 3,3.5 4,2.5" },
            { '↑', "2,6 2,0|0,2 2,0 4,2" },
            { '↓', "2,0 2,6|0,4 2,6 4,4" },
            { '←', "4,3 0,3|2,1 0,3 2,5" },
            { '→', "0,3 4,3|2,1 4,3 2,5" },
        };

        private static readonly Dictionary<char, Vector2[][]> Parsed = new Dictionary<char, Vector2[][]>();

        /// <summary>True when the character has strokes (or is a space); others are drawn as <see cref="Missing"/>.</summary>
        public static bool Covers(char c) => c == ' ' || Shapes.ContainsKey(char.ToUpperInvariant(c));

        /// <summary>True when every character of a text can be drawn.</summary>
        public static bool CoversAll(string text)
        {
            if (text == null)
            {
                return true;
            }

            foreach (char c in text)
            {
                if (!Covers(c))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The width of a text in grid units, spacing included.</summary>
        public static float Width(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0f;
            }

            float width = 0f;
            foreach (char c in text)
            {
                width += (c == ' ' ? SpaceWidth : CellWidth) + Spacing;
            }

            return width - Spacing;
        }

        /// <summary>The strokes of a character in grid units; empty for a space.</summary>
        public static Vector2[][] Strokes(char c)
        {
            if (c == ' ')
            {
                return new Vector2[0][];
            }

            char key = char.ToUpperInvariant(c);
            if (!Shapes.ContainsKey(key))
            {
                key = Missing;
            }

            if (!Parsed.TryGetValue(key, out Vector2[][] strokes))
            {
                strokes = Parse(Shapes[key]);
                Parsed[key] = strokes;
            }

            return strokes;
        }

        private static Vector2[][] Parse(string shape)
        {
            string[] lines = shape.Split('|');
            var strokes = new Vector2[lines.Length][];
            for (int i = 0; i < lines.Length; i++)
            {
                string[] points = lines[i].Split(' ');
                strokes[i] = new Vector2[points.Length];
                for (int p = 0; p < points.Length; p++)
                {
                    string[] xy = points[p].Split(',');
                    strokes[i][p] = new Vector2(
                        float.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            return strokes;
        }
    }
}
