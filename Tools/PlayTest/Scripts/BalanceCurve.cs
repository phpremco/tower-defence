using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;

/// <summary>
/// Balance curve probe (no assets written). Builds the campaign model from the stored data, solves and calibrates every
/// level's waves in memory with <see cref="BalanceRules.Default"/> plus optional overrides from Tools/PlayTest/curve_arg.txt
/// (one "Field=value" per line for a float or float[] field of BalanceRules, "d&lt;difficulty&gt;.Field=value" for a
/// DifficultyStats field), and writes Tools/PlayTest/balance_curve.txt: per level and difficulty the human lens (towers
/// alone), the beatable lens (with the hero), the trim against the solver's shape, the bot-lens peaks against the bands
/// and the band failures. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/BalanceCurve.cs --entry BalanceCurve.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class BalanceCurve
{
    private const string ArgPath = "Tools/PlayTest/curve_arg.txt";
    private const string OutPath = "Tools/PlayTest/balance_curve.txt";

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var upgrades = AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
        var unlocks = AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset");
        BalanceRules rules = BalanceRules.Default;
        string[] overrides = File.Exists(ArgPath) ? File.ReadAllLines(ArgPath) : new string[0];
        foreach (string raw in overrides)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("d"))
            {
                continue;
            }

            string[] kv = line.Split('=');
            var field = typeof(BalanceRules).GetField(kv[0].Trim());
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(rules, Parse(kv[1]));
            }
            else if (field != null && field.FieldType == typeof(float[]))
            {
                string[] parts = kv[1].Split(',');
                var values = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    values[i] = Parse(parts[i]);
                }

                field.SetValue(rules, values);
            }
        }

        CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules);
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            model.Enemies[enemy.name] = BalanceLedger.Stats(enemy);
        }

        foreach (string raw in overrides)
        {
            string line = raw.Trim();
            if (!line.StartsWith("d") || line.Length < 3)
            {
                continue;
            }

            int d = line[1] - '0';
            string[] kv = line.Substring(3).Split('=');
            typeof(DifficultyStats).GetField(kv[0].Trim()).SetValue(model.Difficulties[d], Parse(kv[1]));
        }

        BalanceRules human = rules.HumanTowersOnly();
        BalanceRules hero = rules.HumanWithHero();
        string[] names = { "Easy", "Medium", "Hard", "Psycho" };
        var detail = new StringBuilder();
        var table = new StringBuilder("level        | human surplus E M H P   | with hero E M H P       | worst wave w/ hero E M H P | trim E M H P\n");
        for (int index = 0; index < model.Levels.Count; index++)
        {
            string key = model.Levels[index].Key;
            WaveScriptSpec script = Game.Editor.WaveScripts.ByKey(key);
            if (script == null)
            {
                continue;
            }

            List<List<WaveSpec>> sets = model.BuildWaves(index, script, out _);
            rules.CalibrateToHuman = false;
            List<List<WaveSpec>> shape = model.BuildWaves(index, script, out _);
            rules.CalibrateToHuman = true;
            var hs = new StringBuilder();
            var ws = new StringBuilder();
            var worst = new StringBuilder();
            var trims = new StringBuilder();
            for (int d = 0; d < sets.Count; d++)
            {
                LevelInput input = model.InputFor(index, d);
                List<WaveLine> bot = Ledger.Evaluate(input, sets[d], model.Difficulties[d], rules);
                List<WaveLine> lens = Ledger.Evaluate(input, sets[d], model.Difficulties[d], human);
                List<WaveLine> withHero = Ledger.Evaluate(input, sets[d], model.Difficulties[d], hero);
                float botMax = 0f;
                float botBoss = 0f;
                float heroMax = 0f;
                int heroMaxWave = 0;
                foreach (WaveLine l in bot)
                {
                    if (l.Boss)
                    {
                        botBoss = Math.Max(botBoss, l.Pressure);
                    }
                    else
                    {
                        botMax = Math.Max(botMax, l.Pressure);
                    }
                }

                foreach (WaveLine l in withHero)
                {
                    if (l.Pressure > heroMax)
                    {
                        heroMax = l.Pressure;
                        heroMaxWave = l.Wave;
                    }
                }

                List<string> failures = BalanceChecks.Check(key, names[d], d, bot, rules);
                int mid = sets[d].Count / 2;
                float trim = sets[d][mid].HealthMultiplier / Math.Max(0.001f, shape[d][mid].HealthMultiplier);
                float tough = 0f;
                for (int w = 0; w < sets[d].Count; w++)
                {
                    tough += sets[d][w].HealthMultiplier * model.Difficulties[d].Health / sets[d].Count;
                }

                float hMean = BalanceChecks.PlainMean(lens);
                float wMean = BalanceChecks.PlainMean(withHero);
                hs.Append($" {1f / hMean:0.00}");
                ws.Append($" {1f / wMean:0.00}");
                worst.Append($" {heroMax:0.00}");
                trims.Append($" {trim:0.00}/{tough:0.00}");
                PlayerState player = model.PlayerAt(index, d);
                detail.Append($"{key} {names[d]}: human {1f / hMean:0.00}x (target {rules.HumanSurplusAt(d, model.ContinentIndexOf(index)):0.00}), with hero {1f / wMean:0.00}x, worst wave with hero {heroMax:0.00} (w{heroMaxWave}); bot plain max {botMax:0.00} boss {botBoss:0.00} band {rules.MaxPressure(d):0.00}/{rules.MaxPressure(d) * rules.BossAllowance:0.00}; trim {trim:0.00}; research {player.ResearchEarned} earned {player.ResearchSpent} spent, tower bonus {player.MeanTowerBonus:0.00}, hero dmg {player.HeroDamage:0.00}, gate {player.GateHealth:0.00}; failures {failures.Count}\n");
                foreach (string f in failures)
                {
                    detail.Append("    ").Append(f).Append('\n');
                }

                foreach (WaveLine l in withHero)
                {
                    detail.Append("      hero ").Append(l).Append('\n');
                }
            }

            table.Append($"{key,-12} |{hs,-24} |{ws,-24} |{worst,-27} |{trims}\n");
        }

        File.WriteAllText(OutPath, table + "\n" + detail);
        return table.ToString();
    }

    private static float Parse(string text) => float.Parse(text.Trim(), CultureInfo.InvariantCulture);
}
