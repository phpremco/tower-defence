using System.Collections.Generic;
using System.Text;
using Game.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// Runs the paper balance model over every level, difficulty and wave, writes the table to
    /// Tools/PlayTest/balance_ledger.txt and fails when a difficulty leaves its band: Easy must stay holdable by
    /// towers alone with margin, each step up may ask more, no wave may be a cliff compared with the one before,
    /// and the late waves of Easy must still ask for something. Bosses are judged on their own, looser band.
    /// </summary>
    public sealed class BalanceLedgerTests
    {
        private const string CampaignPath = "Assets/_Project/Data/Levels/CampaignCatalog.asset";
        private const string LedgerPath = "Tools/PlayTest/balance_ledger.txt";

        // The most pressure a plain wave may put on the towers, per difficulty index (Easy to Psycho).
        private static readonly float[] MaxPressure = { 0.7f, 1f, 1.6f, 3.2f };
        private const float BossPressureAllowance = 1.8f;
        private const float MaxCliff = 2.7f;
        private const int CliffMemory = 3;
        private const int OpeningWaves = 4;
        private const float MinLateEasyPressure = 0.15f;
        private const float LateShare = 0.65f;

        [Test]
        public void EveryWave_StaysInsideItsDifficultyBand()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignPath);
            Assert.IsNotNull(catalog, "Run Tools/Generate/Scenes and Starter Data first.");

            var report = new StringBuilder();
            var failures = new List<string>();
            foreach (MapNode node in catalog.Nodes)
            {
                for (int d = 0; d < catalog.Difficulties.Count; d++)
                {
                    DifficultyProfile difficulty = catalog.Difficulties[d];
                    List<BalanceLedger.WaveLine> lines = BalanceLedger.Evaluate(node.Level, difficulty);
                    report.Append(node.Level.name).Append(' ').Append(difficulty.name).Append('\n');
                    float lateSum = 0f;
                    int lateCount = 0;
                    var recent = new List<float>();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        BalanceLedger.WaveLine line = lines[i];
                        report.Append("  ").Append(line).Append('\n');
                        string where = $"{node.Level.name} {difficulty.name} wave {line.Wave}";
                        float limit = MaxPressure[d] * (line.Boss ? BossPressureAllowance : 1f);
                        if (line.Pressure > limit)
                        {
                            failures.Add($"{where}: pressure {line.Pressure:0.00} over {limit:0.00}");
                        }

                        // Cliffs are judged on the ground against the heaviest of the last few plain waves: an air raid is
                        // a different kind of wave and is held to the band only, and an ambush that trickles in from the
                        // nests is not the measure for the wave after it.
                        float recentMax = 0f;
                        foreach (float value in recent)
                        {
                            recentMax = Mathf.Max(recentMax, value);
                        }

                        // The opening waves may ramp as steeply as they like: the first pack of a new species is meant to be felt.
                        if (!line.Boss && i >= OpeningWaves && recentMax > 0f && line.GroundPressure > recentMax * MaxCliff)
                        {
                            failures.Add($"{where}: ground pressure jumps from {recentMax:0.00} to {line.GroundPressure:0.00}");
                        }

                        if (!line.Boss)
                        {
                            recent.Add(line.GroundPressure);
                            if (recent.Count > CliffMemory)
                            {
                                recent.RemoveAt(0);
                            }
                        }

                        if (d == 0 && !line.Boss && i >= lines.Count * LateShare)
                        {
                            lateSum += line.Pressure;
                            lateCount++;
                        }
                    }

                    if (d == 0 && lateCount > 0 && lateSum / lateCount < MinLateEasyPressure)
                    {
                        failures.Add($"{node.Level.name} Easy: late waves average pressure {lateSum / lateCount:0.00}, too easy");
                    }
                }
            }

            System.IO.File.WriteAllText(LedgerPath, report.ToString());
            Assert.IsEmpty(failures, string.Join("\n", failures) + "\nSee " + LedgerPath);
        }
    }
}
