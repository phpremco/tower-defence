using Game.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports Blender round 34 only: runs the boss FX builder (which now also syncs Gravemaw's fissure kit and builds its rock
/// and depth punch materials) and reports what the kit's meshes carry (UV channels, colours, bounds). No creature is rebuilt.
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyFissure.cs --entry ApplyFissure.Run --timeout_ms 600000 --timeout 660
/// </summary>
public static class ApplyFissure
{
    public static string Run()
    {
        BossFxArt fx = Game.Editor.BlenderBossFxBuilder.SyncAndBuild();
        AssetDatabase.SaveAssets();
        string report = $"fx {(fx != null ? fx.name : "none")}; fissure {fx != null && fx.HasFissure}";
        GameObject kit = fx != null ? fx.FissureKit : null;
        if (kit == null)
        {
            return report;
        }

        foreach (MeshFilter filter in kit.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            var uv1 = new System.Collections.Generic.List<Vector2>();
            mesh.GetUVs(1, uv1);
            float maxAlong = 0f;
            foreach (Vector2 v in uv1)
            {
                maxAlong = Mathf.Max(maxAlong, v.x);
            }

            report += $"\n{filter.name}: v{mesh.vertexCount} sub{mesh.subMeshCount} readable {mesh.isReadable} uv1 {mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1)} uv2 {mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord2)} uv3 {mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord3)} col {mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color)} bounds {mesh.bounds.min:F2}..{mesh.bounds.max:F2} along {maxAlong:F2} local {filter.transform.localPosition:F2} rot {filter.transform.localEulerAngles:F0} scale {filter.transform.localScale:F2}";
        }

        return report;
    }
}
