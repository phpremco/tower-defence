// Import stage 4a screenshot phases (environment), one CLI call each; the editor stays paused except between "play"
// and "pause" (see imp2_phase.cs). Tools/PlayTest/imp4_phase.txt holds one phase:
//   setup                      waves off, creatures spread along the main road, a few towers beside the road focus
//   view <overview|road|plaza|bank|nest|border|corner|back|base|gate> <ortho half height>
//   place [off]                picks a tower to build (the pads light up), or stops placing
//   play | pause
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/imp4_phase.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
if (args[0] != "play") UnityEditor.EditorApplication.isPaused = true;
var report = new System.Text.StringBuilder();
var level = context.Level;
var grid = level.Grid;
System.Func<UnityEngine.Vector2Int, UnityEngine.Vector3> world = c => grid.CellToWorld(c) + UnityEngine.Vector3.up * level.Terrain.HeightAt(c);
UnityEngine.Vector2Int[] path = new System.Collections.Generic.List<UnityEngine.Vector2Int>(Game.Levels.LevelValidator.LandPathCells(level.MainRoad)).ToArray();
UnityEngine.Vector2Int roadFocus = path[path.Length / 3];
var blocked = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>(level.BlockedCells);
blocked.UnionWith(level.MineCells);
System.Func<UnityEngine.Vector2Int, bool> buildable = c => grid.Contains(c) && !level.Road.IsRoad(c) && !blocked.Contains(c) && !level.Terrain.IsLiquid(c);
if (args[0] == "setup")
{
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    controller.enabled = false;
    string[] kinds = { "Skitter", "Cragback", "Prowler", "Skitter", "Roller", "Skitter", "Cragback", "Prowler", "Skyray", "Shalebat" };
    var field = typeof(Game.Levels.LevelController).GetField("_spawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var spawner = (Game.Levels.EnemySpawner)field.GetValue(controller);
    foreach (string kind in kinds)
    {
        var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + kind + ".asset");
        if (enemy != null) spawner.Spawn(enemy, 0, Game.Levels.SpawnSource.Gate);
    }

    int k = 0;
    foreach (var walker in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
    {
        for (int i = 0; i < 40 + k * 22; i++) walker.Step(0.1f);
        k++;
    }

    // Towers on free cells beside the road focus (never on the road, a rock, a mine or a liquid).
    string[] towers = { "PulseRifle", "Cannon", "Laser", "Rocket", "PulseRifle", "Cannon" };
    int placed = 0;
    for (int ring = 1; ring <= 3 && placed < towers.Length; ring++)
    {
        for (int dx = -4; dx <= 4 && placed < towers.Length; dx++)
        {
            for (int dy = -4; dy <= 4 && placed < towers.Length; dy++)
            {
                var cell = roadFocus + new UnityEngine.Vector2Int(dx, dy);
                if (!buildable(cell)) continue;
                bool near = false;
                for (int ax = -ring; ax <= ring; ax++) for (int ay = -ring; ay <= ring; ay++) near |= level.Road.IsRoad(cell + new UnityEngine.Vector2Int(ax, ay));
                if (!near || UnityEngine.GameObject.Find("Imp4_" + cell) != null) continue;
                var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/" + towers[placed] + ".asset");
                if (definition == null) { placed++; continue; }
                var go = new UnityEngine.GameObject("Imp4_" + cell);
                go.transform.position = world(cell);
                var tower = go.AddComponent<Game.Towers.Tower>();
                go.AddComponent<Game.Towers.TowerView>();
                tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
                tower.Register(context.Structures);
                if (placed % 2 == 1) tower.SetLevel(definition.MaxLevelIndex);
                placed++;
            }
        }
    }

    report.Append("towers ").Append(placed).Append(" road focus ").Append(roadFocus).Append("; ");
}
else if (args[0] == "view")
{
    UnityEngine.Vector3 focus = world(roadFocus);
    string what = args[1];
    if (what == "overview") focus = UnityEngine.Vector3.zero;
    if (what == "base") focus = Game.Levels.LevelVisualBuilder.BasePosition(level);
    if (what == "plaza" && level.PlazaCells.Length > 0) focus = world(level.PlazaCells[level.PlazaCells.Length / 2]);
    if (what == "nest" && level.Nests.Length > 0) focus = world(level.Nests[0].Cell);
    if (what == "gate")
    {
        var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
        if (placement != null && placement.Blockades.Count > 0) focus = placement.Blockades[0].Position;
    }

    if (what == "bank")
    {
        // The liquid (not rift) cell closest to the middle of the field that has a dry neighbour.
        float best = float.MaxValue;
        foreach (var pair in level.Terrain.Cells)
        {
            if (!Game.Levels.TerrainMap.IsLiquid(pair.Value) || pair.Value == Game.Levels.TerrainKind.Rift) continue;
            float d = grid.CellToWorld(pair.Key).sqrMagnitude;
            if (d < best) { best = d; focus = grid.CellToWorld(pair.Key); }
        }
    }

    UnityEngine.Vector2 half = level.WorldSize * 0.5f;
    if (what == "border") focus = new UnityEngine.Vector3(0f, 0f, -half.y + 1.5f);
    if (what == "corner") focus = new UnityEngine.Vector3(half.x - 2f, 0f, half.y - 2f);
    if (what == "back") focus = new UnityEngine.Vector3(-half.x * 0.4f, 0f, half.y - 1f);
    var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
    var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
    if (pan != null) pan.enabled = false;
    float fit = UnityEngine.Camera.main.orthographicSize * rig.Zoom;
    rig.SetView(focus, fit / float.Parse(args[2], inv));
    report.Append("view ").Append(what).Append(' ').Append(focus.ToString("0.0")).Append("; ");
}

else if (args[0] == "place")
{
    // Picks the first available tower, as a click on its card does: the build pads light up.
    if (args.Length > 1 && args[1] == "off") { context.SelectedTower = null; context.Mode = Game.Levels.PlacementMode.Browsing; }
    else
    {
        context.SelectTower(context.AvailableTowers[0]);
        // The cursor on a free cell beside the road focus, as a player would hover it (the free cells glow round it).
        var cursor = roadFocus;
        for (int r = 1; r <= 3 && !buildable(cursor); r++)
            for (int dx = -r; dx <= r && !buildable(cursor); dx++)
                for (int dy = -r; dy <= r; dy++) { var c = roadFocus + new UnityEngine.Vector2Int(dx, dy); if (buildable(c)) { cursor = c; break; } }
        var placer = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
        typeof(Game.Levels.PlacementController).GetMethod("MoveCursor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(placer, new object[] { cursor });
        report.Append("cursor ").Append(cursor).Append("; ");
    }
    report.Append("mode ").Append(context.Mode).Append("; ");
}

if (args[0] == "play") UnityEditor.EditorApplication.isPaused = false;
return report.Append(args[0]).ToString();
