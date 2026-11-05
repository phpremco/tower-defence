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
    /// Fights the Shattered in the developer arena (Fault Line with a sealed ruin gate), sped up, to its death: it breaks
    /// out of the ruin gate, splits at two thirds into three shards on three routes (its road, the second road, the air)
    /// with a third of its health each, the boss bar following what they have left; the flyer is shot down, and the two
    /// survivors pull back together into a giant that has 30% less than they had. Its body never stands on a tower outside
    /// its mechanic; nothing logs an error.
    /// </summary>
    public sealed class ShatteredArenaTests
    {
        private const float SimulationSpeed = 4f;
        private const float TimeoutSeconds = 300f;
        private const int Towers = 12;
        private const float ExtraSharePerSecond = 0.01f;
        private const float Tolerance = 0.02f;

        [UnityTest]
        public IEnumerator Shattered_FoughtToDeath_SplitsOnThreeRoutes_RejoinsWeakerForADeadShard()
        {
            var harness = new BossArenaHarness();
            yield return harness.Load("Shattered");
            LevelContext context = harness.Context;
            var rifle = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/PulseRifle.asset");
            var rocket = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Rocket.asset");
            var cannon = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Cannon.asset");
            List<Tower> built = harness.Build(new[] { rifle, rocket, cannon }, Towers, 0.3f);
            Assert.GreaterOrEqual(built.Count, Towers / 2, "too few towers could be built");
            context.SkipWaveRequested.Raise();
            Time.timeScale = SimulationSpeed;

            float started = Time.realtimeSinceStartup;
            ContinentBossBrain brain = null;
            Enemy boss = null;
            float sinceExtra = 0f;
            int turn = 0;
            bool fromRuins = false;
            bool flyerShot = false;
            bool barFollowed = true;
            float giantAtSplit = -1f;
            var shardHealth = new List<float>();
            float survivorsBeforeRejoin = 0f;
            while (Time.realtimeSinceStartup - started < TimeoutSeconds && context.Outcome == LevelOutcome.Running)
            {
                if (brain == null)
                {
                    brain = Object.FindAnyObjectByType<ContinentBossBrain>();
                    boss = brain != null ? brain.GetComponent<Enemy>() : null;
                    if (boss != null)
                    {
                        Vector3 gate = context.Level.Grid.CellToWorld(context.Level.BossEntrance.Cell);
                        fromRuins = Vector3.Distance(new Vector3(boss.Position.x, 0f, boss.Position.z), gate) < context.Level.CellSize;
                    }
                }

                if (boss != null && boss.IsAlive)
                {
                    var signature = (SplitSignature)brain.Signature;
                    harness.CheckFootprint(boss, brain);
                    BossRecord record = brain.Record;
                    if (record.Shards.Count > 0 && shardHealth.Count == 0)
                    {
                        giantAtSplit = boss.Health.Current;
                        foreach (Enemy shard in record.Shards)
                        {
                            shardHealth.Add(shard.Health.Max);
                        }
                    }

                    if (signature.IsSplit && brain.State == "Split")
                    {
                        float together = 0f;
                        foreach (Enemy shard in record.Shards)
                        {
                            together += shard.IsAlive ? shard.Health.Current : 0f;
                        }

                        barFollowed &= Mathf.Abs(boss.Health.Current - together) <= boss.Health.Max * Tolerance;
                        survivorsBeforeRejoin = together;
                        if (!flyerShot)
                        {
                            foreach (Enemy shard in record.Shards)
                            {
                                if (shard.IsAlive && shard.Definition.Flies)
                                {
                                    shard.TakeDamageFrom(shard.Health.Current + 1f, built[0], false);
                                    flyerShot = true;
                                }
                            }
                        }
                    }

                    sinceExtra += Time.deltaTime;
                    if (sinceExtra >= 1f && brain.State != "Entrance")
                    {
                        sinceExtra = 0f;
                        Tower source = built[turn++ % built.Count];
                        if (!boss.IsConcealed)
                        {
                            boss.TakeDamageFrom(boss.Health.Max * ExtraSharePerSecond, source, false);
                        }

                        foreach (Enemy shard in record.Shards)
                        {
                            if (shard.IsAlive && signature.IsSplit && !shard.IsDirected)
                            {
                                shard.TakeDamageFrom(shard.Health.Max * ExtraSharePerSecond * 3f, source, false);
                            }
                        }
                    }
                }

                yield return null;
            }

            Time.timeScale = 1f;
            Assert.IsNotNull(brain, "the Shattered never came");
            Assert.AreEqual(LevelOutcome.Won, context.Outcome, $"the fight did not end in a win (last state {brain.State}, health {boss.Health.Fraction:0.00})");
            BossRecord done = brain.Record;
            Assert.IsTrue(fromRuins, "it did not come out of the ruin gate");
            Assert.AreEqual(1, done.Splits, "it did not split once");
            Assert.AreEqual(3, done.Shards.Count, "it did not burst into three shards");
            var kinds = new HashSet<string>();
            foreach (Enemy shard in done.Shards)
            {
                kinds.Add(shard.Definition.name);
            }

            Assert.AreEqual(3, kinds.Count, "the three shards are not the runner, the crawler and the flyer");
            foreach (float health in shardHealth)
            {
                Assert.AreEqual(giantAtSplit / 3f, health, giantAtSplit * Tolerance, "a shard did not get a third of what the giant had left");
            }

            Assert.IsTrue(barFollowed, "the boss bar did not follow the shards' health");
            Assert.IsTrue(flyerShot && done.ShardsKilled >= 1, "the flyer shard was not killed");
            Assert.IsTrue(done.Rejoined, "the survivors did not pull back together");
            Assert.LessOrEqual(done.RejoinedShare * boss.Health.Max, survivorsBeforeRejoin * (1f - 0.3f * done.ShardsKilled) + boss.Health.Max * Tolerance, "a dead shard did not take its share off the giant");
            Assert.IsEmpty(harness.Overlaps, string.Join("\n", harness.Overlaps));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
