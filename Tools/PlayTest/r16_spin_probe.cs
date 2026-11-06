// Round 16: finds every part in the running scene that turns steadily (code spinners and clip-driven rings, rotors,
// radars, gyros, orbs) and reports, per part name, its rate and the tilt of its turning axis from world up. Steps the
// paused editor two frames at a fixed 1/60 s; a part counts as a spinner when it turned the same way about the same
// axis in both frames. Writes Logs/r16_spinners.txt (name | path root | deg/s | axis | tilt from up | from parent up).
UnityEngine.Time.captureDeltaTime = 1f / 60f;
var all = UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Exclude);
int n = all.Length;
var l0 = new UnityEngine.Quaternion[n];
var l1 = new UnityEngine.Quaternion[n];
for (int i = 0; i < n; i++) { l0[i] = all[i].localRotation; }
UnityEditor.EditorApplication.isPaused = true;
UnityEditor.EditorApplication.Step();
for (int i = 0; i < n; i++) { if (all[i] != null) { l1[i] = all[i].localRotation; } }
UnityEditor.EditorApplication.Step();
var lines = new System.Collections.Generic.SortedDictionary<string, int>();
for (int i = 0; i < n; i++)
{
    var t = all[i];
    if (t == null || t.parent == null) { continue; }
    var l2 = t.localRotation;
    (l1[i] * UnityEngine.Quaternion.Inverse(l0[i])).ToAngleAxis(out float a1, out UnityEngine.Vector3 ax1);
    (l2 * UnityEngine.Quaternion.Inverse(l1[i])).ToAngleAxis(out float a2, out UnityEngine.Vector3 ax2);
    if (a1 > 180f) { a1 = 360f - a1; ax1 = -ax1; }
    if (a2 > 180f) { a2 = 360f - a2; ax2 = -ax2; }
    if (t.name.StartsWith("Ring_")) { var key0 = "DEBUG " + t.name + " a1 " + a1.ToString("0.00") + " a2 " + a2.ToString("0.00") + " axes " + UnityEngine.Vector3.Angle(ax1, ax2).ToString("0") + " active " + t.gameObject.activeInHierarchy; lines.TryGetValue(key0, out int c0); lines[key0] = c0 + 1; }
    if (a1 < 0.4f || a2 < 0.4f || UnityEngine.Vector3.Angle(ax1, ax2) > 6f || UnityEngine.Mathf.Abs(a1 - a2) > UnityEngine.Mathf.Max(0.5f, a1 * 0.25f)) { continue; }
    var world = t.parent.rotation * ax2;
    float tilt = UnityEngine.Vector3.Angle(world, UnityEngine.Vector3.up);
    tilt = UnityEngine.Mathf.Min(tilt, 180f - tilt);
    float fromParent = UnityEngine.Vector3.Angle(world, t.parent.up);
    fromParent = UnityEngine.Mathf.Min(fromParent, 180f - fromParent);
    var root = t;
    while (root.parent != null && root.parent.parent != null) { root = root.parent; }
    string key = t.name + " | " + root.name + " | " + (a2 * 60f).ToString("0") + " deg/s | axis " + world.ToString("0.00") + " | tilt " + tilt.ToString("0") + " | parent-up " + fromParent.ToString("0");
    lines.TryGetValue(key, out int count);
    lines[key] = count + 1;
}
UnityEditor.EditorApplication.isPaused = false;
UnityEngine.Time.captureDeltaTime = 0f;
var output = new System.Collections.Generic.List<string>();
foreach (var pair in lines) { output.Add(pair.Key + " | x" + pair.Value); }
System.IO.File.WriteAllLines("Logs/r16_spinners.txt", output);
return output.Count + " spinners (Logs/r16_spinners.txt)";
