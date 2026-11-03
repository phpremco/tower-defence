using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Plays the levels of every continent that has levels, from the Boot scene at high speed with no towers: nothing may
    /// log an error. The smoke run (default) plays each level on Easy; the full run (category "Full", only on request)
    /// plays each level on every difficulty.
    /// </summary>
    public sealed class ContinentSimulationTests
    {
        private const float SimulationSpeed = 20f;
        private const float TimeoutSeconds = 60f;
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";

        /// <summary>The regions that hold at least one level, read from the generated catalog.</summary>
        public static IEnumerable<int> FilledRegions()
        {
#if UNITY_EDITOR
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            if (catalog != null)
            {
                for (int r = 0; r < catalog.Regions.Count; r++)
                {
                    if (LevelsOf(catalog, r).Count > 0)
                    {
                        yield return r;
                    }
                }

                yield break;
            }
#endif
            yield return 0;
        }

        [UnityTest]
        public IEnumerator Smoke_EveryLevelOnEasy_RunsWithoutErrors([ValueSource(nameof(FilledRegions))] int region)
        {
            yield return PlayRegion(region, new[] { 0 });
        }

        [UnityTest]
        [Category("Full")]
        [Explicit("Every level on every difficulty; run on request with the Full category.")]
        public IEnumerator Full_EveryLevelOnEveryDifficulty_RunsWithoutErrors([ValueSource(nameof(FilledRegions))] int region)
        {
            yield return PlayRegion(region, new[] { 0, 1, 2, 3 });
        }

        // Without towers the first level is lost, the commander stands at every gate and at the base, and nothing errs.
        [UnityTest]
        public IEnumerator FirstLevel_WithoutTowers_IsLostWithoutErrors()
        {
            LevelContext context = null;
            yield return Play(0, 1, found => context = found);

            Assert.IsNotNull(context, "LevelContext was never registered.");
            Assert.AreEqual(LevelOutcome.Lost, context.Outcome);
            Assert.IsNotNull(context.Hero, "Level 1 should have a hero.");
            Assert.AreEqual(context.Level.Blockades.Length + 1, context.HeroPostNames.Length, "Hero posts are every blockade plus the base.");
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator PlayRegion(int region, int[] difficulties)
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;
            List<int> nodes = LevelsOf(Services.Get<CampaignCatalog>(), region);
            Assert.Greater(nodes.Count, 0, $"region {region} has no levels");
            foreach (int node in nodes)
            {
                foreach (int difficulty in difficulties)
                {
                    LevelContext context = null;
                    yield return Play(node, difficulty, found => context = found);

                    Assert.IsNotNull(context, $"Node {node} never registered a LevelContext.");
                    Assert.AreNotEqual(LevelOutcome.Running, context.Outcome, $"{context.Level.name} on difficulty {difficulty} did not finish within the time limit.");
                    LogAssert.NoUnexpectedReceived();
                }
            }
        }

        private static List<int> LevelsOf(CampaignCatalog catalog, int region)
        {
            var nodes = new List<int>();
            for (int i = 0; i < catalog.Nodes.Count; i++)
            {
                if (catalog.Nodes[i].Region == region && catalog.Nodes[i].Level != null)
                {
                    nodes.Add(i);
                }
            }

            return nodes;
        }

        private static IEnumerator Play(int nodeIndex, int difficulty, System.Action<LevelContext> result)
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;

            var catalog = Services.Get<CampaignCatalog>();
            Services.Register(new LevelSession(catalog.Nodes[nodeIndex].Level, catalog.Difficulties[difficulty]));
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
