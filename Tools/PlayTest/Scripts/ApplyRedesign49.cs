using System.Collections.Generic;
using Game.Core;
using Game.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 49 import, approved set 1: brings Remco's approved Blender redesigns of the Oozer, Cragback, Slagstrider, Siftjaw
/// and Surgefin into the game. Syncs only these five exports (FBX, LOD1, body and part maps with Low/, the corrupted skins,
/// the report) and rebuilds just their art rows and the corrupted rows of the same exports; every other creature (among them
/// the wolves, the rays, the SlimeMonarch, the Cinder Matriarch and the Knitback, which go back to Blender) stays as it is.
/// The Cragback row names its vent definition, so the build also creates <c>Data/Vfx/Vents_Cragback.asset</c> when it is
/// missing. Reports per row the height, the move distance per loop, the vents and the parts, and per LOD the renderers. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRedesign49.cs --entry ApplyRedesign49.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyRedesign49
{
    private static readonly HashSet<string> Assets = new HashSet<string> { "Oozer", "Cragback", "Slagstrider", "Siftjaw", "Surgefin" };

    public static string Run()
    {
        var ids = new List<string>();
        foreach (BlenderArtEntry entry in BlenderArtCatalog.Creatures)
        {
            if (Assets.Contains(entry.Asset))
            {
                ids.Add(entry.Id);
            }
        }

        int worn = BlenderArtBuilder.RebuildOnly(ids.ToArray());
        AssetDatabase.SaveAssets();
        string report = $"rows {ids.Count} ({string.Join(",", ids)}), worn by {worn}";
        foreach (string id in ids)
        {
            report += "\n" + Describe(id);
        }

        return report;
    }

    private static string Describe(string id)
    {
        var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"Assets/_Project/Data/Art/{id}.asset");
        if (art == null)
        {
            return $"{id}: NO ART";
        }

        int move = art.IndexOf(ModelClipRole.Move);
        string line = $"{id}: height {art.Height:0.000} move {(move >= 0 ? art.Clips[move].DistancePerLoop : 0f):0.000}"
            + $" vents {(art.Vents != null ? art.Vents.name : "-")}";
        foreach (ModelPart part in art.Parts)
        {
            line += $" {part.Object}({Name(part.MaterialFor(false))}/{Name(part.MaterialFor(true))})";
        }

        foreach (GameObject model in new[] { art.Model, art.Lod1 })
        {
            if (model == null)
            {
                continue;
            }

            line += $"\n  {model.name}:";
            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                line += $" {renderer.name}[{renderer.sharedMesh.triangles.Length / 3}t {renderer.bones.Length}b]";
            }

            int smoke = 0;
            foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
            {
                smoke += node.name.StartsWith("Smoke_") ? 1 : 0;
            }

            line += smoke > 0 ? $" smoke sockets {smoke}" : string.Empty;
        }

        return line;
    }

    private static string Name(Object asset) => asset != null ? asset.name : "null";
}
