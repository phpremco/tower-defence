using UnityEditor;

/// <summary>
/// Round 13b: syncs the UI exports (Blender round 38 adds UI_ComicFrame) and cuts the sprites, so the boss-intro cut-in
/// and answer box wear the comic frame. Only the UI art set and the skin are touched.
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyComicFrame.cs --entry ApplyComicFrame.Run --timeout_ms 300000
/// </summary>
public static class ApplyComicFrame
{
    public static string Run()
    {
        Game.Editor.BlenderUiBuilder.BuildMenu();
        Game.UI.UiArtSet art = AssetDatabase.LoadAssetAtPath<Game.UI.UiArtSet>(Game.Editor.BlenderUiBuilder.ArtPath);
        UnityEngine.Sprite frame = art != null ? art.Sprite("ComicFrame") : null;
        return frame != null ? $"ComicFrame {frame.rect} border {frame.border}" : "no ComicFrame";
    }
}
