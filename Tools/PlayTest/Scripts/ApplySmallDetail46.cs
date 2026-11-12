using System.Collections.Generic;
using Game.Core;
using Game.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 46: imports the small-enemy close-up detail pass only. Syncs the seventeen small-species exports (FBX, LOD1,
/// body maps, among them the re-baked ones, the new &lt;Species&gt;_&lt;Part&gt;_* maps and Low/, the report) and rebuilds just
/// their art rows: the element bat skins and the corrupted skins of the same exports included; the bosses and every other
/// creature stay as they are. Reports per row the parts with their High/Low materials and look, and per LOD the renderers
/// (tris, bones). Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplySmallDetail46.cs --entry ApplySmallDetail46.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplySmallDetail46
{
    // The export folders of the detail pass (the five element bats share the Bat export).
    private static readonly HashSet<string> Assets = new HashSet<string>
    {
        "Skitter", "Cragback", "Skyray", "Roller", "Prowler", "Shalebat", "Wisp", "Stinger", "Bulwark", "Shardback",
        "Knitback", "Slagstrider", "Siftjaw", "Surgefin", "Bat", "Oozer", "Mireleech",
    };

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

        string line = $"{id}:";
        foreach (ModelPart part in art.Parts)
        {
            Material high = part.MaterialFor(false);
            line += $" {part.Object}({Name(high)}/{Name(part.MaterialFor(true))} e{Emission(high):0.00} s{Smooth(high):0.00})";
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
        }

        return line;
    }

    private static float Emission(Material material) => material != null ? material.GetColor("_EmissionColor").maxColorComponent : 0f;

    private static float Smooth(Material material) => material != null ? material.GetFloat("_Smoothness") : 0f;

    private static string Name(Object asset) => asset != null ? asset.name : "null";
}
