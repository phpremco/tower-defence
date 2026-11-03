using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Economy;
using Game.Levels;
using Game.PathObjects;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// The S4 story hooks in real levels: the first flyers of Shard Ridge (C1_03) are announced when the first Skyray
    /// appears, and the first Chrono Field placed on Crysium Vein (C1_06) starts its FirstUse dialogue. The real save is
    /// restored afterwards, because finished dialogues are saved as seen.
    /// </summary>
    public sealed class StoryCrystalTests
    {
        private const float SimulationSpeed = 20f;
        private const float TimeoutSeconds = 90f;
        private const int EasyDifficultyIndex = 0;
        private const int ShardRidgeNode = 2;
        private const int CrysiumVeinNode = 5;
        private const int SkipsPerFrame = 1;
        private const string AirDialogue = "Dlg_L03_Air";
        private const string ChronoDialogue = "Dlg_L06_Chrono";

        private static readonly string[] Reset = { "Dlg_L03_Start", AirDialogue, "Dlg_L03_Won", "Dlg_L06_Start", "Dlg_L06_Mine", ChronoDialogue, "Dlg_L06_Fast" };

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
        public IEnumerator ShardRidge_FirstSkyray_IsAnnounced()
        {
            yield return Load(ShardRidgeNode);
            Assert.IsTrue(Services.TryGet(out LevelContext context));
            var progress = Services.Get<ProgressService>();
            context.StoryEnabled = true;
            context.Dialogue.AutoAdvance = true;

            float started = Time.realtimeSinceStartup;
            while (!progress.HasSeenStory(AirDialogue) && context.Outcome == LevelOutcome.Running && Time.realtimeSinceStartup - started < TimeoutSeconds)
            {
                Time.timeScale = context.Held ? 0f : SimulationSpeed;
                // Call the waves early until the first air raid is on the field; the story is what is under test.
                for (int i = 0; i < SkipsPerFrame && !context.Held && context.SkipWaveAllowed; i++)
                {
                    context.SkipWaveRequested.Raise();
                }

                yield return null;
            }

            Assert.IsTrue(progress.HasSeenStory(AirDialogue), $"the first Skyray is announced (wave {context.WaveNumber}, outcome {context.Outcome})");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CrysiumVein_FirstChronoField_StartsItsDialogue()
        {
            yield return Load(CrysiumVeinNode);
            Assert.IsTrue(Services.TryGet(out LevelContext context));
            var progress = Services.Get<ProgressService>();
            context.StoryEnabled = true;
            context.Dialogue.AutoAdvance = true;

            PathObjectDefinition chrono = null;
            foreach (StoryTrigger trigger in context.Level.StoryTriggers)
            {
                if (trigger.Kind == StoryTriggerKind.FirstUse && trigger.Dialogue != null && trigger.Dialogue.Id == ChronoDialogue)
                {
                    chrono = trigger.PathObject;
                }
            }

            Assert.IsNotNull(chrono, "Crysium Vein has a FirstUse trigger for the Chrono Field");
            var go = new GameObject("TestChronoField");
            var field = go.AddComponent<PathObject>();
            field.Initialize(chrono, context.Targets, context.Level.Road.Route(0), 1);
            context.PathObjectSpawned.Raise(field);

            float started = Time.realtimeSinceStartup;
            while (!progress.HasSeenStory(ChronoDialogue) && Time.realtimeSinceStartup - started < TimeoutSeconds)
            {
                Time.timeScale = context.Held ? 0f : 1f;
                yield return null;
            }

            Assert.IsTrue(progress.HasSeenStory(ChronoDialogue), "placing the first Chrono Field starts its dialogue");
            Object.Destroy(go);
            LogAssert.NoUnexpectedReceived();
        }

        private IEnumerator Load(int node)
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var progress = Services.Get<ProgressService>();
            _seenBefore = new List<string>(progress.Data.SeenStoryIds);
            foreach (string id in Reset)
            {
                progress.Data.SeenStoryIds.Remove(id);
            }

            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[node].Level, catalog.Difficulties[EasyDifficultyIndex]));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;
        }
    }
}
