using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Economy;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Plays the landing level with story switched on and a presenter that only records: the opening dialogue holds the
    /// game and blocks gameplay input, advancing releases it, and with auto-advance the level runs to its end.
    /// The real save is restored afterwards, because a finished dialogue is saved as seen.
    /// </summary>
    public sealed class StoryListenerTests
    {
        private const float SimulationSpeed = 20f;
        private const float TimeoutSeconds = 60f;
        private const int MediumDifficultyIndex = 1;
        private const int MaxWaitFrames = 300;
        private const string StartDialogue = "Dlg_L01_Start";
        private const string SecondWaveDialogue = "Dlg_L01_Wave2";

        private sealed class RecordingPresenter : IDialoguePresenter
        {
            public readonly List<string> Opened = new List<string>();

            public void Open(DialogueDefinition dialogue) => Opened.Add(dialogue.Id);

            public void ShowLine(DialogueLine line, int lineIndex)
            {
            }

            public void Reveal(int visibleCharacters, bool complete)
            {
            }

            public void Close()
            {
            }
        }

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
        public IEnumerator Landing_OpeningDialogueHoldsTheGame_AndTheLevelRunsToItsEnd()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var progress = Services.Get<ProgressService>();
            _seenBefore = new List<string>(progress.Data.SeenStoryIds);
            progress.Data.SeenStoryIds.Remove(StartDialogue);
            progress.Data.SeenStoryIds.Remove(SecondWaveDialogue);

            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[0].Level, catalog.Difficulties[MediumDifficultyIndex]));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;

            Assert.IsTrue(Services.TryGet(out LevelContext context), "LevelContext was never registered.");
            Assert.IsNotNull(context.Dialogue, "the level creates a dialogue player");
            Assert.IsFalse(context.StoryEnabled, "a session without briefing plays no story");

            var presenter = new RecordingPresenter();
            context.Dialogue.Presenter = presenter;
            context.StoryEnabled = true;
            for (int i = 0; i < MaxWaitFrames && !context.Dialogue.IsPlaying; i++)
            {
                yield return null;
            }

            Assert.IsTrue(context.Dialogue.IsPlaying, "the opening dialogue starts");
            Assert.AreEqual(StartDialogue, context.Dialogue.Current.Id);
            yield return null;
            Assert.IsTrue(context.Held, "a dialogue holds the game clock");
            Assert.IsTrue(context.InputBlocked);
            Assert.AreEqual(0f, Time.timeScale);
            int wave = context.WaveNumber;
            context.SkipWaveRequested.Raise();
            Assert.AreEqual(wave, context.WaveNumber, "skip wave is ignored during a dialogue");

            for (int i = 0; i < 20 && context.Dialogue.IsPlaying; i++)
            {
                context.Dialogue.Advance();
            }

            yield return null;
            Assert.IsFalse(context.Held, "the hold is released after the last line");
            Assert.IsTrue(progress.HasSeenStory(StartDialogue));

            context.Dialogue.AutoAdvance = true;
            float started = Time.realtimeSinceStartup;
            while (context.Outcome == LevelOutcome.Running && Time.realtimeSinceStartup - started < TimeoutSeconds)
            {
                // A dialogue's hold resets the speed to the player's setting, so the simulation speed is set every frame.
                Time.timeScale = context.Held ? 0f : SimulationSpeed;
                yield return null;
            }

            Assert.AreNotEqual(LevelOutcome.Running, context.Outcome, "the level ends with story on");
            if (context.WaveNumber >= 2)
            {
                CollectionAssert.Contains(presenter.Opened, SecondWaveDialogue);
            }

            LogAssert.NoUnexpectedReceived();
        }
    }
}
