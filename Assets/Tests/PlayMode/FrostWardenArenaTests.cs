using System.Collections;
using System.Collections.Generic;
using Game.Bosses;
using Game.Enemies;
using Game.Levels;
using Game.PathObjects;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    /// <summary>
    /// Fights the Frost Warden in the developer arena (Quartz Flats with its pools frozen), sped up, to its death: it
    /// breaks out of a frozen lake, freezes the gates it reaches, climbs onto them and shatters them into wrecks (its rage
    /// rising), a hero's repair thaws one gate and saves it, a lava ball cracks its armour for double damage, its armour
    /// breaks below 40%, and its body never stands on a footprint except by its mechanic. Nothing may log an error.
    /// </summary>
    public sealed class FrostWardenArenaTests
    {
        private const float SimulationSpeed = 4f;
        private const float TimeoutSeconds = 300f;
        private const int Towers = 10;
        private const float ExtraSharePerSecond = 0.02f;

        [UnityTest]
        public IEnumerator FrostWarden_FoughtToDeath_FreezesAndShattersGates_ThawSaves_FireCracksArmour()
        {
            var harness = new BossArenaHarness();
            yield return harness.Load("FrostWarden");
            LevelContext context = harness.Context;
            var rifle = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/PulseRifle.asset");
            var cannon = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Project/Data/Towers/Cannon.asset");
            var lavaBall = AssetDatabase.LoadAssetAtPath<PathObjectDefinition>("Assets/_Project/Data/PathObjects/LavaBall.asset");
            List<Tower> built = harness.Build(new[] { rifle, cannon }, Towers, 0.5f);
            Assert.GreaterOrEqual(built.Count, Towers / 2, "too few towers could be built");
            context.SkipWaveRequested.Raise();
            Time.timeScale = SimulationSpeed;

            float started = Time.realtimeSinceStartup;
            ContinentBossBrain brain = null;
            Enemy boss = null;
            float sinceExtra = 0f;
            int turn = 0;
            bool rose = false;
            bool burned = false;
            bool doubled = false;
            Tower thawed = null;
            bool thawedSaved = true;
            bool thawChecked = false;
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
                    var breaker = (GateBreakerSignature)brain.Signature;

                    // The hero's repair on the first gate it climbs; the next one must shatter.
                    if (thawed == null && brain.State == "ClimbingGate" && brain.OnStructure)
                    {
                        thawed = FrozenGate(context);
                        thawed?.Restore(0.1f);
                    }

                    if (thawed != null && !thawChecked && brain.Record.GatesThawed > 0 && brain.State == "Road")
                    {
                        thawChecked = true;
                        thawedSaved = thawed.IsAlive && !thawed.IsFrozen;
                    }

                    // A lava ball rolls into it once while its armour holds.
                    if (!burned && brain.State == "Road" && breaker.Armour.Current == IceArmour.Stage.Armoured && !boss.IsConcealed)
                    {
                        burned = true;
                        boss.TakeDamageFrom(1f, lavaBall, true);
                        doubled = Mathf.Approximately(boss.DamageScale, boss.Definition.Boss.GateBreaker.CrackedFactor);
                    }

                    sinceExtra += Time.deltaTime;
                    // The test only speeds the fight up once it has met two gates (one thawed, one shattered).
                    if (sinceExtra >= 1f && !boss.IsConcealed && brain.State != "Entrance" && brain.Record.GatesFrozen >= 2)
                    {
                        sinceExtra = 0f;
                        Tower source = built[turn++ % built.Count];
                        boss.TakeDamageFrom(boss.Health.Max * ExtraSharePerSecond / Mathf.Max(0.2f, boss.DamageScale), source, false);
                    }
                }

                yield return null;
            }

            Time.timeScale = 1f;
            Assert.IsNotNull(brain, "the Frost Warden never came");
            Assert.AreEqual(LevelOutcome.Won, context.Outcome, $"the fight did not end in a win (last state {brain.State}, health {boss.Health.Fraction:0.00}, base {context.BaseHealth.Fraction:0.00}, frozen {brain.Record.GatesFrozen}, shattered {brain.Record.GatesShattered}, thawed {brain.Record.GatesThawed}, rose {rose}, at {boss.Position})");
            BossRecord record = brain.Record;
            Assert.IsTrue(rose, "it did not break out of a frozen lake");
            Assert.Greater(record.GatesFrozen, 0, "it froze no gate");
            Assert.Greater(record.GatesShattered, 0, "it shattered no gate");
            Assert.Greater(record.Rage, 0, "its rage never rose");
            Assert.IsTrue(burned && doubled, "a lava ball did not crack its armour for double damage");
            Assert.Greater(record.ArmourCracks, 0);
            Assert.IsTrue(record.ArmourBroken, "its armour never broke");
            Assert.LessOrEqual(record.MostFieldShare, BossDefinition.MaxFieldShare + 0.001f, "the freeze reached more than a quarter of the field");
            Assert.IsNotNull(thawed, "it never stood on a second frozen gate for the hero to thaw");
            Assert.Greater(record.GatesThawed, 0, "the thawed gate was not counted");
            Assert.IsTrue(thawChecked && thawedSaved, "the thawed gate was not saved");

            Assert.IsEmpty(harness.Overlaps, string.Join("\n", harness.Overlaps));
            LogAssert.NoUnexpectedReceived();
        }

        private static Tower FrozenGate(LevelContext context)
        {
            for (int i = 0; i < context.Structures.Count; i++)
            {
                if (context.Structures[i] is Tower tower && tower.IsFrozen)
                {
                    return tower;
                }
            }

            return null;
        }
    }
}
