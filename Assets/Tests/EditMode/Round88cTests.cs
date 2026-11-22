using System.Collections.Generic;
using System.Linq;
using Game.Balance;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Round 88c, the rules that came out of Remco's playtest of round 88b: the ceiling on a late peak
    /// (<see cref="BalanceRules.WaveCeiling"/>), the first air raid announced (<see cref="FirstRaidRule"/>; its coins are start money since round 88d, <see cref="StartMoneyRule"/>),
    /// a brood that runs ahead of its boss (<see cref="EscortDash"/>), and the win rule read as Remco asked it ("every
    /// enemy needs to be dead").
    /// </summary>
    public sealed class Round88cTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string EnemyFolder = "Assets/_Project/Data/Enemies";

        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        // ---- the ceiling on place x kind x peak ------------------------------------------------------------------

        [Test]
        public void ALatePeak_IsCutToTheCeiling_AnEarlyOneIsNot_AndTheBossReliefComesAfter()
        {
            var rules = new BalanceRules { DifficultyScale = new[] { 1f }, RampStart = 0.5f, RampEnd = 2f, HeavyFactor = 1.2f, PeakFactor = 1.5f, BossCrowdFactor = 0.5f, WaveCeiling = 1.8f };
            // The middle of the ramp is one: a heavy peak there is 1.2 x 1.5 = 1.8, exactly at the ceiling.
            Assert.AreEqual(1.8f, WaveFormula.WalkIn(rules, 0, 1f, 0.5f, WaveComposer.Kind.Heavy, false, false, true), 0.0001f);
            Assert.AreEqual(1.5f, WaveFormula.WalkIn(rules, 0, 1f, 0.5f, WaveComposer.Kind.Plain, false, false, true), 0.0001f, "under the ceiling: the plain product");
            Assert.AreEqual(1.8f, WaveFormula.WalkIn(rules, 0, 1f, 1f, WaveComposer.Kind.Heavy, false, false, true), 0.0001f, "the last wave as a heavy peak would be 3.6");
            Assert.AreEqual(2f * 1.8f, WaveFormula.WalkIn(rules, 0, 2f, 1f, WaveComposer.Kind.Heavy, false, false, true), 0.0001f, "the level scalar moves the ceiling with every other wave");
            Assert.AreEqual(1.8f * 0.5f, WaveFormula.WalkIn(rules, 0, 1f, 1f, WaveComposer.Kind.Heavy, true, false, true), 0.0001f, "the boss crowd's relief is taken after the ceiling");
        }

        [Test]
        public void TheCeiling_LeavesEveryWaveBeforeTheLastThird_AsItWas()
        {
            BalanceRules rules = BalanceRules.Default;
            Assert.Greater(rules.WaveCeiling, rules.RampEnd, "the ramp alone never meets the ceiling: the last ordinary wave is not cut");
            float toughestKind = Mathf.Max(rules.RushFactor, rules.HeavyFactor);
            Assert.GreaterOrEqual(rules.WaveCeiling, WaveFormula.Ramp(rules, 0.6f) * toughestKind * rules.PeakFactor, "a peak in the first three fifths of a level keeps its whole factor");
            Assert.Less(rules.WaveCeiling, rules.RampEnd * toughestKind * rules.PeakFactor, "a peak at the very end is cut: the ceiling is a rule, not decoration");
        }

        // ---- the first air raid: warned, and paid for where nothing held the flyers back ------------------------------

        private static WaveDefinition Wave(bool raid) => new WaveDefinition(new SpawnGroup[0], airRaid: raid);

        [Test]
        public void TheWarning_ComesSoManyWavesBeforeTheFirstRaid_AndNeverBeforeTheFirstWave()
        {
            var waves = new[] { Wave(false), Wave(false), Wave(false), Wave(false), Wave(true), Wave(false), Wave(true) };
            Assert.AreEqual(4, FirstRaidRule.FirstRaid(waves));
            Assert.AreEqual(2, FirstRaidRule.WarningWave(4, 2));
            Assert.AreEqual(0, FirstRaidRule.WarningWave(1, 2), "a raid in the second wave is announced with the first");
            Assert.AreEqual(FirstRaidRule.None, FirstRaidRule.FirstRaid(new[] { Wave(false), Wave(false) }));
            Assert.AreEqual(FirstRaidRule.None, FirstRaidRule.WarningWave(FirstRaidRule.None, 2), "no raid, no warning");
        }

        private TowerDefinition Tower(int cost, TargetLayers targets, bool support = false)
        {
            var tower = ScriptableObject.CreateInstance<TowerDefinition>();
            _made.Add(tower);
            var so = new SerializedObject(tower);
            so.FindProperty("_targets").intValue = (int)targets;
            so.FindProperty("_isSupport").boolValue = support;
            SerializedProperty levels = so.FindProperty("_levels");
            levels.arraySize = 1;
            levels.GetArrayElementAtIndex(0).FindPropertyRelative("Cost").intValue = cost;
            so.ApplyModifiedPropertiesWithoutUndo();
            return tower;
        }

        [Test]
        public void TheSkyGun_IsTheGunMadeForTheSky_OrTheCheapestThatAimsUp()
        {
            var towers = new[] { Tower(80, TargetLayers.Ground), Tower(60, TargetLayers.Ground | TargetLayers.Air), Tower(120, TargetLayers.Air), Tower(150, TargetLayers.Air), Tower(40, TargetLayers.Air, support: true) };
            Assert.AreEqual(120, FirstRaidRule.SkyGunPrice(towers), "the gun made for the sky, not the cheaper all-round one's price and not a support tower");
            Assert.AreEqual(50, FirstRaidRule.SkyGunPrice(new[] { Tower(50, TargetLayers.Ground | TargetLayers.Air), Tower(90, TargetLayers.Ground | TargetLayers.Air) }), "no sky gun on sale: the cheapest gun that can aim up");
            Assert.AreEqual(0, FirstRaidRule.SkyGunPrice(new[] { Tower(80, TargetLayers.Ground) }), "a level that sells no gun for the sky");
        }

        [Test]
        public void TheStoredDifficulties_CarryTheRulesRaidMoney_AndEveryRaidLevelStartsWithIt()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, CatalogPath);
            BalanceRules rules = BalanceRules.Default;
            for (int d = 0; d < catalog.Difficulties.Count; d++)
            {
                Assert.AreEqual(rules.RaidStartGunsFor(d), catalog.Difficulties[d].RaidStartGuns, 0.0001f, $"{catalog.Difficulties[d].name}: run ApplyRound88c");
            }

            Assert.AreEqual(0f, rules.RaidStartGunsFor(0), "Easy waits for the bounties already (FlyersAfterBounties)");
            Assert.Greater(rules.RaidStartGunsFor(2), 0f, "Hard held nothing back: the extra start money is its answer");
            Assert.GreaterOrEqual(rules.FirstRaidWarningWaves, 2, "one wave ahead is the preview; the warning must come in time to save up");

            int raidCells = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                foreach (DifficultyProfile difficulty in catalog.Difficulties)
                {
                    WaveDefinition[] waves = node.Level.GetWaves(difficulty);
                    if (waves == null || FirstRaidRule.FirstRaid(waves) == FirstRaidRule.None)
                    {
                        continue;
                    }

                    raidCells++;
                    Assert.Greater(FirstRaidRule.SkyGunPrice(node.Level.AvailableTowers), 0, $"{node.Level.name}: an air raid on a level that sells no gun for it");
                    int extra = Mathf.RoundToInt(FirstRaidRule.SkyGunPrice(node.Level.AvailableTowers) * difficulty.RaidStartGuns);
                    Assert.AreEqual(extra, StartMoneyRule.RaidAllowance(node.Level, difficulty), $"{node.Level.name} {difficulty.name}");
                    Assert.AreEqual(StartMoneyRule.Base(node.Level, difficulty) + extra, StartMoneyRule.Shared(node.Level, difficulty), $"{node.Level.name} {difficulty.name}");
                }
            }

            Assert.Greater(raidCells, 0, "the first continent has air raids: the test read nothing");
        }

        // ---- a brood runs ahead of its boss ----------------------------------------------------------------------

        [Test]
        public void ADash_GainsTheLeadOnTheBoss_AndIsNeverEndless()
        {
            // 0.6 a second at x2 against a boss at 0.55: 0.65 gained a second.
            Assert.AreEqual(2f, EscortDash.Seconds(1.3f, 0.6f, 0.55f, 2f), 0.0001f);
            Assert.AreEqual(0f, EscortDash.Seconds(0f, 0.6f, 0.55f, 2f), "nothing to gain");
            Assert.AreEqual(0f, EscortDash.Seconds(1f, 0.2f, 0.55f, 2f), "a creature that cannot outrun its boss even at a run does not try");
            Assert.AreEqual(EscortDash.MostSeconds, EscortDash.Seconds(100f, 0.6f, 0.55f, 2f), 0.0001f);
            Assert.IsTrue(EscortDash.Reaches(1.3f, 0.6f, 0.55f, 2f));
            Assert.IsFalse(EscortDash.Reaches(4f, 0.5f, 1.3f, 2.6f), "a plated walker behind a running boss comes out behind it");
        }

        [Test]
        public void EveryLevelBossesBrood_ReachesItsPlaceAheadOfTheBoss_WithinTheDash()
        {
            BalanceRules rules = BalanceRules.Default;
            Assert.Greater(rules.EscortLeadCells, 0f);
            Assert.Greater(rules.EscortSpacingCells, 0f);
            Assert.GreaterOrEqual(rules.EscortSlots, 2, "one place for all would stack the brood on itself instead of on the boss");
            float furthest = rules.EscortLeadCells + (rules.EscortSlots - 1) * rules.EscortSpacingCells;
            int judged = 0;
            var plated = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"{EnemyFolder}/Cragback.asset");
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyFolder }))
            {
                var boss = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                EnemyDefinition creature = EscortRule.Binds(boss) ? EscortRule.CreatureOf(boss) : null;
                if (creature == null)
                {
                    continue;
                }

                foreach (EnemyDefinition escort in boss.Flies ? new[] { creature } : new[] { creature, plated })
                {
                    // The slowest it can be sent out at (the spawner varies the pace by 12 %) and with a body half a cell wide.
                    float slowest = escort.Speed * 0.88f;
                    if (EscortDash.Reaches(furthest + 0.5f, slowest, boss.Speed, rules.EscortDashFactor))
                    {
                        Assert.Less(EscortDash.Seconds(furthest + 0.5f, slowest, boss.Speed, rules.EscortDashFactor), EscortDash.MostSeconds, $"{escort.name} ahead of {boss.name}");
                    }
                    else
                    {
                        // Sent out behind the boss: it must really be left behind, a body length within a few seconds.
                        Assert.Greater(boss.Speed - escort.Speed * 1.12f, 0.15f, $"{escort.name} behind {boss.name}: too slow to run ahead and too fast to be left, it would walk under the boss");
                    }

                    judged++;
                }
            }

            Assert.Greater(judged, 6, "the level bosses with a brood were not read");
        }

        // ---- a boss closes its wave: out behind its last cluster, not inside it --------------------------------------

        [Test]
        public void TheBossEntrance_IsTheClustersTailStretched_PlusTheGap()
        {
            var grammar = new WaveGrammarRules { BossEntranceGap = 8f, BossTailStretch = 1.2f };
            Assert.AreEqual(20f + 10f * 1.2f + 8f, grammar.BossEntrance(20f, 30f), 0.0001f);
            Assert.AreEqual(8f, grammar.BossEntrance(0f, 0f), 0.0001f, "a boss alone still waits the gap");
            WaveGrammarRules stored = BalanceRules.Default.Grammar;
            Assert.GreaterOrEqual(stored.BossEntranceGap, 5f, "the crowd's tail must be cells down the road, not a body length");
            Assert.GreaterOrEqual(stored.BossTailStretch, 1.16f, "Psycho's head count times its tempo stretches a cluster by 1.155");
        }

        [Test]
        public void NoStoredLevelBoss_ComesOutWhileItsOwnRoadsCrowdStillDoes()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CatalogPath);
            Assert.IsNotNull(catalog, CatalogPath);
            float gap = BalanceRules.Default.Grammar.BossEntranceGap;
            int bosses = 0;
            foreach (MapNode node in catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                foreach (DifficultyProfile difficulty in catalog.Difficulties)
                {
                    WaveDefinition[] waves = node.Level.GetWaves(difficulty);
                    for (int w = 0; waves != null && w < waves.Length; w++)
                    {
                        foreach (SpawnGroup boss in waves[w].Groups)
                        {
                            if (boss.Enemy == null || !boss.Enemy.IsBoss)
                            {
                                continue;
                            }

                            bosses++;
                            foreach (SpawnGroup group in waves[w].Groups)
                            {
                                // The crowd that walks in at the boss's own entrance and started before it: as play runs it.
                                if (group.Enemy == null || group.Enemy.IsBoss || group.Enemy.Flies || group.PathIndex != boss.PathIndex || group.Source != boss.Source || group.Delay >= boss.Delay)
                                {
                                    continue;
                                }

                                int count = WaveScheduler.CountOf(group, difficulty);
                                float lastOut = group.Delay + (group.Stagger + (count - 1) * group.Interval) * difficulty.SpawnIntervalMultiplier;
                                Assert.GreaterOrEqual(boss.Delay - lastOut, gap * 0.5f, $"{node.Level.name} {difficulty.name} wave {w + 1}: the last {group.Enemy.name} comes out at {lastOut:0.0} s, the boss at {boss.Delay:0.0} s");
                            }
                        }
                    }
                }
            }

            Assert.Greater(bosses, 20, "the campaign's level bosses were not read");
        }

        // ---- the win rule ----------------------------------------------------------------------------------------

        [Test]
        public void TheLevel_IsWonOnlyWhenTheLastWaveIsOut_AndNothingLives_WhateverSentIt()
        {
            var clock = new WaveClock(new[] { 5f, 5f });
            clock.BeginWave();
            clock.EndSpawning();
            clock.BeginWave();
            // The last wave is still coming out: its boss may be dead already, the level is not over.
            Assert.IsFalse(clock.IsCleared(0), "the last wave is still spawning");
            clock.EndSpawning();
            // The boss is dead and its brood, a split shard or a creature of an earlier wave still lives: one is enough.
            Assert.IsFalse(clock.IsCleared(1), "something a boss sent out is still alive");
            Assert.IsTrue(clock.IsCleared(0));
        }
    }
}
