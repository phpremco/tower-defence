// Round 54: how much of the field the RuinGate hides from the play camera. Bakes the gate's posed mesh into world
// triangles and casts a ray from each grid cell near it towards the (orthographic) camera: a road cell counts as hidden
// when its ground point (+0.3 m) is behind stone, a build cell when a tower's foot (+0.3) and its head (+1.2) both are
// (partly: one of them). When Tools/PlayTest/r54_gate_scale.txt holds a number, the gate is set to that scale first (a
// live look at another size; the data is not changed). Reports the gate's top and the counts with the hidden cells.
var gate = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.RuinGateView>();
if (gate == null) { return "no RuinGate"; }
string scaleArg = System.IO.File.Exists("Tools/PlayTest/r54_gate_scale.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r54_gate_scale.txt").Trim() : "-";
if (scaleArg != "-") { gate.transform.localScale = UnityEngine.Vector3.one * float.Parse(scaleArg, System.Globalization.CultureInfo.InvariantCulture); }
var camera = UnityEngine.Camera.main;
var toCamera = -camera.transform.forward;
var tris = new System.Collections.Generic.List<UnityEngine.Vector3>();
var baked = new UnityEngine.Mesh();
float top = float.MinValue;
foreach (var skinned in gate.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(false))
{
    skinned.BakeMesh(baked, true);
    var toWorld = skinned.transform.localToWorldMatrix;
    var v = baked.vertices;
    var world = new UnityEngine.Vector3[v.Length];
    for (int i = 0; i < v.Length; i++) { world[i] = toWorld.MultiplyPoint3x4(v[i]); top = UnityEngine.Mathf.Max(top, world[i].y); }
    var t = baked.triangles;
    for (int i = 0; i < t.Length; i++) { tris.Add(world[t[i]]); }
}
UnityEngine.Object.Destroy(baked);
System.Func<UnityEngine.Vector3, bool> hidden = p =>
{
    for (int i = 0; i < tris.Count; i += 3)
    {
        var e1 = tris[i + 1] - tris[i];
        var e2 = tris[i + 2] - tris[i];
        var h = UnityEngine.Vector3.Cross(toCamera, e2);
        float a = UnityEngine.Vector3.Dot(e1, h);
        if (a > -1e-7f && a < 1e-7f) { continue; }
        float f = 1f / a;
        var s = p - tris[i];
        float u = f * UnityEngine.Vector3.Dot(s, h);
        if (u < 0f || u > 1f) { continue; }
        var q = UnityEngine.Vector3.Cross(s, e1);
        float w = f * UnityEngine.Vector3.Dot(toCamera, q);
        if (w < 0f || u + w > 1f) { continue; }
        if (f * UnityEngine.Vector3.Dot(e2, q) > 0.01f) { return true; }
    }

    return false;
};
Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
var level = context.Level;
var grid = level.Grid;
var build = new Game.Levels.BuildGrid(level);
var centre = grid.WorldToCell(gate.transform.position);
int roadHidden = 0, roadNear = 0, buildHidden = 0, buildPartly = 0, buildNear = 0;
var sb = new System.Text.StringBuilder();
var cells = new System.Text.StringBuilder();
float ground = gate.transform.position.y;
for (int dx = -12; dx <= 12; dx++)
{
    for (int dz = -12; dz <= 12; dz++)
    {
        var cell = new UnityEngine.Vector2Int(centre.x + dx, centre.y + dz);
        var at = grid.CellToWorld(cell);
        at.y = ground;
        if (build.IsRoad(cell))
        {
            roadNear++;
            if (hidden(at + UnityEngine.Vector3.up * 0.3f)) { roadHidden++; cells.Append(" R").Append(cell); }
        }
        else if (build.CanBuild(cell))
        {
            buildNear++;
            bool foot = hidden(at + UnityEngine.Vector3.up * 0.3f);
            bool head = hidden(at + UnityEngine.Vector3.up * 1.2f);
            if (foot && head) { buildHidden++; cells.Append(" B").Append(cell); }
            else if (foot || head) { buildPartly++; }
        }
    }
}
sb.Append("gate scale ").Append(gate.transform.localScale.x.ToString("0.00")).Append(" top ").Append((top - ground).ToString("0.00"));
sb.Append(" m, opened ").Append(gate.Opened.ToString("0.00")).Append(" s, tris ").Append(tris.Count / 3);
sb.Append(" | road hidden ").Append(roadHidden).Append('/').Append(roadNear);
sb.Append(" | build hidden ").Append(buildHidden).Append(" partly ").Append(buildPartly).Append(" of ").Append(buildNear);
sb.Append(" |").Append(cells);
return sb.ToString();
