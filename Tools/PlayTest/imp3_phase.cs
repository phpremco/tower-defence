// Import stage 3a screenshot phases, one CLI call each (see imp2_phase.cs for why the editor is held paused between them).
// Tools/PlayTest/imp3_phase.txt holds one phase:
//   setup                      waves off, a few creatures sent down the road, one drone of each kind bought
//   gadgets <road index>       every path object of the level placed along the road leg from that waypoint (again on each call)
//   view <gate|base|gadgets|mine|drone|showcase> <ortho half height>
//   play | pause               lets the level run in real time, or holds it, between captures
//   gate <share|wreck|rebuild|upgrade>   the first gate hurt to a health share, wrecked, rebuilt or upgraded
//   base <share>               the base hurt to a health share
//   mine <halve|empty>         the first mining node told it is half or fully mined
//   showcase <Repair|Harvest|Attack|off>  a big display copy of that drone beside the base (lesson: judge drones at zoom 9)
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/imp3_phase.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
if (args[0] != "play") UnityEditor.EditorApplication.isPaused = true;
var report = new System.Text.StringBuilder();
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
Game.Towers.Tower gate = placement != null && placement.Blockades.Count > 0 ? placement.Blockades[0] : null;
UnityEngine.Vector3 basePosition = Game.Levels.LevelVisualBuilder.BasePosition(context.Level);
var grid = context.Level.Grid;
System.Func<UnityEngine.Vector2Int, UnityEngine.Vector3> world = c => grid.CellToWorld(c) + UnityEngine.Vector3.up * context.Level.Terrain.HeightAt(c);
if (args[0] == "setup")
{
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    controller.enabled = false;
    string[] kinds = { "Skitter", "Cragback", "Skitter", "Roller" };
    var field = typeof(Game.Levels.LevelController).GetField("_spawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var spawner = (Game.Levels.EnemySpawner)field.GetValue(controller);
    foreach (string kind in kinds)
    {
        var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + kind + ".asset");
        if (enemy != null) spawner.Spawn(enemy, 0, Game.Levels.SpawnSource.Gate);
    }

    foreach (var walker in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
    {
        for (int i = 0; i < 100; i++) walker.Step(0.1f);
    }

    foreach (Game.Drones.DroneDefinition drone in context.Level.Drones)
    {
        context.Wallet.Add(10000);
        context.DroneRequested.Raise(drone);
    }

    report.Append("gate ").Append(gate != null ? gate.Position.ToString("0.0") : "none").Append(", base ").Append(basePosition.ToString("0.0")).Append(", mines ").Append(context.Level.MineCells.Length).Append("; ");
}
else if (args[0] == "gadgets")
{
    var old = UnityEngine.GameObject.Find("Imp3Gadgets");
    if (old != null) UnityEngine.Object.DestroyImmediate(old);
    var parent = new UnityEngine.GameObject("Imp3Gadgets");
    // Every cell of the main road (MainRoad holds only its corners).
    UnityEngine.Vector2Int[] road = new System.Collections.Generic.List<UnityEngine.Vector2Int>(Game.Levels.LevelValidator.LandPathCells(context.Level.MainRoad)).ToArray();
    int at = int.Parse(args[1]);
    int index = 0;
    // Every path object in the data, not only the level's loadout, so one level shows them all.
    foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:PathObjectDefinition", new[] { "Assets/_Project/Data/PathObjects" }))
    {
        var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.PathObjects.PathObjectDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
        bool beside = definition.Placement == Game.PathObjects.PathObjectPlacement.Beside;
        UnityEngine.Vector2Int cell = road[System.Math.Min(road.Length - 1, at + index * 3)];
        UnityEngine.Vector2Int step = road[System.Math.Min(road.Length - 1, at + index * 3 + 1)] - cell;
        if (beside) cell += new UnityEngine.Vector2Int(-step.y, step.x) * (context.Level.Road.Width + 1);
        index++;
        var go = new UnityEngine.GameObject("Imp3_" + definition.name);
        go.transform.SetParent(parent.transform, true);
        go.transform.position = world(cell);
        UnityEngine.Vector3[] route = new UnityEngine.Vector3[0];
        int next = 0;
        if (definition.Kind == Game.PathObjects.PathObjectKind.Roller && context.Level.Road.TryLocate(cell, out UnityEngine.Vector3[] found, out int nextIndex)) { route = found; next = nextIndex; }
        var pathObject = go.AddComponent<Game.PathObjects.PathObject>();
        pathObject.Initialize(definition, context.Targets, route, next);
        go.AddComponent<Game.PathObjects.PathObjectView>().Initialize(pathObject);
        report.Append(definition.name).Append(definition.Art != null ? "(art)" : "(proc)").Append(' ');
    }
}
else if (args[0] == "view")
{
    UnityEngine.Vector3 focus = basePosition;
    if (args[1] == "gate" && gate != null) focus = gate.Position;
    if (args[1] == "mine" && context.Level.MineCells.Length > 0) focus = world(context.Level.MineCells[0]);
    if (args[1] == "showcase") focus = basePosition + new UnityEngine.Vector3(0f, 0.4f, 4f);
    if (args[1] == "drone")
    {
        var drone = UnityEngine.Object.FindAnyObjectByType<Game.Drones.Drone>();
        if (drone != null) focus = drone.Position + UnityEngine.Vector3.down * 2f;
    }

    if (args[1] == "gadgets")
    {
        var parent = UnityEngine.GameObject.Find("Imp3Gadgets");
        focus = UnityEngine.Vector3.zero;
        int count = 0;
        foreach (UnityEngine.Transform child in parent.transform) { focus += child.position; count++; }
        focus /= System.Math.Max(1, count);
    }

    var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
    var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
    if (pan != null) pan.enabled = false;
    float fit = UnityEngine.Camera.main.orthographicSize * rig.Zoom;
    rig.SetView(focus, fit / float.Parse(args[2], inv));
    report.Append("view ").Append(args[1]).Append(' ').Append(focus.ToString("0.0")).Append("; ");
}
else if (args[0] == "gate" && gate != null)
{
    if (args[1] == "wreck") gate.TakeDamage(1000000f);
    else if (args[1] == "rebuild") gate.RepairFully();
    else if (args[1] == "upgrade") { context.Wallet.Add(10000); gate.SetLevel(gate.Definition.MaxLevelIndex); }
    else
    {
        // The hero on the post absorbs part of each hit, so the gate is hit until it is down to the share.
        float share = float.Parse(args[1], inv);
        for (int i = 0; i < 40 && gate.Health.Fraction > share + 0.01f; i++) gate.TakeDamage(gate.Health.Current - gate.Health.Max * share);
    }
    report.Append("gate level ").Append(gate.LevelIndex).Append(" health ").Append(gate.Health.Fraction.ToString("0.00")).Append(" wrecked ").Append(gate.IsWrecked).Append("; ");
}
else if (args[0] == "base")
{
    var health = context.BaseHealth;
    health.TakeDamage(health.Current - health.Max * float.Parse(args[1], inv));
    report.Append("base ").Append(health.Fraction.ToString("0.00")).Append("; ");
}
else if (args[0] == "mine")
{
    if (args[1] == "halve") context.MineHalved.Raise(0); else context.MineDepleted.Raise(0);
}
else if (args[0] == "showcase")
{
    var old = UnityEngine.GameObject.Find("DroneShowcase");
    if (old != null) UnityEngine.Object.DestroyImmediate(old);
    if (args[1] != "off")
    {
        var showcase = new UnityEngine.GameObject("DroneShowcase");
        int slot = 0;
        foreach (Game.Drones.DroneDefinition drone in context.Level.Drones)
        {
            var holder = new UnityEngine.GameObject("Showcase_" + drone.name);
            holder.transform.SetParent(showcase.transform, false);
            holder.transform.position = basePosition + new UnityEngine.Vector3((slot - 1) * 2.2f, 1.2f, 4f);
            slot++;
            holder.transform.localScale = UnityEngine.Vector3.one * 2.5f;
            holder.transform.rotation = UnityEngine.Quaternion.Euler(0f, -35f, 0f);
            Game.Drones.DroneModelBuilder.BuildDisplay(holder.transform, drone);
        }
    }
}

// EditorApplication.Step only queues one frame however often it is called, so time passes by playing between phases.
if (args[0] == "play") UnityEditor.EditorApplication.isPaused = false;
return report.Append(args[0]).ToString();
