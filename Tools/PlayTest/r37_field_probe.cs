// Round 37: interpenetration scrub of the opening cutscene's field shots. Shots from Tools/PlayTest/r37_probe_arg.txt
// ("3 4 5 6 7 ..."), each sought in 120 steps. Static meshes (base modules, gates, the drill, the landed capsule, the
// ore heap and decor props; nothing larger than 25 m, no terrain) get temporary mesh colliders; every mover (haulers,
// builders, Spencer, the brood) is tested with its renderers' bounds shrunk to 60 %, and the eyes and the decor through
// EnvironmentInstancer.IsClear. Prints each mover/obstacle pair once per shot with its first and last moment and count.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var shots = System.IO.File.ReadAllText("Tools/PlayTest/r37_probe_arg.txt").Trim().Split(' ');
var sb = new System.Text.StringBuilder();
System.Func<UnityEngine.Transform, string> moverOf = t =>
{
    for (var p = t; p != null; p = p.parent)
    {
        string n = p.name;
        if (n.StartsWith("Hauler") || n.StartsWith("Builder") || n == "Mercer" || (p.parent != null && p.parent.name == "Swarm")) return n;
    }
    return null;
};
foreach (string shotWord in shots)
{
    int shot = int.Parse(shotWord);
    player.Seek(shot, 0.01f);
    UnityEditor.EditorApplication.Step();
    player.Seek(shot, 0.01f);
    var added = new System.Collections.Generic.List<UnityEngine.MeshCollider>();
    foreach (var mf in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshFilter>(UnityEngine.FindObjectsSortMode.None))
    {
        var r = mf.GetComponent<UnityEngine.MeshRenderer>();
        if (r == null || mf.sharedMesh == null || !r.enabled || moverOf(mf.transform) != null) continue;
        if (r.bounds.size.magnitude > 25f || r.bounds.size.magnitude < 0.15f) continue;
        var mat = r.sharedMaterial;
        if (mat == null || mat.renderQueue >= 2500) continue; // glows, sprites, fields: transparent, not solid
        if (mf.sharedMesh.vertexCount < 4) continue;
        var mc = mf.gameObject.AddComponent<UnityEngine.MeshCollider>();
        mc.sharedMesh = mf.sharedMesh;
        added.Add(mc);
    }
    var instancers = UnityEngine.Object.FindObjectsByType<Game.Levels.EnvironmentInstancer>(UnityEngine.FindObjectsSortMode.None);
    var pairs = new System.Collections.Generic.Dictionary<string, (float first, float last, int count)>();
    var overlaps = new UnityEngine.Collider[16];
    for (int step = 0; step <= 120; step++)
    {
        float p = step / 120f;
        player.Seek(shot, p);
        UnityEngine.Physics.SyncTransforms();
        var movers = new System.Collections.Generic.Dictionary<string, UnityEngine.Bounds>();
        foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>(UnityEngine.FindObjectsSortMode.None))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r is UnityEngine.ParticleSystemRenderer || r is UnityEngine.LineRenderer || r is UnityEngine.TrailRenderer) continue;
            var mat = r.sharedMaterial;
            if (mat == null || mat.renderQueue >= 2500) continue;
            string m = moverOf(r.transform);
            if (m == null) continue;
            if (movers.TryGetValue(m, out var b)) { b.Encapsulate(r.bounds); movers[m] = b; } else movers[m] = r.bounds;
        }
        foreach (var kv in movers)
        {
            var b = kv.Value;
            int n = UnityEngine.Physics.OverlapBoxNonAlloc(b.center, b.extents * 0.6f, overlaps);
            for (int k = 0; k < n; k++)
            {
                string key = kv.Key + " x " + overlaps[k].name;
                pairs[key] = pairs.TryGetValue(key, out var e) ? (e.first, p, e.count + 1) : (p, p, 1);
            }
            bool clear = true;
            float radius = UnityEngine.Mathf.Min(b.extents.x, b.extents.z) * 0.6f;
            foreach (var i in instancers) clear &= i.IsClear(b.center - UnityEngine.Vector3.up * (b.extents.y * 0.4f), radius, 0.25f);
            if (!clear)
            {
                string key = kv.Key + " x decor";
                pairs[key] = pairs.TryGetValue(key, out var e) ? (e.first, p, e.count + 1) : (p, p, 1);
            }
        }
    }
    foreach (var c in added) UnityEngine.Object.DestroyImmediate(c);
    sb.Append("shot ").Append(shot).Append(" (").Append(added.Count).Append(" solids):");
    foreach (var kv in pairs) sb.Append(" | ").Append(kv.Key).Append(' ').Append(kv.Value.first.ToString("0.00", inv)).Append('-').Append(kv.Value.last.ToString("0.00", inv)).Append(" n").Append(kv.Value.count);
    sb.Append('\n');
}
System.IO.File.WriteAllText("Tools/PlayTest/r37_field_probe.txt", sb.ToString());
return sb.Length > 2800 ? sb.ToString().Substring(0, 2800) : sb.ToString();
