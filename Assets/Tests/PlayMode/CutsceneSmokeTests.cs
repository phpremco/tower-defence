using System.Collections;
using System.IO;
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
    /// The opening cutscene from Boot: run to its end with auto-advance (it hands over to the first level and marks
    /// the intro seen), skipped with two presses of the skip button in the third shot (same handoff), and replayed
    /// with a return to the main menu. The real save is restored afterwards, because the cutscene saves the intro as seen.
    /// </summary>
    public sealed class CutsceneSmokeTests
    {
        private const float Speed = 8f;
        private const float TimeoutSeconds = 120f;
        private const int SkipInShot = 3;

        private string _savePath;
        private byte[] _saveBytes;

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
        public IEnumerator AutoAdvance_RunsToTheFirstLevel()
        {
            yield return StartIntro(CutsceneHandoff.FirstCampaignLevel);

            yield return WaitForScene(SceneNames.Level);
            Assert.AreEqual(SceneNames.Level, SceneManager.GetActiveScene().name, "the cutscene hands over to the first level");
            yield return null;
            yield return null;
            Assert.IsTrue(Services.TryGet(out LevelContext context), "the level is built");
            var catalog = Services.Get<CampaignCatalog>();
            Assert.AreSame(CutsceneDefinition.FirstCampaignLevel(catalog), context.Level);
            Assert.AreSame(catalog.Story.OpeningCutscene.HandoffDifficulty, context.Difficulty);
            Assert.IsTrue(Services.Get<ProgressService>().Data.IntroSeen, "the intro counts as seen");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SkipInTheThirdShot_AlsoReachesTheFirstLevel()
        {
            yield return StartIntro(CutsceneHandoff.FirstCampaignLevel);
            var player = Object.FindAnyObjectByType<CutscenePlayer>();
            Assert.IsNotNull(player);
            float started = Time.realtimeSinceStartup;
            while (player.Sequencer.ShotIndex < SkipInShot && Time.realtimeSinceStartup - started < TimeoutSeconds)
            {
                yield return null;
            }

            Assert.AreEqual(SkipInShot, player.Sequencer.ShotIndex);
            Button skip = SkipButton();
            skip.onClick.Invoke();
            yield return null;
            Assert.IsFalse(player.Sequencer.IsFinished, "one press only arms the skip");
            skip.onClick.Invoke();
            Assert.IsTrue(player.Sequencer.IsFinished, "the second press skips");
            Assert.IsTrue(Services.Get<ProgressService>().Data.IntroSeen, "a skip counts as seen at once");

            yield return WaitForScene(SceneNames.Level);
            Assert.AreEqual(SceneNames.Level, SceneManager.GetActiveScene().name);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Replay_ReturnsToTheMainMenu()
        {
            yield return StartIntro(CutsceneHandoff.ReturnScene);
            var player = Object.FindAnyObjectByType<CutscenePlayer>();
            float started = Time.realtimeSinceStartup;
            while (player.Sequencer.ShotIndex < 1 && Time.realtimeSinceStartup - started < TimeoutSeconds)
            {
                yield return null;
            }

            Button skip = SkipButton();
            skip.onClick.Invoke();
            skip.onClick.Invoke();
            yield return WaitForScene(SceneNames.MainMenu);
            Assert.AreEqual(SceneNames.MainMenu, SceneManager.GetActiveScene().name, "a replay goes back where it came from");
            LogAssert.NoUnexpectedReceived();
        }

        // Boot first (services, the save), then the intro as if it were the first launch, sped up and unattended.
        private static IEnumerator StartIntro(CutsceneHandoff handoff)
        {
            // A menu score left playing by the test before would sound through Boot, which has no listener of its own.
            foreach (MenuMusic music in Object.FindObjectsByType<MenuMusic>())
            {
                Object.Destroy(music.gameObject);
            }

            yield return null;
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var progress = Services.Get<ProgressService>();
            progress.Data.IntroSeen = false;
            var catalog = Services.Get<CampaignCatalog>();
            Assert.IsNotNull(catalog.Story.OpeningCutscene, "run Tools/Generate/Scenes and Starter Data");
            Services.Register(new CutsceneRequest(catalog.Story.OpeningCutscene, handoff, SceneNames.MainMenu) { AutoAdvance = true, Speed = Speed });
            SceneManager.LoadScene(SceneNames.Intro);
            yield return null;
            yield return null;
            Assert.IsNotNull(Object.FindAnyObjectByType<CutscenePlayer>(), "the Intro scene plays the cutscene");
            Assert.IsFalse(Services.TryGet(out CutsceneRequest _), "the stage consumes the request, so a later visit never reuses it");
        }

        private static IEnumerator WaitForScene(string scene)
        {
            float started = Time.realtimeSinceStartup;
            while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup - started < TimeoutSeconds)
            {
                yield return null;
            }
        }

        private static Button SkipButton()
        {
            foreach (Button button in Object.FindObjectsByType<Button>())
            {
                if (button.name == "Skip")
                {
                    return button;
                }
            }

            Assert.Fail("the cutscene shows a skip button");
            return null;
        }
    }
}
