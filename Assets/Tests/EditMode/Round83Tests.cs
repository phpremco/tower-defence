using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Balance;
using Game.Editor;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;

namespace Game.Tests
{
    /// <summary>
    /// Round 83: the simplified balance model. The ledger's player decides its purchase before it knows a wave's
    /// toughness, so pressure only rises with health; every wave is solved to its own target; a cell's level as played
    /// is one scalar near one; no ground wave is paper; the default plan sends no walker alone out of a spawn point.
    /// </summary>
    public sealed class Round83Tests
    {
        private const string LevelFolder = "Assets/_Project/Data/Levels";

        /// <summary>The multiples a wave's health is walked through, a tenth to ten times the stored wave.</summary>
        private const int Steps = 14;
        private const float Lowest = 0.1f;
        private const float Highest = 10f;

        /// <summary>A pressure is a quotient of floats: a rise smaller than this is no fall.</summary>
        private const float Rounding = 1e-4f;

        /// <summary>A ground wave is paper under this share of the median ground wave of its level and difficulty.</summary>
        private const float LeastOfTheMedian = 0.4f;

        [Test]
        public void AWavesPressure_NeverFalls_AsItsCreaturesGrowTougher()
        {
            // The critic's finding A (Docs/BALANCE_CRITIC_2026-11-18.md): the ledger's player re-bought its defence for every
            // trial multiplier, so a raid met its target two or three times and a level's mean was not monotone in its scale.
            // Now the purchase is decided on the wave as planned (book health), and a wave's pressure can only rise with it:
            // in both lenses the solver reads, for every stored wave of every cell.
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            CampaignModel model = loaded.Model;
            int walked = 0;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                int index = node.Level != null ? model.IndexOf(Key(node.Level)) : -1;
                if (index < 0 || WaveScripts.ByKey(Key(node.Level)) == null)
                {
                    continue;
                }

                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    List<WaveSpec> waves = BalanceStages.Waves(model, node.Level, loaded.Catalog.Difficulties[d]);
                    LevelInput input = model.InputFor(index, d);
                    foreach (BalanceRules lens in new[] { model.Rules.HumanTowersOnly(), model.Rules.HumanWithHero() })
                    {
                        for (int w = 0; w < waves.Count; w++)
                        {
                            float stored = waves[w].HealthMultiplier;
                            float before = 0f;
                            for (int step = 0; step <= Steps; step++)
                            {
                                waves[w].HealthMultiplier = stored * Lowest * (float)Math.Pow(Highest / Lowest, step / (double)Steps);
                                WaveLine line = Ledger.Evaluate(input, waves, model.Difficulties[d], lens)[w];
                                Assert.GreaterOrEqual(line.GroundPressure, before - Rounding, $"{node.Level.name} difficulty {d} wave {w + 1} at x{waves[w].HealthMultiplier:0.000}: the ground pressure fell");
                                before = line.GroundPressure;
                                walked++;
                            }

                            waves[w].HealthMultiplier = stored;
                        }
                    }
                }
            }

            Assert.Greater(walked, 0);
        }

        [Test]
        public void AWavesAirPressure_NeverFalls_AsItsFlyersGrowTougher()
        {
            // The raid of Quartz Flats wave 13 stood at x1.18 or x2.46 by a hair of the purse (round 79): the player bought a
            // sky gun's next step somewhere in between. With the purchase fixed before the multiplier there is one crossing.
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            CampaignModel model = loaded.Model;
            int raids = 0;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                int index = node.Level != null ? model.IndexOf(Key(node.Level)) : -1;
                if (index < 0 || WaveScripts.ByKey(Key(node.Level)) == null)
                {
                    continue;
                }

                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    List<WaveSpec> waves = BalanceStages.Waves(model, node.Level, loaded.Catalog.Difficulties[d]);
                    LevelInput input = model.InputFor(index, d);
                    BalanceRules lens = model.Rules.HumanTowersOnly();
                    for (int w = 0; w < waves.Count; w++)
                    {
                        if (!waves[w].AirRaid)
                        {
                            continue;
                        }

                        raids++;
                        float stored = waves[w].HealthMultiplier;
                        float before = 0f;
                        for (int step = 0; step <= Steps; step++)
                        {
                            waves[w].HealthMultiplier = stored * Lowest * (float)Math.Pow(Highest / Lowest, step / (double)Steps);
                            float air = Ledger.Evaluate(input, waves, model.Difficulties[d], lens)[w].AirPressure;
                            Assert.GreaterOrEqual(air, before - Rounding, $"{node.Level.name} difficulty {d} raid {w + 1} at x{waves[w].HealthMultiplier:0.000}: the air pressure fell");
                            before = air;
                        }

                        waves[w].HealthMultiplier = stored;
                    }
                }
            }

            Assert.Greater(raids, 0);
        }

        [Test]
        public void NoStoredGroundWave_IsPaper()
        {
            // Round 82 stored ten ground waves under x0.4 (creatures of paper between tough waves): no health of such a wave
            // met its target, because slow plated creatures of earlier waves carried the pressure in, and the solver let it
            // fall to its floor. The band's lower edge holds such a wave now. The opening waves ramp up from little, and the
            // crowd of a boss wave gives way to its boss: neither is judged here.
            // Round 87: read against the median of the whole level's ground waves again. Round 86 had narrowed it to the
            // waves up to and including the wave when Fault Line Hard wave 7 failed without having moved; a wave is the
            // formula's now (WaveFormula) and the gate has its first meaning back.
            int judged = 0;
            foreach (LevelDefinition level in AssetDatabase.FindAssets("t:LevelDefinition", new[] { LevelFolder }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<LevelDefinition>))
            {
                foreach (DifficultyWaves set in level.DifficultyWaves)
                {
                    for (int i = BalanceChecks.OpeningWaves; i < set.Waves.Length; i++)
                    {
                        WaveDefinition wave = set.Waves[i];
                        List<float> ground = set.Waves.Where(w => !w.AirRaid).Select(w => w.HealthMultiplier).OrderBy(m => m).ToList();
                        if (ground.Count == 0)
                        {
                            continue;
                        }

                        float median = ground.Count % 2 == 1 ? ground[ground.Count / 2] : (ground[ground.Count / 2 - 1] + ground[ground.Count / 2]) * 0.5f;
                        if (wave.AirRaid || wave.Groups.Any(g => g.Enemy != null && g.Enemy.IsBoss))
                        {
                            continue;
                        }

                        Assert.GreaterOrEqual(wave.HealthMultiplier, median * LeastOfTheMedian, $"{level.name} {set.Difficulty.name} wave {i + 1}: x{wave.HealthMultiplier:0.00} against a median of x{median:0.00}");
                        judged++;
                    }
                }
            }

            Assert.Greater(judged, 0);
        }

        [Test]
        public void ALevelsScalar_LiesNearOne()
        {
            // One scalar per level since round 87 (rounds 83 to 86: one per level and difficulty). It is found by the closed
            // loop (Tools/PlayTest/closed_loop.py); far from one, the formula misreads the level's map or script and the
            // level wants a look, not a bigger scalar.
            BalanceRules rules = BalanceRules.Default;
            int levels = 0;
            foreach (KeyValuePair<string, float> scalar in WaveTuning.Read())
            {
                Assert.GreaterOrEqual(scalar.Value, rules.LevelScalarLeast, scalar.Key);
                Assert.LessOrEqual(scalar.Value, rules.LevelScalarMost, scalar.Key);
                levels++;
            }

            // A level without a line is at one.
            Assert.IsTrue(levels > 0 || File.Exists(WaveTuning.Path));
        }

        [Test]
        public void TheTuningFile_HoldsALevelsScalar_AndNothingElse()
        {
            // Round 84: what a wave is made of is a rule per difficulty (BalanceRules: plated and swarm weight, the growth
            // of plated troops) or the level's own script. Round 87: one scalar per level; a line per level and difficulty
            // is not read any more.
            Assert.IsEmpty(WaveTuning.OtherLines(), "Tools/PlayTest/wave_tuning.txt holds 'Key scalar' lines only");
        }

        [Test]
        public void TheDefaultPlan_SendsNoWalkerAlone_OutOfARift()
        {
            // Remco saw one Cragback walk the road behind a gate. A default surprise gives a rift two walkers or none, so the
            // rule holds as played on every difficulty; one big creature out of a rift is the level's to write.
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            Assert.IsNotNull(loaded.Catalog, "Run Tools/Generate/Scenes and Starter Data first.");
            int surprises = 0;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                int index = node.Level != null ? loaded.Model.IndexOf(Key(node.Level)) : -1;
                WaveScriptSpec script = index >= 0 ? WaveScripts.ByKey(Key(node.Level)) : null;
                if (script == null)
                {
                    continue;
                }

                List<List<WaveSpec>> sets = loaded.Model.BuildWaves(index, script, out _);
                for (int d = 0; d < sets.Count; d++)
                {
                    for (int w = 0; w < sets[d].Count; w++)
                    {
                        if (sets[d][w].Surprise != SurpriseKind.Rift)
                        {
                            continue;
                        }

                        surprises++;
                        foreach (IGrouping<int, GroupSpec> rift in sets[d][w].Groups.Where(g => g.Source == WaveComposer.NestSource && g.Enemy != null && !g.Enemy.Flies).GroupBy(g => g.Nest))
                        {
                            Assert.GreaterOrEqual(rift.Sum(g => g.Count), 2, $"{node.Level.name} difficulty {d} wave {w + 1}: one walker alone out of rift {rift.Key}");
                        }
                    }
                }
            }

            Assert.Greater(surprises, 0);
        }

        private static string Key(LevelDefinition level) => level.name.StartsWith("Level_") ? level.name.Substring(6) : level.name;
    }
}
