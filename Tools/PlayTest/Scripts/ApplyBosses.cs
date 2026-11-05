using System.Reflection;
using Game.Core;
using Game.Enemies;
using UnityEditor;

/// <summary>
/// Imports Gravemaw's round-28 mechanic clips (syncs only its Blender export and rebuilds only its art) and runs only the
/// generator's "Continent bosses" step (the six bosses' data, their Codex rows, the developer boss arena), for a boss
/// change while the full generator must not run (it syncs every Blender export). Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyBosses.cs --entry ApplyBosses.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyBosses
{
    public static string Run()
    {
        int worn = Game.Editor.BlenderArtBuilder.RebuildOnly("Gravemaw");
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        typeof(Game.Editor.ProjectGenerator).GetMethod("GenerateContinentBosses", flags).Invoke(null, null);
        AssetDatabase.SaveAssets();
        var gravemaw = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Gravemaw.asset");
        ModelArtDefinition art = gravemaw != null ? gravemaw.Art : null;
        string clips = string.Empty;
        if (art != null)
        {
            foreach (ModelClip clip in art.Clips)
            {
                clips += $"{clip.Role}({clip.Length:0.00}s ev {clip.EventSeconds:0.00} end {clip.EndOffset} anchor {clip.AnchorForward:0.00}) ";
            }
        }

        return $"worn {worn}; Gravemaw boss {(gravemaw != null && gravemaw.Boss != null)}; clips: {clips}";
    }
}
