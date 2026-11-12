using System.Collections.Generic;
using System.Text;
using Game.Core;
using Game.Editor;
using Game.Enemies;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 57 import (Blender rounds 52/52b and 56, approved): the Sealed One's opening seal with its cracked arms and the
/// tightened crack glow, the Skyray and the ray boss (export StormMatriarch) with rooted talons and readable mouths, the Bulwark
/// (its Face part now in LOD1 too) and the Crystal Warlord with bigger teeth, the Pack Alpha without specks, the Fire Larva's
/// planted tripod crawl (crawl_distance_per_loop 0.2 -> 0.4) and the new bog skin Bat_Mire, which the Mirebat now wears.
/// Syncs only these exports and rebuilds just their rows and the corrupted rows of the same exports; never the full generator.
/// Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound57.cs --entry ApplyRound57.Run --timeout_ms 900000 --timeout 960
/// <c>ApplyRound57.Report</c> prints the same description without rebuilding (the state before the import).
/// </summary>
public static class ApplyRound57
{
    private static readonly HashSet<string> Exports = new HashSet<string>
    {
        "SealedOne", "Skyray", "StormMatriarch", "Bulwark", "CrystalWarlord", "PackAlpha", "FireLarva",
    };

    // The bat export is shared by six skins; only the new bog skin's row is rebuilt.
    private static readonly string[] ExtraRows = { "Bat_Mire" };

    // Bones the views, clips and tests hang on: the Sealed One's maw and halo, the ray boss's beam, the jaws of the new
    // mouths, the howl collar.
    private static readonly string[] SocketNames =
    {
        "Head", "Neck", "Jaw", "Plate_R", "Plate_L", "Ring_1", "Ring_2", "Ring_3", "Emitter", "Tail_5", "Mane_L", "Mane_R", "Pelvis",
    };

    public static string Run()
    {
        List<string> ids = Rows();
        int worn = BlenderArtBuilder.RebuildOnly(ids.ToArray());
        AssetDatabase.SaveAssets();
        return $"rows {ids.Count} ({string.Join(",", ids)}), worn by {worn}" + Describe(ids) + Wearers(ids);
    }

    /// <summary>Rebuilds only the Sealed One's row (after its halo became crest bones for the bar). Run with --entry ApplyRound57.RunSealedOne</summary>
    public static string RunSealedOne()
    {
        int worn = BlenderArtBuilder.RebuildOnly("SealedOne");
        AssetDatabase.SaveAssets();
        return $"SealedOne worn by {worn}\n" + Describe("SealedOne");
    }

    /// <summary>The same description without rebuilding: compare before and after for clip, event and socket parity.</summary>
    public static string Report()
    {
        List<string> ids = Rows();
        return Describe(ids) + Wearers(ids);
    }

    private static List<string> Rows()
    {
        var ids = new List<string>();
        foreach (BlenderArtEntry entry in BlenderArtCatalog.Creatures)
        {
            if (Exports.Contains(entry.Asset) || System.Array.IndexOf(ExtraRows, entry.Id) >= 0)
            {
                ids.Add(entry.Id);
            }
        }

        foreach (string extra in ExtraRows)
        {
            if (!ids.Contains(extra))
            {
                ids.Add(extra);
            }
        }

        return ids;
    }

    private static string Wearers(List<string> ids)
    {
        var line = new StringBuilder("\nwearers:");
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition", new[] { "Assets/_Project/Data/Enemies" }))
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (enemy.Art != null && ids.Contains(enemy.Art.name))
            {
                line.Append($" {enemy.name}->{enemy.Art.name}");
            }
        }

        return line.ToString();
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
        line.Append($"{id}: height {art.Height:0.000} move {(move >= 0 ? art.Clips[move].DistancePerLoop : 0f):0.000} material {Name(art.Material)}");
        foreach (ModelPart part in art.Parts)
        {
            line.Append($" {part.Object}({Name(part.MaterialFor(false))}/{Name(part.MaterialFor(true))})");
        }

        SerializedProperty clips = new SerializedObject(art).FindProperty("_clips");
        for (int i = 0; i < art.Clips.Length; i++)
        {
            ModelClip clip = art.Clips[i];
            SerializedProperty offset = clips.GetArrayElementAtIndex(i).FindPropertyRelative("_socketOffset");
            string socket = clip.Socket.Length > 0 ? $" socket {clip.Socket}" + (offset != null ? $" offset {offset.vector3Value.ToString("0.000")}" : string.Empty) : string.Empty;
            line.Append($"\n  clip {clip.Role} {Name(clip.Clip)} {clip.Length:0.000}s event {clip.EventSeconds:0.000}s{socket} sustain {clip.Sustain}");
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
            if (System.Array.IndexOf(SocketNames, node.name) >= 0)
            {
                Vector3 at = model.transform.InverseTransformPoint(node.position);
                line.Append($" {node.name}@({at.x:0.000},{at.y:0.000},{at.z:0.000})");
            }
        }

        return line.ToString();
    }

    private static string Name(Object asset) => asset != null ? asset.name : "null";
}
