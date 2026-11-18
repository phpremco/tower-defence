using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Game.Balance;
using Game.Levels;
using UnityEditor;

/// <summary>
/// Round 84, what the growth of plated troops does to a wave's make-up, on paper (no asset written): per cell and wave
/// the share of the wave's threat that is plated troops (bosses apart) and their number, composed without growth and
/// with the rules as they are, and the growth factor of that wave. For reading how far a growth factor carries a level
/// whose roster is already heavy with armour. Arguments as the ledger lab's, in Tools/PlayTest/plated_share_arg.txt
/// ("rule.&lt;Field&gt;=" a float or a comma list, "label=name"); output Tools/PlayTest/plated_share_&lt;label&gt;.txt. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/PlatedShareLab.cs --entry PlatedShareLab.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class PlatedShareLab
{
    private const string ArgPath = "Tools/PlayTest/plated_share_arg.txt";

    public static string Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var upgrades = AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
        var unlocks = AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset");
        var calibration = AssetDatabase.LoadAssetAtPath<BalanceCalibration>(BalanceCalibration.AssetPath);
        BalanceRules rules = BalanceRules.Default;
        string label = "run";
        var notes = new List<string>();
        foreach (string raw in File.Exists(ArgPath) ? File.ReadAllLines(ArgPath) : new string[0])
        {
            string[] kv = raw.Trim().Split('=');
            if (kv.Length != 2)
            {
                continue;
            }

            if (kv[0] == "label")
            {
                label = kv[1].Trim();
            }
            else if (kv[0].StartsWith("rule.") && typeof(BalanceRules).GetField(kv[0].Substring(5)) is System.Reflection.FieldInfo field)
            {
                Set(field, rules, kv[1]);
                notes.Add(field.Name + " = " + kv[1].Trim());
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
        var text = new StringBuilder();
        text.Append("Plated share lab; overrides: ").Append(notes.Count > 0 ? string.Join("; ", notes) : "none").Append('\n');
        text.Append("Per wave: plated share of the wave's threat without growth -> with it (plated troops without -> with; growth factor). A '-' is a wave without plated troops.\n");
        foreach (string key in new[] { "C1_01", "C1_02", "C1_03", "C1_04", "C1_05", "C1_06" })
        {
            int index = model.IndexOf(key);
            WaveScriptSpec script = Game.Editor.WaveScripts.ByKey(key);
            if (index < 0 || script == null)
            {
                continue;
            }

            int secondRoad = model.Facts[index].SecondRoad;
            for (int d = 0; d < 4; d++)
            {
                int first = WaveComposer.FirstTraitWave(script, model.Enemies, d, secondRoad, rules, false);
                text.Append(string.Format(CultureInfo.InvariantCulture, "\n{0} {1}: first plated wave {2}, grace {3}, growth {4:0.##} per wave through {5} waves\n",
                    key, names[d], first, script.GraceWavesFor(d), rules.ArmourGrowthFor(d), script.FallWithinWavesFor(d)));
                float most = 0f;
                float mostPlain = 0f;
                for (int number = 1; number <= script.Waves; number++)
                {
                    WaveComposer.TraitGrowth growth = WaveComposer.GrowthOn(script, model.Enemies, d, number, secondRoad, rules);
                    float[] kept = rules.ArmourGrowth;
                    rules.ArmourGrowth = new[] { 1f };
                    (float plainShare, int plainCount) = Plated(WaveComposer.Compose(script, model.Enemies, d, number, 1f, secondRoad, rules), rules);
                    rules.ArmourGrowth = kept;
                    (float share, int count) = Plated(WaveComposer.Compose(script, model.Enemies, d, number, 1f, secondRoad, rules), rules);
                    most = System.Math.Max(most, share);
                    mostPlain = System.Math.Max(mostPlain, plainShare);
                    if (count == 0 && plainCount == 0)
                    {
                        text.Append(string.Format(CultureInfo.InvariantCulture, "  w{0,-2} -\n", number));
                        continue;
                    }

                    text.Append(string.Format(CultureInfo.InvariantCulture, "  w{0,-2} {1:0.00} -> {2:0.00}  ({3} -> {4}; x{5:0.00})\n", number, plainShare, share, plainCount, count, growth.Armour));
                }

                text.Append(string.Format(CultureInfo.InvariantCulture, "  most: {0:0.00} -> {1:0.00}\n", mostPlain, most));
            }
        }

        string path = "Tools/PlayTest/plated_share_" + label + ".txt";
        File.WriteAllText(path, text.ToString());
        return "written " + path;
    }

    // The plated troops' share of the wave's threat and their number, bosses apart.
    private static (float Share, int Count) Plated(WaveSpec wave, BalanceRules rules)
    {
        float plated = 0f;
        float all = 0f;
        int count = 0;
        foreach (GroupSpec group in wave.Groups)
        {
            if (group.Enemy == null || group.Enemy.IsBoss)
            {
                continue;
            }

            float threat = Threat.Of(group.Enemy, rules) * group.Count;
            all += threat;
            if (group.Enemy.Armor > 0f)
            {
                plated += threat;
                count += group.Count;
            }
        }

        return (all > 0f ? plated / all : 0f, count);
    }

    private static void Set(System.Reflection.FieldInfo field, object target, string value)
    {
        string[] parts = value.Trim().Split(',');
        if (field.FieldType == typeof(float))
        {
            field.SetValue(target, float.Parse(parts[0], CultureInfo.InvariantCulture));
        }
        else if (field.FieldType == typeof(float[]))
        {
            var values = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                values[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
            }

            field.SetValue(target, values);
        }
    }
}
