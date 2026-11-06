using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

/// <summary>
/// Round 14: syncs only the expanded colony base (Blender round 37, ArtSource/Exports/Base) into Assets, rebuilds its
/// BaseArtDefinition (modules, overlay, wreck, timelapse order, measured footprint) through the colony builder's own
/// BuildBase, points every visual theme at it and writes the base footprint figure on every theme, without the full
/// generator (which re-syncs every Blender export). Re-runnable: unchanged files are skipped, assets update in place. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyBase2.cs --entry ApplyBase2.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class ApplyBase2
{
    // Round 37's footprint (Base_report.json "reach" 3.655), for the procedural fallback and the menus that read the theme.
    private const float ThemeFootprint = 3.7f;
    private const float RadarDegreesPerSecond = 24f;

    public static string Run()
    {
        string asset = Game.Editor.BlenderColonyCatalog.BaseAsset;
        IReadOnlyDictionary<string, string[]> extras = Game.Editor.BlenderColonyCatalog.ExtraModels();
        var only = new Dictionary<string, string[]> { { asset, extras[asset] } };
        int copied = Game.Editor.BlenderSync.Sync(new[] { asset }, only);

        // The importer's map size for this folder changed in round 14 (2048 High, 1024 Low): reimport the maps.
        string folder = $"{Game.Editor.BlenderSync.TargetRoot}/{asset}";
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }

        MethodInfo build = typeof(Game.Editor.BlenderColonyBuilder).GetMethod("BuildBase", BindingFlags.NonPublic | BindingFlags.Static);
        var art = (Game.Levels.BaseArtDefinition)build.Invoke(null, null);
        if (art == null)
        {
            throw new InvalidOperationException("The base did not build (no maps or model).");
        }

        // The radar dish sweeps slowly (the old small dish spun at 40).
        var life = new SerializedObject(art);
        life.FindProperty("_turnDegreesPerSecond").floatValue = RadarDegreesPerSecond;
        life.ApplyModifiedPropertiesWithoutUndo();

        int themes = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:VisualTheme", new[] { "Assets/_Project/Data" }))
        {
            var theme = AssetDatabase.LoadAssetAtPath<Game.Levels.VisualTheme>(AssetDatabase.GUIDToAssetPath(guid));
            var so = new SerializedObject(theme);
            so.FindProperty("_baseArt").objectReferenceValue = art;
            so.FindProperty("_baseFootprint").floatValue = ThemeFootprint;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            themes++;
        }

        EditorUtility.SetDirty(art);
        AssetDatabase.SaveAssets();
        return $"{copied} file(s) synced; base modules {art.TimelapseOrder.Length} ({string.Join(",", art.TimelapseOrder)}); footprint {art.FootprintMin} .. {art.FootprintMax}; {themes} themes";
    }
}
