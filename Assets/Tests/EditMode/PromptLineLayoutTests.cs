using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Core;
using Game.UI;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// The help line under the planet map is spaced by its layout, not by the spaces of its text: a template is read into
    /// rows, hint groups and pieces, and the map's own templates stay short enough to fit in two rows.
    /// </summary>
    public sealed class PromptLineLayoutTests
    {
        private const string TableFile = "Assets/_Project/Resources/Localization/en.ui_menus.txt";
        private const int MaxRows = 2;
        private const int MaxGroupsPerRow = 4;

        // What a row may hold with every hole counted as a short key name: a longer row would be shrunk below the
        // readable size on a 1280x720 screen.
        private const int MaxRowCharacters = 96;
        private const int HoleCharacters = 4;

        private static readonly string[] HintKeys = { "map.hint_planet", "map.hint_region_pad", "map.hint_region_mouse" };

        [Test]
        public void Parse_SplitsRowsGroupsAndPieces()
        {
            List<List<List<PromptPiece>>> rows = PromptLineLayout.Parse("{0} left / right or {1} next     {2} play\nDown: buttons     Click twice");

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(2, rows[0].Count);
            Assert.AreEqual("[0] _left / right or _[1] _next", Describe(rows[0][0]));
            Assert.AreEqual("[2] _play", Describe(rows[0][1]));
            Assert.AreEqual(2, rows[1].Count);
            Assert.AreEqual("Down: buttons", Describe(rows[1][0]));
            Assert.AreEqual("Click twice", Describe(rows[1][1]));
        }

        [Test]
        public void Parse_KeepsPunctuationTightToItsHole()
        {
            List<List<List<PromptPiece>>> rows = PromptLineLayout.Parse("Hold ({0}), then {1}.");

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(1, rows[0].Count);
            Assert.AreEqual("Hold ( [0] ), then _[1] .", Describe(rows[0][0]));
        }

        [Test]
        public void Parse_NeverLeavesSpacesAtTheEndsOfAPiece()
        {
            string[] templates = { "  {0}   a  b {1}  ", " a {0}", "{0}{1}", "a\n\n b ", string.Empty, null, "   " };
            foreach (string template in templates)
            {
                foreach (PromptPiece piece in Pieces(PromptLineLayout.Parse(template)))
                {
                    AssertClean(piece, template);
                }
            }

            Assert.IsEmpty(PromptLineLayout.Parse("   "));
            Assert.IsEmpty(PromptLineLayout.Parse(null));
        }

        [Test]
        public void MapHints_AreCleanGroupsThatFitTwoRows()
        {
            Dictionary<string, string> table = Table();
            foreach (string key in HintKeys)
            {
                Assert.IsTrue(table.TryGetValue(key, out string template), key);
                List<List<List<PromptPiece>>> rows = PromptLineLayout.Parse(template);
                Assert.That(rows.Count, Is.InRange(1, MaxRows), key);
                foreach (List<List<PromptPiece>> row in rows)
                {
                    Assert.LessOrEqual(row.Count, MaxGroupsPerRow, $"{key}: hints in one row");
                    Assert.LessOrEqual(Characters(row), MaxRowCharacters, $"{key}: row too long");
                    foreach (List<PromptPiece> group in row)
                    {
                        AssertGroup(group, key);
                    }
                }
            }
        }

        // A glyph is followed by its words after a gap: no colon or other mark hangs behind a button.
        private static void AssertGroup(List<PromptPiece> group, string key)
        {
            for (int i = 0; i < group.Count; i++)
            {
                AssertClean(group[i], key);
                if (i == 0 || group[i].IsHole || !group[i - 1].IsHole)
                {
                    continue;
                }

                Assert.IsTrue(group[i].GapBefore, $"{key}: \"{group[i].Text}\" touches its glyph");
                Assert.IsTrue(char.IsLetterOrDigit(group[i].Text[0]), $"{key}: \"{group[i].Text}\" starts with punctuation after a glyph");
            }
        }

        private static void AssertClean(PromptPiece piece, string where)
        {
            if (piece.IsHole)
            {
                Assert.IsNull(piece.Text);
                return;
            }

            Assert.IsNotEmpty(piece.Text, where);
            Assert.AreEqual(piece.Text.Trim(), piece.Text, where);
            StringAssert.DoesNotContain("  ", piece.Text, where);
        }

        private static int Characters(List<List<PromptPiece>> row)
        {
            return row.Sum(group => group.Sum(piece => (piece.IsHole ? HoleCharacters : piece.Text.Length) + 1));
        }

        private static IEnumerable<PromptPiece> Pieces(List<List<List<PromptPiece>>> rows)
        {
            return rows.SelectMany(row => row).SelectMany(group => group);
        }

        // Holes as "[n]", a gap before a piece as "_", pieces separated by a space.
        private static string Describe(List<PromptPiece> group)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < group.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ').Append(group[i].GapBefore ? "_" : string.Empty);
                }

                builder.Append(group[i].IsHole ? $"[{group[i].Hole}]" : group[i].Text);
            }

            return builder.ToString();
        }

        private static Dictionary<string, string> Table()
        {
            var table = new Dictionary<string, string>();
            var errors = new List<string>();
            StringTableParser.Parse(File.ReadAllText(TableFile), Path.GetFileName(TableFile), table, errors);
            Assert.IsEmpty(errors);
            return table;
        }
    }
}
