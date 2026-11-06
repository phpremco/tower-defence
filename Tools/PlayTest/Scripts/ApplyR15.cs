using UnityEditor;

/// <summary>
/// Round 15: writes the Frost Warden's display exposure (BlenderArtCatalog: DisplayExposure 0.6) into its model art without
/// re-running the whole creature import, as BlenderArtBuilder writes it on the next full run; and writes the base art's
/// new fields (idle spots for drones past the bays, the wreck look) with their defaults, so the asset carries them and an
/// editor that loaded the asset before the fields existed does not keep stale values.
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyR15.cs --entry ApplyR15.Run --timeout_ms 60000
/// </summary>
public static class ApplyR15
{
    private static readonly string[] BaseFields =
    {
        "_idlePoints", "_wreckLift", "_wreckGlow", "_wreckFireColor", "_wreckFireIntensity", "_wreckFireRange",
        "_wreckFireHeight", "_wreckRim", "_wreckEmberRate", "_wreckSmokeRate",
    };

    public static string Run()
    {
        var art = AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>("Assets/_Project/Data/Art/FrostWarden.asset");
        var so = new SerializedObject(art);
        so.FindProperty("_displayExposure").floatValue = 0.6f;
        so.ApplyModifiedPropertiesWithoutUndo();

        var baseArt = AssetDatabase.LoadAssetAtPath<Game.Levels.BaseArtDefinition>("Assets/_Project/Data/Art/Colony/BaseArt.asset");
        var fresh = UnityEngine.ScriptableObject.CreateInstance<Game.Levels.BaseArtDefinition>();
        var target = new SerializedObject(baseArt);
        var source = new SerializedObject(fresh);
        foreach (string field in BaseFields)
        {
            target.CopyFromSerializedProperty(source.FindProperty(field));
        }

        target.ApplyModifiedPropertiesWithoutUndo();
        UnityEngine.Object.DestroyImmediate(fresh);
        AssetDatabase.SaveAssets();
        return "FrostWarden display exposure " + art.DisplayExposure + ", base idle spots " + baseArt.IdlePoints.Length + " first " + baseArt.IdlePoints[0];
    }
}
