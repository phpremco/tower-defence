// Round 39: the landing capsule's exit geometry. Seeks the landing shot with the ramp shut and open and prints, in the
// capsule's own space (z towards the beacon), the Ramp bone, the Exit socket, the hull bounds and Spencer's stops.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var sb = new System.Text.StringBuilder();
System.Func<UnityEngine.Vector3, string> f = v => v.x.ToString("0.00") + "," + v.y.ToString("0.00") + "," + v.z.ToString("0.00");
foreach (float p in new[] { 0.25f, 0.35f })
{
    player.Seek(3, p);
    UnityEditor.EditorApplication.Step();
    player.Seek(3, p);
    UnityEngine.Transform capsule = null;
    foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None)) if (t.name == "Capsule" && t.parent != null && t.parent.name == "Set_Field") capsule = t;
    if (capsule == null) return "no capsule";
    sb.Append("p=").Append(p).Append(" capsule ").Append(f(capsule.position)).Append(" yaw ").Append(capsule.eulerAngles.y.ToString("0")).Append(" scale ").Append(f(capsule.lossyScale)).Append('\n');
    foreach (var t in capsule.GetComponentsInChildren<UnityEngine.Transform>(true))
    {
        if (t.name == "Ramp" || t.name == "Exit" || t.name == "Root" || t.name.StartsWith("Leg"))
            sb.Append("  ").Append(t.name).Append(" local ").Append(f(capsule.InverseTransformPoint(t.position))).Append(" fwd ").Append(f(capsule.InverseTransformDirection(t.forward))).Append(" up ").Append(f(capsule.InverseTransformDirection(t.up))).Append('\n');
    }
    foreach (var r in capsule.GetComponentsInChildren<UnityEngine.Renderer>(true))
    {
        var b = r.bounds;
        sb.Append("  R ").Append(r.name).Append(" min ").Append(f(capsule.InverseTransformPoint(b.min))).Append(" max ").Append(f(capsule.InverseTransformPoint(b.max))).Append('\n');
        if (r is UnityEngine.SkinnedMeshRenderer sk && p > 0.3f)
        {
            var m = new UnityEngine.Mesh(); sk.BakeMesh(m, true);
            var v = m.vertices; float rampMinY = 9, rampMaxZ = -9, rampTopY = -9;
            // The ramp: vertices out in front of the hull (local z > 0.75), lowest and furthest.
            for (int i = 0; i < v.Length; i++) { var w = capsule.InverseTransformPoint(sk.transform.TransformPoint(v[i])); if (w.z > 0.75f && UnityEngine.Mathf.Abs(w.x) < 0.5f) { rampMinY = UnityEngine.Mathf.Min(rampMinY, w.y); rampMaxZ = UnityEngine.Mathf.Max(rampMaxZ, w.z); if (w.z < 0.9f) rampTopY = UnityEngine.Mathf.Max(rampTopY, w.y); } }
            sb.Append("  ramp verts z>0.75: minY ").Append(rampMinY.ToString("0.00")).Append(" maxZ ").Append(rampMaxZ.ToString("0.00")).Append(" topY(z<0.9) ").Append(rampTopY.ToString("0.00")).Append('\n');
            // Hull profile: the hull's front face z at heights 0.2..2.4.
            for (float h = 0.2f; h < 2.6f; h += 0.3f) { float zf = -9, xw = 0; for (int i = 0; i < v.Length; i++) { var w = capsule.InverseTransformPoint(sk.transform.TransformPoint(v[i])); if (UnityEngine.Mathf.Abs(w.y - h) < 0.08f && UnityEngine.Mathf.Abs(w.x) < 0.3f && w.z < 0.8f) zf = UnityEngine.Mathf.Max(zf, w.z); if (UnityEngine.Mathf.Abs(w.y - h) < 0.08f) xw = UnityEngine.Mathf.Max(xw, UnityEngine.Mathf.Abs(w.x)); } sb.Append("  h ").Append(h.ToString("0.0")).Append(" frontZ ").Append(zf.ToString("0.00")).Append(" halfW ").Append(xw.ToString("0.00")).Append('\n'); }
        }
    }
    var hero = UnityEngine.GameObject.Find("Mercer");
    if (hero != null) sb.Append("  hero active ").Append(hero.activeInHierarchy).Append(" local ").Append(f(capsule.InverseTransformPoint(hero.transform.position))).Append(" yaw ").Append((hero.transform.eulerAngles.y - capsule.eulerAngles.y).ToString("0")).Append('\n');
    var beacon = UnityEngine.GameObject.Find("Beacon");
    if (beacon != null) sb.Append("  beacon local ").Append(f(capsule.InverseTransformPoint(beacon.transform.position))).Append('\n');
}
return sb.ToString();
