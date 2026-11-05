using System.Reflection;
using UnityEditor;

/// <summary>
/// Runs only the generator's "Boss intros" step (Docs/BOSS_INTROS.md): the intro assets, their answer dialogues and the
/// story's list, then validates them. No art and no other data is touched.
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyBossIntros.cs --entry ApplyBossIntros.Run --timeout_ms 120000
/// </summary>
public static class ApplyBossIntros
{
    public static string Run()
    {
        typeof(Game.Editor.ProjectGenerator).GetMethod("GenerateBossIntros", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        AssetDatabase.SaveAssets();
        string report = string.Empty;
        foreach (string guid in AssetDatabase.FindAssets("t:BossIntroDefinition"))
        {
            var intro = AssetDatabase.LoadAssetAtPath<Game.Levels.BossIntroDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            var errors = new System.Collections.Generic.List<string>();
            intro.Validate(errors);
            report += $"{intro.name}: {intro.Boss?.name} barks {intro.Barks.Count} again {intro.RepeatBarks.Count} errors {errors.Count} {string.Join(" ", errors)}; ";
        }

        return report;
    }
}
