using System.Reflection;
using UnityEditor;

/// <summary>
/// Runs only the generator's "Continent bosses" step (boss data, Codex rows, the developer boss arena), without touching any
/// art. Run: unity command run_script --file Tools/PlayTest/Scripts/ApplyBossData.cs --entry ApplyBossData.Run --timeout_ms 300000 --timeout 360
/// </summary>
public static class ApplyBossData
{
    public static string Run()
    {
        typeof(Game.Editor.ProjectGenerator).GetMethod("GenerateContinentBosses", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        AssetDatabase.SaveAssets();
        return "continent bosses written";
    }
}
