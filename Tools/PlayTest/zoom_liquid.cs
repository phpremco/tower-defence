// Zooms onto the centre of the largest pool of the terrain kind named in zoom_arg.txt (Water, Lava, Bog, Ice,
// Quicksand) at the zoom in zoom_size.txt, so the surface effects can be screenshotted.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim();
float size = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var kind = (Game.Levels.TerrainKind)System.Enum.Parse(typeof(Game.Levels.TerrainKind), wanted);
var cells = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>();
foreach (var entry in context.Level.Terrain.Cells) if (entry.Value == kind) cells.Add(entry.Key);
if (cells.Count == 0) return "no " + wanted + " cells";
var seen = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>();
var best = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
foreach (var start in cells)
{
    if (seen.Contains(start)) continue;
    var cluster = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
    var stack = new System.Collections.Generic.Stack<UnityEngine.Vector2Int>();
    stack.Push(start); seen.Add(start);
    while (stack.Count > 0)
    {
        var c = stack.Pop(); cluster.Add(c);
        foreach (var d in new[] { UnityEngine.Vector2Int.up, UnityEngine.Vector2Int.down, UnityEngine.Vector2Int.left, UnityEngine.Vector2Int.right })
        {
            var n = c + d;
            if (cells.Contains(n) && seen.Add(n)) stack.Push(n);
        }
    }
    if (cluster.Count > best.Count) best = cluster;
}
UnityEngine.Vector3 sum = UnityEngine.Vector3.zero;
foreach (var c in best) sum += context.Level.Grid.CellToWorld(c);
var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
pan.SetView(sum / best.Count, size);
return "zoomed on " + best.Count + " of " + cells.Count + " " + wanted + " cells";
