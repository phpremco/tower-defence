using Game.Core;
using UnityEditor;

/// <summary>
/// Imports the round-29 boss art only: rebuilds the re-exported bosses and shards (Frost Warden and Sealed One with the
/// clip scale fix, OnBase_Loop, the shards' climbs, the Frost Warden's ice look) and runs the boss FX builder (ground
/// marks, flipbooks, ruin gate, seal crystals, ice block, drill wreck, drained gate maps, dampened icon). Nothing else is
/// synced or rebuilt. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyBossFx.cs --entry ApplyBossFx.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyBossFx
{
    private static readonly string[] Art = { "FrostWarden", "SealedOne", "ShardRunner", "ShardCrawler" };

    public static string Run()
    {
        int worn = Game.Editor.BlenderArtBuilder.RebuildOnly(Art);
        BossFxArt fx = Game.Editor.BlenderBossFxBuilder.SyncAndBuild();
        AssetDatabase.SaveAssets();
        string report = $"worn {worn}; fx {(fx != null ? fx.name : "none")}; ruin gate {fx?.RuinGate != null}; crystals {fx?.SealCrystals != null}; ice {fx?.IceBlock != null}; dampened {fx?.Dampened != null}";
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
                report += $"{clip.Role}({clip.Length:0.00}s) ";
            }
        }

        var rig = AssetDatabase.LoadAssetAtPath<Game.Levels.ColonyRigDefinition>("Assets/_Project/Data/Art/Environment/ColonyRig_CrystalPlains.asset");
        report += $"\nrig wreck {(rig != null && rig.Wreck != null)}";
        return report;
    }
}
