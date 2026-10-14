using System.Collections;
using Game.Core;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>Plays levels from the Boot scene at high speed with no towers: nothing may log an error.</summary>
    public sealed class LevelSimulationTests
    {
        private const float SimulationSpeed = 20f;
        private const float TimeoutSeconds = 60f;
        private const int MediumDifficultyIndex = 1;
        private const int LevelCount = 5;

        [UnityTest]
        public IEnumerator Level1_WithoutTowers_IsLostWithoutErrors()
        {
            LevelContext context = null;
            yield return Play(0, found => context = found);

            Assert.IsNotNull(context, "LevelContext was never registered.");
            Assert.AreEqual(LevelOutcome.Lost, context.Outcome);
            Assert.IsNotNull(context.Hero, "Level 1 should have a hero.");
            Assert.AreEqual(context.Level.Blockades.Length + 1, context.HeroPostNames.Length, "Hero posts are every blockade plus the base.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EveryLevel_RunsWithoutErrors()
        {
            for (int i = 1; i < LevelCount; i++)
            {
                LevelContext context = null;
                yield return Play(i, found => context = found);

                Assert.IsNotNull(context, $"Level {i} never registered a LevelContext.");
                Assert.AreNotEqual(LevelOutcome.Running, context.Outcome, $"Level {i} did not finish within the time limit.");
                LogAssert.NoUnexpectedReceived();
            }
        }

        private static IEnumerator Play(int nodeIndex, System.Action<LevelContext> result)
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[nodeIndex].Level, catalog.Difficulties[MediumDifficultyIndex]));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;

            Time.timeScale = SimulationSpeed;
            float started = Time.realtimeSinceStartup;
            LevelContext context = null;

            while (Time.realtimeSinceStartup - started < TimeoutSeconds)
            {
                if (context == null && Services.TryGet(out LevelContext found))
                {
                    context = found;
                }

                if (context != null && context.Outcome != LevelOutcome.Running)
                {
                    break;
                }

                yield return null;
            }

            Time.timeScale = 1f;
            result(context);
        }
    }
}
