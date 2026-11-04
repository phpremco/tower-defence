// Night 1, item 4: builds a tower beside the road through the real placement path (or reuses the one built before) and
// reports the screen point of its upper body, the cell a plain ground ray there would hit, and the cursor cell now.
// Arg n1_pick_arg.txt: "place" (build it), "point" (report only).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var towers = (System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, Game.Towers.Tower>)typeof(Game.Levels.PlacementController).GetField("_towers", flags).GetValue(placement);
string arg = System.IO.File.Exists("Tools/PlayTest/n1_pick_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/n1_pick_arg.txt").Trim() : "point";
Game.Towers.Tower tower = null; UnityEngine.Vector2Int towerCell = default;
foreach (var pair in towers) { if (!pair.Value.Definition.BlocksGroundEnemies && !pair.Value.Definition.IsBaseDefence) { tower = pair.Value; towerCell = pair.Key; } }
if (arg == "place" && tower == null)
{
    var grid = typeof(Game.Levels.PlacementController).GetField("_buildGrid", flags).GetValue(placement) as Game.Levels.BuildGrid;
    var road = context.Level.MainRoad;
    UnityEngine.Vector2Int cell = default; bool found = false;
    for (int i = road.Length / 2; i < road.Length && !found; i++)
        foreach (var d in new[] { UnityEngine.Vector2Int.down, UnityEngine.Vector2Int.up, UnityEngine.Vector2Int.left, UnityEngine.Vector2Int.right })
            for (int k = 1; k <= 3 && !found; k++) { var c = road[i] + d * k; if (grid.CanBuild(c) && grid.CanBuild(c + UnityEngine.Vector2Int.up)) { cell = c; found = true; } }
    var terrain = context.Level.Terrain;
    for (int x = 0; x < context.Level.Grid.Size.x && System.IO.File.Exists("Tools/PlayTest/n1_raised.txt"); x++)
        for (int y = 0; y < context.Level.Grid.Size.y; y++)
        { var c = new UnityEngine.Vector2Int(x, y); if (terrain.KindAt(c) == Game.Levels.TerrainKind.Raised && grid.CanBuild(c)) { cell = c; found = true; } }
    if (!found) return "no free cell";
    context.Wallet.Add(5000);
    context.SelectTower(context.AvailableTowers[0]);
    typeof(Game.Levels.PlacementController).GetMethod("MoveCursor", flags).Invoke(placement, new object[] { cell });
    context.Mode = Game.Levels.PlacementMode.Placing;
    typeof(Game.Levels.PlacementController).GetMethod("TryPlace", flags).Invoke(placement, null);
    if (!towers.TryGetValue(cell, out tower)) return "place failed at " + cell;
    towerCell = cell;
    // Away from the tower, so the next pointer move is a real change.
    typeof(Game.Levels.PlacementController).GetMethod("MoveCursor", flags).Invoke(placement, new object[] { cell + new UnityEngine.Vector2Int(3, 3) });
}
if (tower == null) return "no tower";
var cam = UnityEngine.Camera.main;
float top = 0f; foreach (var r in tower.GetComponentsInChildren<UnityEngine.MeshRenderer>()) top = UnityEngine.Mathf.Max(top, r.bounds.max.y);
foreach (var r in tower.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>()) top = UnityEngine.Mathf.Max(top, r.bounds.max.y);
var body = tower.Position + UnityEngine.Vector3.up * ((top - tower.Position.y) * 0.75f);
var screen = cam.WorldToScreenPoint(body);
var ray = cam.ScreenPointToRay(screen);
new UnityEngine.Plane(UnityEngine.Vector3.up, UnityEngine.Vector3.zero).Raycast(ray, out float t);
var groundCell = context.Level.Grid.WorldToCell(ray.GetPoint(t));
System.IO.File.WriteAllText("Tools/PlayTest/n1_pick_xy.txt", UnityEngine.Mathf.RoundToInt(screen.x) + " " + UnityEngine.Mathf.RoundToInt(screen.y));
return "tower cell " + towerCell + " height " + (top - tower.Position.y).ToString("0.00") + " body screen " + screen.ToString("0") + " plain ground ray cell " + groundCell + " cursor " + context.CursorCell + " mode " + context.Mode + " selected " + (context.SelectedPlacedTower != null ? context.SelectedPlacedTower.name : "none") + " screen " + cam.pixelWidth + "x" + cam.pixelHeight;
