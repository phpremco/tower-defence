// Round 16: for every art whose name contains a word in r16_arg.txt (space separated), samples each clip twice 1/30 s
// apart at a third of its length and reports, for bones named like a spinning part (Rotor, Ring, Drill, Emitter, Gyro,
// Orb, Radar, Dish, Fan, Claw, Halo, Spin, Turn), how far it turned and about which axis in its OWN frame (a bone
// spinning about its own length turns about local Y) and in the model root's frame (Y = the model's up).
string[] words = System.IO.File.ReadAllText("Tools/PlayTest/r16_arg.txt").Trim().Split(' ');
string[] parts = { "Rotor", "Ring", "Drill", "Emitter", "Gyro", "Orb", "Radar", "Dish", "Fan", "Claw", "Halo", "Spin", "Turn", "Core", "Ball" };
string result = "";
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ModelArtDefinition"))
{
    var art = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Core.ModelArtDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    bool wanted = false; foreach (var w in words) { if (w.Length > 0 && art.name.Contains(w)) { wanted = true; } }
    if (!wanted || art.Model == null || art.Clips == null) { continue; }
    var go = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(art.Model);
    go.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
    var bones = new System.Collections.Generic.List<UnityEngine.Transform>();
    foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>(true)) { foreach (var p in parts) { if (t.name.Contains(p)) { bones.Add(t); break; } } }
    foreach (var clip in art.Clips)
    {
        if (clip == null || clip.Clip == null) { continue; }
        float at = clip.Clip.length / 3f;
        clip.Clip.SampleAnimation(go, at);
        var l0 = new UnityEngine.Quaternion[bones.Count]; var w0 = new UnityEngine.Quaternion[bones.Count];
        for (int b = 0; b < bones.Count; b++) { l0[b] = bones[b].localRotation; w0[b] = UnityEngine.Quaternion.Inverse(go.transform.rotation) * bones[b].rotation; }
        clip.Clip.SampleAnimation(go, at + 1f / 30f);
        for (int b = 0; b < bones.Count; b++)
        {
            (UnityEngine.Quaternion.Inverse(l0[b]) * bones[b].localRotation).ToAngleAxis(out float angle, out UnityEngine.Vector3 own);
            if (angle > 180f) { angle = 360f - angle; own = -own; }
            if (angle < 1f) { continue; }
            var model = w0[b] * own;
            result += art.name + " | " + clip.Role + " | " + bones[b].name + " | " + (angle * 30f).ToString("0") + " deg/s | own " + own.ToString("0.00") + " | model " + model.ToString("0.00") + "\n";
        }
    }
    UnityEngine.Object.DestroyImmediate(go);
}
System.IO.File.WriteAllText("Logs/r16_own_axis.txt", result);
return result.Length > 3000 ? result.Substring(0, 3000) : result;
