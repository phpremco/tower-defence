using System.Reflection;
using UnityEditor;

/// <summary>
/// Runs only the generator's "Continent bosses" step (boss data, their brood, Codex rows, the developer boss arena and its
/// liquid levels), without touching any art, and validates the arena levels. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyBossData.cs --entry ApplyBossData.Run --timeout_ms 300000 --timeout 360
/// </summary>
public static class ApplyBossData
{
    public static string Run()
    {
        typeof(Game.Editor.ProjectGenerator).GetMethod("GenerateContinentBosses", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        AssetDatabase.SaveAssets();
        string report = "continent bosses written";
        foreach (string id in new[] { "Arena_Lava", "Arena_Ice", "Arena_Bog", "Arena_Ruins" })
        {
            var level = AssetDatabase.LoadAssetAtPath<Game.Levels.LevelDefinition>($"Assets/_Project/Data/Levels/Arena/{id}.asset");
            var errors = new System.Collections.Generic.List<string>();
            Game.Levels.LevelValidator.Validate(level, errors);
            report += $"; {id}: {errors.Count} errors " + string.Join(" / ", errors);
        }

        return report;
    }
}
