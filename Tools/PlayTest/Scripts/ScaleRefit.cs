using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;

/// <summary>
/// Round 80b: re-expresses the builder's target scale of every cell after a change of what the human curve is read on
/// (BalanceChecks.PlainMean without the air raids). Per cell it searches the scale at which this solve gives the stored
/// waves' sum of health multipliers again, raids aside, so the cell stays the level that was measured. The measured waves are read from copies of the wave assets in Tools/PlayTest/r81_waves (the last measured round, taken
/// from a worker clone), so it can run after a solve. No asset is written: the result is Tools/PlayTest/scale_refit.txt
/// with the lines for Tools/PlayTest/wave_tuning.txt. A cell whose waves moved less than <see cref="Keep"/> keeps its
/// scale; the cells named in Tools/PlayTest/scale_refit_arg.txt ("skip=C1_04:0", one per line) keep theirs whatever moved.
/// unity command run_script --file Tools/PlayTest/Scripts/ScaleRefit.cs --entry ScaleRefit.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ScaleRefit
{
    private const float Keep = 0.03f;
    private const int Iterations = 9;
    private const float Lowest = 0.3f;
    private const float Highest = 1.6f;
    private const string Measured = "Tools/PlayTest/r81_waves/Waves_{0}.asset";
    private const string ArgPath = "Tools/PlayTest/scale_refit_arg.txt";

    // The health multipliers of a stored wave asset, per difficulty in the asset's order, read from its text.
    private static List<List<float>> Multipliers(string path)
    {
        var sets = new List<List<float>>();
        foreach (string line in File.ReadAllLines(path))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("- Difficulty:"))
            {
                sets.Add(new List<float>());
            }
            else if (trimmed.StartsWith("_healthMultiplier:") && sets.Count > 0)
            {
                sets[sets.Count - 1].Add(float.Parse(trimmed.Substring("_healthMultiplier:".Length).Trim(), CultureInfo.InvariantCulture));
            }
        }

        return sets;
    }

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var upgrades = AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
        var unlocks = AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset");
        var calibration = AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath);
        CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, BalanceRules.Default, calibration);
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            model.Enemies[enemy.name] = BalanceLedger.Stats(enemy);
        }

        var skip = new HashSet<string>();
        foreach (string line in File.Exists(ArgPath) ? File.ReadAllLines(ArgPath) : new string[0])
        {
            if (line.Trim().StartsWith("skip="))
            {
                skip.Add(line.Trim().Substring(5));
            }
        }

        int count = catalog.Difficulties.Count;
        var text = new StringBuilder($"Scale refit {DateTime.Now:yyyy-MM-dd HH:mm}: per cell the scale now, the sum of multipliers (raids aside) at that scale against the stored waves, the scale that gives the stored sum again, and the sum there.\n");
        var lines = new StringBuilder();
        foreach (MapNode node in catalog.Nodes)
        {
            if (node.Level == null)
            {
                continue;
            }

            string key = node.Level.name.Substring("Level_".Length);
            int index = model.IndexOf(key);
            if (index < 0 || Game.Editor.WaveScripts.ByKey(key) == null)
            {
                continue;
            }

            List<List<float>> measured = Multipliers(string.Format(Measured, key));
            var now = new float[count];
            for (int d = 0; d < count; d++)
            {
                now[d] = Game.Editor.WaveScripts.ByKey(key).TargetScaleFor(d);
            }

            float[] Ratios(float[] scales)
            {
                WaveScriptSpec script = Game.Editor.WaveScripts.ByKey(key);
                script.TargetScale = (float[])scales.Clone();
                List<List<WaveSpec>> sets = model.BuildWaves(index, script, out _);
                var ratios = new float[count];
                for (int d = 0; d < count; d++)
                {
                    List<float> stored = measured[d];
                    float sum = 0f;
                    float was = 0f;
                    for (int w = 0; w < sets[d].Count && w < stored.Count; w++)
                    {
                        if (!sets[d][w].AirRaid)
                        {
                            sum += sets[d][w].HealthMultiplier;
                            was += stored[w];
                        }
                    }

                    ratios[d] = was > 0f ? sum / was : 1f;
                }

                return ratios;
            }

            float[] first = Ratios(now);
            var low = new float[count];
            var high = new float[count];
            for (int d = 0; d < count; d++)
            {
                low[d] = Lowest;
                high[d] = Highest;
            }

            // A larger scale asks more, so the sum grows with it: a bisection per difficulty, all four in one solve.
            var mid = new float[count];
            for (int i = 0; i < Iterations; i++)
            {
                for (int d = 0; d < count; d++)
                {
                    mid[d] = (low[d] + high[d]) * 0.5f;
                }

                float[] ratios = Ratios(mid);
                for (int d = 0; d < count; d++)
                {
                    if (ratios[d] < 1f)
                    {
                        low[d] = mid[d];
                    }
                    else
                    {
                        high[d] = mid[d];
                    }
                }
            }

            var fitted = new float[count];
            for (int d = 0; d < count; d++)
            {
                fitted[d] = Math.Abs(first[d] - 1f) <= Keep || skip.Contains(key + ":" + d) ? now[d] : (float)Math.Round((low[d] + high[d]) * 0.5f, 2);
            }

            float[] last = Ratios(fitted);
            for (int d = 0; d < count; d++)
            {
                text.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1}: scale {2:0.###} sum x{3:0.000} -> scale {4:0.###} sum x{5:0.000}{6}\n", key, d, now[d], first[d], fitted[d], last[d], fitted[d] == now[d] ? " (kept)" : ""));
                if (fitted[d] != now[d])
                {
                    lines.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2:0.###}\n", key, d, fitted[d]));
                }
            }
        }

        text.Append("\nLines for wave_tuning.txt:\n").Append(lines);
        File.WriteAllText("Tools/PlayTest/scale_refit.txt", text.ToString());
        return text.ToString();
    }
}
