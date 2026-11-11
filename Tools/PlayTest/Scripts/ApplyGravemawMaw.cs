using Game.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 38: imports the Gravemaw worm maw only. Syncs the Gravemaw export (FBX, LOD1, body maps, the new
/// Gravemaw_Maw_* maps and Low/) and rebuilds just the Gravemaw art row (the Maw part gets Gravemaw_Maw.mat and
/// Gravemaw_Maw_Low.mat); every other creature stays as it is. Reports the parts, the renderers per LOD and the clips. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyGravemawMaw.cs --entry ApplyGravemawMaw.Run --timeout_ms 900000 --timeout 960
/// </summary>
public static class ApplyGravemawMaw
{
    public static string Run()
    {
        int worn = Game.Editor.BlenderArtBuilder.RebuildOnly("Gravemaw");
        AssetDatabase.SaveAssets();
        var art = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>("Assets/_Project/Data/Art/Gravemaw.asset");
        string report = $"worn by {worn}; parts:";
        foreach (ModelPart part in art.Parts)
        {
            report += $" {part.Object} ({Name(part.MaterialFor(false))} / {Name(part.MaterialFor(true))})";
        }

        foreach (GameObject model in new[] { art.Model, art.Lod1 })
        {
            report += $"\n{model.name}:";
            foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                report += $" {renderer.name}[{renderer.sharedMesh.subMeshCount} sub, {renderer.sharedMesh.triangles.Length / 3} tris, {renderer.bones.Length} bones]";
            }
        }

        report += "\nclips:";
        foreach (ModelClip clip in art.Clips)
        {
            report += $" {clip.Role}:{clip.Length:0.00}s/ev{clip.EventSeconds * 30f:0}";
        }

        return report;
    }

    private static string Name(Object asset) => asset != null ? asset.name : "null";
}
