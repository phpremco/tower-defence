// Round 39: crystals stuck in rocks. Every drawn crystal of the field (the instanced decor and the cutscene's own vein
// crystals and ore heap) has its mesh vertices tested against every rock, boulder, spire, cliff, terrace, scree patch and
// silhouette near it, and against the base modules, the drill and the capsule: a vertex counts as inside when it lies
// inside the obstacle's convex hull (one convex MeshCollider per mesh, the point taken into the piece's own space). Prints
// each crystal with more than 8 % of its vertices inside something, and a summary. Seeks Tools/PlayTest/r39_base_arg.txt.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var parts = System.IO.File.ReadAllText("Tools/PlayTest/r39_base_arg.txt").Trim().Split(' ');
int shot = int.Parse(parts[0]); float p = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
player.Seek(shot, p);
UnityEditor.EditorApplication.Step();
player.Seek(shot, p);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var crystals = new System.Collections.Generic.List<(string name, UnityEngine.Mesh mesh, UnityEngine.Matrix4x4 m)>();
var obstacles = new System.Collections.Generic.List<(string name, UnityEngine.Mesh mesh, UnityEngine.Matrix4x4 m, UnityEngine.Bounds b)>();
System.Func<UnityEngine.Mesh, UnityEngine.Matrix4x4, UnityEngine.Bounds> world = (mesh, m) =>
{
    var b = mesh.bounds; var r = new UnityEngine.Bounds(m.MultiplyPoint3x4(b.center), UnityEngine.Vector3.zero);
    for (int c = 0; c < 8; c++) r.Encapsulate(m.MultiplyPoint3x4(new UnityEngine.Vector3((c & 1) == 0 ? b.min.x : b.max.x, (c & 2) == 0 ? b.min.y : b.max.y, (c & 4) == 0 ? b.min.z : b.max.z)));
    return r;
};
string[] solid = { "Rock_", "Decor_Boulder", "Decor_Spire", "Cliff_", "TerraceStep", "Scree_Patch", "Silhouette_Mesa", "Silhouette_Ridge", "GatePlinth", "Rift_Cap" };
foreach (var inst in UnityEngine.Object.FindObjectsByType<Game.Levels.EnvironmentInstancer>(UnityEngine.FindObjectsSortMode.None))
{
    var batches = (System.Collections.IList)typeof(Game.Levels.EnvironmentInstancer).GetField("_batches", flags).GetValue(inst);
    foreach (var b in batches)
    {
        var t = b.GetType();
        var mesh = (UnityEngine.Mesh)t.GetField("Mesh").GetValue(b);
        var all = (UnityEngine.Matrix4x4[])t.GetField("All").GetValue(b);
        if (mesh == null || all == null) continue;
        bool crystal = mesh.name.StartsWith("Crystal_") || mesh.name == "CrystalVein" || mesh.name == "Silhouette_CrystalTower";
        bool isSolid = false; foreach (var s in solid) isSolid |= mesh.name.StartsWith(s);
        foreach (var m in all)
        {
            if (crystal) crystals.Add(("decor " + mesh.name, mesh, m));
            else if (isSolid) obstacles.Add(("decor " + mesh.name, mesh, m, world(mesh, m)));
        }
    }
}
UnityEngine.Transform set = null;
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None)) if (t.name == "Set_Field") set = t;
foreach (var mf in set.GetComponentsInChildren<UnityEngine.MeshFilter>(false))
{
    var r = mf.GetComponent<UnityEngine.MeshRenderer>(); if (r == null || !r.enabled || mf.sharedMesh == null) continue;
    string top = mf.transform.name; for (var q = mf.transform.parent; q != null && q != set; q = q.parent) top = q.name + "/" + top;
    if (top.StartsWith("Vein") || top.StartsWith("OreHeap") || top.StartsWith("Pebble")) { if (top.StartsWith("Vein") || mf.sharedMesh.name.Contains("Crystal")) crystals.Add(("cut " + top, mf.sharedMesh, mf.transform.localToWorldMatrix)); }
    else if (top.StartsWith("Command") || top.StartsWith("Gate") || top.StartsWith("Capsule") || top.StartsWith("Beacon")) obstacles.Add(("cut " + top, mf.sharedMesh, mf.transform.localToWorldMatrix, r.bounds));
}
foreach (var sk in set.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(false))
{
    string top = sk.transform.name; for (var q = sk.transform.parent; q != null && q != set; q = q.parent) top = q.name + "/" + top;
    if (!(top.StartsWith("Capsule") || top.StartsWith("Drill"))) continue;
    var mesh = new UnityEngine.Mesh(); sk.BakeMesh(mesh, true); mesh.name = top;
    obstacles.Add(("cut " + top, mesh, sk.transform.localToWorldMatrix, sk.bounds));
}
var colliders = new System.Collections.Generic.Dictionary<UnityEngine.Mesh, UnityEngine.MeshCollider>();
var holder = new UnityEngine.GameObject("r39_hulls");
var sb = new System.Text.StringBuilder(); int stuck = 0, tested = 0;
var report = new System.Collections.Generic.List<string>();
foreach (var c in crystals)
{
    var cb = world(c.mesh, c.m);
    var verts = c.mesh.vertices; int stepV = UnityEngine.Mathf.Max(1, verts.Length / 150);
    string worstName = null; float worst = 0f;
    foreach (var o in obstacles)
    {
        if (!o.b.Intersects(cb)) continue;
        if (!colliders.TryGetValue(o.mesh, out var col))
        {
            var go = new UnityEngine.GameObject(o.mesh.name); go.transform.SetParent(holder.transform, false);
            col = go.AddComponent<UnityEngine.MeshCollider>(); col.convex = true; col.sharedMesh = o.mesh; colliders[o.mesh] = col;
        }
        var inv = o.m.inverse; int inside = 0, n = 0;
        for (int i = 0; i < verts.Length; i += stepV)
        {
            var local = inv.MultiplyPoint3x4(c.m.MultiplyPoint3x4(verts[i]));
            var cp = UnityEngine.Physics.ClosestPoint(local, col, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
            n++; if ((cp - local).sqrMagnitude < 1e-8f) inside++;
        }
        float share = n > 0 ? inside / (float)n : 0f;
        if (share > worst) { worst = share; worstName = o.name; }
    }
    tested++;
    if (worst > 0.08f) { stuck++; report.Add(c.name + " at " + cb.center.ToString("F1") + " h" + cb.size.y.ToString("0.0") + " in " + worstName + " " + (worst * 100f).ToString("0") + "%"); }
}
UnityEngine.Object.DestroyImmediate(holder);
sb.Append("crystals ").Append(tested).Append(" obstacles ").Append(obstacles.Count).Append(" stuck ").Append(stuck).Append('\n');
foreach (var s in report) sb.Append(s).Append('\n');
System.IO.File.WriteAllText("Tools/PlayTest/r39_crystal_probe.txt", sb.ToString());
return sb.Length > 2500 ? sb.ToString().Substring(0, 2500) : sb.ToString();
