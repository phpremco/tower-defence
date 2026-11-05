using System.Reflection;
using Game.Core;
using UnityEditor;

/// <summary>
/// Imports Blender round 35 only: re-syncs Mercer (and his weapon files) and rebuilds his hero art with the five reaction
/// clips (Startled, Startled_Hold, Disbelief, Point_Shout, Lower_Weapon), then reports every clip's role, length, cue and
/// layer. No other story export is touched; the hero definitions keep pointing at the same art asset.
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyMercer.cs --entry ApplyMercer.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class ApplyMercer
{
    public static string Run()
    {
        int copied = Game.Editor.BlenderSync.Sync(new[] { Game.Editor.BlenderStoryCatalog.HeroAsset }, Game.Editor.BlenderStoryCatalog.ExtraModels());
        MethodInfo build = typeof(Game.Editor.BlenderStoryBuilder).GetMethod("BuildHero", BindingFlags.NonPublic | BindingFlags.Static);
        var hero = (Game.Hero.HeroArtDefinition)build.Invoke(null, null);
        AssetDatabase.SaveAssets();
        string report = $"copied {copied}; hero {(hero != null ? hero.name : "none")}";
        if (hero == null || hero.Body == null)
        {
            return report;
        }

        foreach (ModelClip clip in hero.Body.Clips)
        {
            report += $"\n{clip.Role} {clip.Length:0.00}s cue {clip.EventSeconds:0.000}{(clip.UpperBody ? " upper" : string.Empty)} loop {(clip.Clip != null && clip.Clip.isLooping)}";
        }

        return report;
    }
}
