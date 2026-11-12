using System.Collections.Generic;
using Game.Core;
using Game.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 49 import, approved set 3: brings Remco's approved round-49E second pass into the game: the Prowler and the Pack
/// Alpha (rebuilt wolves), the Skyray and the ray boss (export StormMatriarch, row RayBoss, worn by the Sky Sovereign) with
/// the mouth cut into the head, the Slime Monarch's acid vat (new Slosh bone), the Cinder Matriarch's re-seated hood shards
/// and the Knitback's attached tusks (art only, no enemy yet). Syncs only these seven exports and rebuilds just their art
/// rows and the corrupted rows of the same exports (the Cinder Matriarch has none), like sets 1 and 2. Bulwark, Crystal
/// Warlord, Shattered, Sealed One and Shardback (round 49F, still in Blender) stay as they are. Reports per row the height,
/// the move distance, the parts, per LOD the renderers and where each socket sits. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRedesign49Set3.cs --entry ApplyRedesign49Set3.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyRedesign49Set3
{
    private static readonly HashSet<string> Assets = new HashSet<string>
    {
        "Prowler", "PackAlpha", "Skyray", "StormMatriarch", "SlimeMonarch", "CinderMatriarch", "Knitback",
    };

    // Bones the views and clips hang on: the brood spouts, the acid slosh, the howl collar, the ray jaw and tail laser,
    // the Cinder Matriarch's spawn vent, the Knitback's regrow pod, and the wolves' moved rest bones.
    private static readonly string[] Sockets =
    {
        "Sac_L", "Sac_R", "Slosh", "Mane_L", "Mane_R", "Jaw", "Tail_5", "Vent_1", "Pod_1", "Head", "Pelvis",
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
        return $"rows {ids.Count} ({string.Join(",", ids)}), worn by {worn}" + DescribeAll(ids);
    }

    /// <summary>The same report without rebuilding: the state before the import, to compare heights and sockets.</summary>
    public static string Report()
    {
        var ids = new List<string>();
        foreach (BlenderArtEntry entry in BlenderArtCatalog.Creatures)
        {
            if (Assets.Contains(entry.Asset) && entry.Skin == null)
            {
                ids.Add(entry.Id);
            }
        }

        return DescribeAll(ids);
    }

    private static string DescribeAll(List<string> ids)
    {
        string report = string.Empty;
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
        string line = $"{id}: height {art.Height:0.000} move {(move >= 0 ? art.Clips[move].DistancePerLoop : 0f):0.000}";
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

            foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
            {
                if (System.Array.IndexOf(Sockets, node.name) >= 0)
                {
                    Vector3 at = model.transform.InverseTransformPoint(node.position);
                    line += $" {node.name}@({at.x:0.000},{at.y:0.000},{at.z:0.000})";
                }
            }
        }

        return line;
    }

    private static string Name(Object asset) => asset != null ? asset.name : "null";
}
