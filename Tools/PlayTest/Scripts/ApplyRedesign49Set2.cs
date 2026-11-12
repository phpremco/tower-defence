using System.Collections.Generic;
using Game.Core;
using Game.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 49 import, approved set 2: brings Remco's approved Blender redesigns of rounds 49C and 49D into the game: the element
/// bats (Bat, Bat_Moss/Dust/Tide/Frost), the bat boss (export SkySovereign, row BatBoss, worn by the Storm Matriarch), the
/// Stinger, the Acid Empress, the Frost Warden, the Brood Mother and the Hive Queen (export Queen). Syncs only these seven
/// exports and rebuilds just their art rows and the corrupted rows of the same exports, like set 1 (ApplyRedesign49). The
/// Sealed One, Shardback, Bulwark, Crystal Warlord, Shattered and the ray boss (export StormMatriarch) stay as they are.
/// Reports per row the height, the move distance, the parts, per LOD the renderers and where each power socket sits. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRedesign49Set2.cs --entry ApplyRedesign49Set2.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyRedesign49Set2
{
    private static readonly HashSet<string> Assets = new HashSet<string>
    {
        "Bat", "SkySovereign", "Stinger", "AcidEmpress", "FrostWarden", "BroodMother", "Queen",
    };

    // The bones the power effects hang on (clip sockets in the catalog), to see them after the rest poses moved.
    private static readonly string[] Sockets = { "Emitter", "Gland", "Vent", "Socket_Breath", "Head", "Jaw" };

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

    /// <summary>The same report without rebuilding: the state before the import, to compare the socket positions.</summary>
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
