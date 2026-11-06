using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Game.Bosses;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Fights Gravemaw in the developer boss arena, sped up, to its death: it enters through the colony rig and smashes it,
    /// dives under at each of its three thresholds, throws towers that survive the throw, and its body never stands on a
    /// tower's or a living gate's footprint except when it climbs onto a structure by design. Nothing may log an error.
    /// </summary>
    public sealed class GravemawArenaTests
    {
        // Every frame advances the fight by exactly this much game time (Time.captureDeltaTime), and the level's draws are
        // seeded: until round 20 the test ran at four times real time, so the editor's frame rate decided where the boss stood
        // at each threshold, which tower had hurt it most and so which burrow it planned (an intermittent "Burrowed: on ...").
        private const float StepSeconds = 1f / 15f;
        private const int Seed = 1;
        private const string ArgPath = "Tools/PlayTest/gravemaw_arg.txt";
        private const float TimeoutSeconds = 240f;
        private const int Towers = 10;

        // The test speeds the fight up: every second of game time the towers, in turn, add this share of the boss's health.
        private const float ExtraSharePerSecond = 0.012f;

        [UnityTest]
        public IEnumerator Gravemaw_FoughtToDeath_BurrowsThreeTimes_ThrowsTowersThatSurvive_NeverStandsOnAFootprint()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return null;
            yield return null;
            var catalog = Services.Get<CampaignCatalog>();
            BossArenaDefinition arena = catalog.BossArena;
            Assert.IsNotNull(arena, "no boss arena on the campaign");
            EnemyDefinition gravemaw = arena.Bosses[0];
            Services.Register(new LevelSession(arena.Level, arena.Difficulty, false, null, arena.WavesFor(gravemaw), arena.StartingMoney, Setting("seed", Seed)));
            SceneManager.LoadScene(SceneNames.Level);
            yield return null;
            yield return null;
            LevelContext context = null;
            while (context == null)
            {
                Services.TryGet(out context);
                yield return null;
            }

            List<Tower> built = Build(context, Towers);
            Assert.GreaterOrEqual(built.Count, Towers / 2, "too few towers could be built");
            context.SkipWaveRequested.Raise();
            Time.timeScale = 1f;
            Time.captureDeltaTime = Setting("step", StepSeconds);

            float started = Time.realtimeSinceStartup;
            ContinentBossBrain brain = null;
            Enemy boss = null;
            var overlaps = new List<string>();
            float sinceExtra = 0f;
            int turn = 0;
            while (Time.realtimeSinceStartup - started < TimeoutSeconds && context.Outcome == LevelOutcome.Running)
            {
                if (brain == null)
                {
                    brain = Object.FindAnyObjectByType<ContinentBossBrain>();
                    boss = brain != null ? brain.GetComponent<Enemy>() : null;
                }

                if (boss != null && boss.IsAlive)
                {
                    CheckFootprint(context, boss, brain, overlaps);
                    sinceExtra += Time.deltaTime;
                    if (sinceExtra >= 1f && !boss.IsConcealed && brain.State == "Road")
                    {
                        sinceExtra = 0f;
                        Tower source = built[turn++ % built.Count];
                        if (source.IsAlive)
                        {
                            boss.TakeDamageFrom(boss.Health.Max * ExtraSharePerSecond, source, false);
                        }
                    }
                }

                yield return null;
            }

            Time.captureDeltaTime = 0f;
            Assert.IsNotNull(brain, "Gravemaw never came");
            Assert.AreEqual(LevelOutcome.Won, context.Outcome, $"the fight did not end in a win (last state {brain.State}, health {boss.Health.Fraction:0.00})");
            Assert.AreEqual(3, brain.Burrows, "it dives under at 75, 50 and 25 percent");
            Assert.Greater(brain.Thrown.Count, 0, "it never threw a tower");
            foreach (Tower thrown in brain.Thrown)
            {
                Assert.IsTrue(thrown.IsAlive, $"{thrown.Definition.name} was wrecked by the throw");
            }

            Assert.IsEmpty(overlaps, string.Join("\n", overlaps));
            ColonyRigView rig = Object.FindAnyObjectByType<ColonyRigView>();
            Assert.IsTrue(rig == null || rig.Smashed, "the colony rig was not smashed by the entrance");
            LogAssert.NoUnexpectedReceived();
        }

        // A run cut off by the timeout must not leave the editor stepping in fixed game time.
        [TearDown]
        public void ResetTime()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
        }

        // An optional "step=S" or "seed=N" line in the arg file replays the fight with other steps or draws (Docs/BOSSES.md).
        private static float Setting(string key, float fallback)
        {
            if (!System.IO.File.Exists(ArgPath))
            {
                return fallback;
            }

            foreach (string line in System.IO.File.ReadAllLines(ArgPath))
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2 && parts[0].Trim() == key && float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                {
                    return value;
                }
            }

            return fallback;
        }

        private static int Setting(string key, int fallback) => (int)Setting(key, (float)fallback);

        // Its pivot inside a standing tower's cell, or a living gate's, while it is not climbing a structure by design.
        private static void CheckFootprint(LevelContext context, Enemy boss, ContinentBossBrain brain, List<string> overlaps)
        {
            if (brain.OnStructure || overlaps.Count > 5)
            {
                return;
            }

            GridMap grid = context.Level.Grid;
            Vector2Int cell = grid.WorldToCell(boss.Position);
            for (int i = 0; i < context.Structures.Count; i++)
            {
                if (!(context.Structures[i] is Tower tower))
                {
                    continue;
                }

                Vector2Int at = grid.WorldToCell(tower.Position);
                bool gate = tower.Definition.BlocksGroundEnemies;
                bool hit = gate ? tower.IsAlive && new List<Vector2Int>(context.Level.Road.Rung(at)).Contains(cell) : at == cell;
                if (hit)
                {
                    overlaps.Add($"{brain.State}: on {tower.Definition.name} at {cell}");
                }
            }
        }

        // The arena's money buys towers on free pads beside the road, spread out, the cheapest kinds first.
        private static List<Tower> Build(LevelContext context, int count)
        {
            var placement = Object.FindAnyObjectByType<PlacementController>();
            MethodInfo create = typeof(PlacementController).GetMethod("CreateTower", BindingFlags.NonPublic | BindingFlags.Instance);
            LevelDefinition level = context.Level;
            var grid = new BuildGrid(level);
            var road = new List<Vector2Int>(level.Road.Cells);
            var spots = new List<Vector2Int>();
            for (int x = 0; x < level.GridSize.x; x++)
            {
                for (int y = 0; y < level.GridSize.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (grid.CanBuild(cell) && road.Exists(r => Mathf.Abs(r.x - cell.x) <= 1 && Mathf.Abs(r.y - cell.y) <= 1))
                    {
                        spots.Add(cell);
                    }
                }
            }

            Vector2Int middle = level.MainRoad[level.MainRoad.Length / 3];
            spots.Sort((a, b) => Vector2Int.Distance(a, middle).CompareTo(Vector2Int.Distance(b, middle)));
            var built = new List<Tower>();
            var used = new List<Vector2Int>();
            foreach (Vector2Int cell in spots)
            {
                if (built.Count >= count)
                {
                    break;
                }

                if (used.Exists(u => Vector2Int.Distance(u, cell) < 2.5f))
                {
                    continue;
                }

                TowerDefinition kind = context.AvailableTowers[built.Count % context.AvailableTowers.Count];
                built.Add((Tower)create.Invoke(placement, new object[] { kind, cell, false }));
                used.Add(cell);
            }

            return built;
        }
    }
}
