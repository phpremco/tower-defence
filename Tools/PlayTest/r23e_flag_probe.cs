// Round 23e: where the NTI flag sits on the base and what covers it. Lists the flag's renderers (material, shader, bounds,
// screen point), the base's position and scale, and every other renderer whose bounds hold the flag's pole foot or cloth
// centre (a structure the flag might be inside).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var report = new System.Text.StringBuilder();
UnityEngine.Transform flag = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>())
{
    if (t.name.StartsWith("NTI_Flag") && (flag == null || t.parent == null || !t.parent.name.StartsWith("NTI_Flag"))) flag = t;
}
if (flag == null) return "no flag";
var cam = UnityEngine.Camera.main;
report.Append("flag root ").Append(flag.name).Append(" parent ").Append(flag.parent != null ? flag.parent.name : "-")
    .Append(" world ").Append(flag.position.ToString("0.00")).Append(" lossy ").Append(flag.lossyScale.ToString("0.00"))
    .Append(" | base ").Append(context.Base.Position.ToString("0.00")).Append(" | ");
UnityEngine.Bounds all = new UnityEngine.Bounds(flag.position, UnityEngine.Vector3.zero);
foreach (var r in flag.GetComponentsInChildren<UnityEngine.Renderer>(true))
{
    all.Encapsulate(r.bounds);
    var sp = cam.WorldToViewportPoint(r.bounds.center);
    report.Append(r.name).Append(" on ").Append(r.enabled && r.gameObject.activeInHierarchy).Append(" mat ")
        .Append(r.sharedMaterial != null ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "null")
        .Append(" c ").Append(r.bounds.center.ToString("0.00")).Append(" s ").Append(r.bounds.size.ToString("0.00"))
        .Append(" vp ").Append(sp.ToString("0.00")).Append(" | ");
}
report.Append("inside: ");
var foot = new UnityEngine.Vector3(flag.position.x, flag.position.y + 0.3f, flag.position.z);
var mid = all.center;
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>())
{
    if (r.transform.IsChildOf(flag)) continue;
    if (r.bounds.size.magnitude > 30f) continue;
    if (r.bounds.Contains(foot) || r.bounds.Contains(mid))
    {
        report.Append(r.name).Append(" c ").Append(r.bounds.center.ToString("0.00")).Append(" s ").Append(r.bounds.size.ToString("0.00")).Append("; ");
    }
}
return report.ToString();
