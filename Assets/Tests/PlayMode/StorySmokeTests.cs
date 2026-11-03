using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests
{
    /// <summary>
    /// The landing level with story on and the real dialogue box: the opening conversation shows in the box and holds
    /// the game, the box closes after the last line, and with auto-advance the level runs to its end without errors.
    /// A second run plays a test-only conversation with every text effect. The real save is restored afterwards,
    /// because finished dialogues are saved as seen.
    /// </summary>
    public sealed class StorySmokeTests
    {
        private const float SimulationSpeed = 20f;
        private const float TimeoutSeconds = 90f;
        private const float FadeTimeoutSeconds = 2f;
        private const int MediumDifficultyIndex = 1;
        private const int MaxWaitFrames = 300;
        private const int EffectFrames = 60;
        private const string StartDialogue = "Dlg_L01_Start";
        private const string SecondWaveDialogue = "Dlg_L01_Wave2";
        private const string WonDialogue = "Dlg_L01_Won";
        private const string BossDialogue = "Dlg_L01_Boss";

        private string _savePath;
        private byte[] _saveBytes;
        private List<string> _seenBefore;

        [SetUp]
        public void SetUp()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "Saves", "progress.json");
            _saveBytes = File.Exists(_savePath) ? File.ReadAllBytes(_savePath) : null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_seenBefore != null && Services.TryGet(out ProgressService progress))
            {
                progress.Data.SeenStoryIds.Clear();
                progress.Data.SeenStoryIds.AddRange(_seenBefore);
            }

            if (_saveBytes != null)
            {
                File.WriteAllBytes(_savePath, _saveBytes);
            }
            else if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
        }

        [UnityTest]
        public IEnumerator Landing_StoryInTheRealBox_RunsToTheEnd()
        {
            yield return LoadLanding();
            Assert.IsTrue(Services.TryGet(out LevelContext context));
            var view = Object.FindAnyObjectByType<DialogueView>();
            Assert.IsNotNull(view, "the HUD builds a dialogue box");
            Assert.AreSame(view, context.Dialogue.Presenter, "the HUD registers the box as presenter");

            context.StoryEnabled = true;
            for (int i = 0; i < MaxWaitFrames && !context.Dialogue.IsPlaying; i++)
            {
                yield return null;
            }

            Assert.IsTrue(context.Dialogue.IsPlaying, "the opening dialogue starts");
            Assert.AreEqual(StartDialogue, context.Dialogue.Current.Id);
            yield return null;
            Assert.IsTrue(view.IsShown, "the box is up");
            Assert.IsTrue(context.Held, "the box holds the game");
            Assert.AreEqual(PlainText(context.Dialogue.Current.Lines[0]), view.ShownText);

            // Let the typewriter run a little on unscaled time, then step through the conversation as a player would.
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            for (int i = 0; i < 20 && context.Dialogue.IsPlaying; i++)
            {
                context.Dialogue.Advance();
                yield return null;
            }

            Assert.IsFalse(context.Dialogue.IsPlaying);
            float closing = Time.realtimeSinceStartup;
            while (view.IsShown && Time.realtimeSinceStartup - closing < FadeTimeoutSeconds)
            {
                yield return null;
            }

            Assert.IsFalse(view.IsShown, "the box fades out after the last line");
            Assert.IsFalse(context.Held, "the hold is released");

            context.Dialogue.AutoAdvance = true;
            float started = Time.realtimeSinceStartup;
            while (context.Outcome == LevelOutcome.Running && Time.realtimeSinceStartup - started < TimeoutSeconds)
            {
                Time.timeScale = context.Held ? 0f : SimulationSpeed;
                yield return null;
            }

            Assert.AreNotEqual(LevelOutcome.Running, context.Outcome, "the level ends with story in the real box");
            var progress = Services.Get<ProgressService>();
            if (context.WaveNumber >= 2)
            {
                Assert.IsTrue(progress.HasSeenStory(SecondWaveDialogue), "the second wave's dialogue played");
            }

            // A won level plays its closing dialogue before the result screen.
            for (int i = 0; i < MaxWaitFrames && context.StoryBusy; i++)
            {
                yield return null;
            }

            Assert.IsFalse(context.StoryBusy);
            if (context.Outcome == LevelOutcome.Won)
            {
                Assert.IsTrue(progress.HasSeenStory(WonDialogue));
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EffectsFixture_RedactedStruckAndGlitch_RenderWithoutErrors()
        {
            yield return LoadLanding();
            Assert.IsTrue(Services.TryGet(out LevelContext context));
            var view = Object.FindAnyObjectByType<DialogueView>();
            DialogueDefinition fixture = EffectsFixture(context);
            context.Dialogue.Enqueue(fixture);
            for (int i = 0; i < MaxWaitFrames && !context.Dialogue.IsPlaying; i++)
            {
                yield return null;
            }

            Assert.AreSame(fixture, context.Dialogue.Current);
            for (int line = 0; line < fixture.Lines.Count; line++)
            {
                context.Dialogue.Advance();
                Assert.IsTrue(context.Dialogue.LineComplete);
                for (int i = 0; i < EffectFrames; i++)
                {
                    yield return null;
                }

                if (line == 0)
                {
                    Assert.Greater(ActiveRedactedBars(view), 0, "a redacted segment draws a bar");
                }

                context.Dialogue.Advance();
            }

            Assert.IsFalse(context.Dialogue.IsPlaying);
            Object.Destroy(fixture);
            LogAssert.NoUnexpectedReceived();
        }

        private IEnumerator LoadLanding()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var progress = Services.Get<ProgressService>();
            _seenBefore = new List<string>(progress.Data.SeenStoryIds);
            progress.Data.SeenStoryIds.Remove(StartDialogue);
            progress.Data.SeenStoryIds.Remove(SecondWaveDialogue);
            progress.Data.SeenStoryIds.Remove(BossDialogue);
            progress.Data.SeenStoryIds.Remove(WonDialogue);

            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[0].Level, catalog.Difficulties[MediumDifficultyIndex]));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;
        }

        // Test-only content, not shipped: a line with a redacted and a struck word, then a strongly glitching line.
        private static DialogueDefinition EffectsFixture(LevelContext context)
        {
            SpeakerDefinition speaker = context.Level.StoryTriggers[0].Dialogue.Lines[0].Speaker;
            var lines = new[]
            {
                new DialogueLine
                {
                    Id = "fx_1",
                    Speaker = speaker,
                    Segments = new[]
                    {
                        new DialogueSegment("Protocol ", SegmentStyle.Normal),
                        new DialogueSegment("Omega", SegmentStyle.Redacted),
                        new DialogueSegment(" is ", SegmentStyle.Normal),
                        new DialogueSegment("available", SegmentStyle.Struck),
                        new DialogueSegment(" <not> authorised.", SegmentStyle.Normal),
                    },
                },
                new DialogueLine
                {
                    Id = "fx_2",
                    Speaker = speaker,
                    GlitchIntensity = 1f,
                    Segments = new[] { new DialogueSegment("Disregard the last seventeen transmissions.", SegmentStyle.Normal) },
                },
            };

            var dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
            dialogue.name = "Test_EffectsFixture";
            typeof(DialogueDefinition).GetField("_lines", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(dialogue, lines);
            return dialogue;
        }

        private static int ActiveRedactedBars(DialogueView view)
        {
            int count = 0;
            foreach (RawImage image in view.GetComponentsInChildren<RawImage>())
            {
                if (image.name == "Redacted" && image.rectTransform.rect.width > 0f)
                {
                    count++;
                }
            }

            return count;
        }

        private static string PlainText(DialogueLine line)
        {
            var text = new System.Text.StringBuilder();
            foreach (DialogueSegment segment in line.Segments)
            {
                text.Append(segment.Text);
            }

            return text.ToString();
        }
    }
}
