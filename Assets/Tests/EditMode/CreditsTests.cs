using System.Collections.Generic;
using Game.Core;
using Game.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 72: the credits data. Every heading and line is a string-table key with an English text, the font licences
    /// ship with it, MPFB/MakeHuman is credited, and the Boot scene hands the asset to the main menu.
    /// </summary>
    public sealed class CreditsTests
    {
        private const string CreditsPath = "Assets/_Project/Data/Menu/Credits.asset";
        private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

        private static CreditsDefinition Credits()
        {
            var credits = AssetDatabase.LoadAssetAtPath<CreditsDefinition>(CreditsPath);
            Assert.IsNotNull(credits, $"{CreditsPath} is missing: run Tools/Generate/Credits");
            return credits;
        }

        [Test]
        public void EveryKey_HasAnEnglishText()
        {
            CreditsDefinition credits = Credits();
            var missing = new List<string>();
            Expect(credits.TitleKey, missing);
            Expect(credits.LicencesTitleKey, missing);
            foreach (CreditsSection section in credits.Sections)
            {
                Expect(section.TitleKey, missing);
                foreach (string key in section.LineKeys)
                {
                    Expect(key, missing);
                }
            }

            Assert.Greater(credits.Sections.Length, 3);
            CollectionAssert.IsEmpty(missing, "credits keys without a text");
        }

        [Test]
        public void TheFontLicences_ShipWithTheCredits()
        {
            TextAsset[] licences = Credits().LicenceTexts;

            Assert.AreEqual(2, licences.Length);
            foreach (TextAsset licence in licences)
            {
                Assert.IsNotNull(licence);
                StringAssert.Contains("SIL OPEN FONT LICENSE", licence.text.ToUpperInvariant());
            }
        }

        [Test]
        public void MakeHuman_AndEveryThirdPartySound_AreCredited()
        {
            string all = string.Empty;
            foreach (CreditsSection section in Credits().Sections)
            {
                foreach (string key in section.LineKeys)
                {
                    all += Loc.Get(key) + "\n";
                }
            }

            StringAssert.Contains("MakeHuman", all);
            StringAssert.Contains("MPFB", all);
            StringAssert.Contains("Kenney", all);
            StringAssert.Contains("Lentikula", all);
            StringAssert.Contains("Pixabay", all);
            StringAssert.Contains("Chakra Petch", all);
        }

        [Test]
        public void TheBootScene_ReferencesTheCredits()
        {
            string guid = AssetDatabase.AssetPathToGUID(CreditsPath);
            string scene = System.IO.File.ReadAllText(BootScenePath);

            StringAssert.Contains("_credits: {fileID: 11400000, guid: " + guid, scene);
        }

        private static void Expect(string key, List<string> missing)
        {
            if (string.IsNullOrEmpty(key) || !Loc.Has(key))
            {
                missing.Add(key ?? "(null)");
            }
        }
    }
}
