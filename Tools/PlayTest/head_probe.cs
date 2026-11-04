// For every live creature: is its head bone (first transform named like Head/Jaw/Mandible) ahead of its centre along its heading?
var sb = new System.Text.StringBuilder();
foreach (var e in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
{
    if (!e.IsAlive) continue;
    UnityEngine.Transform head = null;
    foreach (var t in e.GetComponentsInChildren<UnityEngine.Transform>())
    {
        string n = t.name.ToLowerInvariant();
        if (n.Contains("head") || n.Contains("jaw") || n.Contains("mandible")) { head = t; break; }
    }
    if (head == null) { sb.Append(e.name + " nohead; "); continue; }
    var d = head.position - e.transform.position; d.y = 0f;
    sb.Append(e.name + " " + head.name + " dot=" + UnityEngine.Vector3.Dot(d.normalized, e.Heading).ToString("F2") + "; ");
}
return sb.ToString();
