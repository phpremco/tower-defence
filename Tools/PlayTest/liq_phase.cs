// Liquids and origins round screenshot phases, one CLI call each (the editor stays paused except between "play" and
// "pause"). Tools/PlayTest/liq_phase.txt holds one phase:
//   swap apply <level index>     in memory only: every other water pool of that campaign level becomes lava (no level
//                                has both); run before start_level.cs. "swap restore <level index>" puts the water back.
//   view <Water|Lava|...|all|origin> <ortho half height> [origin index]   frames the largest pool of a kind, the field, or an origin
//   play | pause                 lets the level run, or holds it
//   waves off                    stops the wave timeline (for still shots of the field)
//   wave <n>                     starts wave n (1-based) now and holds the controller
//   simulate <seconds>           runs the wave logic (spawns, origins) without frames
//   stepto <origin> <state> <s>  steps real frames until that origin has been in that state for s seconds
//   probe                        lists liquid sheets, bank meshes, mist sheets and spawn origins with their state
string[] args = System.IO.File.ReadAllText("Tools/PlayTest/liq_phase.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
if (args[0] == "swap")
{
    var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.CampaignCatalog>("Assets/_Project/Data/Levels/CampaignCatalog.asset");
    var level = catalog.Nodes[int.Parse(args[2])].Level;
    var patches = level.TerrainPatches;
    var map = new Game.Levels.TerrainMap(patches);
    var from = args[1] == "apply" ? Game.Levels.TerrainKind.Water : Game.Levels.TerrainKind.Lava;
    var to = args[1] == "apply" ? Game.Levels.TerrainKind.Lava : Game.Levels.TerrainKind.Water;
    var seen = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>();
    int pool = 0;
    int swapped = 0;
    var pools = new System.Collections.Generic.List<System.Collections.Generic.List<UnityEngine.Vector2Int>>();
    foreach (var patch in patches)
    {
        if (patch.Kind != from || seen.Contains(patch.Cell)) continue;
        var cluster = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
        var stack = new System.Collections.Generic.Stack<UnityEngine.Vector2Int>();
        stack.Push(patch.Cell); seen.Add(patch.Cell);
        while (stack.Count > 0)
        {
            var c = stack.Pop(); cluster.Add(c);
            foreach (var d in new[] { UnityEngine.Vector2Int.up, UnityEngine.Vector2Int.down, UnityEngine.Vector2Int.left, UnityEngine.Vector2Int.right })
            {
                var n = c + d;
                if (map.KindAt(n) == from && seen.Add(n)) stack.Push(n);
            }
        }
        pools.Add(cluster);
    }
    pools.Sort((a, b) => b.Count.CompareTo(a.Count));
    var chosen = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>();
    for (int p = 0; p < pools.Count; p++)
    {
        // Apply: the second, fourth, ... largest pools turn to lava; restore: every lava pool turns back.
        if (args[1] == "apply" && p % 2 == 0) continue;
        foreach (var c in pools[p]) chosen.Add(c);
        pool++;
    }
    for (int i = 0; i < patches.Length; i++)
    {
        if (chosen.Contains(patches[i].Cell)) { patches[i].Kind = to; swapped++; }
    }
    typeof(Game.Levels.LevelDefinition).GetField("_terrainMap", flags).SetValue(level, null);
    return "swap " + args[1] + ": " + pool + " pool(s), " + swapped + " cell(s) of " + pools.Count + " " + from + " pools";
}

if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
if (args[0] != "play") UnityEditor.EditorApplication.isPaused = true;
var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
if (args[0] == "waves")
{
    controller.enabled = args[1] != "off";
    return "waves " + args[1];
}

if (args[0] == "wave")
{
    int target = int.Parse(args[1]) - 1;
    typeof(Game.Levels.LevelController).GetField("_nextWaveIndex", flags).SetValue(controller, target);
    typeof(Game.Levels.LevelController).GetMethod("StartNextWave", flags).Invoke(controller, null);
    controller.enabled = false;
    return "wave " + (target + 1) + " started, controller held";
}

if (args[0] == "simulate")
{
    // Runs the wave logic (spawns and origins) for the given seconds without rendering frames, controller held.
    float seconds = float.Parse(args[1], inv);
    var spawn = typeof(Game.Levels.LevelController).GetMethod("SpawnDueEnemies", flags);
    for (float t = 0f; t < seconds; t += 0.05f)
    {
        context.Origins?.Advance(0.05f);
        if (typeof(Game.Levels.LevelController).GetField("_scheduler", flags).GetValue(controller) != null) spawn.Invoke(controller, new object[] { 0.05f, false });
    }
    return "simulated " + seconds + "s, origin 0 " + (context.Origins != null && context.Origins.Count > 0 ? context.Origins.StateOf(0) + " " + context.Origins.TimeInState(0).ToString("0.00", inv) : "-") + ", alive " + context.Level.name;
}

if (args[0] == "stepto")
{
    // Steps real frames (controller running) until origin args[1] is in state args[2] for args[3] seconds.
    int origin = 0;
    if (args[1] == "busy")
    {
        for (int i = 0; i < context.Origins.Count; i++)
        {
            var s = context.Origins.StateOf(i);
            if (s == Game.Levels.OriginState.Opening || s == Game.Levels.OriginState.Open) { origin = i; break; }
        }
    }
    else origin = int.Parse(args[1]);
    var state = (Game.Levels.OriginState)System.Enum.Parse(typeof(Game.Levels.OriginState), args[2]);
    float after = float.Parse(args[3], inv);
    controller.enabled = true;
    int frames = 0;
    while (frames < 400 && !(context.Origins.StateOf(origin) == state && context.Origins.TimeInState(origin) >= after))
    {
        UnityEditor.EditorApplication.Step();
        frames++;
    }
    controller.enabled = false;
    return "stepped " + frames + " frames: origin " + origin + " " + context.Origins.StateOf(origin) + " " + context.Origins.TimeInState(origin).ToString("0.00", inv);
}

if (args[0] == "view")
{
    var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
    UnityEngine.Vector3 focus = UnityEngine.Vector3.zero;
    string what = args[1];
    if (what == "origin")
    {
        var origins = UnityEngine.Object.FindObjectsByType<Game.Levels.SpawnOriginView>();
        if (origins.Length == 0) return "no origins";
        int index = 0;
        for (int i = 0; i < origins.Length; i++)
        {
            bool busy = origins[i].State == Game.Levels.OriginState.Opening || origins[i].State == Game.Levels.OriginState.Open;
            if (args.Length > 3 && args[3] == "busy" ? busy : origins[i].name.StartsWith("Origin_" + (args.Length > 3 ? args[3] : "0") + "_")) { index = i; break; }
        }
        focus = origins[index].transform.position;
    }
    else if (what == "gate")
    {
        var gates = new System.Collections.Generic.List<UnityEngine.Transform>();
        foreach (var gate in UnityEngine.Object.FindObjectsByType<Game.Levels.SpawnGateView>()) gates.Add(gate.transform);
        gates.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        if (gates.Count == 0) return "no gates";
        var chosen = gates[UnityEngine.Mathf.Min(int.Parse(args[3]), gates.Count - 1)];
        focus = new UnityEngine.Vector3(chosen.position.x, 0f, chosen.position.z);
        what = chosen.name + " of " + gates.Count;
    }
    else if (what != "all")
    {
        var kind = (Game.Levels.TerrainKind)System.Enum.Parse(typeof(Game.Levels.TerrainKind), what);
        int best = 0;
        var pools = Game.Levels.LiquidShape.ForLevel(context.Level);
        foreach (var shape in pools)
        {
            if (shape.Kind != kind || shape.Cells.Count <= best) continue;
            best = shape.Cells.Count;
            focus = new UnityEngine.Vector3(shape.Bounds.center.x, 0f, shape.Bounds.center.y);
        }
        if (best == 0) return "no " + what + " pool";
    }
    pan.enabled = true;
    pan.SetView(focus, 1f);
    pan.enabled = false;
    float half = float.Parse(args[2], inv);
    if (half > 0f) UnityEngine.Camera.main.orthographicSize = half;
    return "view " + what + " at " + focus.ToString("0.0") + " half " + half;
}

if (args[0] == "probe")
{
    var report = new System.Text.StringBuilder();
    foreach (var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>())
    {
        string n = renderer.gameObject.name;
        if (!n.StartsWith("Liquid_") && !n.StartsWith("Bank_") && !n.StartsWith("Mist_")) continue;
        var mesh = renderer.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
        report.Append(n).Append(" v").Append(mesh != null ? mesh.vertexCount : 0).Append(" ").Append(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "-").Append("; ");
    }
    foreach (var origin in UnityEngine.Object.FindObjectsByType<Game.Levels.SpawnOriginView>())
    {
        report.Append(origin.name).Append(" ").Append(origin.Describe()).Append("; ");
    }
    report.Append("wave ").Append(context.WaveNumber).Append(" countdown ").Append(context.NextWaveCountdown.ToString("0.0", inv));
    return report.ToString();
}

if (args[0] == "play") UnityEditor.EditorApplication.isPaused = false;
return args[0];
