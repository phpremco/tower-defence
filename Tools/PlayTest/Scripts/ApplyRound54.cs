using System.Text;
using Game.Core;
using Game.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round 54 import (Blender round 53): the ruin gate rebuilt as one arch with a folding iris (its petal bones now scale-keyed)
/// and the Shattered with each shell plate on its own Shell bone. Syncs only these two exports, rebuilds only the gate (not
/// the rest of the boss FX) and only the Shattered's row. The Sealed One is not part of this set. Run:
/// unity command run_script --file Tools/PlayTest/Scripts/ApplyRound54.cs --entry ApplyRound54.Run --timeout_ms 900000 --timeout 960
/// <c>ApplyRound54.Report</c> prints the same description without rebuilding.
/// </summary>
public static class ApplyRound54
{
    private const string Shattered = "Shattered";
    private static readonly float[] OpenFrames = { 0f, 30f, 52f, 70f, 90f, 110f, 130f, 159f };

    public static string Run()
    {
        ModelArtDefinition gate = BlenderBossFxBuilder.SyncAndBuildRuinGate();
        int worn = BlenderArtBuilder.RebuildOnly(Shattered);
        AssetDatabase.SaveAssets();
        return $"gate {(gate != null ? gate.name : "none")}, Shattered worn by {worn}" + Report();
    }

    public static string Report()
    {
        var line = new StringBuilder();
        BossFxArt fx = AssetDatabase.LoadAssetAtPath<BossFxArt>("Assets/_Project/Data/Vfx/BossFxArt.asset");
        ModelArtDefinition gate = fx != null ? fx.RuinGate : null;
        if (gate == null)
        {
            return "\nno gate";
        }

        line.Append($"\ngate height {gate.Height:0.000} scale {fx.RuinGateScale:0.00} emission {gate.Material.GetColor("_EmissionColor")}");
        foreach (ModelClip clip in gate.Clips)
        {
            line.Append($"\n  clip {clip.Role} {clip.Clip.name} {clip.Length:0.000}s scaleCurves {ScaleCurves(clip.Clip)}");
        }

        int open = gate.IndexOf(ModelClipRole.Open);
        if (open >= 0)
        {
            line.Append(PetalScales(gate.Model, gate.Clips[open].Clip));
        }

        var shattered = AssetDatabase.LoadAssetAtPath<ModelArtDefinition>($"Assets/_Project/Data/Art/{Shattered}.asset");
        if (shattered != null)
        {
            line.Append($"\nShattered height {shattered.Height:0.000} clips {shattered.Clips.Length} shed [{string.Join(",", shattered.ShedBones)}]");
        }

        return line.ToString();
    }

    private static string ScaleCurves(AnimationClip clip)
    {
        var names = new StringBuilder();
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (binding.propertyName == "m_LocalScale.x")
            {
                string path = binding.path;
                names.Append(path.Substring(path.LastIndexOf('/') + 1)).Append(' ');
            }
        }

        return names.ToString();
    }

    // The petals' local scale through the Open take: they must fold flat (one axis to nothing) and end at zero.
    private static string PetalScales(GameObject model, AnimationClip clip)
    {
        GameObject probe = Object.Instantiate(model);
        var line = new StringBuilder();
        try
        {
            foreach (float frame in OpenFrames)
            {
                clip.SampleAnimation(probe, frame / 30f);
                line.Append($"\n  f{frame:0}:");
                foreach (Transform bone in probe.GetComponentsInChildren<Transform>(true))
                {
                    if (bone.name.StartsWith("Petal_"))
                    {
                        Vector3 s = bone.localScale;
                        line.Append($" {bone.name}({s.x:0.00},{s.y:0.00},{s.z:0.00})");
                    }
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(probe);
        }

        return line.ToString();
    }
}
