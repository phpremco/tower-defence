using System.Collections.Generic;
using System.Text;

namespace Game.UI
{
    /// <summary>
    /// Reads a help-line template into rows, hint groups and pieces, so the line is spaced by the layout and not by
    /// the spaces of the text: a line break starts a row, two or more spaces start a new hint group, and one space
    /// between a hole and its words is a fixed word gap. A text component does not measure the spaces at its ends, which
    /// is why they are never left to it.
    /// </summary>
    public static class PromptLineLayout
    {
        private const char RowBreak = '\n';
        private const int GroupBreakSpaces = 2;

        /// <summary>The rows of a template, each a list of hint groups, each a list of pieces.</summary>
        public static List<List<List<PromptPiece>>> Parse(string template)
        {
            var rows = new List<List<List<PromptPiece>>>();
            if (string.IsNullOrEmpty(template))
            {
                return rows;
            }

            foreach (string line in template.Split(RowBreak))
            {
                List<List<PromptPiece>> row = ParseRow(line);
                if (row.Count > 0)
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        private static List<List<PromptPiece>> ParseRow(string line)
        {
            var row = new List<List<PromptPiece>>();
            var words = new StringBuilder();
            int spaces = 0;
            int spacesBeforeWords = 0;
            for (int i = 0; i < line.Length; i++)
            {
                int hole = HoleAt(line, i);
                if (hole >= 0)
                {
                    FlushWords(row, words, spacesBeforeWords);
                    Add(row, PromptPiece.ForHole(hole, spaces > 0), spaces);
                    spaces = 0;
                    i += 2;
                }
                else if (char.IsWhiteSpace(line[i]))
                {
                    spaces++;
                }
                else
                {
                    if (words.Length > 0 && spaces >= GroupBreakSpaces)
                    {
                        FlushWords(row, words, spacesBeforeWords);
                    }

                    if (words.Length == 0)
                    {
                        spacesBeforeWords = spaces;
                    }
                    else if (spaces > 0)
                    {
                        words.Append(' ');
                    }

                    words.Append(line[i]);
                    spaces = 0;
                }
            }

            FlushWords(row, words, spacesBeforeWords);
            return row;
        }

        private static void FlushWords(List<List<PromptPiece>> row, StringBuilder words, int spacesBefore)
        {
            if (words.Length == 0)
            {
                return;
            }

            Add(row, PromptPiece.Words(words.ToString(), spacesBefore > 0), spacesBefore);
            words.Clear();
        }

        private static void Add(List<List<PromptPiece>> row, PromptPiece piece, int spacesBefore)
        {
            if (row.Count == 0 || spacesBefore >= GroupBreakSpaces)
            {
                row.Add(new List<PromptPiece>());
            }

            row[row.Count - 1].Add(piece);
        }

        // "{3}" with a single digit is a hole; anything else is text.
        private static int HoleAt(string template, int i)
        {
            if (i + 2 >= template.Length || template[i] != '{' || template[i + 2] != '}' || !char.IsDigit(template[i + 1]))
            {
                return -1;
            }

            return template[i + 1] - '0';
        }
    }
}
