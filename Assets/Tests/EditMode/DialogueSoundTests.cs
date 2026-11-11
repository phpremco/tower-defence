using Game.Core;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 43: the dialogue box sounds. The data (an opener, the cadence, six blips per speaker, no close sound) and the
    /// cadence as pure logic: a blip every two letters or digits, punctuation and spaces silent, no variant twice in a row,
    /// and nothing once the line jumps to its end.
    /// </summary>
    public sealed class DialogueSoundTests
    {
        private const string SfxLibraryPath = "Assets/_Project/Data/Audio/SfxLibrary.asset";
        private const string SpeakerFolder = "Assets/_Project/Data/Story/Speakers";
        private const int BlipsPerSpeaker = 6;
        private const int Every = 2;

        private static readonly string[] Speakers = { "Speaker_Mercer", "Speaker_Voss", "Speaker_Ora", "Speaker_Nti", "Speaker_Pioneer1" };

        [Test]
        public void TheLibrary_CarriesTheOpener_AndTheCadence()
        {
            var library = AssetDatabase.LoadAssetAtPath<SfxLibrary>(SfxLibraryPath);
            Assert.IsNotNull(library);
            DialogueSounds sounds = library.Dialogue;
            Assert.IsNotNull(sounds, "the library has no dialogue sounds: run Tools/Generate/Dialogue Sounds");
            Assert.AreEqual(1, sounds.Opener.Count, "one opener");
            Assert.AreEqual("open_static_squelch", sounds.Opener[0].name);
            Assert.AreEqual(Every, sounds.BlipEveryLetters);
            Assert.AreEqual(BlipsPerSpeaker, sounds.DefaultBlips.Count, "the default (ORA) blips");
            Assert.IsNull(typeof(DialogueSounds).GetProperty("Closer"), "Remco: no close sound");
        }

        [Test]
        public void EverySpeaker_HasSixBlipsOfItsOwnVoice()
        {
            string[] voices = { "spencer_", "voss_", "ora_", "nti_", "pioneer1_" };
            for (int i = 0; i < Speakers.Length; i++)
            {
                var speaker = AssetDatabase.LoadAssetAtPath<SpeakerDefinition>($"{SpeakerFolder}/{Speakers[i]}.asset");
                Assert.IsNotNull(speaker, Speakers[i]);
                Assert.AreEqual(BlipsPerSpeaker, speaker.TypingBlips.Count, $"{Speakers[i]} blips");
                for (int b = 0; b < speaker.TypingBlips.Count; b++)
                {
                    StringAssert.StartsWith(voices[i], speaker.TypingBlips[b].name, $"{Speakers[i]} types with another voice");
                }
            }
        }

        [Test]
        public void ABlip_FallsOnEverySecondLetter_AndPunctuationIsSilent()
        {
            var cadence = new TypingCadence(Every, 1);
            cadence.BeginLine(Line("Hi, you 42!"));
            // H i , _ y o u _ 4 2 !  -> letters at 0,1,4,5,6,8,9: blips on the 2nd (i), 4th (o), 6th (4).
            bool[] expected = { false, true, false, false, false, true, false, false, true, false, false };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], cadence.Reveal(i + 1, i + 1 == expected.Length), $"character {i + 1}");
            }
        }

        [Test]
        public void ASlowFrame_GivesOneBlip_NotABurst()
        {
            var cadence = new TypingCadence(Every, 1);
            cadence.BeginLine(Line("abcdefghij"));
            Assert.IsTrue(cadence.Reveal(8, false), "a hitch revealing eight letters");
            Assert.IsFalse(cadence.Reveal(9, false));
            Assert.IsTrue(cadence.Reveal(10, true));
        }

        [Test]
        public void JumpingToTheEnd_StopsTheBlips()
        {
            var cadence = new TypingCadence(Every, 1);
            cadence.BeginLine(Line("The ground is moving under us"));
            Assert.IsTrue(cadence.Reveal(2, false));
            Assert.IsFalse(cadence.Reveal(29, true), "confirm showed the rest: no blip");
            Assert.IsFalse(cadence.Reveal(29, true));
        }

        [Test]
        public void RedactedText_IsSilent()
        {
            var cadence = new TypingCadence(Every, 1);
            var line = new DialogueLine { Segments = new[] { new DialogueSegment("abcd", SegmentStyle.Redacted), new DialogueSegment("ef", SegmentStyle.Normal) } };
            cadence.BeginLine(line);
            for (int i = 1; i <= 5; i++)
            {
                Assert.IsFalse(cadence.Reveal(i, false), $"bar {i}");
            }

            Assert.IsTrue(cadence.Reveal(6, true));
        }

        [Test]
        public void TheOpener_PlaysPerSpeakerTurn_AndAfterTheBoxCloses()
        {
            var voss = ScriptableObject.CreateInstance<SpeakerDefinition>();
            var ora = ScriptableObject.CreateInstance<SpeakerDefinition>();
            var cadence = new TypingCadence(Every, 1);
            Assert.IsTrue(cadence.BeginLine(Line("a", voss)), "a fresh box");
            Assert.IsFalse(cadence.BeginLine(Line("b", voss)), "the same speaker goes on in the box");
            Assert.IsTrue(cadence.BeginLine(Line("c", ora)), "another speaker's turn");
            cadence.BoxClosed();
            Assert.IsTrue(cadence.BeginLine(Line("d", ora)), "the box opened again");
            Object.DestroyImmediate(voss);
            Object.DestroyImmediate(ora);
        }

        [Test]
        public void AVariant_NeverRepeatsBackToBack()
        {
            var cadence = new TypingCadence(Every, 3);
            int last = -1;
            for (int i = 0; i < 500; i++)
            {
                int next = cadence.NextVariant(BlipsPerSpeaker);
                Assert.That(next, Is.InRange(0, BlipsPerSpeaker - 1));
                Assert.AreNotEqual(last, next, $"pick {i} repeats");
                last = next;
            }
        }

        private static DialogueLine Line(string text, SpeakerDefinition speaker = null) => new DialogueLine { Speaker = speaker, Segments = new[] { new DialogueSegment(text, SegmentStyle.Normal) } };
    }
}
