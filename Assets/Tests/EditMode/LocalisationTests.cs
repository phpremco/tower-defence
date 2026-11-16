using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Game.Core;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 69: the string tables. Parser and fallback rules, every table loads clean with unique keys, every key that
    /// code or data refers to has an English text, other languages carry the same keys as English, and a scan of the
    /// runtime scripts flags new hardcoded UI text (Docs/LOCALISATION.md).
    /// </summary>
    public sealed class LocalisationTests
    {
        private const string ScriptsRoot = "Assets/_Project/Scripts";
        private const string KeepMarker = "// loc: keep";

        private static readonly Regex KeyInCode = new Regex(@"Loc\.(?:Get|Format|Has)\(\s*""([^""]+)""", RegexOptions.Compiled);
        private static readonly Regex UiTextCall = new Regex(@"\bUiKit\.(CreateText|CreateFixedText|CreateButton|CreateSlider|SetLabel|Keyed)\s*\(|\bSetText\s*\(|\.text\s*\+?=[^=]", RegexOptions.Compiled);
        private static readonly Regex StringLiteral = new Regex(@"\$?@?""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);
        private static readonly Regex Hole = new Regex(@"\{[^{}]*\}", RegexOptions.Compiled);
        private static readonly Regex Word = new Regex(@"[A-Za-z]{2,}", RegexOptions.Compiled);

        [SetUp]
        public void FreshTables()
        {
            Loc.ResetFallback();
        }

        [Test]
        public void Parser_ReadsKeysTextsEscapesAndComments()
        {
            var table = new Dictionary<string, string>();
            var errors = new List<string>();
            StringTableParser.Parse("# comment\n\nmenu.start = Start\r\nhud.wave = Wave {0}/{1}\nline.two = One\\nTwo \\\\ \\t!\nspaced =    -   locked\nrich = <color=#FFD45C>A</color>  Go", "t", table, errors);

            CollectionAssert.IsEmpty(errors);
            Assert.AreEqual("Start", table["menu.start"]);
            Assert.AreEqual("Wave {0}/{1}", table["hud.wave"]);
            Assert.AreEqual("One\nTwo \\ \t!", table["line.two"]);
            Assert.AreEqual("   -   locked", table["spaced"], "only the one layout space after '=' is dropped");
            Assert.AreEqual("<color=#FFD45C>A</color>  Go", table["rich"]);
        }

        [Test]
        public void Parser_ReportsBadLinesAndDuplicates_AndKeepsTheRest()
        {
            var table = new Dictionary<string, string>();
            var errors = new List<string>();
            StringTableParser.Parse("a = 1\nno separator\na = 2\nbad key = x\nb = 3", "t", table, errors);

            Assert.AreEqual(3, errors.Count, string.Join("\n", errors));
            Assert.AreEqual("1", table["a"]);
            Assert.AreEqual("3", table["b"]);
        }

        [Test]
        public void Escape_IsTheInverseOfParse()
        {
            const string text = "Line one\nC:\\path\twith tab  ";
            var table = new Dictionary<string, string>();
            StringTableParser.Parse("k = " + StringTableParser.Escape(text), "t", table, null);

            Assert.AreEqual(text, table["k"]);
        }

        [Test]
        public void Service_FallsBackToEnglish_ThenToTheKey()
        {
            var sources = new[]
            {
                new KeyValuePair<string, string>("en.ui", "language.name = English\nmenu.start = Start\nmenu.quit = Quit"),
                new KeyValuePair<string, string>("nl.ui", "language.name = Nederlands\nmenu.start = Beginnen"),
            };
            var service = new StringTableService(sources, "nl");
            int changed = 0;
            service.LanguageChanged += () => changed++;

            Assert.AreEqual("nl", service.CurrentLanguage);
            Assert.IsTrue(service.TryGet("menu.start", out string start));
            Assert.AreEqual("Beginnen", start);
            Assert.IsTrue(service.TryGet("menu.quit", out string quit), "a key missing in Dutch falls back to English");
            Assert.AreEqual("Quit", quit);
            Assert.IsFalse(service.TryGet("menu.nothing", out _));
            Assert.AreEqual(2, service.Languages.Count);
            Assert.AreEqual("Nederlands", service.Languages[1].NativeName);

            Assert.IsTrue(service.SetLanguage("en"));
            Assert.AreEqual(1, changed);
            Assert.IsFalse(service.SetLanguage("xx"), "an unknown language keeps the current one");
            Assert.AreEqual("en", service.CurrentLanguage);
        }

        [Test]
        public void UnknownLanguageAtBoot_ShowsEnglish()
        {
            var service = new StringTableService(new[] { new KeyValuePair<string, string>("en.ui", "a = A") }, "xx");

            Assert.AreEqual("en", service.CurrentLanguage);
        }

        [Test]
        public void ShippedTables_LoadWithoutErrors_AndEnglishIsAvailable()
        {
            var service = new StringTableService(StringTableService.FallbackLanguage);

            CollectionAssert.IsEmpty(service.Errors, "bad lines or keys used twice across the table files");
            Assert.IsTrue(service.TryGet(StringTableService.LanguageNameKey, out string name));
            Assert.AreEqual("English", name);
            Assert.Greater(service.KeysOf("en").Count, 500, "the UI and data tables are found in Resources");
        }

        [Test]
        public void OtherLanguages_CarryExactlyTheEnglishKeys()
        {
            var service = new StringTableService(StringTableService.FallbackLanguage);
            var english = new HashSet<string>(service.KeysOf("en"));
            var problems = new List<string>();
            foreach (LanguageInfo language in service.Languages)
            {
                if (language.Code == "en")
                {
                    continue;
                }

                var keys = new HashSet<string>(service.KeysOf(language.Code));
                foreach (string key in english)
                {
                    if (!keys.Contains(key))
                    {
                        problems.Add($"{language.Code}: missing '{key}'");
                    }
                }

                foreach (string key in keys)
                {
                    if (!english.Contains(key))
                    {
                        problems.Add($"{language.Code}: '{key}' is not an English key");
                    }
                }
            }

            CollectionAssert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void EveryKeyInCode_HasAnEnglishText()
        {
            var missing = new List<string>();
            foreach (string file in RuntimeScripts())
            {
                foreach (Match match in KeyInCode.Matches(File.ReadAllText(file)))
                {
                    if (!Loc.Has(match.Groups[1].Value))
                    {
                        missing.Add($"{file}: '{match.Groups[1].Value}'");
                    }
                }
            }

            CollectionAssert.IsEmpty(missing, "keys without a text:\n" + string.Join("\n", missing));
        }

        [Test]
        public void EveryDefinitionTextField_HoldsAKeyWithAnEnglishText()
        {
            var problems = new List<string>();
            int fields = 0;
            foreach (ScriptableObject asset in LocalisationTables.Definitions())
            {
                SerializedProperty property = new SerializedObject(asset).GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.String || !LocalisationTables.IsLocalised(asset.GetType().Name, property.propertyPath))
                    {
                        continue;
                    }

                    string value = property.stringValue;
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    fields++;
                    if (!value.StartsWith(LocalisationTables.KeyPrefix, StringComparison.Ordinal) || !Loc.Has(value))
                    {
                        problems.Add($"{asset.name} {property.propertyPath}: '{Shorten(value)}'");
                    }
                }
            }

            Assert.Greater(fields, 300, "the definitions were found");
            CollectionAssert.IsEmpty(problems, "text fields without a table key (run Tools/Generate/Localisation Tables):\n" + string.Join("\n", problems));
        }

        [Test]
        public void RuntimeScripts_HaveNoHardcodedUiText()
        {
            var found = new List<string>();
            foreach (string file in RuntimeScripts())
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string literal = HardcodedUiText(lines[i]);
                    if (literal != null)
                    {
                        found.Add($"{file}:{i + 1}: \"{literal}\"");
                    }
                }
            }

            CollectionAssert.IsEmpty(found, "UI text written in code: move it to a table key, or end the line with '" + KeepMarker + "' and a reason:\n" + string.Join("\n", found));
        }

        [TestCase("label.text = \"Ready\";", true)]
        [TestCase("UiKit.CreateButton(column, \"Back\", Close);", true)]
        [TestCase("_caption.text = $\"{index + 1}. Locked\";", true)]
        [TestCase("UiKit.CreateButton(column, Loc.Get(\"common.back\"), Close);", false)]
        [TestCase("_label.text = Loc.Format(\"hud.wave\", wave, total);", false)]
        [TestCase("_label.text = $\"+{value}\";", false)]
        [TestCase("_label.text = $\"{name.ToUpperInvariant()}  -  {phase}\";", false)]
        [TestCase("_label.text = \"Logo\"; // loc: keep (brand)", false)]
        [TestCase("// _label.text = \"Old\";", false)]
        [TestCase("bool same = label.text == \"Ready\";", false)]
        [TestCase("_header = CreateText(\"Header\", TextAnchor.MiddleCenter);", false)]
        public void Scanner_FlagsLettersInUiCallsOnly(string line, bool flagged)
        {
            Assert.AreEqual(flagged, HardcodedUiText(line) != null);
        }

        // A literal with two letters in a row, outside Loc keys and interpolation holes, on a line that sets UI text.
        private static string HardcodedUiText(string line)
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("//", StringComparison.Ordinal) || line.Contains(KeepMarker) || !UiTextCall.IsMatch(line))
            {
                return null;
            }

            string code = KeyInCode.Replace(line, "Loc.Key(");
            foreach (Match literal in StringLiteral.Matches(code))
            {
                string text = Hole.Replace(literal.Groups[1].Value, string.Empty);
                if (Word.IsMatch(text))
                {
                    return literal.Groups[1].Value;
                }
            }

            return null;
        }

        // Editor tools and the balance model are developer-facing; everything else can reach the screen.
        private static IEnumerable<string> RuntimeScripts()
        {
            foreach (string file in Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                if (path.Contains("/Editor/") || path.Contains("/Balance/"))
                {
                    continue;
                }

                yield return path;
            }
        }

        private static string Shorten(string text) => text.Length > 60 ? text.Substring(0, 60) + "..." : text;
    }
}
