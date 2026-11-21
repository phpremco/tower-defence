using System;
using System.IO;
using System.Text;
using Game.Core;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEditor;

/// <summary>
/// Round 88b: how much of each level's ground an air-only gun reaches since round 88a (creatures on both layers: the Wisp,
/// the Core Wisp), from the stored waves, and whether the level sells an air-only gun at all. This is the part of the
/// ground stream the balance model now credits the Flak for (WaveLoad.StreamShareFor). Writes
/// Tools/PlayTest/r88b_wisp_reach.txt. Run: bash Tools/PlayTest/stage.sh run WispReachLab.cs WispReachLab.Run
/// </summary>
public static class WispReachLab
{
    private const string ReportPath = "Tools/PlayTest/r88b_wisp_reach.txt";

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var sb = new StringBuilder("Round 88b: the share of the ground bar (body + armour, as spawned) that is on both layers, per level and difficulty; most = the wave with the largest share\n");
        sb.AppendLine("| Level | Sells an air-only gun | Easy | Medium | Hard | Psycho |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (MapNode node in catalog.Nodes)
        {
            LevelDefinition level = node.Level;
            if (level == null)
            {
                continue;
            }

            string airOnly = "no";
            foreach (TowerDefinition tower in level.AvailableTowers)
            {
                airOnly = tower != null && tower.Targets == TargetLayers.Air ? tower.DisplayName : airOnly;
            }

            sb.Append($"| {level.DisplayName} | {airOnly} |");
            foreach (DifficultyProfile difficulty in catalog.Difficulties)
            {
                double all = 0.0;
                double both = 0.0;
                double most = 0.0;
                foreach (WaveDefinition wave in level.GetWaves(difficulty))
                {
                    double waveAll = 0.0;
                    double waveBoth = 0.0;
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        EnemyDefinition enemy = group.Enemy;
                        if (enemy == null || (enemy.Layer & TargetLayers.Ground) == 0)
                        {
                            continue;
                        }

                        int count = enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * difficulty.CountMultiplier));
                        double bar = (enemy.MaxHealth + enemy.Armor) * wave.HealthMultiplier * (enemy.IsBoss ? wave.BossHealthScale : 1f) * count;
                        waveAll += bar;
                        waveBoth += (enemy.Layer & TargetLayers.Air) != 0 ? bar : 0.0;
                    }

                    all += waveAll;
                    both += waveBoth;
                    most = Math.Max(most, waveAll > 0.0 ? waveBoth / waveAll : 0.0);
                }

                sb.Append(FormattableString.Invariant($" {(all > 0.0 ? both / all : 0.0):P1} (most {most:P0}) |"));
            }

            sb.AppendLine();
        }

        File.WriteAllText(ReportPath, sb.ToString());
        return "written " + ReportPath;
    }
}
