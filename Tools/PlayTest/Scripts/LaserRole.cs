using System;
using System.IO;
using System.Text;
using Game.Core;
using Game.Editor;
using Game.Enemies;
using Game.Levels;
using Game.Towers;
using UnityEditor;

/// <summary>
/// The Laser's role in numbers (round 87: the swarm gun; weak on armour and on one big target). The Gun Turret, the
/// Laser and the Cannon at each level, as bar taken off per second per 100 coins of the whole price, per difficulty:
///  - on a fast light swarm (that difficulty's mean Skitter of the stored waves; a body a second, two a second, and a
///    stream that overloads the Laser of that level), played through the firing rules (Game.Editor.StreamDuel);
///  - on one big plain body, on one armoured body and on a boss (an armour pool as big as its body), played the same;
///  - on a slow clustered pack (Oozers, more than the Cannon can blast) and on an energy stream (Wisps), played the same;
///  - on the difficulty's real ground mix of levels 1 to 6, from the data (a gun that works on armour at f pays 1 / f
///    points for a point of armour, one worth k on a family pays 1 / k).
/// And the price rule: what the Laser may cost at most at each level (BalanceSheet.SpecialistPriceCap) against what it costs.
/// Writes Tools/PlayTest/laser_role_r87.txt (round 86's table stays in laser_role.txt).
/// Run: stage.sh run LaserRole.cs LaserRole.Run
/// </summary>
public static class LaserRole
{
    private const string ReportPath = "Tools/PlayTest/laser_role_r87.txt";
    private const int LevelsShown = 3;
    private const int Gun = 0;
    private const int Laser = 1;
    private const int Cannon = 2;
    private const int Columns = 9;
    private const int OverloadColumn = 2;
    private const int MixColumn = 8;
    private const int StreamCount = 160;
    private const float BigBodyScale = 12f;

    // A blast takes three at most: the pack is three times what the Cannon's data could kill one by one.
    private const float BlastBodies = 3f;
    private static readonly string[] Towers = { "PulseRifle", "Laser", "Cannon" };
    private static readonly string[] Names = { "Gun Turret", "Laser", "Cannon" };
    private static readonly CreatureFamily[] Families = (CreatureFamily[])Enum.GetValues(typeof(CreatureFamily));

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var rules = AssetDatabase.LoadAssetAtPath<CombatRules>(CombatRules.AssetPath);
        var towers = new TowerDefinition[Towers.Length];
        for (int t = 0; t < Towers.Length; t++)
        {
            towers[t] = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{Towers[t]}.asset");
        }

        EnemyDefinition skitter = Enemy("Skitter");
        EnemyDefinition prowler = Enemy("Prowler");
        EnemyDefinition cragback = Enemy("Cragback");
        EnemyDefinition oozer = Enemy("Oozer");
        EnemyDefinition wisp = Enemy("Wisp");

        var sb = new StringBuilder();
        sb.AppendLine($"The Laser beside the Gun Turret and the Cannon {DateTime.Now:yyyy-MM-dd HH:mm}. Every number is bar (body + armour) taken off per second per 100 coins of the tower's whole price at that level, unless it says otherwise.");
        sb.AppendLine();
        sb.Append(Inv($"Rules: Beam on armour x{rules.RuleOf(DamageType.Beam).Armour:0.##}, Bullet on armour x{rules.RuleOf(DamageType.Bullet).Armour:0.##};"));
        foreach (FamilyRule rule in rules.Families)
        {
            sb.Append(Inv($" {rule.Type} on {rule.Family} x{rule.Factor:0.##};"));
        }

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("The towers as data:");
        sb.AppendLine("| Tower | Damage a shot | Shots a second | Data DPS | Coins | Data DPS per 100 coins | Retarget (s) | DPS in one cell against the Gun Turret's |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        for (int t = 0; t < Towers.Length; t++)
        {
            for (int l = 0; l < LevelsShown; l++)
            {
                TowerLevel level = towers[t].GetLevel(l);
                sb.AppendLine(Inv($"| {Names[t]} L{l + 1} | {level.Damage:0.##} | {1f / level.FireInterval:0.##} | {Data(towers[t], l):0.0} | {StreamDuel.Coins(towers[t], l)} | {Data(towers[t], l) / StreamDuel.Coins(towers[t], l) * 100f:0.0} | {towers[t].RetargetSeconds:0.##} | x{Data(towers[t], l) / Data(towers[Gun], l):0.00} |"));
            }
        }

        foreach (DifficultyProfile difficulty in catalog.Difficulties)
        {
            float swarmScale = StreamDuel.MeanHealthScale(catalog, difficulty, skitter);
            float swarmBar = skitter.MaxHealth * swarmScale;
            StreamDuel.Stream big = StreamDuel.Stream.Of(prowler, StreamDuel.MeanHealthScale(catalog, difficulty, prowler) * BigBodyScale, 1f, 1);
            StreamDuel.Stream plated = StreamDuel.Stream.Of(cragback, StreamDuel.MeanHealthScale(catalog, difficulty, cragback) * BigBodyScale, 1f, 1);
            // No stored wave sends Oozers as a group on some difficulties: they then walk in at the Skitter's scale.
            float oozerScale = StreamDuel.MeanHealthScale(catalog, difficulty, oozer);
            oozerScale = oozerScale > 0f ? oozerScale : swarmScale;
            StreamDuel.Stream boss = big;
            boss.Armour = boss.Body;
            sb.AppendLine();
            sb.AppendLine(Inv($"## {difficulty.name}: Skitter {swarmBar:0.0} health at {skitter.AverageSpeed:0.0#} cells a second (mean of the stored waves)"));
            sb.AppendLine("| Tower | Swarm, a body a second | Swarm, two a second | Swarm that overloads the Laser of that level | One big plain body | One armoured body | Boss (armour = body) | Slow pack (Oozers, more than the Cannon of that level can blast) | Energy stream (Wisps, two a second) | The difficulty's ground mix (data) |");
            sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            var table = new float[Towers.Length, LevelsShown, Columns];
            Mix mix = MixOf(catalog, difficulty);
            for (int l = 0; l < LevelsShown; l++)
            {
                StreamDuel.Stream[] streams =
                {
                    StreamDuel.Stream.Of(skitter, swarmScale, 1f, StreamCount),
                    StreamDuel.Stream.Of(skitter, swarmScale, 0.5f, StreamCount),
                    StreamDuel.Stream.Of(skitter, swarmScale, StreamDuel.OverloadSpacing(towers[Laser], l, swarmBar), StreamCount),
                    big, plated, boss,
                    StreamDuel.Stream.Of(oozer, oozerScale, StreamDuel.OverloadSpacing(towers[Cannon], l, oozer.MaxHealth * oozerScale) / BlastBodies, StreamCount),
                    StreamDuel.Stream.Of(wisp, StreamDuel.MeanHealthScale(catalog, difficulty, wisp), 0.5f, StreamCount),
                };
                for (int t = 0; t < Towers.Length; t++)
                {
                    float per = 100f / StreamDuel.Coins(towers[t], l);
                    for (int s = 0; s < streams.Length; s++)
                    {
                        table[t, l, s] = StreamDuel.Dps(towers[t], l, streams[s], StreamDuel.GameStep) * per;
                    }

                    table[t, l, MixColumn] = Data(towers[t], l) * per * (float)mix.Lands(towers[t].Profile);
                }
            }

            for (int t = 0; t < Towers.Length; t++)
            {
                for (int l = 0; l < LevelsShown; l++)
                {
                    sb.Append($"| {Names[t]} L{l + 1} |");
                    for (int s = 0; s < Columns; s++)
                    {
                        sb.Append(Inv($" {table[t, l, s]:0.0} |"));
                    }

                    sb.AppendLine();
                }
            }

            Ratios(sb, "Laser / Gun", table, Laser, Gun);
            Ratios(sb, "Cannon / Laser", table, Cannon, Laser);
            sb.AppendLine();
            sb.Append(Inv($"Ground mix: armour {mix.ArmourShare:P0} of the bar, energy bodies {mix.ShareOf(CreatureFamily.Energy):P1}. The swarm that overloads the Laser at 0.2 s frames (the probe), Laser / Gun per coin:"));
            for (int l = 0; l < LevelsShown; l++)
            {
                StreamDuel.Stream stream = StreamDuel.Stream.Of(skitter, swarmScale, StreamDuel.OverloadSpacing(towers[Laser], l, swarmBar), StreamCount);
                float laser = StreamDuel.Dps(towers[Laser], l, stream, StreamDuel.ProbeStep) / StreamDuel.Coins(towers[Laser], l);
                float gun = StreamDuel.Dps(towers[Gun], l, stream, StreamDuel.ProbeStep) / StreamDuel.Coins(towers[Gun], l);
                sb.Append(Inv($" L{l + 1} x{laser / gun:0.00} (a body every {stream.Spacing:0.00} s);"));
            }

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(Inv($"The price rule (BalanceSheet.SpecialistPriceCap, premium x{BalanceSheet.SpecialistPremium:0.##}) on the swarm that overloads the Laser:"));
            sb.AppendLine("| Level | Laser takes off (bar a second) | Gun Turret takes off | Gun Turret's coins | The Laser may cost at most | It costs |");
            sb.AppendLine("| --- | --- | --- | --- | --- | --- |");
            for (int l = 0; l < LevelsShown; l++)
            {
                float laser = table[Laser, l, OverloadColumn] * StreamDuel.Coins(towers[Laser], l) / 100f;
                float gun = table[Gun, l, OverloadColumn] * StreamDuel.Coins(towers[Gun], l) / 100f;
                sb.AppendLine(Inv($"| L{l + 1} | {laser:0.0} | {gun:0.0} | {StreamDuel.Coins(towers[Gun], l)} | {BalanceSheet.SpecialistPriceCap(laser, gun, StreamDuel.Coins(towers[Gun], l))} | {StreamDuel.Coins(towers[Laser], l)} |"));
            }
        }

        File.WriteAllText(ReportPath, sb.ToString());
        return "written " + ReportPath;
    }

    private static EnemyDefinition Enemy(string id) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");

    private static void Ratios(StringBuilder sb, string name, float[,,] table, int over, int under)
    {
        for (int l = 0; l < LevelsShown; l++)
        {
            sb.Append($"| **{name} L{l + 1}** |");
            for (int s = 0; s < Columns; s++)
            {
                sb.Append(Inv($" x{table[over, l, s] / table[under, l, s]:0.00} |"));
            }

            sb.AppendLine();
        }
    }

    private static float Data(TowerDefinition tower, int level) => tower.GetLevel(level).Damage / tower.GetLevel(level).FireInterval;

    /// <summary>What a difficulty sends on the ground: body and armour per creature family.</summary>
    private sealed class Mix
    {
        public readonly double[] Body = new double[Families.Length];
        public readonly double[] Armour = new double[Families.Length];

        public double Bar => Sum(Body) + Sum(Armour);

        public double ArmourShare => Bar > 0.0 ? Sum(Armour) / Bar : 0.0;

        public double ShareOf(CreatureFamily family) => Bar > 0.0 ? (Body[(int)family] + Armour[(int)family]) / Bar : 0.0;

        /// <summary>The share of a gun's data damage that is worth something on this mix.</summary>
        public double Lands(DamageProfile profile)
        {
            double needed = 0.0;
            for (int f = 0; f < Families.Length; f++)
            {
                needed += (Body[f] + Armour[f] / profile.Armour) / profile.Against(Families[f]);
            }

            return needed > 0.0 ? Bar / needed : 0.0;
        }

        private static double Sum(double[] values)
        {
            double sum = 0.0;
            foreach (double value in values)
            {
                sum += value;
            }

            return sum;
        }
    }

    // Ground creatures of the stored waves as spawned, level bosses included, flyers and continent bosses left out.
    private static Mix MixOf(CampaignCatalog catalog, DifficultyProfile difficulty)
    {
        var mix = new Mix();
        foreach (MapNode node in catalog.Nodes)
        {
            if (node.Level == null)
            {
                continue;
            }

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
                    mix.Body[(int)enemy.Family] += enemy.MaxHealth * scale * count;
                    mix.Armour[(int)enemy.Family] += enemy.Armor * scale * count;
                }
            }
        }

        return mix;
    }

    private static string Inv(FormattableString text) => FormattableString.Invariant(text);
}
