using UnityEditor;

/// <summary>
/// Round 15b: the base wreck's glow 2.5 -> 1 (tried 1.8: the core still burned out white; at 1 it glows soft pink; BaseArtDefinition default changed too).
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyR15b.cs --entry ApplyR15b.Run --timeout_ms 60000
/// </summary>
public static class ApplyR15b
{
    public static string Run()
    {
        var art = AssetDatabase.LoadAssetAtPath<Game.Levels.BaseArtDefinition>("Assets/_Project/Data/Art/Colony/BaseArt.asset");
        var so = new SerializedObject(art);
        so.FindProperty("_wreckGlow").floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return "wreck glow " + art.WreckGlow;
    }
}
