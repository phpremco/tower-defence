using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Enemies;
using UnityEditor;

/// <summary>
/// Runs only the generator's "Difficulties" and "Waves" steps (profiles; level payouts and the solved, human-calibrated
/// waves of every campaign level and difficulty) and saves, for a balance change while the full generator must not run
/// (it syncs the Blender exports from ArtSource). Same code as those steps of Tools/Generate/Scenes and Starter Data. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyWaves.cs --entry ApplyWaves.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyWaves
{
    public static string Run()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        Type generator = typeof(Game.Editor.ProjectGenerator);
        object context = Activator.CreateInstance(generator.GetNestedType("GeneratorContext", BindingFlags.NonPublic), true);
        generator.GetMethod("GenerateDifficulties", flags).Invoke(null, new[] { context });
        AssetDatabase.SaveAssets();

        // The bestiary the generator would hold: every creature asset by its field's name (the Queen is HiveQueen).
        Type bestiaryType = generator.GetNestedType("Bestiary", BindingFlags.NonPublic);
        object bestiary = Activator.CreateInstance(bestiaryType, true);
        var byName = new Dictionary<string, EnemyDefinition>();
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            byName[enemy.name] = enemy;
        }

        foreach (FieldInfo field in bestiaryType.GetFields())
        {
            if (field.FieldType != typeof(EnemyDefinition))
            {
                continue;
            }

            if (!byName.TryGetValue(field.Name, out EnemyDefinition found))
            {
                foreach (KeyValuePair<string, EnemyDefinition> pair in byName)
                {
                    if (pair.Key.EndsWith(field.Name, StringComparison.Ordinal))
                    {
                        found = pair.Value;
                    }
                }
            }

            if (found == null)
            {
                throw new InvalidOperationException("No creature asset for " + field.Name);
            }

            field.SetValue(bestiary, found);
        }

        var campaign = AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
        var upgrades = AssetDatabase.LoadAssetAtPath<Game.Economy.UpgradeCatalog>("Assets/_Project/Data/Upgrades/UpgradeCatalog.asset");
        var unlocks = AssetDatabase.LoadAssetAtPath<Game.Economy.UnlockTable>("Assets/_Project/Data/Unlocks/UnlockTable.asset");
        generator.GetMethod("GenerateCampaignWaves", flags).Invoke(null, new object[] { campaign, upgrades, unlocks, bestiary });
        AssetDatabase.SaveAssets();

        var report = new System.Text.StringBuilder();
        foreach (string guid in AssetDatabase.FindAssets("t:DifficultyProfile", new[] { "Assets/_Project/Data/Difficulty" }))
        {
            var profile = AssetDatabase.LoadAssetAtPath<Game.Levels.DifficultyProfile>(AssetDatabase.GUIDToAssetPath(guid));
            report.Append($"{profile.name}: health {profile.HealthMultiplier}, count {profile.CountMultiplier}, income {profile.IncomeMultiplier}, start {profile.StartingMoneyMultiplier}\n");
        }

        return report.ToString();
    }
}
