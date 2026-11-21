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
/// Round 88b, the Cannon's blast cap on paper (no Play Mode). The Cannon with the old cap (three creatures at every
/// level) and with the cap per level (two, three, four), beside the Gun Turret and the Laser of the same level, as bar
/// (body plus armour) taken off per second per 100 coins of the tower's whole price, played through the firing rules
/// (Game.Editor.StreamDuel): a dense swarm, a spaced stream, a dense pack of bigger bodies, a column of plated walkers,
/// one plated walker and a boss (an armour pool as big as its body). Then, for the two levels without flyers, what the
/// stored waves ask of a gun without any blast at all: the armour share of the ground bar and what a coin of each gun is
/// worth on that mix shot one creature at a time. Writes Tools/PlayTest/r88b_cannon_cap.txt.
/// Run: bash Tools/PlayTest/stage.sh run CannonCapLab.cs CannonCapLab.Run
/// </summary>
public static class CannonCapLab
{
    private const string ReportPath = "Tools/PlayTest/r88b_cannon_cap.txt";
    private const int LevelsShown = 3;
    private const int StreamCount = 160;
    private const int ColumnCount = 40;
    private const float BigBodyScale = 12f;
    private const float DenseSeconds = 0.25f;
    private const float SpacedSeconds = 1f;
    private const float PackSeconds = 0.5f;
    private const int FlyerFreeLevels = 2;
    private static readonly int[] OldCap = { 3, 3, 3 };
    private static readonly int[] OneCap = { 1, 1, 1 };
    private static readonly string[] Towers = { "PulseRifle", "Laser", "Cannon" };
    private static readonly string[] Names = { "Gun Turret", "Laser", "Cannon" };
    private static readonly string[] Columns = { "Dense swarm (Skitters, four a second)", "Spaced stream (Skitters, one a second)", "Dense pack (Prowlers, two a second)", "Plated column (Cragbacks, two a second)", "One plated walker", "Boss (armour = body)" };
    private static readonly CreatureFamily[] Families = (CreatureFamily[])Enum.GetValues(typeof(CreatureFamily));

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var towers = new TowerDefinition[Towers.Length];
        for (int t = 0; t < Towers.Length; t++)
        {
            towers[t] = AssetDatabase.LoadAssetAtPath<TowerDefinition>($"Assets/_Project/Data/Towers/{Towers[t]}.asset");
        }

        TowerDefinition cannon = towers[2];
        EnemyDefinition skitter = Enemy("Skitter");
        EnemyDefinition prowler = Enemy("Prowler");
        EnemyDefinition cragback = Enemy("Cragback");
        var sb = new StringBuilder();
        sb.AppendLine($"The Cannon's blast cap per level {DateTime.Now:yyyy-MM-dd HH:mm}. Bar (body + armour) taken off per second per 100 coins of the tower's whole price at that level (StreamDuel, 60 frames a second).");
        sb.Append("The Cannon's data now: cap");
        for (int l = 0; l < LevelsShown; l++)
        {
            sb.Append(Inv($" L{l + 1} {cannon.SplashTargetsAt(l)}"));
        }

        sb.AppendLine(Inv($", blast radius {cannon.SplashRadius:0.0#}. 'before' is three creatures at every level, 'after' is two, three, four, 'no blast' is one creature a shell (what the shell is worth without its splash)."));
        var after = new int[LevelsShown];
        for (int l = 0; l < LevelsShown; l++)
        {
            after[l] = l + 2;
        }

        foreach (DifficultyProfile difficulty in catalog.Difficulties)
        {
            float swarm = StreamDuel.MeanHealthScale(catalog, difficulty, skitter);
            float pack = Scale(catalog, difficulty, prowler, swarm);
            float plate = Scale(catalog, difficulty, cragback, swarm);
            StreamDuel.Stream boss = StreamDuel.Stream.Of(prowler, pack * BigBodyScale, 1f, 1);
            boss.Armour = boss.Body;
            StreamDuel.Stream[] streams =
            {
                StreamDuel.Stream.Of(skitter, swarm, DenseSeconds, StreamCount),
                StreamDuel.Stream.Of(skitter, swarm, SpacedSeconds, StreamCount),
                StreamDuel.Stream.Of(prowler, pack, PackSeconds, StreamCount),
                StreamDuel.Stream.Of(cragback, plate, PackSeconds, ColumnCount),
                StreamDuel.Stream.Of(cragback, plate * BigBodyScale, 1f, 1),
                boss,
            };
            sb.AppendLine();
            sb.AppendLine(Inv($"## {difficulty.name}: Skitter {skitter.MaxHealth * swarm:0.0} health at {skitter.AverageSpeed:0.0#} cells a second, Prowler {prowler.MaxHealth * pack:0.0}, Cragback {cragback.MaxHealth * plate:0.0} + {cragback.Armor * plate:0.0} armour (means of the stored waves)"));
            sb.Append("| Tower |");
            foreach (string column in Columns)
            {
                sb.Append(' ').Append(column).Append(" |");
            }

            sb.AppendLine();
            sb.Append("| --- |");
            foreach (string unused in Columns)
            {
                sb.Append(" --- |");
            }

            sb.AppendLine();
            var gun = new float[LevelsShown, streams.Length];
            var laser = new float[LevelsShown, streams.Length];
            var before = new float[LevelsShown, streams.Length];
            var now = new float[LevelsShown, streams.Length];
            var bare = new float[LevelsShown, streams.Length];
            for (int l = 0; l < LevelsShown; l++)
            {
                for (int s = 0; s < streams.Length; s++)
                {
                    gun[l, s] = Per(towers[0], l, streams[s], towers[0].SplashTargetsAt(l));
                    laser[l, s] = Per(towers[1], l, streams[s], towers[1].SplashTargetsAt(l));
                    before[l, s] = Per(cannon, l, streams[s], OldCap[l]);
                    now[l, s] = Per(cannon, l, streams[s], after[l]);
                    bare[l, s] = Per(cannon, l, streams[s], OneCap[l]);
                }
            }

            Rows(sb, "Gun Turret", gun, null);
            Rows(sb, "Laser", laser, null);
            Rows(sb, "Cannon before (3/3/3)", before, null);
            Rows(sb, "Cannon after (2/3/4)", now, null);
            Rows(sb, "Cannon, no blast", bare, null);
            Rows(sb, "**after / before**", now, before);
            Rows(sb, "**Cannon before / Gun**", before, gun);
            Rows(sb, "**Cannon after / Gun**", now, gun);
            Rows(sb, "**Cannon before / Laser**", before, laser);
            Rows(sb, "**Cannon after / Laser**", now, laser);
            Rows(sb, "**Cannon, no blast / Gun**", bare, gun);
            Rows(sb, "**Cannon, no blast / Laser**", bare, laser);
        }

        sb.AppendLine();
        sb.AppendLine("## The levels without flyers: what the stored waves ask of a gun that hurts one creature a shot");
        sb.AppendLine("Ground creatures as spawned (level bosses in, continent bosses out). 'worth' = data damage per second per 100 coins times the share of it that is worth something on the mix (a gun that works on armour at f pays 1 / f points for a point of armour, one worth k on a family pays 1 / k). No blast, no overkill, no aiming: the rules alone.");
        sb.AppendLine("| Level, difficulty | Armour share of the ground bar | Boss share of the ground bar | Gun L1 / L2 / L3 worth | Laser L1 / L2 / L3 worth | Cannon L1 / L2 / L3 worth | Cannon / Gun | Cannon / Laser |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        int shown = 0;
        foreach (MapNode node in catalog.Nodes)
        {
            if (node.Level == null || shown >= FlyerFreeLevels)
            {
                continue;
            }

            shown++;
            foreach (DifficultyProfile difficulty in catalog.Difficulties)
            {
                Mix mix = MixOf(node.Level, difficulty);
                sb.Append(Inv($"| {node.Level.DisplayName} {difficulty.name} | {mix.ArmourShare:P0} | {mix.BossShare:P0} |"));
                var worth = new double[Towers.Length, LevelsShown];
                for (int t = 0; t < Towers.Length; t++)
                {
                    sb.Append(' ');
                    for (int l = 0; l < LevelsShown; l++)
                    {
                        TowerLevel level = towers[t].GetLevel(l);
                        worth[t, l] = level.Damage / level.FireInterval * 100.0 / StreamDuel.Coins(towers[t], l) * mix.Lands(towers[t].Profile);
                        sb.Append(Inv($"{(l > 0 ? " / " : string.Empty)}{worth[t, l]:0.0}"));
                    }

                    sb.Append(" |");
                }

                sb.Append(Inv($" x{worth[2, 0] / worth[0, 0]:0.00} / x{worth[2, 1] / worth[0, 1]:0.00} / x{worth[2, 2] / worth[0, 2]:0.00} |"));
                sb.AppendLine(Inv($" x{worth[2, 0] / worth[1, 0]:0.00} / x{worth[2, 1] / worth[1, 1]:0.00} / x{worth[2, 2] / worth[1, 2]:0.00} |"));
            }
        }

        File.WriteAllText(ReportPath, sb.ToString());
        return "written " + ReportPath;
    }

    /// <summary>
    /// The role rule on the armoured and the mob reference fights at level 1 (RoleChecks, the mean over the health ladder):
    /// every tower's groups cleared per second per 100 coins, the Cannon with a blast of three and of two, on the rules'
    /// group and on a group of two. Writes Tools/PlayTest/r88b_cannon_roles.txt.
    /// Run: bash Tools/PlayTest/stage.sh run CannonCapLab.cs CannonCapLab.Roles
    /// </summary>
    public static string Roles()
    {
        const string path = "Tools/PlayTest/r88b_cannon_roles.txt";
        CombatRules rules = RoleMatrix.Rules();
        RoleChecks.Moment moment = RoleChecks.Moments[0];
        var sb = new StringBuilder();
        sb.AppendLine("Round 88b: the reference fights at level 1, groups cleared per second per 100 coins (mean over the health ladder)");
        foreach (RoleScenario scenario in rules.Scenarios)
        {
            if (scenario.Trait != EnemyTraits.Armoured && scenario.Trait != EnemyTraits.Mob)
            {
                continue;
            }

            EnemyDefinition enemy = RoleMatrix.Enemy(scenario.Enemy);
            foreach (int count in new[] { scenario.Count, 2 })
            {
                RoleScenario fight = scenario;
                fight.Count = count;
                sb.AppendLine(Inv($"## {scenario.Trait}: {count} x {scenario.Enemy}, {scenario.Spacing} cells apart, margin x{scenario.Margin:0.##}"));
                float cannonOld = 0f;
                float cannonNew = 0f;
                foreach (RoleMatrix.Source source in RoleMatrix.Towers(rules, moment.Level))
                {
                    if (source.Kind == RoleMatrix.SourceKind.Structure)
                    {
                        continue;
                    }

                    if (source.Id == "Cannon")
                    {
                        source.SplashTargets = OldCap[0];
                        cannonOld = RoleChecks.Rate(source, fight, enemy, rules, moment);
                        source.SplashTargets = 2;
                        cannonNew = RoleChecks.Rate(source, fight, enemy, rules, moment);
                        sb.AppendLine(Inv($"Cannon, blast of three {cannonOld:0.0000}; blast of two {cannonNew:0.0000}"));
                    }
                }

                foreach (RoleMatrix.Source source in RoleMatrix.Towers(rules, moment.Level))
                {
                    if (source.Kind == RoleMatrix.SourceKind.Structure || source.Id == "Cannon")
                    {
                        continue;
                    }

                    float rate = RoleChecks.Rate(source, fight, enemy, rules, moment);
                    sb.AppendLine(Inv($"{source.Name} ({(source.Tower.Answers(scenario.Trait) ? "on-role" : "off-role")}, blast cap {source.SplashTargets}, {source.Coins} coins) {rate:0.0000}: the Cannon is x{(rate > 0f ? cannonOld / rate : 0f):0.00} of it with three, x{(rate > 0f ? cannonNew / rate : 0f):0.00} with two"));
                }
            }
        }

        File.WriteAllText(path, sb.ToString());
        return "written " + path;
    }

    private static float Per(TowerDefinition tower, int level, StreamDuel.Stream stream, int cap) =>
        StreamDuel.Dps(tower, level, stream, StreamDuel.GameStep, cap) * 100f / StreamDuel.Coins(tower, level);

    // A creature no stored wave of the difficulty sends walks in at the swarm's scale.
    private static float Scale(CampaignCatalog catalog, DifficultyProfile difficulty, EnemyDefinition enemy, float fallback)
    {
        float scale = StreamDuel.MeanHealthScale(catalog, difficulty, enemy);
        return scale > 0f ? scale : fallback;
    }

    private static void Rows(StringBuilder sb, string name, float[,] values, float[,] under)
    {
        for (int l = 0; l < LevelsShown; l++)
        {
            sb.Append($"| {name} L{l + 1} |");
            for (int s = 0; s < values.GetLength(1); s++)
            {
                sb.Append(under == null ? Inv($" {values[l, s]:0.0} |") : Inv($" x{values[l, s] / Math.Max(0.0001f, under[l, s]):0.00} |"));
            }

            sb.AppendLine();
        }
    }

    private static EnemyDefinition Enemy(string id) => AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");

    /// <summary>What one level sends on the ground on a difficulty: body and armour per creature family.</summary>
    private sealed class Mix
    {
        public readonly double[] Body = new double[Families.Length];
        public readonly double[] Armour = new double[Families.Length];
        public double Boss;

        public double Bar => Sum(Body) + Sum(Armour);

        public double ArmourShare => Bar > 0.0 ? Sum(Armour) / Bar : 0.0;

        public double BossShare => Bar > 0.0 ? Boss / Bar : 0.0;

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

    private static Mix MixOf(LevelDefinition level, DifficultyProfile difficulty)
    {
        var mix = new Mix();
        foreach (WaveDefinition wave in level.GetWaves(difficulty))
        {
            foreach (SpawnGroup group in wave.Groups)
            {
                EnemyDefinition enemy = group.Enemy;
                if (enemy == null || enemy.Flies || enemy.Boss != null)
                {
                    continue;
                }

                float scale = difficulty.HealthMultiplier * level.EnemyHealthScale * wave.HealthMultiplier * (enemy.IsBoss ? wave.BossHealthScale : 1f);
                int count = enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * difficulty.CountMultiplier));
                mix.Body[(int)enemy.Family] += enemy.MaxHealth * scale * count;
                mix.Armour[(int)enemy.Family] += enemy.Armor * scale * count;
                mix.Boss += enemy.IsBoss ? (enemy.MaxHealth + enemy.Armor) * scale * count : 0.0;
            }
        }

        return mix;
    }

    private static string Inv(FormattableString text) => FormattableString.Invariant(text);
}
