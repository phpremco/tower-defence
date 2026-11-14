// Round 62: puts the HUD in the state that shows a tower's ground/air read. Tools/PlayTest/r62_hint_arg.txt holds one line:
// "hand <TowerId>"  the tower's card picked from the bar, the cursor on a free build cell (the status line names it),
// "menu <TowerId>"  a tower of that kind built beside the road and its menu opened (the info line names it),
// "state"           what the HUD says now.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) return "no context";
string[] a = System.IO.File.ReadAllText("Tools/PlayTest/r62_hint_arg.txt").Trim().Split(' ');
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var type = typeof(Game.Levels.PlacementController);
var cursorField = type.GetField("_cursor", flags);
var canBuild = type.GetMethod("CanBuildTower", flags);
var refresh = type.GetMethod("Refresh", flags);
var towers = (System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, Game.Towers.Tower>)type.GetField("_towers", flags).GetValue(placement);
Game.Towers.TowerDefinition wanted = null;
if (a.Length > 1)
{
    foreach (var t in c.AvailableTowers) if (t.name == a[1]) wanted = t;
    if (wanted == null) return "tower " + a[1] + " is not in this level's bar";
}
// A free build cell beside the middle of the main road, where the play camera looks.
System.Func<UnityEngine.Vector2Int?> freeCell = () =>
{
    var road = c.Level.MainRoad;
    for (int r = road.Length / 2; r < road.Length; r++)
        for (int d = 2; d <= 4; d++)
            foreach (var step in new[] { new UnityEngine.Vector2Int(0, d), new UnityEngine.Vector2Int(0, -d), new UnityEngine.Vector2Int(d, 0), new UnityEngine.Vector2Int(-d, 0) })
            {
                var cell = road[r] + step;
                if (!towers.ContainsKey(cell) && (bool)canBuild.Invoke(placement, new object[] { cell })) return cell;
            }
    return null;
};
if (a[0] == "hand")
{
    var cell = freeCell();
    if (cell == null) return "no free build cell";
    c.Wallet.Add(wanted.BuildCost);
    c.SelectedPlacedTower = null;
    cursorField.SetValue(placement, cell.Value);
    c.SelectTower(wanted);
    refresh.Invoke(placement, null);
}
else if (a[0] == "menu")
{
    Game.Towers.Tower built = null;
    foreach (var pair in towers) if (pair.Value.Definition == wanted) built = pair.Value;
    if (built == null)
    {
        var cell = freeCell();
        if (cell == null) return "no free build cell";
        c.Wallet.Add(wanted.BuildCost);
        cursorField.SetValue(placement, cell.Value);
        c.SelectTower(wanted);
        type.GetMethod("TryPlace", flags).Invoke(placement, null);
        towers.TryGetValue(cell.Value, out built);
        if (built == null) return "could not build " + wanted.name + " at " + cell.Value;
    }
    cursorField.SetValue(placement, c.Level.Grid.WorldToCell(built.Position));
    c.SelectedPlacedTower = built;
    c.Mode = Game.Levels.PlacementMode.TowerMenu;
    refresh.Invoke(placement, null);
}
string info = string.Empty;
var menu = UnityEngine.Object.FindAnyObjectByType<Game.UI.TowerMenuView>();
if (menu != null)
{
    var text = (UnityEngine.UI.Text)typeof(Game.UI.TowerMenuView).GetField("_info", flags).GetValue(menu);
    info = menu.Panel.gameObject.activeSelf && text != null ? text.text.Replace("\n", " / ") : "(closed)";
    // Where the panel sits on its canvas (0..1 across it, y up), so a still that misses it can be told from a panel off screen.
    var canvas = menu.Panel.GetComponentInParent<UnityEngine.Canvas>(true).rootCanvas;
    var canvasRect = (UnityEngine.RectTransform)canvas.transform;
    var corners = new UnityEngine.Vector3[4];
    menu.Panel.GetWorldCorners(corners);
    UnityEngine.Vector3 low = canvasRect.InverseTransformPoint(corners[0]), high = canvasRect.InverseTransformPoint(corners[2]);
    UnityEngine.Rect r = canvasRect.rect;
    info += string.Format(System.Globalization.CultureInfo.InvariantCulture, " [panel x {0:0.00}..{1:0.00} y {2:0.00}..{3:0.00} of canvas {4:0}x{5:0}, {6}, sorting {7}]",
        (low.x - r.xMin) / r.width, (high.x - r.xMin) / r.width, (low.y - r.yMin) / r.height, (high.y - r.yMin) / r.height, r.width, r.height, canvas.renderMode, canvas.sortingOrder);
}
return a[0] + " mode=" + c.Mode + " status='" + c.Status + "' menu='" + info + "'";
