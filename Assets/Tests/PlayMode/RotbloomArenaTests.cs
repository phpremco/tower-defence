using System.Collections;
using System.Collections.Generic;
using Game.Bosses;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Fights Rotbloom in the developer arena (Glass Meadows with its pools turned to bog), sped up, to its death: its roots
    /// grow over the three short waves before it, it grows out of the bog, spores the fullest cluster of towers within the
    /// quarter cap (a pod shot down in the air spares its pad, the hero's repair burns a pad clean, nothing is built on an
    /// overgrown pad), and below half its health it uproots and walks. Its body never stands on a tower; nothing logs an error.
    /// </summary>
    public sealed class RotbloomArenaTests
    {
        private const float SimulationSpeed = 4f;
        private const float TimeoutSeconds = 300f;
        private const int Towers = 12;
        private const float ExtraSharePerSecond = 0.008f;

        [UnityTest]
        public IEnumerator Rotbloom_FoughtToDeath_RootsAnnounceIt_SporesWithinTheCap_Uproots()
        {
            var harness = new BossArenaHarness();
            yield return harness.Load("Rotbloom");
            LevelContext context = harness.Context;
            var rifle = Tower("PulseRifle");
            var rocket = Tower("Rocket");
            var cannon = Tower("Cannon");
            var laser = Tower("Laser");
            List<Tower> built = harness.Build(new[] { rifle, rocket, rifle, laser, cannon }, Towers, 0.3f, 2f);
            Assert.GreaterOrEqual(built.Count, Towers / 2, "too few towers could be built");
            context.SkipWaveRequested.Raise();
            Time.timeScale = SimulationSpeed;

            float started = Time.realtimeSinceStartup;
            ContinentBossBrain brain = null;
            Enemy boss = null;
            var director = Object.FindAnyObjectByType<BossDirector>();
            int stageBeforeBoss = -1;
            float sinceExtra = 0f;
            int turn = 0;
            bool rose = false;
            bool podShot = false;
            bool padRepaired = false;
            bool closedMatches = true;
            while (Time.realtimeSinceStartup - started < TimeoutSeconds && context.Outcome == LevelOutcome.Running)
            {
                if (brain == null)
                {
                    RootsView roots = director.Omen != null ? director.Omen.RootsOf(harness.Boss) : null;
                    stageBeforeBoss = roots != null ? roots.Stage : -1;
                    brain = Object.FindAnyObjectByType<ContinentBossBrain>();
                    boss = brain != null ? brain.GetComponent<Enemy>() : null;
                }

                if (boss != null && boss.IsAlive)
                {
                    var signature = (OvergrowthSignature)brain.Signature;
                    rose |= brain.State == "Bursting";
                    harness.CheckFootprint(boss, brain);
                    closedMatches &= context.ClosedPads.Count == signature.Pads.Count;
                    podShot |= ShootOnePod(podShot);
                    padRepaired |= RepairOneTower(signature, context, padRepaired);
                    sinceExtra += Time.deltaTime;
                    if (sinceExtra >= 1f && !boss.IsConcealed && brain.State != "Entrance")
                    {
                        sinceExtra = 0f;
                        boss.TakeDamageFrom(boss.Health.Max * ExtraSharePerSecond, built[turn++ % built.Count], false);
                    }
                }

                yield return null;
            }

            Time.timeScale = 1f;
            Assert.IsNotNull(brain, "Rotbloom never came");
            Assert.AreEqual(LevelOutcome.Won, context.Outcome, $"the fight did not end in a win (last state {brain.State}, health {boss.Health.Fraction:0.00})");
            BossRecord record = brain.Record;
            Assert.AreEqual(2, stageBeforeBoss, "its roots had not grown to their last stage before it came");
            Assert.IsTrue(rose, "it did not grow out of the bog");
            Assert.Greater(record.Volleys, 0, "it never fired a spore volley");
            Assert.Greater(record.PadsOvergrown, 0, "no pad was overgrown");
            Assert.LessOrEqual(record.MostFieldShare, BossDefinition.MaxFieldShare + 0.001f, "more than a quarter of the towers overgrown at once");
            Assert.IsTrue(podShot && record.SporesShot > 0, "no spore pod was shot down in the air");
            Assert.IsTrue(padRepaired && record.PadsCleaned > 0, "the hero's repair burned no pad clean");
            Assert.IsTrue(closedMatches, "the closed pads and the overgrown pads disagreed");
            Assert.IsTrue(record.Uprooted, "it did not uproot below half its health");
            Assert.IsEmpty(context.ClosedPads, "its overgrowth outlived it");
            foreach (Tower tower in built)
            {
                Assert.IsFalse(tower != null && tower.IsOvergrown, "a tower stayed overgrown after it died");
            }

            Assert.IsEmpty(harness.Overlaps, string.Join("\n", harness.Overlaps));
            LogAssert.NoUnexpectedReceived();
        }

        // Plays an air tower that brings down the first pod it sees in flight.
        private static bool ShootOnePod(bool done)
        {
            if (done)
            {
                return false;
            }

            foreach (Enemy enemy in Object.FindObjectsByType<Enemy>())
            {
                if (enemy.IsAlive && enemy.Definition.name == "RotSporePod" && enemy.Position.y > 1f)
                {
                    enemy.TakeDamageFrom(enemy.Health.Current + 1f, null, false);
                    return true;
                }
            }

            return false;
        }

        // Plays the hero's repair on the first overgrown tower: the vines burn off and the pad is free again.
        private static bool RepairOneTower(OvergrowthSignature signature, LevelContext context, bool done)
        {
            if (done || signature.OvergrownTowers == 0)
            {
                return false;
            }

            foreach (Tower tower in Object.FindObjectsByType<Tower>())
            {
                if (tower.IsOvergrown)
                {
                    Vector2Int cell = context.Level.Grid.WorldToCell(tower.Position);
                    Assert.IsTrue(context.ClosedPads.Contains(cell), "an overgrown pad is open for building");
                    tower.Restore(0.1f);
                    Assert.IsFalse(tower.IsOvergrown, "the repair left the vines on");
                    Assert.IsFalse(context.ClosedPads.Contains(cell), "the repaired pad stayed closed");
                    return true;
                }
            }

            return false;
        }

        private static TowerDefinition Tower(string id) => AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{id}.asset");
    }
}
