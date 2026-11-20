using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;

/// <summary>
/// Round 64, the level bosses on paper (no assets written). Builds the campaign model as the solve stage does (calibration
/// included), solves every level's waves in memory and prints, per level and difficulty: the human surplus against the curve,
/// the level's total health against the stored set, and for every boss wave the boss with its health as spawned (the
/// difficulty, the level, the wave's multiplier and the boss's own scale), what the boss asks alone and what the whole wave
/// asks of the expected player with the hero, next to the stored wave. Overrides in Tools/PlayTest/r64_lab_arg.txt, one per
/// line: "rule.&lt;Field&gt;=" a BalanceRules field (a float or a comma list), "enemy.&lt;Id&gt;=60,160" a creature's body and armour,
/// "armour.&lt;TowerId&gt;=0.2" a tower's armour factor, "hero.blade=0.5", "eff.&lt;TowerId&gt;=" a calibrated efficiency,
/// "cal.&lt;Field&gt;=" a fitted factor, "levels=C1_03", "label=name" (output Tools/PlayTest/r64_lab_&lt;name&gt;.txt). Run:
/// unity command run_script --file Tools/PlayTest/Scripts/BossLab.cs --entry BossLab.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class BossLab
{
    private const string ArgPath = "Tools/PlayTest/r64_lab_arg.txt";

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var upgrades = AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
        var unlocks = AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset");
        var calibration = AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath);
        // A fresh rules instance, overridden before the model is built (AirRaidLab, round 62).
        BalanceRules rules = BalanceRules.Default;
        string[] args = File.Exists(ArgPath) ? File.ReadAllLines(ArgPath) : new string[0];
        var notes = new List<string>();
        foreach (string raw in args)
        {
            string[] kv = raw.Trim().Split('=');
            if (kv.Length != 2 || !kv[0].Trim().StartsWith("rule."))
            {
                continue;
            }

            System.Reflection.FieldInfo field = typeof(BalanceRules).GetField(kv[0].Trim().Substring(5));
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(rules, Parse(kv[1]));
                notes.Add($"{field.Name} = {kv[1].Trim()}");
            }
            else if (field != null && field.FieldType == typeof(float[]))
            {
                field.SetValue(rules, Array.ConvertAll(kv[1].Trim().Split(','), Parse));
                notes.Add($"{field.Name} = {kv[1].Trim()}");
            }
            else if (field != null && field.FieldType == typeof(bool[]))
            {
                field.SetValue(rules, Array.ConvertAll(kv[1].Trim().Split(','), bool.Parse));
                notes.Add($"{field.Name} = {kv[1].Trim()}");
            }
        }

        CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules, calibration);
        rules = model.Rules;
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            model.Enemies[enemy.name] = BalanceLedger.Stats(enemy);
        }

        string label = "run";
        HashSet<string> wanted = null;
        foreach (string raw in args)
        {
            string[] kv = raw.Trim().Split('=');
            if (kv.Length != 2)
            {
                continue;
            }

            string key = kv[0].Trim();
            if (key == "label")
            {
                label = kv[1].Trim();
            }
            else if (key == "levels")
            {
                wanted = new HashSet<string>(kv[1].Trim().Split(','));
            }
            else if (key.StartsWith("enemy.") && model.Enemies.TryGetValue(key.Substring(6), out EnemyStats creature))
            {
                string[] parts = kv[1].Split(',');
                notes.Add($"{creature.Id} {creature.Health:0}+{creature.Armor:0} -> {kv[1].Trim()}");
                creature.Health = Parse(parts[0]);
                creature.Armor = Parse(parts[1]);
            }
            else if (key.StartsWith("armour.") && model.Towers.TryGetValue(key.Substring(7), out TowerStats plated))
            {
                notes.Add($"{plated.Id} armour factor {plated.ArmourFactor.ToString("0.##", CultureInfo.InvariantCulture)} -> {kv[1].Trim()}");
                plated.ArmourFactor = Parse(kv[1]);
            }
            else if (key == "hero.blade" && model.Hero != null)
            {
                notes.Add($"blade armour factor {model.Hero.BladeArmourFactor.ToString("0.##", CultureInfo.InvariantCulture)} -> {kv[1].Trim()}");
                model.Hero.BladeArmourFactor = Parse(kv[1]);
            }
            else if (key.StartsWith("eff.") && model.Towers.TryGetValue(key.Substring(4), out TowerStats fitted))
            {
                notes.Add($"{fitted.Id} efficiency {fitted.Efficiency.ToString("0.###", CultureInfo.InvariantCulture)} -> {kv[1].Trim()}");
                fitted.Efficiency = Parse(kv[1]);
            }
            else if (key.StartsWith("cal.") && typeof(BalanceRules).GetField(key.Substring(4)) is System.Reflection.FieldInfo fit && fit.FieldType == typeof(float))
            {
                notes.Add($"{fit.Name} {fit.GetValue(rules)} -> {kv[1].Trim()}");
                fit.SetValue(rules, Parse(kv[1]));
            }
        }

        BalanceRules human = rules.HumanTowersOnly();
        BalanceRules withHero = rules.HumanWithHero();
        string[] names = { "Easy", "Medium", "Hard", "Psycho" };
        var text = new StringBuilder();
        text.Append($"Boss lab {DateTime.Now:yyyy-MM-dd HH:mm}; overrides: {(notes.Count > 0 ? string.Join("; ", notes) : "none")}\n");
        text.Append("Per boss wave: boss health as spawned (stored), the wave's multiplier and the boss's own scale (stored), boss alone / whole wave pressure with the hero (cap).\n\n");
        for (int node = 0; node < catalog.Nodes.Count; node++)
        {
            LevelDefinition level = catalog.Nodes[node].Level;
            if (level == null)
            {
                continue;
            }

            string key = level.name.StartsWith("Level_") ? level.name.Substring(6) : level.name;
            int index = model.IndexOf(key);
            WaveScriptSpec script = Game.Editor.WaveScripts.ByKey(key);
            if (script == null || index < 0 || (wanted != null && !wanted.Contains(key)))
            {
                continue;
            }

            List<List<WaveSpec>> sets = model.BuildWaves(index, script, out _);
            for (int d = 0; d < sets.Count; d++)
            {
                LevelInput input = model.InputFor(index, d);
                DifficultyStats difficulty = model.Difficulties[d];
                List<WaveLine> lens = Ledger.Evaluate(input, sets[d], difficulty, human);
                List<WaveLine> hero = Ledger.Evaluate(input, sets[d], difficulty, withHero);
                WaveDefinition[] stored = level.GetWaves(catalog.Difficulties[d]);
                int moved = 0;
                float total = 0f;
                float storedTotal = 0f;
                var bosses = new StringBuilder();
                for (int w = 0; w < sets[d].Count; w++)
                {
                    WaveSpec wave = sets[d][w];
                    float storedHealth = stored != null && w < stored.Length ? stored[w].HealthMultiplier : -1f;
                    float storedBoss = stored != null && w < stored.Length ? stored[w].BossHealthScale : 1f;
                    float bossScale = wave.BossHealthScale;
                    moved += Math.Abs(storedHealth - wave.HealthMultiplier) > 0.0015f || Math.Abs(storedBoss - bossScale) > 0.0015f ? 1 : 0;
                    foreach (GroupSpec group in wave.Groups)
                    {
                        int count = group.Enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * difficulty.Count));
                        float each = group.Enemy.EffectiveHealth * difficulty.Health * input.HealthScale * difficulty.ShareFor(group.Enemy);
                        total += count * each * wave.HealthMultiplier * (group.Enemy.IsBoss ? bossScale : 1f);
                        storedTotal += storedHealth > 0f ? count * each * storedHealth * (group.Enemy.IsBoss ? storedBoss : 1f) : 0f;
                        if (!group.Enemy.IsBoss)
                        {
                            continue;
                        }

                        var alone = new List<WaveSpec>(sets[d]);
                        var only = new WaveSpec { StartDelay = wave.StartDelay, HealthMultiplier = wave.HealthMultiplier, BossHealthScale = bossScale };
                        only.Groups.AddRange(wave.Groups.FindAll(g => g.Enemy.IsBoss));
                        alone[w] = only;
                        float asksAlone = Ledger.Evaluate(input, alone, difficulty, withHero)[w].Pressure;
                        bosses.Append(string.Format(CultureInfo.InvariantCulture, "  w{0,-2} {1,-15} {2,5:0} hp (stored {3,5:0}) | wave x{4:0.000} boss x{5:0.000} (stored x{6:0.000} x{7:0.000}) | alone {8:0.00}, wave {9:0.00} (cap {10:0.00}) | money {11}\n",
                            w + 1, group.Enemy.Id, each * wave.HealthMultiplier * bossScale, storedHealth > 0f ? each * storedHealth * storedBoss : 0f, wave.HealthMultiplier, bossScale, storedHealth, storedBoss,
                            asksAlone, hero[w].Pressure, rules.HeroCapFor(d), hero[w].Money));
                    }
                }

                float surplus = 1f / Math.Max(0.001f, BalanceChecks.PlainMean(lens));
                text.Append(string.Format(CultureInfo.InvariantCulture, "{0} {1}: human surplus {2:0.00}x (curve {3:0.00}x), waves moved against the stored set {4}/{5}, total health {6:0} (x{7:0.000} of the stored set)\n",
                    key, names[d], surplus, 1f / Math.Max(0.001f, model.ModelTarget(index, d)), moved, sets[d].Count, total, storedTotal > 0f ? total / storedTotal : 0f));
                text.Append(bosses);
            }
        }

        string path = "Tools/PlayTest/r64_lab_" + label + ".txt";
        File.WriteAllText(path, text.ToString());
        return "written " + path + "\n" + text;
    }

    private static float Parse(string value) => float.Parse(value.Trim(), CultureInfo.InvariantCulture);
}
