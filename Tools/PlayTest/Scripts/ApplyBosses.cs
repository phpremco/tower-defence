using System.Reflection;
using Game.Core;
using Game.Enemies;
using UnityEditor;

/// <summary>
/// Imports the continent bosses' round-28 art (syncs only their Blender exports and rebuilds only their art: Gravemaw, the
/// Cinder Matriarch with her egg and fire larva, the Frost Warden with its ice armour) and runs only the generator's
/// "Continent bosses" step (the bosses' data, their brood, their Codex rows, the developer boss arena and its liquid
/// levels), for a boss change while the full generator must not run (it syncs every Blender export). Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyBosses.cs --entry ApplyBosses.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyBosses
{
    private static readonly string[] Art = { "Gravemaw", "CinderMatriarch", "CinderEgg", "FireLarva", "FrostWarden" };

    public static string Run()
    {
        int worn = Game.Editor.BlenderArtBuilder.RebuildOnly(Art);
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        typeof(Game.Editor.ProjectGenerator).GetMethod("GenerateContinentBosses", flags).Invoke(null, null);
        AssetDatabase.SaveAssets();
        string report = $"worn {worn}; ";
        foreach (string id in new[] { "Gravemaw", "CinderMatriarch", "FrostWarden", "CinderEgg" })
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
            ModelArtDefinition art = enemy != null ? enemy.Art : null;
            report += $"\n{id}: boss {(enemy != null && enemy.Boss != null)} parts {(art != null ? art.Parts.Length : -1)} clips: ";
            if (art == null)
            {
                continue;
            }

            foreach (ModelClip clip in art.Clips)
            {
                report += $"{clip.Role}({clip.Length:0.00}s ev {clip.EventSeconds:0.00} cue {clip.CueSeconds:0.00} end {clip.EndOffset} anchor {clip.AnchorForward:0.00} d {clip.DistancePerLoop:0.00}) ";
            }
        }

        return report;
    }
}
