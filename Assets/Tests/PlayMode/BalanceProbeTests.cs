using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// A simple bot plays every level on Easy and Medium and logs how it fares. Not a pass/fail balance check:
    /// it only fails on errors. The numbers in the log show where a level is far too easy or far too hard.
    /// </summary>
    public sealed class BalanceProbeTests
    {
        private const float SimulationSpeed = 20f;
        private const float TimeoutSeconds = 90f;
        private const float BuyIntervalSeconds = 1f;
        private const int LevelCount = 5;
        private const string FilterPath = "Tools/PlayTest/probe_arg.txt";

        [UnityTest]
        [NUnit.Framework.Timeout(1800000)]
        public IEnumerator GreedyBot_PlaysEveryLevel_WithoutErrors()
        {
            var report = new List<string>();
            for (int level = 0; level < LevelCount; level++)
            {
                for (int difficulty = 0; difficulty < 4; difficulty++)
                {
                    if (!Wanted("levels", level) || !Wanted("difficulties", difficulty))
                    {
                        continue;
                    }

                    SceneManager.LoadScene(SceneNames.Boot);
                    yield return null;
                    yield return null;

                    var catalog = Services.Get<CampaignCatalog>();
                    LevelDefinition definition = catalog.Nodes[level].Level;
                    Services.Register(new LevelSession(definition, catalog.Difficulties[difficulty]));
                    SceneManager.LoadScene(SceneNames.Level);
                    yield return null;
                    yield return null;

                    Time.timeScale = SimulationSpeed;
                    float started = Time.realtimeSinceStartup;
                    float nextBuy = 0f;
                    LevelContext context = null;
                    List<Vector2Int> spots = null;
                    int placed = 0;

                    while (Time.realtimeSinceStartup - started < TimeoutSeconds)
                    {
                        if (context == null && Services.TryGet(out LevelContext found))
                        {
                            context = found;
                            spots = RankBuildSpots(definition);
                        }

                        if (context != null)
                        {
                            if (context.Outcome != LevelOutcome.Running)
                            {
                                break;
                            }

                            nextBuy -= Time.deltaTime;
                            if (nextBuy <= 0f)
                            {
                                nextBuy = BuyIntervalSeconds;
                                placed += TryBuy(context, spots, placed) ? 1 : 0;
                            }
                        }

                        yield return null;
                    }

                    Time.timeScale = 1f;
                    Assert.IsNotNull(context);
                    LogAssert.NoUnexpectedReceived();
                    report.Add($"[Probe] {definition.DisplayName} on difficulty {difficulty}: {context.Outcome}, wave {context.WaveNumber}/{context.WaveCount}, base {context.BaseHealth.Current:0}/{context.BaseHealth.Max:0}, towers {placed}, money {context.Wallet.Money}");
                }
            }

            // Logged last: an earlier plain log would count as unexpected for the checks above.
            Debug.Log(string.Join(" | ", report));
        }

        // An optional filter file narrows a run, one "levels=0,1" or "difficulties=1,2" line each; no file means everything.
        private static bool Wanted(string key, int value)
        {
            if (!System.IO.File.Exists(FilterPath))
            {
                return true;
            }

            foreach (string line in System.IO.File.ReadAllLines(FilterPath))
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key)
                {
                    foreach (string item in parts[1].Split(','))
                    {
                        if (int.TryParse(item.Trim(), out int wanted) && wanted == value)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        // Cells beside the road, nearest first, so the bot always builds where towers can reach the path.
        private static List<Vector2Int> RankBuildSpots(LevelDefinition level)
        {
            var grid = level.Grid;
            var buildGrid = new BuildGrid(level);
            var road = new List<Vector2Int>(level.Road.Cells);
            var spots = new List<(Vector2Int Cell, float Score)>();

            for (int x = 0; x < grid.Size.x; x++)
            {
                for (int y = 0; y < grid.Size.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!buildGrid.CanBuild(cell))
                    {
                        continue;
                    }

                    float nearest = float.MaxValue;
                    foreach (Vector2Int roadCell in road)
                    {
                        nearest = Mathf.Min(nearest, Vector2Int.Distance(cell, roadCell));
                    }

                    // Prefer cells next to many road cells: they cover long stretches of the path.
                    int covered = 0;
                    foreach (Vector2Int roadCell in road)
                    {
                        if (Vector2Int.Distance(cell, roadCell) <= 2.5f)
                        {
                            covered++;
                        }
                    }

                    spots.Add((cell, nearest - covered * 0.2f));
                }
            }

            spots.Sort((a, b) => a.Score.CompareTo(b.Score));
            var result = new List<Vector2Int>();
            foreach (var spot in spots)
            {
                result.Add(spot.Cell);
            }

            return result;
        }

        private static bool TryBuy(LevelContext context, List<Vector2Int> spots, int placed)
        {
            if (placed >= spots.Count)
            {
                return false;
            }

            // Mix the standard towers in turn, the strongest affordable first.
            TowerDefinition best = null;
            for (int i = 0; i < context.AllTowers.Count; i++)
            {
                TowerDefinition candidate = context.AllTowers[i];
                if (candidate.IsSupport || !context.Wallet.CanAfford(candidate.BuildCost))
                {
                    continue;
                }

                if (best == null || (candidate.BuildCost > best.BuildCost && placed % 3 != 0))
                {
                    best = candidate;
                }
            }

            if (best == null || !context.Wallet.TrySpend(best.BuildCost))
            {
                return false;
            }

            Vector2Int cell = spots[placed];
            var go = new GameObject($"BotTower_{best.DisplayName}");
            go.transform.position = context.Level.Grid.CellToWorld(cell);
            var tower = go.AddComponent<Tower>();
            go.AddComponent<TowerView>();
            tower.Initialize(best, context.Targets, context.Projectiles, context.ModifiersFor(best));
            tower.Register(context.Structures);
            return true;
        }
    }
}

