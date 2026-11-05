using Game.Core;
using UnityEditor;

/// <summary>
/// Imports the round-30/31 art only: rebuilds the re-exported creatures named below (round 29 rebuilt the Sealed One
/// and the shards, round 30 the Frost Warden) and runs the boss FX builder (ground marks, flipbooks, ruin gate, seal crystals,
/// ice block, crack slabs, drill wreck, drained gate maps, dampened icon). Nothing else is
/// synced or rebuilt. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyBossFx.cs --entry ApplyBossFx.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyBossFx
{
    // Round 30 rebuilt the Frost Warden; round 31 the slugs with their Strike (the builder adds slabs, ice block, crystals
    // and the rigged drill wreck).
    private static readonly string[] Art = { "Oozer", "SlimeMonarch" };

    public static string Run()
    {
        int worn = Game.Editor.BlenderArtBuilder.RebuildOnly(Art);
        BossFxArt fx = Game.Editor.BlenderBossFxBuilder.SyncAndBuild();
        AssetDatabase.SaveAssets();
        string report = $"worn {worn}; fx {(fx != null ? fx.name : "none")}; ruin gate {fx?.RuinGate != null}; crystals {fx?.SealCrystals != null}; ice {fx?.IceBlock != null}; slabs {fx?.CrackSlabs != null}; dampened {fx?.Dampened != null}";
        foreach (BossFxArt.Mark mark in System.Enum.GetValues(typeof(BossFxArt.Mark)))
        {
            report += $"\nmark {mark}: {fx != null && fx.Has(mark)}";
        }

        foreach (BossFxArt.Sheet sheet in System.Enum.GetValues(typeof(BossFxArt.Sheet)))
        {
            report += $"\nsheet {sheet}: {fx != null && fx.Has(sheet)}";
        }

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
                report += $"{clip.Role}({clip.Length:0.00}s d{clip.DistancePerLoop:0.000} a{clip.AnchorForward:0.000} e{clip.EndOffset.x:0.000}/{clip.EndOffset.y:0.000} ev{clip.EventSeconds * 30f:0}) ";
            }
        }

        var rig = AssetDatabase.LoadAssetAtPath<Game.Levels.ColonyRigDefinition>("Assets/_Project/Data/Art/Environment/ColonyRig_CrystalPlains.asset");
        report += $"\nrig wreck {(rig != null && rig.Wreck != null)}";
        return report;
    }
}
