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
    /// Fights the Cinder Matriarch in the developer arena (Crysium Vein with its pools turned to lava), sped up, to her
    /// death: she rises out of a lava pool, hunts the cold towers off the road and smashes them into wrecks within the
    /// fairness limits (one per approach, never more than a quarter), leaves a lava trail, lays eggs below half her health,
    /// and never stands on a tower's footprint. Nothing may log an error.
    /// </summary>
    public sealed class CinderMatriarchArenaTests
    {
        private const float SimulationSpeed = 4f;
        private const float TimeoutSeconds = 300f;
        private const int Towers = 12;
        private const float ExtraSharePerSecond = 0.008f;

        [UnityTest]
        public IEnumerator CinderMatriarch_FoughtToDeath_HuntsTheCold_SmashesWithinTheLimits_LaysEggs()
        {
            var harness = new BossArenaHarness();
            yield return harness.Load("CinderMatriarch");
            LevelContext context = harness.Context;
            var cryo = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Cryo.asset");
            var cannon = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Cannon.asset");
            var rifle = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/PulseRifle.asset");
            List<Tower> built = harness.Build(new[] { rifle, cannon, cryo, rifle }, Towers, 0.35f);
            Assert.GreaterOrEqual(built.Count, Towers / 2, "too few towers could be built");
            context.SkipWaveRequested.Raise();
            Time.timeScale = SimulationSpeed;

            float started = Time.realtimeSinceStartup;
            ContinentBossBrain brain = null;
            Enemy boss = null;
            float sinceExtra = 0f;
            int turn = 0;
            bool rose = false;
            bool trailed = false;
            var wreckedAtSmash = new List<bool>();
            while (Time.realtimeSinceStartup - started < TimeoutSeconds && context.Outcome == LevelOutcome.Running)
            {
                if (brain == null)
                {
                    brain = Object.FindAnyObjectByType<ContinentBossBrain>();
                    boss = brain != null ? brain.GetComponent<Enemy>() : null;
                }

                if (boss != null && boss.IsAlive)
                {
                    rose |= brain.State == "Bursting";
                    harness.CheckFootprint(boss, brain);
                    var hunt = (HuntSignature)brain.Signature;
                    for (int i = 0; i < hunt.Trail.Capacity && !trailed; i++)
                    {
                        trailed |= hunt.Trail.IsLive(i);
                    }

                    while (wreckedAtSmash.Count < brain.Record.Smashed.Count)
                    {
                        wreckedAtSmash.Add(brain.Record.Smashed[wreckedAtSmash.Count].IsWrecked);
                    }

                    sinceExtra += Time.deltaTime;
                    if (sinceExtra >= 1f && !boss.IsConcealed && brain.State != "Entrance" && brain.State != "Brooding" && brain.State != "LayingEggs")
                    {
                        sinceExtra = 0f;
                        Tower source = built[turn++ % built.Count];
                        boss.TakeDamageFrom(boss.Health.Max * ExtraSharePerSecond, source, false);
                    }
                }

                yield return null;
            }

            Time.timeScale = 1f;
            Assert.IsNotNull(brain, "the Cinder Matriarch never came");
            Assert.AreEqual(LevelOutcome.Won, context.Outcome, $"the fight did not end in a win (last state {brain.State}, health {boss.Health.Fraction:0.00})");
            BossRecord record = brain.Record;
            Assert.IsTrue(rose, "she did not rise out of a lava pool");
            Assert.Greater(record.Hunts, 0, "she never hunted a cold tower");
            Assert.Greater(record.Smashed.Count, 0, "she never smashed a cold tower");
            Assert.LessOrEqual(record.Smashed.Count, ColdHunt.SmashCap(built.Count), "more than a quarter of the towers smashed");
            Assert.LessOrEqual(record.MostFieldShare, BossDefinition.MaxFieldShare + 0.001f, "one effect reached more than a quarter of the field");
            foreach (Tower smashed in record.Smashed)
            {
                Assert.IsTrue(smashed.Definition == cryo, $"she smashed a {smashed.Definition.name}, not a cold tower");
            }

            CollectionAssert.DoesNotContain(wreckedAtSmash, false, "a smashed tower was not a wreck");
            Assert.IsTrue(trailed, "she left no lava trail");
            Assert.Greater(record.EggsLaid, 0, "she laid no eggs below half her health");
            Assert.IsEmpty(harness.Overlaps, string.Join("\n", harness.Overlaps));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
