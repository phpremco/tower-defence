using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Game.Core;
using Game.Platform;
using Game.UI;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Round 71: texts name buttons through tokens ("{action:Confirm}") that the UI draws as the glyph of the device in
    /// use; no table string names a raw key or pad button in words, except the labels that exist to name a fixed
    /// keyboard control or a device (listed below with the reason).
    /// </summary>
    public sealed class PromptTokenTests
    {
        private const string TableFolder = "Assets/_Project/Resources/Localization";

        // Words that name a key, a mouse action or a pad button. Key names are matched with their capital, so "enter the
        // region" (a verb) passes and "Enter" (the key) does not.
        private static readonly Regex[] RawNames =
        {
            new Regex(@"\b(click|clicks|clicking|right-click|double-click)\b", RegexOptions.IgnoreCase),
            new Regex(@"\b(Esc|Escape|Enter|Tab|Space|Spacebar|Shift|Ctrl|Alt|Backspace|WASD)\b"),
            new Regex(@"\b[Pp]ress(es)?\s+(the\s+)?([ABXY]|LB|RB|LT|RT)\b"),
            new Regex(@"\b(LB|RB|LT|RT|L1|R1|L2|R2|ZL|ZR)\b"),
            new Regex(@"\b[ABXY] button\b"),
            new Regex(@"\b(Start|Select|View|Menu|Share|Create|Options) button\b"),
            new Regex(@"\b(mouse|keyboard|arrow keys?|d-pad|dpad|thumbstick|stick)\b", RegexOptions.IgnoreCase),
        };

        // Labels whose job is to name a device or a fixed control, each shown only with that device.
        private static readonly Dictionary<string, string> Allowed = new Dictionary<string, string>
        {
            { "controls.header_keys", "the keyboard column's header on the controls page" },
            { "controls.cursor_keys", "the fixed keyboard/mouse control in the keyboard column" },
            { "controls.camera_keys", "the fixed keyboard control in the keyboard column" },
            { "controls.zoom_keys", "the fixed keyboard/mouse control in the keyboard column" },
            { "map.turn_keys", "the planet-turn hole of the map hint, chosen when the keyboard is in use" },
            { "map.turn_stick", "the planet-turn hole of the map hint, chosen when a pad is in use (beside its stick glyph)" },
            { "map.hint_region_mouse", "the map hint variant shown only while the mouse is in use" },
        };

        [Test]
        public void NoTableString_NamesARawKeyOrButton()
        {
            var found = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in EnglishTable())
            {
                if (Allowed.ContainsKey(entry.Key))
                {
                    continue;
                }

                string text = WithoutTokens(entry.Value);
                foreach (Regex raw in RawNames)
                {
                    Match match = raw.Match(text);
                    if (match.Success)
                    {
                        found.AppendLine($"{entry.Key}: \"{match.Value}\" in {entry.Value}");
                        break;
                    }
                }
            }

            Assert.IsEmpty(found.ToString(), "Name the control with a token ({action:Confirm}) instead:\n" + found);
        }

        [Test]
        public void EveryToken_InTheTables_NamesARealActionOrControl()
        {
            var bad = new StringBuilder();
            int tokens = 0;
            foreach (KeyValuePair<string, string> entry in EnglishTable())
            {
                PromptTokens.Walk(entry.Value, _ => { }, token =>
                {
                    tokens++;
                    if (!PromptText.TryParse(token, out _, out _))
                    {
                        bad.AppendLine($"{entry.Key}: {token}");
                    }
                });
            }

            Assert.IsEmpty(bad.ToString());
            Assert.GreaterOrEqual(tokens, 4, "the hints that named keys now carry tokens");
        }

        [Test]
        public void Walk_SplitsRunsAndTokens()
        {
            var parts = new List<string>();
            PromptTokens.Walk("Press {action:Confirm} or {control:<Keyboard>/escape}.", run => parts.Add("t:" + run), token => parts.Add("k:" + token));
            CollectionAssert.AreEqual(new[] { "t:Press ", "k:{action:Confirm}", "t: or ", "k:{control:<Keyboard>/escape}", "t:." }, parts);
        }

        [Test]
        public void VisibleLength_CountsATokenAsOneGlyph()
        {
            Assert.AreEqual("press ".Length + 1 + " now".Length, PromptTokens.VisibleLength("press {action:Confirm} now"));
            Assert.AreEqual(5, PromptTokens.VisibleLength("plain"));
            Assert.AreEqual(0, PromptTokens.VisibleLength(null));
            Assert.AreEqual("{0} stays".Length, PromptTokens.VisibleLength("{0} stays"), "format holes are no tokens");
        }

        [Test]
        public void TryParse_KnowsActionsAndControls()
        {
            Assert.IsTrue(PromptText.TryParse("{action:HeroPost}", out GameAction action, out string path));
            Assert.AreEqual(GameAction.HeroPost, action);
            Assert.IsNull(path);
            Assert.IsTrue(PromptText.TryParse("{control:<Keyboard>/escape}", out _, out path));
            Assert.AreEqual("<Keyboard>/escape", path);
            Assert.IsFalse(PromptText.TryParse("{action:Jump}", out _, out _));
            Assert.IsFalse(PromptText.TryParse("{control:escape}", out _, out _));
        }

        [Test]
        public void ForText_LeavesNoTokenBehind()
        {
            string text = PromptText.ForText("Press {action:Confirm} to buy.");
            StringAssert.DoesNotContain("{action:", text);
            StringAssert.Contains(PromptText.KeyColour, text);
            StringAssert.StartsWith("Press ", PromptText.Plain("Press {action:Confirm} to buy."));
            Assert.AreEqual("No tokens here.", PromptText.ForText("No tokens here."));
        }

        [Test]
        public void ForTmp_GuardsMarkupAroundTheToken()
        {
            string text = PromptText.ForTmp("a <b> {action:Confirm}", out int visible, out _);
            StringAssert.Contains("<noparse>a <b> </noparse>", text);
            Assert.Greater(visible, "a <b> ".Length);
        }

        private static string WithoutTokens(string text)
        {
            var builder = new StringBuilder(text.Length);
            PromptTokens.Walk(text, run => builder.Append(run), _ => builder.Append('#'));
            return builder.ToString();
        }

        private static Dictionary<string, string> EnglishTable()
        {
            var table = new Dictionary<string, string>();
            var errors = new List<string>();
            foreach (string file in Directory.GetFiles(TableFolder, "en.*.txt"))
            {
                StringTableParser.Parse(File.ReadAllText(file), Path.GetFileName(file), table, errors);
            }

            Assert.IsEmpty(errors);
            return table;
        }
    }
}
