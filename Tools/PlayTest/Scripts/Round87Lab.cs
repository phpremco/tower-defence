using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Editor;
using Game.Levels;

/// <summary>
/// Round 87, the stored waves on paper: per cell every wave's walk-in multiplier with its kind and its marks (peak,
/// boss wave, breather after a boss, raid, surprise), and every level boss as spawned (bar, body, armour) against the
/// formula bar and against the bar of the toughest ordinary walker met before it (how many times it, and the bar the
/// floor would give), with whether the floor binds. Reads the assets only. Run: bash Tools/PlayTest/stage.sh run Round87Lab.cs Round87Lab.Run
/// (writes Tools/PlayTest/r87_waves.txt and Tools/PlayTest/r87_bosses.txt).
/// </summary>
public static class Round87Lab
{
    private const string WavesPath = "Tools/PlayTest/r87_waves.txt";
    private const string BossesPath = "Tools/PlayTest/r87_bosses.txt";
    private static readonly string[] DifficultyNames = { "Easy", "Medium", "Hard", "Psycho" };

    public static string Run()
    {
        BalanceStages.Loaded loaded = BalanceStages.Load(true);
        CampaignModel model = loaded.Model;
        BalanceRules rules = model.Rules;
        var waves = new StringBuilder(Line($"Stored waves, round 87: walk-in multiplier = DifficultyScale x level scalar x ramp x kind x peak. DifficultyScale {Join(rules.DifficultyScale)}; ramp {rules.RampStart:0.###} to {rules.RampEnd:0.###}; rush {rules.RushFactor:0.##}, heavy {rules.HeavyFactor:0.##}, raid {rules.AirFactor:0.##}, after a boss {rules.AfterBossFactor:0.##}, boss crowd {rules.BossCrowdFactor:0.##}, peak {rules.PeakFactor:0.##}.\n"));
        var bosses = new StringBuilder(Line($"Level bosses as spawned, round 87: bar = BossBase {rules.BossBase:0} x BossDifficulty {Join(rules.BossDifficulty)} x (1 + {rules.BossLevelGrowth:0.##} x level) x ({rules.BossEarlyShare:0.##} + {1f - rules.BossEarlyShare:0.##} x phase) x (flies ? {rules.FlyingBossShare:0.##} : 1); floor {Join(rules.BossOverRegular)} times the bar of the toughest ordinary walker before it.\n"));
        bosses.Append("level | difficulty | wave | boss | bar | body | armour | formula bar | x toughest walker's bar | floor's bar | floor binds\n");
        foreach (MapNode node in loaded.Catalog.Nodes)
        {
            if (node.Level == null)
            {
                continue;
            }

            string key = node.Level.name.StartsWith("Level_") ? node.Level.name.Substring(6) : node.Level.name;
            int index = model.IndexOf(key);
            WaveScriptSpec script = WaveScripts.ByKey(key);
            if (index < 0 || script == null)
            {
                continue;
            }

            // Sizing the waves leaves the flyer floors on the script: kinds and peaks are read with them, as the stored waves were.
            model.BuildWaves(index, script, out _);
            int level = model.ContinentIndexOf(index);
            for (int d = 0; d < loaded.Catalog.Difficulties.Count; d++)
            {
                float spawn = model.Difficulties[d].Health * model.Facts[index].HealthScale;
                List<int> peaks = WaveSolver.PeaksOf(script, model.Enemies, d, model.Facts[index].SecondRoad, rules);
                waves.Append(Line($"\n{key} {DifficultyNames[d]} (level scalar {script.TargetScaleFor(d):0.###}, peaks {string.Join(", ", peaks)})\n"));
                var met = new BossYardstick();
                int number = 0;
                foreach (WaveDefinition definition in node.Level.GetWaves(loaded.Catalog.Difficulties[d]))
                {
                    number++;
                    WaveSpec wave = BalanceLedger.Spec(definition, model.Enemies);
                    BossSpec boss = WaveComposer.BossOn(script, d, number);
                    var marks = new List<string> { WaveComposer.KindOf(script, d, number).ToString() };
                    AddIf(marks, peaks.Contains(number), "PEAK");
                    AddIf(marks, boss != null, "boss " + (boss != null ? boss.Enemy : string.Empty));
                    AddIf(marks, WaveComposer.BossOn(script, d, number - 1) != null, "after boss");
                    AddIf(marks, wave.AirRaid, "raid");
                    AddIf(marks, wave.Surprise != SurpriseKind.None, "surprise " + wave.Surprise);
                    waves.Append(Line($"  wave {number,2}: x{wave.HealthMultiplier:0.000}  {string.Join(", ", marks)}\n"));
                    foreach (GroupSpec group in wave.Groups)
                    {
                        EnemyStats stats = group.Enemy;
                        if (stats == null || !stats.IsBoss || stats.IsContinentBoss)
                        {
                            continue;
                        }

                        float multiple = wave.HealthMultiplier * (wave.BossHealthScale > 0f ? wave.BossHealthScale : 1f);
                        float phase = WaveFormula.Phase(number, script.Waves);
                        float formula = WaveFormula.BossBar(rules, level, d, phase, stats.Flies);
                        float bar = BossYardstick.Bar(stats) * multiple * spawn;
                        float floor = BossYardstick.Bar(stats) * met.Multiple(stats, rules.BossOverRegularFor(d)) * spawn * (stats.Flies ? rules.FlyingBossShare : 1f);
                        bool binds = met.HasMet && bar > formula * 1.01f;
                        bosses.Append(Line($"{key} | {DifficultyNames[d]} | {number} | {stats.Id}{(stats.Flies ? " (flies)" : string.Empty)} | {bar:0} | {stats.Health * multiple * spawn:0} | {stats.Armor * multiple * spawn:0} | {formula:0} | x{met.Over(stats, multiple):0.0} | {floor:0} | {(binds ? "YES" : "no")}\n"));
                    }

                    if (!wave.AirRaid)
                    {
                        met.Meet(wave, wave.HealthMultiplier);
                    }
                }
            }
        }

        File.WriteAllText(WavesPath, waves.ToString());
        File.WriteAllText(BossesPath, bosses.ToString());
        return "written " + WavesPath + " and " + BossesPath;
    }

    private static void AddIf(List<string> marks, bool condition, string mark)
    {
        if (condition)
        {
            marks.Add(mark);
        }
    }

    private static string Join(float[] values) => string.Join(" / ", Array.ConvertAll(values, v => v.ToString("0.###", CultureInfo.InvariantCulture)));

    private static string Line(FormattableString text) => FormattableString.Invariant(text);
}
