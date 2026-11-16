using UnityEditor;

/// <summary>
/// Round 68: the Rotbloom back to its round 67 health (3550) after two finale apply steps took it to 28200. In play it held
/// x8.9 on Medium at 3550 and lost at 28200 (x0.92); on paper 28200 asks 6.8 times the hero cap. Play and paper disagree for
/// this boss since round 67, so the paper cap holds it until its mechanic is re-modelled (ContinentFinaleTests.PaperHeldBosses).
/// Run: unity command run_script --file Tools/PlayTest/Scripts/R68Rotbloom.cs --entry R68Rotbloom.Run --timeout_ms 60000 --timeout 90
/// </summary>
public static class R68Rotbloom
{
    public const float Health = 3550f;

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
