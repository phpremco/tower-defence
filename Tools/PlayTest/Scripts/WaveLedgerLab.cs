using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;

/// <summary>
/// Round 79, one cell's waves through the solver's ledger on paper (no assets written): per wave the grammar's pattern, the
/// multiplier this solve gives against the stored one, the solver's purse and what it buys, the ground and flyer health,
/// the ground window (active seconds), the flyers' window and both pressures. For reading why a wave's multiplier moved.
/// Arguments in Tools/PlayTest/wave_ledger_arg.txt, one per line: "cell=C1_05:2" (level key, difficulty index; several
/// lines allowed), "grammar.&lt;Field&gt;=" a float field of WaveGrammarRules (this run only: the rules are a fresh instance),
/// "rule.&lt;Field&gt;=" a float field of BalanceRules, "label=name" (output Tools/PlayTest/wave_ledger_&lt;name&gt;.txt). Run:
/// unity command run_script --file Tools/PlayTest/Scripts/WaveLedgerLab.cs --entry WaveLedgerLab.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class WaveLedgerLab
{
    private const string ArgPath = "Tools/PlayTest/wave_ledger_arg.txt";

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var upgrades = AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
        var unlocks = AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset");
        var calibration = AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath);
        string[] args = File.Exists(ArgPath) ? File.ReadAllLines(ArgPath) : new string[0];
        BalanceRules rules = BalanceRules.Default;
        var notes = new List<string>();
        var cells = new List<(string Key, int Difficulty)>();
        string label = "run";
        foreach (string raw in args)
        {
            string[] kv = raw.Trim().Split('=');
            if (kv.Length != 2)
            {
                continue;
            }

            string key = kv[0].Trim();
            if (key == "cell")
            {
                string[] parts = kv[1].Trim().Split(':');
                cells.Add((parts[0], int.Parse(parts[1])));
            }
            else if (key == "label")
            {
                label = kv[1].Trim();
            }
            else if (key.StartsWith("grammar.") && typeof(WaveGrammarRules).GetField(key.Substring(8)) is System.Reflection.FieldInfo g)
            {
                Set(g, rules.Grammar, kv[1]);
                notes.Add($"{g.Name} = {kv[1].Trim()}");
            }
            else if (key.StartsWith("rule.") && typeof(BalanceRules).GetField(key.Substring(5)) is System.Reflection.FieldInfo r)
            {
                Set(r, rules, kv[1]);
                notes.Add($"{r.Name} = {kv[1].Trim()}");
            }
        }

        CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules, calibration);
        rules = model.Rules;
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            model.Enemies[enemy.name] = BalanceLedger.Stats(enemy);
        }

        string[] names = { "Easy", "Medium", "Hard", "Psycho" };
        string letters = "sCWTXA";
        var text = new StringBuilder();
        text.Append($"Wave ledger lab {DateTime.Now:yyyy-MM-dd HH:mm}; overrides: {(notes.Count > 0 ? string.Join("; ", notes) : "none")}\n");
        var summary = new StringBuilder();
        var solved = new Dictionary<string, List<List<WaveSpec>>>();
        BalanceRules withHero = rules.HumanWithHero();
        foreach ((string key, int d) in cells)
        {
            int index = model.IndexOf(key);
            WaveScriptSpec script = Game.Editor.WaveScripts.ByKey(key);
            LevelDefinition level = null;
            for (int node = 0; node < catalog.Nodes.Count; node++)
            {
                if (catalog.Nodes[node].Level != null && catalog.Nodes[node].Level.name == "Level_" + key)
                {
                    level = catalog.Nodes[node].Level;
                }
            }

            if (script == null || index < 0 || level == null)
            {
                text.Append($"{key}: unknown\n");
                continue;
            }

            if (!solved.TryGetValue(key, out List<List<WaveSpec>> sets))
            {
                sets = model.BuildWaves(index, script, out _);
                solved[key] = sets;
            }

            // The lens the solver reads (round 83): a competent player's towers alone, with the difficulty's real money.
            LevelInput input = model.InputFor(index, d);
            DifficultyStats difficulty = model.Difficulties[d];
            var loads = new List<WaveLoad>();
            List<WaveLine> lines = Ledger.Evaluate(input, sets[d], difficulty, rules.HumanTowersOnly(), loads);
            WaveDefinition[] stored = level.GetWaves(catalog.Difficulties[d]);
            // The solver's own multipliers, before the difficulty's factor and the cell's scalar.
            float scale = rules.DifficultyScaleFor(d) * script.TargetScaleFor(d);
            float[] shares = WaveSolver.Shares(script, model.Enemies, d, model.Facts[index].SecondRoad, rules);
            float aim = model.ModelTarget(index, d);
            var raw = new float[sets[d].Count];
            for (int w = 0; w < raw.Length; w++)
            {
                raw[w] = shares[w] * aim;
            }

            List<WaveLine> hero = Ledger.Evaluate(model.InputFor(index, d), sets[d], difficulty, withHero);
            List<WaveLine> lens = Ledger.Evaluate(model.InputFor(index, d), sets[d], difficulty, rules.HumanTowersOnly());
            text.Append($"\n{key} {names[d]} (the human lens, towers alone; scale x{scale:0.###}; aim = the wave's own target at scale one; hero = the same player with the hero, cap {rules.HeroCapFor(d)}; span = active seconds of the ground stream, sky = the flyers' window; boss = the health multiplier a level boss carries, now and stored)\n");
            float sumNow = 0f;
            float sumStored = 0f;
            var moved = new List<string>();
            for (int w = 0; w < lines.Count; w++)
            {
                WaveSpec wave = sets[d][w];
                WaveLine line = lines[w];
                float was = stored != null && w < stored.Length ? stored[w].HealthMultiplier : 0f;
                sumNow += wave.HealthMultiplier;
                sumStored += was;
                if (was > 0f && Math.Abs(wave.HealthMultiplier / was - 1f) > 0.03f)
                {
                    moved.Add(string.Format(CultureInfo.InvariantCulture, "w{0} x{1:0.###}->x{2:0.###}", w + 1, was, wave.HealthMultiplier));
                }

                text.Append(string.Format(CultureInfo.InvariantCulture,
                    "  w{0,-2} {1}{2}{3}{4} x{5,-7:0.###} (stored x{6,-7:0.###}, aim {17:0.00}, hero {18:0.00}, human {19:0.00}{20}, boss x{21:0.###} stored x{22:0.###}) money {7,5} hp {8,6:0}/{9,5:0} span {10,5:0.0}s sky {11,5:0.0}s n {12,3}/{13,2} pressure {14:0.00}/{15:0.00} | {16}\n",
                    w + 1, letters[(int)wave.Pattern], wave.Beats, wave.Surprise == SurpriseKind.Rift ? "!" : wave.Surprise == SurpriseKind.Flank ? "^" : wave.Surprise == SurpriseKind.Authored ? "*" : " ", line.Boss ? "B" : " ",
                    wave.HealthMultiplier, was, line.Money, line.GroundHealth, line.AirHealth, loads[w].Span, loads[w].AirWindow, loads[w].GroundCount, loads[w].AirCount,
                    line.GroundPressure, line.AirPressure, line.Purchase, raw[w], hero[w].Pressure, lens[w].Pressure, w < BalanceChecks.OpeningWaves || lens[w].Boss || lens[w].CounterCheck || lens[w].AirRaid ? "-" : "p", wave.HealthMultiplier * wave.BossHealthScale, stored != null && w < stored.Length ? stored[w].HealthMultiplier * stored[w].BossHealthScale : 0f));
            }

            float plain = BalanceChecks.PlainMean(lens);
            float asked = model.HumanTarget(index, d);
            var ground = new List<float>();
            for (int w = 0; w < sets[d].Count; w++)
            {
                if (!sets[d][w].AirRaid)
                {
                    ground.Add(sets[d][w].HealthMultiplier);
                }
            }

            ground.Sort();
            float median = ground.Count > 0 ? ground[ground.Count / 2] : 0f;
            summary.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} {1,-6} scale x{8:0.###}; sum of multipliers x{2:0.000} of the stored set; plain mean {4:0.00} against {5:0.00} at the scale; ground waves x{6:0.00} to x{7:0.00} of their median; moved more than 3 %: {3}", key, names[d], sumStored > 0f ? sumNow / sumStored : 0f, moved.Count > 0 ? string.Join(", ", moved) : "none",
                plain, asked, median > 0f ? ground[0] / median : 0f, median > 0f ? ground[ground.Count - 1] / median : 0f, scale));
        }

        text.AppendLine().AppendLine("Against the stored waves:").Append(summary);

        string path = "Tools/PlayTest/wave_ledger_" + label + ".txt";
        File.WriteAllText(path, text.ToString());
        return "written " + path + "\n" + text;
    }

    private static void Set(System.Reflection.FieldInfo field, object target, string value)
    {
        if (field.FieldType == typeof(float))
        {
            field.SetValue(target, float.Parse(value.Trim(), CultureInfo.InvariantCulture));
        }
        else if (field.FieldType == typeof(int))
        {
            field.SetValue(target, int.Parse(value.Trim(), CultureInfo.InvariantCulture));
        }
        else if (field.FieldType == typeof(float[]))
        {
            field.SetValue(target, Array.ConvertAll(value.Trim().Split(','), v => float.Parse(v.Trim(), CultureInfo.InvariantCulture)));
        }
        else if (field.FieldType == typeof(int[]))
        {
            field.SetValue(target, Array.ConvertAll(value.Trim().Split(','), v => int.Parse(v.Trim(), CultureInfo.InvariantCulture)));
        }
        else if (field.FieldType == typeof(bool[]))
        {
            field.SetValue(target, Array.ConvertAll(value.Trim().Split(','), v => bool.Parse(v.Trim())));
        }
        else if (field.FieldType == typeof(bool))
        {
            field.SetValue(target, bool.Parse(value.Trim()));
        }
    }
}
