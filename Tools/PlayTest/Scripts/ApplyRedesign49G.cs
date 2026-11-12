using System.Collections.Generic;
using System.Text;
using Game.Core;
using Game.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 52 import (Blender round 49G and its curator follow-up, approved by Remco): the Bulwark and the Crystal Warlord with
/// a real mouth, the Shardback with every spike grown from the shell and its six clusters still pure islands on their Shard
/// bones, and the Shattered with a hinged jaw, rooted teeth and crystals and its shell plates strapped to the core (each plate
/// still its own island on its Shell bone). Syncs only these four exports and rebuilds just their rows and the corrupted rows
/// of the same exports (the Shattered has none). The Sealed One is not part of this set. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRedesign49G.cs --entry ApplyRedesign49G.Run --timeout_ms 900000 --timeout 960
/// <c>ApplyRedesign49G.Report</c> prints the same description without rebuilding (the state before the import).
/// </summary>
public static class ApplyRedesign49G
{
    private static readonly HashSet<string> Exports = new HashSet<string> { "Bulwark", "CrystalWarlord", "Shardback", "Shattered" };

    // Bones the views and clips hang on: the jaws of the new mouths, the shed clusters, the phase-break plates and the
    // follow-through chains of the Bulwark rig.
    private static readonly string[] SocketPrefixes = { "Jaw", "Head", "Neck", "Tail_3", "Shard_", "Shell_", "Weapon" };

    public static string Run()
    {
        List<string> ids = Rows();
        int worn = BlenderArtBuilder.RebuildOnly(ids.ToArray());
        AssetDatabase.SaveAssets();
        return $"rows {ids.Count} ({string.Join(",", ids)}), worn by {worn}" + Describe(ids);
    }

    /// <summary>The same description without rebuilding: compare before and after for clip, event and socket parity.</summary>
    public static string Report() => Describe(Rows());

    private static List<string> Rows()
    {
        var ids = new List<string>();
        foreach (BlenderArtEntry entry in BlenderArtCatalog.Creatures)
        {
            if (Exports.Contains(entry.Asset))
            {
                ids.Add(entry.Id);
            }
        }

        return ids;
    }

    private static string Describe(List<string> ids)
    {
        var report = new StringBuilder();
        foreach (string id in ids)
        {
            report.Append('\n').Append(Describe(id));
        }

        return report.ToString();
    }

    private static string Describe(string id)
    {
        var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"Assets/_Project/Data/Art/{id}.asset");
        if (art == null)
        {
            return $"{id}: NO ART";
        }

        var line = new StringBuilder();
        int move = art.IndexOf(ModelClipRole.Move);
        line.Append($"{id}: height {art.Height:0.000} move {(move >= 0 ? art.Clips[move].DistancePerLoop : 0f):0.000}");
        line.Append($" shed [{string.Join(",", art.ShedBones)}]");
        foreach (ModelPart part in art.Parts)
        {
            line.Append($" {part.Object}({Name(part.MaterialFor(false))}/{Name(part.MaterialFor(true))})");
        }

        foreach (ModelClip clip in art.Clips)
        {
            line.Append($"\n  clip {clip.Role} {Name(clip.Clip)} {clip.Length:0.000}s event {clip.EventSeconds:0.000}s");
        }

        foreach (GameObject model in new[] { art.Model, art.Lod1 })
        {
            if (model != null)
            {
                line.Append(DescribeModel(model));
            }
        }

        return line.ToString();
    }

    private static string DescribeModel(GameObject model)
    {
        var line = new StringBuilder($"\n  {model.name}: transforms {model.GetComponentsInChildren<Transform>(true).Length}");
        foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            line.Append($" {renderer.name}[{renderer.sharedMesh.triangles.Length / 3}t {renderer.bones.Length}b]");
        }

        foreach (Transform node in model.GetComponentsInChildren<Transform>(true))
        {
            if (IsSocket(node.name))
            {
                Vector3 at = model.transform.InverseTransformPoint(node.position);
                line.Append($" {node.name}@({at.x:0.000},{at.y:0.000},{at.z:0.000})");
            }
        }

        return line.ToString();
    }

    private static bool IsSocket(string name)
    {
        foreach (string prefix in SocketPrefixes)
        {
            if (name.StartsWith(prefix))
            {
                return true;
            }
        }

        return false;
    }

    private static string Name(Object asset) => asset != null ? asset.name : "null";
}
