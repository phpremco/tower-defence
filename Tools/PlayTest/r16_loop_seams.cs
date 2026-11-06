// Round 16: audits every Blender clip of every ModelArtDefinition (edit mode, no play mode needed). For each clip it
// samples the model at 1/30 s steps and reports, per bone, the largest turn between two samples (a flip or tumble) and,
// for a looping clip, the turn between its last and first frame (a seam the loop snaps across). Writes the full list to
// Logs/r16_loop_seams.txt and returns the worst lines. Optional filter (art asset name part) in Tools/PlayTest/r16_arg.txt.
string filter = System.IO.File.Exists("Tools/PlayTest/r16_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r16_arg.txt").Trim() : "";
var lines = new System.Collections.Generic.List<string>();
var worst = new System.Collections.Generic.List<string>();
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
    var prevPos = new UnityEngine.Vector3[bones.Length];
    var startPos = new UnityEngine.Vector3[bones.Length];
    foreach (var clip in art.Clips)
    {
        if (clip == null || clip.Clip == null) { continue; }
        var anim = clip.Clip;
        float length = anim.length;
        int steps = UnityEngine.Mathf.Max(2, UnityEngine.Mathf.CeilToInt(length * 30f));
        var maxStep = new float[bones.Length];
        for (int s = 0; s <= steps; s++)
        {
            anim.SampleAnimation(go, length * s / steps);
            for (int b = 0; b < bones.Length; b++)
            {
                var q = bones[b].localRotation;
                if (s == 0) { start[b] = q; startPos[b] = bones[b].localPosition; }
                else { maxStep[b] = UnityEngine.Mathf.Max(maxStep[b], UnityEngine.Quaternion.Angle(prev[b], q)); }
                prev[b] = q;
                prevPos[b] = bones[b].localPosition;
            }
        }
        bool loops = anim.isLooping;
        for (int b = 0; b < bones.Length; b++)
        {
            float seam = loops ? UnityEngine.Quaternion.Angle(start[b], prev[b]) : 0f;
            float seamPos = loops ? (startPos[b] - prevPos[b]).magnitude : 0f;
            if (maxStep[b] > 12f || seam > 8f || seamPos > 0.05f)
            {
                string line = art.name + " | " + clip.Role + " (" + anim.name + (loops ? ", loop" : "") + ") | " + bones[b].name + " | max step " + maxStep[b].ToString("0.0") + " deg/frame" + (loops ? " | seam " + seam.ToString("0.0") + " deg, " + seamPos.ToString("0.00") + " m" : "");
                lines.Add(line);
                if (seam > 20f || maxStep[b] > 25f) { worst.Add(line); }
            }
        }
    }
    UnityEngine.Object.DestroyImmediate(go);
}
System.IO.File.WriteAllLines("Logs/r16_loop_seams.txt", lines);
return lines.Count + " flagged, " + worst.Count + " worst:\n" + string.Join("\n", worst.GetRange(0, System.Math.Min(60, worst.Count)));
