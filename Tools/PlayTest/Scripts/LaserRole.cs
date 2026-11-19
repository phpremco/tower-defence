using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEditor;

/// <summary>
/// Round 86, "the laser is weak on Psycho" in numbers: what the ground creatures of the stored waves are made of per
/// difficulty (body against armour, as spawned) and what each level of the Gun Turret, the Laser and the Cannon lands on
/// that mix per coin. A gun that works on armour at f pays 1 / f points of damage for a point of armour, so of its data
/// damage (body + armour) / (body + armour / f) is worth something on the mix. No simulation: the beam and the gun waste
/// next to nothing on a body (Tools/PlayTest/laser_frames.txt, laser_share.txt), so the data is what they land.
/// Writes Tools/PlayTest/laser_role.txt. Run: stage.sh run LaserRole.cs LaserRole.Run
/// </summary>
public static class LaserRole
{
    private static readonly string[] Towers = { "PulseRifle", "Laser", "Cannon" };
    private static readonly string[] Names = { "Gun Turret", "Laser", "Cannon" };

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var sb = new StringBuilder();
        sb.AppendLine($"The Laser beside the Gun Turret and the Cannon, per difficulty {DateTime.Now:yyyy-MM-dd HH:mm}; ground creatures of the stored waves of levels 1 to 6 as spawned, level bosses included.");
        sb.AppendLine();
        sb.AppendLine("What a difficulty sends on the ground (all six levels together):");
        sb.AppendLine("| Difficulty | Bar sent (body + armour) | Of it armour | Plated creatures, share of the bodies | Light fire must deal (armour at 0.2) | Light fire lands of its data |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- |");
        int difficulties = catalog.Difficulties.Count;
        var body = new double[difficulties];
        var armour = new double[difficulties];
        var bodies = new double[difficulties];
        var plated = new double[difficulties];
        foreach (MapNode node in catalog.Nodes)
        {
            if (node.Level == null)
            {
                continue;
            }

            for (int d = 0; d < difficulties; d++)
            {
                DifficultyProfile difficulty = catalog.Difficulties[d];
                foreach (WaveDefinition wave in node.Level.GetWaves(difficulty))
                {
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        EnemyDefinition enemy = group.Enemy;
                        if (enemy == null || enemy.Flies || enemy.Boss != null)
                        {
                            continue;
                        }

                        float scale = difficulty.HealthMultiplier * node.Level.EnemyHealthScale * wave.HealthMultiplier * (enemy.IsBoss ? wave.BossHealthScale : 1f);
                        int count = enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * difficulty.CountMultiplier));
                        body[d] += enemy.MaxHealth * scale * count;
                        armour[d] += enemy.Armor * scale * count;
                        bodies[d] += count;
                        plated[d] += enemy.Armor > 0f ? count : 0;
                    }
                }
            }
        }

        var lands = new Dictionary<float, double[]>();
        float[] factors = { 0.2f, 1f };
        foreach (float f in factors)
        {
            lands[f] = new double[difficulties];
        }

        for (int d = 0; d < difficulties; d++)
        {
            double bar = body[d] + armour[d];
            foreach (float f in factors)
            {
                lands[f][d] = bar / (body[d] + armour[d] / f);
            }

            sb.AppendLine(Inv($"| {catalog.Difficulties[d].name} | {bar:0} | {armour[d] / bar:P0} | {plated[d] / bodies[d]:P0} | {body[d] + armour[d] / 0.2:0} | {lands[0.2f][d]:P0} |"));
        }

        sb.AppendLine();
        sb.AppendLine("Damage per second per 100 coins (the whole price of the tower at that level): on a soft body, on armour, and what of it is worth something on the difficulty's mix above. The Cannon is counted on one creature; its blast catches up to three in a pack.");
        sb.AppendLine("| Tower | Coins | Data DPS | Per 100 coins, soft | Per 100 coins, on armour | Easy mix | Medium mix | Hard mix | Psycho mix |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        var perCoin = new Dictionary<string, double[]>();
        for (int t = 0; t < Towers.Length; t++)
        {
            var tower = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{Towers[t]}.asset");
            float f = tower.Profile.Armour;
            int total = 0;
            for (int l = 0; l <= tower.MaxLevelIndex; l++)
            {
                TowerLevel level = tower.GetLevel(l);
                total += level.Cost;
                double dps = level.Damage / level.FireInterval;
                double soft = dps / total * 100.0;
                var mix = new double[difficulties];
                var cells = new StringBuilder();
                for (int d = 0; d < difficulties; d++)
                {
                    double bar = body[d] + armour[d];
                    mix[d] = soft * bar / (body[d] + armour[d] / f);
                    cells.Append(Inv($" {mix[d]:0.0} |"));
                }

                perCoin[$"{Names[t]} L{l + 1}"] = mix;
                sb.AppendLine(Inv($"| {Names[t]} L{l + 1} | {total} | {dps:0.0} | {soft:0.0} | {soft * f:0.0} |{cells}"));
            }
        }

        sb.AppendLine();
        sb.AppendLine("The Laser over the Gun Turret and under the Cannon, per coin on the mix:");
        sb.AppendLine("| Difficulty | Laser L1 / Gun L1 | Laser L3 / Gun L3 | Cannon L1 / Laser L1 | Cannon L3 / Laser L3 |");
        sb.AppendLine("| --- | --- | --- | --- | --- |");
        for (int d = 0; d < difficulties; d++)
        {
            sb.AppendLine(Inv($"| {catalog.Difficulties[d].name} | x{perCoin["Laser L1"][d] / perCoin["Gun Turret L1"][d]:0.00} | x{perCoin["Laser L3"][d] / perCoin["Gun Turret L3"][d]:0.00} | x{perCoin["Cannon L1"][d] / perCoin["Laser L1"][d]:0.00} | x{perCoin["Cannon L3"][d] / perCoin["Laser L3"][d]:0.00} |"));
        }

        File.WriteAllText("Tools/PlayTest/laser_role.txt", sb.ToString());
        return "written Tools/PlayTest/laser_role.txt";
    }

    private static string Inv(FormattableString text) => FormattableString.Invariant(text);
}
