// Round 17: sweeps every Blender clip of every ModelArtDefinition (edit mode) at 1/30 s steps and lists each bone that
// turns more than the threshold in one step (a flip), with the time of the worst step, plus loop seams for looping clips.
// Optional filter (art name part) in Tools/PlayTest/r17_arg.txt; threshold in degrees is the second word (default 60).
string[] args = System.IO.File.Exists("Tools/PlayTest/r17_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r17_arg.txt").Trim().Split(' ') : new string[0];
string filter = args.Length > 0 && args[0] != "*" ? args[0] : "";
float threshold = args.Length > 1 ? float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 60f;
var lines = new System.Collections.Generic.List<string>();
int clipsChecked = 0;
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ModelArtDefinition"))
{
    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(path);
    if (art == null || art.Model == null || art.Clips == null) { continue; }
    if (filter.Length > 0 && !art.name.Contains(filter)) { continue; }
    var go = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(art.Model);
    go.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
    var bones = go.GetComponentsInChildren<UnityEngine.Transform>(true);
    var start = new UnityEngine.Quaternion[bones.Length];
    var prev = new UnityEngine.Quaternion[bones.Length];
    foreach (var clip in art.Clips)
    {
        if (clip == null || clip.Clip == null) { continue; }
        clipsChecked++;
        var anim = clip.Clip;
        float length = anim.length;
        int steps = UnityEngine.Mathf.Max(2, UnityEngine.Mathf.CeilToInt(length * 30f));
        var maxStep = new float[bones.Length];
        var maxAt = new float[bones.Length];
        var maxIdx = new int[bones.Length];
        var steps1 = new float[bones.Length, steps + 1];
        var scaleAt = new float[bones.Length, steps + 1];
        for (int s = 0; s <= steps; s++)
        {
            float t = length * s / steps;
            anim.SampleAnimation(go, t);
            for (int b = 0; b < bones.Length; b++)
            {
                var q = bones[b].localRotation;
                scaleAt[b, s] = bones[b].lossyScale.magnitude / 1.7320508f;
                if (s == 0) { start[b] = q; }
                else
                {
                    float a = UnityEngine.Quaternion.Angle(prev[b], q);
                    steps1[b, s] = a;
                    if (a > maxStep[b]) { maxStep[b] = a; maxAt[b] = t; maxIdx[b] = s; }
                }
                prev[b] = q;
            }
        }
        bool loops = anim.isLooping;
        for (int b = 0; b < bones.Length; b++)
        {
            float seam = loops ? UnityEngine.Quaternion.Angle(start[b], prev[b]) : 0f;
            if (maxStep[b] > threshold || seam > threshold)
            {
                lines.Add(art.name + " | " + clip.Role + " (" + anim.name + (loops ? ", loop" : "") + ", " + length.ToString("0.00") + " s) | " + bones[b].name + " | max step " + maxStep[b].ToString("0.0") + " deg at " + maxAt[b].ToString("0.00") + " s (neighbours " + (maxIdx[b] > 1 ? steps1[b, maxIdx[b] - 1] : 0f).ToString("0") + "/" + (maxIdx[b] < steps ? steps1[b, maxIdx[b] + 1] : 0f).ToString("0") + ", scale " + System.Math.Min(scaleAt[b, maxIdx[b]], scaleAt[b, maxIdx[b] - 1]).ToString("0.00") + ")" + (loops ? " | seam " + seam.ToString("0.0") + " deg" : ""));
            }
        }
    }
    UnityEngine.Object.DestroyImmediate(go);
}
System.IO.File.WriteAllLines("Logs/r17_seam_sweep.txt", lines);
string all = string.Join("\n", lines);
return clipsChecked + " clips, " + lines.Count + " flagged:\n" + (all.Length > 6000 ? all.Substring(0, 6000) : all);
