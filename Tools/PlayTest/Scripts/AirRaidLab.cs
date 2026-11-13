using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;

/// <summary>
/// Round 61, the sky on paper (no assets written). Builds the campaign model as the solve stage does (calibration included),
/// solves every level's waves in memory and prints, per level and difficulty, every wave with flyers: how many flyers, their
/// health as spawned, the ledger's pressure on ground and sky and what the ledger bought for it (so one sees whether a raid was
/// sized against sky guns or against the guns that merely reach the air), next to the multiplier the stored waves carry.
/// Optional overrides in Tools/PlayTest/r61_lab_arg.txt, one per line: "air.&lt;TowerId&gt;=0.2" sets a tower's air share in
/// memory, "rule.&lt;Field&gt;=0.5" a float field of BalanceRules, "levels=C1_03,C1_04" narrows the levels, "label=name" names the output Tools/PlayTest/r61_lab_&lt;name&gt;.txt. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/AirRaidLab.cs --entry AirRaidLab.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class AirRaidLab
{
    private const string ArgPath = "Tools/PlayTest/r61_lab_arg.txt";

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var upgrades = AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
        var unlocks = AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset");
        var calibration = AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath);
        BalanceRules rules = Game.Editor.BalanceSheet.Rules;
        CampaignModel model = BalanceLedger.Campaign(catalog, upgrades, unlocks.StartingTowerIds, rules, calibration);
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            model.Enemies[enemy.name] = BalanceLedger.Stats(enemy);
        }

        string label = "run";
        HashSet<string> wanted = null;
        var notes = new List<string>();
        foreach (string raw in File.Exists(ArgPath) ? File.ReadAllLines(ArgPath) : new string[0])
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
            else if (key.StartsWith("rule.") && typeof(BalanceRules).GetField(key.Substring(5)) is System.Reflection.FieldInfo field && field.FieldType == typeof(float))
            {
                notes.Add($"{field.Name} {field.GetValue(rules)} -> {kv[1].Trim()}");
                field.SetValue(rules, float.Parse(kv[1].Trim(), CultureInfo.InvariantCulture));
            }
            else if (key.StartsWith("rule.") && typeof(BalanceRules).GetField(key.Substring(5)) is System.Reflection.FieldInfo list && list.FieldType == typeof(float[]))
            {
                notes.Add($"{list.Name} -> {kv[1].Trim()}");
                list.SetValue(rules, Array.ConvertAll(kv[1].Trim().Split(','), part => float.Parse(part, CultureInfo.InvariantCulture)));
            }
            else if (key.StartsWith("air.") && model.Towers.TryGetValue(key.Substring(4), out TowerStats tower))
            {
                notes.Add($"{tower.Id} air share {tower.AirFactor.ToString("0.###", CultureInfo.InvariantCulture)} -> {kv[1].Trim()}");
                tower.AirFactor = float.Parse(kv[1].Trim(), CultureInfo.InvariantCulture);
            }
        }

        BalanceRules human = rules.HumanTowersOnly();
        string[] names = { "Easy", "Medium", "Hard", "Psycho" };
        var text = new StringBuilder();
        text.Append($"Air raid lab {DateTime.Now:yyyy-MM-dd HH:mm}; overrides: {(notes.Count > 0 ? string.Join("; ", notes) : "none")}\n");
        text.Append("Per raid wave: flyers, flyer health as spawned, the ledger's line (bot lens), what it bought (type:level x count), solved multiplier (stored).\n\n");
        var summary = new StringBuilder();
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
                List<WaveLine> bot = Ledger.Evaluate(input, sets[d], difficulty, rules);
                List<WaveLine> lens = Ledger.Evaluate(input, sets[d], difficulty, human);
                Game.Levels.WaveDefinition[] stored = level.GetWaves(catalog.Difficulties[d]);
                int moved = 0;
                float airTotal = 0f;
                float allTotal = 0f;
                var raids = new StringBuilder();
                for (int w = 0; w < sets[d].Count; w++)
                {
                    float storedHealth = stored != null && w < stored.Length ? stored[w].HealthMultiplier : -1f;
                    moved += Math.Abs(storedHealth - sets[d][w].HealthMultiplier) > 0.0015f ? 1 : 0;
                    int flyers = 0;
                    float air = 0f;
                    var kinds = new SortedDictionary<string, int>();
                    foreach (GroupSpec group in sets[d][w].Groups)
                    {
                        int count = group.Enemy.IsBoss ? group.Count : Math.Max(1, (int)Math.Ceiling(group.Count * difficulty.Count));
                        float health = count * group.Enemy.Health * difficulty.Health * input.HealthScale * sets[d][w].HealthMultiplier;
                        allTotal += health;
                        if (!group.Enemy.Flies)
                        {
                            continue;
                        }

                        flyers += count;
                        air += health;
                        kinds[group.Enemy.Id] = (kinds.TryGetValue(group.Enemy.Id, out int have) ? have : 0) + count;
                    }

                    airTotal += air;
                    if (flyers == 0)
                    {
                        continue;
                    }

                    var who = new List<string>();
                    foreach (KeyValuePair<string, int> kind in kinds)
                    {
                        who.Add(kind.Value + "x " + kind.Key);
                    }

                    raids.Append(string.Format(CultureInfo.InvariantCulture, "  w{0,-2} {1,-22} air hp {2,5:0} ({3,4:0} each) | {4} | human sky {5:0.00} | bought {6} | x{7:0.000} (stored {8:0.000})\n",
                        w + 1, string.Join(", ", who), air, air / flyers, bot[w], lens[w].AirPressure, Bought(input, bot[w].Purchase), sets[d][w].HealthMultiplier, storedHealth));
                }

                float surplus = 1f / Math.Max(0.001f, BalanceChecks.PlainMean(lens));
                string head = string.Format(CultureInfo.InvariantCulture, "{0} {1}: flyer health {2:0} of {3:0} ({4:0.0}%), human surplus {5:0.00}x (curve {8:0.00}x), waves moved against the stored set {6}/{7}",
                    key, names[d], airTotal, allTotal, allTotal > 0f ? 100f * airTotal / allTotal : 0f, surplus, moved, sets[d].Count, 1f / Math.Max(0.001f, model.HumanTarget(index, d)));
                text.Append(head).Append('\n').Append(raids);
                summary.Append(head).Append('\n');
            }
        }

        string path = "Tools/PlayTest/r61_lab_" + label + ".txt";
        File.WriteAllText(path, text.ToString());
        return "written " + path + "\n" + summary;
    }

    // "1:0x2 3:1x1" as "Cannon L1 x2, Rocket L2 x1".
    private static string Bought(LevelInput input, string purchase)
    {
        if (string.IsNullOrEmpty(purchase))
        {
            return "nothing";
        }

        var parts = new List<string>();
        foreach (string part in purchase.Split(' '))
        {
            string[] head = part.Split(':');
            string[] tail = head.Length == 2 ? head[1].Split('x') : new string[0];
            if (tail.Length != 2 || !int.TryParse(head[0], out int tower) || tower < 0 || tower >= input.Towers.Count)
            {
                parts.Add(part);
                continue;
            }

            parts.Add(input.Towers[tower].Id + " L" + (int.Parse(tail[0], CultureInfo.InvariantCulture) + 1) + " x" + tail[1]);
        }

        return string.Join(", ", parts);
    }
}
