using System.Linq;
using Game.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports the round-36 attack clips only: syncs and rebuilds the twelve species, the bat skins and their corrupted skins, whose FBX carry a new
/// or re-exported strike, and reports each one's Attack clips and whether the Mireleech's Latch keeps its segment scale keys.
/// Nothing else is synced or rebuilt (the base stays as it is). Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyAttacks36.cs --entry ApplyAttacks36.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyAttacks36
{
    private static readonly string[] Art =
    {
        "Skitter", "Prowler", "Roller", "Cragback", "Bulwark", "Shardback", "Shalebat", "Skyray", "Stinger", "Mireleech", "Wisp",
        "Bat", "Bat_Moss", "Bat_Dust", "Bat_Tide", "Bat_Frost",
        "Skitter_Corrupted", "Prowler_Corrupted", "Roller_Corrupted", "Cragback_Corrupted", "Bulwark_Corrupted", "Shardback_Corrupted",
        "Shalebat_Corrupted", "Skyray_Corrupted", "Stinger_Corrupted", "Mireleech_Corrupted", "Wisp_Corrupted", "Bat_Corrupted",
    };

    public static string Run()
    {
        int worn = Game.Editor.BlenderArtBuilder.RebuildOnly(Art);
        string report = $"worn {worn}";
        foreach (string id in Art)
        {
            var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"Assets/_Project/Data/Art/{id}.asset");
            report += $"\n{id}: ";
            if (art == null)
            {
                report += "no art";
                continue;
            }

            foreach (ModelClip clip in art.Clips)
            {
                if (clip.Role == ModelClipRole.Attack || clip.Role == ModelClipRole.GroundedAttack)
                {
                    report += $"{clip.Role} {clip.Length:0.00}s ev{clip.EventSeconds * 30f:0} ";
                }
            }
        }

        report += "\nMireleech scale keys: " + ScaleCurves("Mireleech", "Latch");
        return report;
    }

    private static string ScaleCurves(string model, string take)
    {
        foreach (string guid in AssetDatabase.FindAssets(model + " t:Model"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("/" + model + ".fbx"))
            {
                continue;
            }

            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name.EndsWith(take));
            if (clip == null)
            {
                return path + ": no " + take;
            }

            var scaled = AnimationUtility.GetCurveBindings(clip).Where(b => b.propertyName.StartsWith("m_LocalScale")).Select(b => b.path.Split('/').Last()).Distinct();
            return path + ": " + string.Join(",", scaled);
        }

        return "no model";
    }
}
