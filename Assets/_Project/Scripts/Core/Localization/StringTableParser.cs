using System.Collections.Generic;
using System.Text;

namespace Game.Core
{
    /// <summary>
    /// Reads one string-table file: one <c>key = text</c> per line, <c>#</c> starts a comment line, blank lines are
    /// skipped. In the text, <c>\n</c> is a line break, <c>\t</c> a tab and <c>\\</c> a backslash; everything after
    /// the "= " is kept as written, so a text may hold rich-text tags and <c>{0}</c> placeholders. A plain line format
    /// keeps the tables easy to diff, review and hand to a translator, and parsing it needs no reflection.
    /// </summary>
    public static class StringTableParser
    {
        private const char Separator = '=';
        private const char CommentMark = '#';

        /// <summary>
        /// Adds every entry of <paramref name="source"/> to <paramref name="into"/>. Malformed lines and keys that are
        /// already present are reported in <paramref name="errors"/> (prefixed with <paramref name="sourceName"/>) and
        /// skipped, so one bad line never hides the rest of a table.
        /// </summary>
        public static void Parse(string source, string sourceName, IDictionary<string, string> into, ICollection<string> errors)
        {
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            string[] lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (line.Trim().Length == 0 || line.TrimStart()[0] == CommentMark)
                {
                    continue;
                }

                int separator = line.IndexOf(Separator);
                string key = separator > 0 ? line.Substring(0, separator).Trim() : string.Empty;
                if (key.Length == 0 || key.IndexOf(' ') >= 0)
                {
                    errors?.Add($"{sourceName}:{i + 1}: expected 'key = text'");
                    continue;
                }

                if (into.ContainsKey(key))
                {
                    errors?.Add($"{sourceName}:{i + 1}: duplicate key '{key}'");
                    continue;
                }

                into.Add(key, Unescape(TextAfter(line, separator)));
            }
        }

        /// <summary>Writes a text the way <see cref="Parse"/> reads it back (the inverse of <see cref="Unescape"/>).</summary>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text.Replace("\\", "\\\\").Replace("\r", string.Empty).Replace("\n", "\\n").Replace("\t", "\\t");
        }

        // One space after the separator belongs to the layout ("key = text"); any further spaces are part of the text.
        private static string TextAfter(string line, int separator)
        {
            int start = separator + 1;
            if (start < line.Length && line[start] == ' ')
            {
                start++;
            }

            return line.Substring(start);
        }

        private static string Unescape(string text)
        {
            if (text.IndexOf('\\') < 0)
            {
                return text;
            }

            var builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c != '\\' || i == text.Length - 1)
                {
                    builder.Append(c);
                    continue;
                }

                char next = text[++i];
                switch (next)
                {
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    default:
                        builder.Append(c).Append(next);
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
