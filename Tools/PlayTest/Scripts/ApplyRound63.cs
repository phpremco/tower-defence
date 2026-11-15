using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 63: writes this round's base data the way the changed defaults and BlenderColonyBuilder.BuildBase would (without
/// rebuilding the base art): the angry beat's length and clip rate on the cutscene cast, the shuttle's longer, higher climb and drift, and its launch roar. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound63.cs --entry ApplyRound63.Run
/// </summary>
public static class ApplyRound63
{
    private const string BaseArtPath = "Assets/_Project/Data/Art/Colony/BaseArt.asset";
    private const string CastPath = "Assets/_Project/Data/Story/Cutscene/CutsceneCast.asset";
    private const string RoarPath = "Assets/_Project/Audio/Approved/Intro/Launch/launch_booster_burn_loop.wav";

    public static string Run()
    {
        var art = AssetDatabase.LoadAssetAtPath<Game.Levels.BaseArtDefinition>(BaseArtPath);
        if (art == null)
        {
            return "no base art";
        }

        var so = new SerializedObject(art);
        so.FindProperty("_shuttleFlightSeconds").floatValue = 8f;
        so.FindProperty("_shuttleClimb").floatValue = 22f;
        so.FindProperty("_shuttleDrift").floatValue = 10f;
        var roar = AssetDatabase.LoadAssetAtPath<AudioClip>(RoarPath);
        so.FindProperty("_shuttleRoar").objectReferenceValue = roar;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(art);
        var cast = AssetDatabase.LoadAssetAtPath<Game.Levels.CutsceneCast>(CastPath);
        var castSo = new SerializedObject(cast);
        // The point-and-shout clip (1.4 s) at 0.9 fills the beat, so the point never drops before the draw.
        castSo.FindProperty("_angrySeconds").floatValue = 1.6f;
        castSo.FindProperty("_angryClipRate").floatValue = 0.9f;
        // The brace's second sway beat: 2.1 Hz keeps the torso under a degree per frame at 60 fps (2.7 Hz jerked 1.005).
        castSo.FindProperty("_brace._swayHertzSecond").floatValue = 2.1f;
        castSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cast);
        AssetDatabase.SaveAssets();
        return "cast: angry 1.6 s at 0.9; base art: flight 8 s, climb 22 m, drift 10 m, roar " + (roar != null ? roar.name : "MISSING");
    }
}
