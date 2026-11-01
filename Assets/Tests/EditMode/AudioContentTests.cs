using Game.Core;
using Game.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>The recorded sounds as data: every set filled and measured, every species with a voice.</summary>
    public sealed class AudioContentTests
    {
        private const string LibraryPath = "Assets/_Project/Data/Audio/SfxLibrary.asset";
        private const string EnemiesFolder = "Assets/_Project/Data/Enemies";

        [Test]
        public void SoundSet_HandsOutVariantsInTurnAndNullWhenEmpty()
        {
            var empty = new SoundSet();
            int cursor = 0;
            Assert.IsTrue(empty.IsEmpty);
            Assert.IsNull(empty.Pick(ref cursor));
            Assert.AreEqual(0, cursor, "an empty set does not advance");
        }

        [Test]
        public void Library_EverySetIsFilledAndMeasured()
        {
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            Assert.IsNotNull(library, "generate the starter data first");
            foreach ((string label, SoundSet set) in library.Sets())
            {
                Assert.IsFalse(set.IsEmpty, $"{label} has no clips");
                Assert.That(set.Gain, Is.InRange(SoundSet.MinGain, SoundSet.MaxGain), label);
                for (int i = 0; i < set.Count; i++)
                {
                    Assert.IsNotNull(set[i], $"{label} variant {i} is missing");
                }
            }
        }

        [Test]
        public void Library_LoopsAreLongEnoughToLoop()
        {
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            Assert.IsNotNull(library);
            foreach (SoundSet loop in new[] { library.DroneFanLoops, library.CartLoops, library.FireballLoops, library.WaterLoops, library.LavaLoops, library.SaberHums })
            {
                Assert.Greater(loop[0].length, 1f, loop[0].name);
            }
        }

        [Test]
        public void EverySpecies_HasAVoiceThatMovesOrSteps()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemiesFolder }))
            {
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsNotNull(enemy.Voice, $"{enemy.name} has no voice");
                Assert.IsTrue(!enemy.Voice.MoveLoop.IsEmpty || !enemy.Voice.Steps.IsEmpty, $"{enemy.name} neither moves nor steps audibly");
                foreach ((string label, SoundSet set) in enemy.Voice.Sets())
                {
                    Assert.That(set.Gain, Is.InRange(SoundSet.MinGain, SoundSet.MaxGain), $"{enemy.name} {label}");
                }
            }
        }

        [Test]
        public void Library_ListsEveryVoiceOnce()
        {
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
            Assert.IsNotNull(library);
            var seen = new System.Collections.Generic.HashSet<CreatureVoice>();
            foreach (CreatureVoice voice in library.Voices)
            {
                Assert.IsNotNull(voice);
                Assert.IsTrue(seen.Add(voice), $"{voice.name} listed twice");
            }

            Assert.GreaterOrEqual(seen.Count, 20);
        }
    }
}
