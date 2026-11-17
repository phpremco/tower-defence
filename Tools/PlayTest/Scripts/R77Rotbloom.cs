using UnityEditor;

/// <summary>
/// Round 77 (Remco, 2026-11-17): the Rotbloom back to the 28200 the finale stage applied in round 68, in the asset; the
/// generator's spec (ProjectGenerator.Bosses.cs) carries the same number. Round 68 had set it back to 3550 by hand
/// (R68Rotbloom.cs) because the paper cap and the probe disagreed. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/R77Rotbloom.cs --entry R77Rotbloom.Run --timeout_ms 60000 --timeout 90
/// </summary>
public static class R77Rotbloom
{
    public const float Health = 28200f;

    public static string Run()
    {
        var boss = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/Rotbloom.asset");
        var serialized = new SerializedObject(boss);
        float before = serialized.FindProperty("_maxHealth").floatValue;
        serialized.FindProperty("_maxHealth").floatValue = Health;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(boss);
        AssetDatabase.SaveAssets();
        return $"Rotbloom health {before} -> {boss.MaxHealth}";
    }
}
