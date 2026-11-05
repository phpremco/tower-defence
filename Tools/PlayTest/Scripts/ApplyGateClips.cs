using System.Reflection;
using Game.Core;
using UnityEditor;

/// <summary>
/// Imports the round-33 gate crossings only: syncs and rebuilds Gravemaw and the Cinder Matriarch (their Gate_Mount,
/// Gate_Straddle_Loop and Gate_Dismount with weights and the per-frame gate burden) and runs the generator's
/// "Continent bosses" step (each boss's GateBehaviour: Rotbloom batters gates). Nothing else is synced or rebuilt. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyGateClips.cs --entry ApplyGateClips.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyGateClips
{
    private static readonly string[] Art = { "Gravemaw", "CinderMatriarch" };

    public static string Run()
    {
        int worn = Game.Editor.BlenderArtBuilder.RebuildOnly(Art);
        typeof(Game.Editor.ProjectGenerator).GetMethod("GenerateContinentBosses", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        AssetDatabase.SaveAssets();
        string report = $"worn {worn}";
        foreach (string id in Art)
        {
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"Assets/_Project/Data/Art/{id}.asset");
            report += $"\n{id}: ";
            if (art == null)
            {
                continue;
            }

            foreach (ModelClip clip in art.Clips)
            {
                if (clip.Role == ModelClipRole.GateMount || clip.Role == ModelClipRole.GateStraddle || clip.Role == ModelClipRole.GateDismount)
                {
                    report += $"{clip.Role}({clip.Length:0.00}s a{clip.AnchorForward:0.00} e{clip.EndOffset.x:0.00}/{clip.EndOffset.y:0.00} ev{clip.EventSeconds * 30f:0} cue{clip.CueSeconds * 30f:0} w[{string.Join(",", System.Array.ConvertAll(clip.MomentSeconds, s => (s * 30f).ToString("0")))}] burden {clip.BurdenAt(0f):0.00}..{clip.BurdenAt(clip.Length * 0.5f):0.00}..{clip.BurdenAt(clip.Length):0.00}) ";
                }
            }
        }

        foreach (string id in new[] { "Gravemaw", "CinderMatriarch", "FrostWarden", "Rotbloom", "Shattered", "SealedOne" })
        {
            var enemy = AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>($"Assets/_Project/Data/Enemies/{id}.asset");
            report += $"\n{id} gates: {(enemy != null && enemy.Boss != null ? enemy.Boss.Climb.GateBehaviour.ToString() : "?")}";
        }

        return report;
    }
}
