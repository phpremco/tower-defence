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
    /// Fights the Sealed One in the developer arena (Shard Ridge, gates with guns), sped up, to its release: it rises out
    /// of the rift at the centre, learns at each phase change the tower type that hurt it most in the phase before (ORA says
    /// so, the boss bar knows it) and takes only half from that type, never nothing; each phase drains exactly one gate's
    /// gun (the gate still blocks) and gives it back. Its body never stands on a tower outside its mechanic; nothing logs
    /// an error.
    /// </summary>
    public sealed class SealedOneArenaTests
    {
        private const float SimulationSpeed = 4f;
        private const float TimeoutSeconds = 300f;
        private const int Towers = 12;
        private const float ExtraSharePerSecond = 0.013f;
        private const float Probe = 100f;

        [UnityTest]
        public IEnumerator SealedOne_FoughtToItsRelease_LearnsTheTopWeapon_DrainsOneGatePerPhase()
        {
            var harness = new BossArenaHarness();
            yield return harness.Load("SealedOne");
            LevelContext context = harness.Context;
            var kinds = new[] { Tower("Cannon"), Tower("PulseRifle"), Tower("Laser"), Tower("Rocket") };
            List<Tower> built = harness.Build(kinds, Towers, 0.4f);
            Assert.GreaterOrEqual(built.Count, Towers / 2, "too few towers could be built");
            var gates = new List<Tower>();
            foreach (Tower tower in Object.FindObjectsByType<Tower>())
            {
                if (tower.Definition.BlocksGroundEnemies && !tower.Definition.IsBaseDefence)
                {
                    // Gates only have a gun from their second level: the drain needs a gun to take.
                    tower.SetLevel(tower.Definition.MaxLevelIndex);
                    gates.Add(tower);
                }
            }

            Assert.Greater(gates.Count, 0, "the arena has no gates");
            var lines = new List<string>();
            context.BossAdapted.Subscribe(lines.Add);
            context.SkipWaveRequested.Raise();
            Time.timeScale = SimulationSpeed;

            float started = Time.realtimeSinceStartup;
            ContinentBossBrain brain = null;
            Enemy boss = null;
            float sinceExtra = 0f;
            int mostDrained = 0;
            bool drainedStillBlocks = true;
            bool halfChecked = false;
            bool rose = false;
            while (Time.realtimeSinceStartup - started < TimeoutSeconds && context.Outcome == LevelOutcome.Running)
            {
                if (brain == null)
                {
                    brain = Object.FindAnyObjectByType<ContinentBossBrain>();
                    boss = brain != null ? brain.GetComponent<Enemy>() : null;
                }

                if (boss != null && boss.IsAlive)
                {
                    harness.CheckFootprint(boss, brain);
                    rose |= brain.State == "Rising";
                    int drained = 0;
                    foreach (Tower gate in gates)
                    {
                        drained += gate.IsDrained ? 1 : 0;
                        drainedStillBlocks &= !gate.IsDrained || !gate.IsAlive || gate.BlocksGround || gate.IsBridged;
                    }

                    mostDrained = Mathf.Max(mostDrained, drained);
                    if (!halfChecked && context.BossDampened != null && !boss.IsConcealed)
                    {
                        halfChecked = true;
                        CheckHalfDamage(boss, built, context.BossDampened);
                    }

                    sinceExtra += Time.deltaTime;
                    if (sinceExtra >= 1f && !boss.IsConcealed && brain.State != "Entrance")
                    {
                        sinceExtra = 0f;
                        // Each phase one weapon type does the most damage, so each phase it learns that type.
                        int phase = harness.Boss.Boss.PhaseAt(boss.Health.Fraction);
                        Tower source = SourceOf(built, kinds[phase % kinds.Length]);
                        boss.TakeDamageFrom(boss.Health.Max * ExtraSharePerSecond / boss.DamageScale / ScaleFor(boss, source), source, false);
                    }
                }

                yield return null;
            }

            Time.timeScale = 1f;
            context.BossAdapted.Unsubscribe(lines.Add);
            Assert.IsNotNull(brain, "the Sealed One never came");
            Assert.AreEqual(LevelOutcome.Won, context.Outcome, $"the fight did not end in a win (last state {brain.State}, health {boss.Health.Fraction:0.00})");
            BossRecord record = brain.Record;
            Assert.IsTrue(rose && record.RoseFromRift, "it did not rise out of the rift");
            Assert.AreEqual(kinds.Length - 1, record.Learned.Count, "it did not learn a weapon at every phase change");
            for (int i = 0; i < record.Learned.Count; i++)
            {
                Assert.AreSame(kinds[i], record.Learned[i], $"phase {i + 1}: it learned the wrong weapon");
            }

            Assert.AreEqual(record.Learned.Count, lines.Count, "ORA did not name every learned weapon");
            StringAssert.Contains(kinds[0].DisplayName, lines[0]);
            Assert.IsTrue(halfChecked, "the half damage was never checked");
            Assert.GreaterOrEqual(record.Drained.Count, 2, "it did not drain a gate each phase");
            Assert.AreEqual(1, mostDrained, "more than one gate was drained at once");
            Assert.IsTrue(drainedStillBlocks, "a drained gate stopped blocking");
            foreach (Tower gate in gates)
            {
                Assert.IsFalse(gate.IsDrained, "a gate stayed drained after the fight");
            }

            Assert.IsNull(context.BossDampened, "the learned weapon outlived the boss");
            Assert.IsEmpty(harness.Overlaps, string.Join("\n", harness.Overlaps));
            LogAssert.NoUnexpectedReceived();
        }

        // A hit from the learned type lands half, a hit from any other type in full.
        private static void CheckHalfDamage(Enemy boss, List<Tower> built, TowerDefinition learned)
        {
            Tower learnedTower = SourceOf(built, learned);
            Tower other = null;
            foreach (Tower tower in built)
            {
                if (tower.Definition != learned)
                {
                    other = tower;
                    break;
                }
            }

            float before = boss.Health.Current;
            boss.TakeDamageFrom(Probe, learnedTower, false);
            float learnedHit = before - boss.Health.Current;
            before = boss.Health.Current;
            boss.TakeDamageFrom(Probe, other, false);
            float otherHit = before - boss.Health.Current;
            Assert.AreEqual(otherHit * 0.5f, learnedHit, 0.01f, "the learned weapon did not land half its damage");
            Assert.Greater(learnedHit, 0f, "the learned weapon did nothing: that is immunity");
        }

        private static float ScaleFor(Enemy boss, Tower source) => boss.SourceScale != null ? Mathf.Max(0.01f, boss.SourceScale(source)) : 1f;

        private static Tower SourceOf(List<Tower> built, TowerDefinition kind)
        {
            foreach (Tower tower in built)
            {
                if (tower.Definition == kind)
                {
                    return tower;
                }
            }

            return built[0];
        }

        private static TowerDefinition Tower(string id) => AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{id}.asset");
    }
}
