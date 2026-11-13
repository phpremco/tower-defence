// Round 60: points the play camera (its fixed angle) at the first scene object whose name starts with the word in
// Tools/PlayTest/r60_view.txt ("<name prefix> <zoom> [<index>]"), and lists the named objects with renderers or lights
// within 4 m of it (spawn props, decor roots), so overlapping spawn visuals show up by name.
var words = System.IO.File.ReadAllText("Tools/PlayTest/r60_view.txt").Trim().Split(' ');
float zoom = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture);
int wantedIndex = words.Length > 2 ? int.Parse(words[2]) : 0;
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
UnityEngine.Transform target = null;
int seen = 0;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.InstanceID))
{
    if (!t.name.StartsWith(words[0])) continue;
    if (seen++ == wantedIndex) { target = t; break; }
}
if (target == null) return "no " + words[0];
rig.SetView(target.position, zoom);
var sb = new System.Text.StringBuilder("view on " + target.name + " at " + target.position.ToString("0.00") + " zoom " + zoom + " | near:");
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.InstanceID))
{
    if (t == target || t.IsChildOf(target) || t.parent == null) continue;
    var d = t.position - target.position; d.y = 0f;
    if (d.magnitude > 4f) continue;
    if (t.parent.GetComponent<UnityEngine.Renderer>() != null) continue;
    if (t.GetComponent<UnityEngine.Renderer>() == null && t.GetComponent<UnityEngine.Light>() == null && t.childCount == 0) continue;
    if (t.parent.parent != null && t.parent.name != "Level" && t.parent.parent.parent != null) continue;
    sb.Append(" ").Append(t.name).Append("@").Append(d.magnitude.ToString("0.0"));
}
return sb.ToString();
