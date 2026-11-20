// Round 88a: the drawn size of every spawn prop of the level (living origins, route gates, nests) in cells, and the
// build cells whose square its bounds touch.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext lc)) return "no context";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var sb = new System.Text.StringBuilder();
float cell = lc.Level.CellSize;
sb.Append("cell ").Append(cell.ToString("0.00", inv));
var build = new Game.Levels.BuildGrid(lc.Level);
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsSortMode.None))
{
    if (!(t.name.StartsWith("Origin_") || t.name.StartsWith("SpawnGate_") || t.name.StartsWith("Nest_"))) continue;
    bool any = false; var b = new UnityEngine.Bounds();
    foreach (var r in t.GetComponentsInChildren<UnityEngine.Renderer>())
    {
        if (r is UnityEngine.ParticleSystemRenderer || r.name.Contains("Pool") || r.name.Contains("Glow")) continue;
        if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
    }
    var at = lc.Level.Grid.WorldToCell(t.position);
    int can = 0;
    for (int x = -4; x <= 4; x++) for (int y = -4; y <= 4; y++)
    {
        var c = at + new UnityEngine.Vector2Int(x, y);
        var w = lc.Level.Grid.CellToWorld(c);
        if (build.CanBuild(c) && UnityEngine.Mathf.Abs(w.x - b.center.x) < b.extents.x + cell * 0.5f && UnityEngine.Mathf.Abs(w.z - b.center.z) < b.extents.z + cell * 0.5f) can++;
    }
    sb.Append(" | ").Append(t.name).Append(" cell ").Append(at).Append(" size ").Append((b.size.x / cell).ToString("0.0", inv)).Append("x").Append((b.size.z / cell).ToString("0.0", inv)).Append(" off ").Append(((b.center.x - t.position.x) / cell).ToString("0.0", inv)).Append(",").Append(((b.center.z - t.position.z) / cell).ToString("0.0", inv)).Append(" buildable under ").Append(can);
}
return sb.ToString();
