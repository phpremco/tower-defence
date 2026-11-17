using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// The structure damage factor (round 77): creatures bite at the multiplier times the factor in play, the balance model
    /// keeps reading the multiplier alone, and the generated profiles soften Easy and Medium only.
    /// </summary>
    public sealed class DifficultyProfileTests
    {
        private const string DifficultyFolder = "Assets/_Project/Data/Difficulty/";

        [Test]
        public void StructureDamage_InPlay_IsTheMultiplierTimesTheFactor_AndTheModelReadsTheMultiplier()
        {
            var profile = ScriptableObject.CreateInstance<DifficultyProfile>();
            var data = new SerializedObject(profile);
            data.FindProperty("_structureDamageMultiplier").floatValue = 0.75f;
            data.FindProperty("_structureDamageFactor").floatValue = 0.6f;
            data.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(0.45f, profile.StructureDamageMultiplier, 1e-5f);
            Assert.AreEqual(0.75f, profile.CalibratedStructureDamage, 1e-5f);
            Assert.AreEqual(0.75f, BalanceLedger.Stats(profile, 1).StructureDamage, 1e-5f, "the wave solver must not answer softer bites with tougher creatures");
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void GeneratedProfiles_SoftenEasyAndMediumOnly_AndEasyNeverBitesHarderThanMedium()
        {
            DifficultyProfile Load(string id) => AssetDatabase.LoadAssetAtPath<DifficultyProfile>(DifficultyFolder + id + ".asset");
            DifficultyProfile easy = Load("Easy");
            DifficultyProfile medium = Load("Medium");
            DifficultyProfile hard = Load("Hard");
            DifficultyProfile psycho = Load("Psycho");
            Assert.IsNotNull(easy, "run the generator first");

            Assert.Less(easy.StructureDamageFactor, 1f);
            Assert.Less(medium.StructureDamageFactor, 1f);
            Assert.AreEqual(1f, hard.StructureDamageFactor, 1e-5f);
            Assert.AreEqual(1f, psycho.StructureDamageFactor, 1e-5f);
            Assert.LessOrEqual(easy.StructureDamageMultiplier, medium.StructureDamageMultiplier + 1e-5f);
            Assert.Less(medium.StructureDamageMultiplier, hard.StructureDamageMultiplier);
        }
    }
}
