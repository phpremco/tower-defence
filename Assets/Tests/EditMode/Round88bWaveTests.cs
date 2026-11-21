using System;
using System.Collections.Generic;
using Game.Balance;
using Game.Editor;
using Game.Levels;
using NUnit.Framework;

namespace Game.Tests
{
    /// <summary>
    /// Round 88b: a swarm that comes between two air raids carries no peak (<see cref="BalanceRules.PeakOnARushBetweenRaids"/>).
    /// Fault Line on Psycho lost every bot run because its only late ground wave, a swarm between raids, took the ramp, the
    /// rush factor and a peak at once. Held on every level's script and difficulty as the solve stage will read them (no
    /// stored wave is read, so these tests say what the rule does before and after the waves are solved again).
    /// </summary>
    public sealed class Round88bWaveTests
    {
        private const int FewestPeaks = 2;
        private const float Slack = 0.0001f;

        private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Psycho" };

        private sealed class ScriptCell
        {
            public string Name;
            public CampaignModel Model;
            public int Index;
            public int Difficulty;
            public WaveScriptSpec Script;
        }

        [Test]
        public void ARushBetweenTwoRaids_IsRecognised_ByItsNeighbours()
        {
            var script = new WaveScriptSpec { Waves = 6, Rhythm = "PRPRPR", AirWaves = new[] { 3, 5, 1 }, AirWavesPerDifficulty = new[] { 2, 3 } };
            Assert.IsTrue(WaveSolver.IsRushBetweenRaids(script, 0, 4), "raids in waves 3 and 5 around the swarm of wave 4");
            Assert.IsFalse(WaveSolver.IsRushBetweenRaids(script, 0, 2), "wave 1 is no raid on the first difficulty");
            Assert.IsTrue(WaveSolver.IsRushBetweenRaids(script, 1, 2), "it is on the second");
            Assert.IsFalse(WaveSolver.IsRushBetweenRaids(script, 0, 3), "a raid is no swarm");
            Assert.IsFalse(WaveSolver.IsRushBetweenRaids(script, 0, 6), "the last wave has no wave after it");
        }

        [Test]
        public void NoRushBetweenTwoRaids_CarriesAPeak_OnAnyLevelAndDifficulty()
        {
            int cells = 0;
            foreach (ScriptCell cell in Cells())
            {
                List<int> peaks = WaveSolver.PeaksOf(cell.Script, cell.Model.Enemies, cell.Difficulty, cell.Model.Facts[cell.Index].SecondRoad, cell.Model.Rules);
                foreach (int number in peaks)
                {
                    Assert.IsFalse(WaveSolver.IsRushBetweenRaids(cell.Script, cell.Difficulty, number), $"{cell.Name}: the swarm of wave {number} stands between two raids and is a peak");
                }

                Assert.GreaterOrEqual(peaks.Count, FewestPeaks, $"{cell.Name}: the rule left the level without its peaks ({string.Join(", ", peaks)})");
                cells++;
            }

            Assert.Greater(cells, 0);
        }

        [Test]
        public void TheRule_TakesThePeakOffThatWave_AndMovesNoOtherPeak()
        {
            int dropped = 0;
            foreach (ScriptCell cell in Cells())
            {
                BalanceRules before = cell.Model.Rules.Copy();
                before.PeakOnARushBetweenRaids = true;
                int secondRoad = cell.Model.Facts[cell.Index].SecondRoad;
                List<int> was = WaveSolver.PeaksOf(cell.Script, cell.Model.Enemies, cell.Difficulty, secondRoad, before);
                List<int> now = WaveSolver.PeaksOf(cell.Script, cell.Model.Enemies, cell.Difficulty, secondRoad, cell.Model.Rules);
                foreach (int number in was)
                {
                    bool between = WaveSolver.IsRushBetweenRaids(cell.Script, cell.Difficulty, number);
                    Assert.AreEqual(!between, now.Contains(number), $"{cell.Name}: peak of wave {number}");
                    dropped += between ? 1 : 0;
                }

                foreach (int number in now)
                {
                    Assert.IsTrue(was.Contains(number), $"{cell.Name}: wave {number} became a peak; the rule places none anew");
                }
            }

            Assert.Greater(dropped, 0, "the first continent has swarms between raids that carried a peak (Fault Line on Psycho, wave 13): the test reads nothing");
        }

        [Test]
        public void ARushBetweenTwoRaids_WalksInWithoutThePeakFactor()
        {
            int judged = 0;
            foreach (ScriptCell cell in Cells())
            {
                CampaignModel model = cell.Model;
                BalanceRules rules = model.Rules;
                int d = cell.Difficulty;
                WaveSolver.Solution solution = null;
                for (int number = 1; number <= cell.Script.Waves; number++)
                {
                    if (!WaveSolver.IsRushBetweenRaids(cell.Script, d, number) || WaveComposer.BossOn(cell.Script, d, number) != null)
                    {
                        continue;
                    }

                    solution = solution ?? WaveSolver.Solve(cell.Script, model.Enemies, d, model.Facts[cell.Index].SecondRoad, rules, cell.Script.TargetScaleFor(d), model.ContinentIndexOf(cell.Index), model.Difficulties[d].Health * model.Facts[cell.Index].HealthScale);
                    float plain = WaveFormula.WalkIn(rules, d, cell.Script.TargetScaleFor(d), WaveFormula.Phase(number, cell.Script.Waves), WaveComposer.Kind.Rush, false, WaveComposer.BossOn(cell.Script, d, number - 1) != null, false);
                    // The stored multiplier is rounded to three decimals before the composer's lift comes back on.
                    Assert.AreEqual(plain, solution.WalkIn[number - 1], plain * 0.01f + Slack, $"{cell.Name} wave {number}: a swarm between two raids walks in on the ramp and the rush factor, without the peak");
                    judged++;
                }
            }

            Assert.Greater(judged, 0);
        }

        // Every level of the campaign with its script as the solve stage reads it (the flyer floors set), per difficulty.
        private static IEnumerable<ScriptCell> Cells()
        {
            BalanceStages.Loaded loaded = BalanceStages.Load(true);
            CampaignModel model = loaded.Model;
            foreach (MapNode node in loaded.Catalog.Nodes)
            {
                if (node.Level == null)
                {
                    continue;
                }

                string key = node.Level.name.StartsWith("Level_", StringComparison.Ordinal) ? node.Level.name.Substring(6) : node.Level.name;
                int index = model.IndexOf(key);
                WaveScriptSpec script = WaveScripts.ByKey(key);
                if (index < 0 || script == null)
                {
                    continue;
                }

                // Sizing the waves leaves the flyer floors on the script: raids are read where the solve puts them.
                model.BuildWaves(index, script, out _);
                for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
                {
                    yield return new ScriptCell { Name = key + " " + DifficultyNames[Math.Min(d, DifficultyNames.Length - 1)], Model = model, Index = index, Difficulty = d, Script = script };
                }
            }
        }
    }
}
