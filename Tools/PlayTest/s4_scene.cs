// S4 screenshot helper. Reads one command from Tools/PlayTest/s4_arg.txt and acts on the running level:
//   dialogue <id>                 plays the dialogue asset Data/Story/Dialogues/<id> in the box
//   spawn <Enemy> <count>         spawns creatures through the level's spawner (gate or flight route)
//   tower <Id> <anchor> <n>       builds n towers of that id on the free cells nearest the anchor
//   gadget <Id> <anchor>          lays a path object on the road cell nearest the anchor
//   ability <slot>                presses a hero ability through the HUD event (gates and price apply)
//   focus <anchor> <zoom>         points the camera at the anchor (zoom: bigger is closer)
//   rift <zoom>                   points the camera at the largest rift
//   freeze | thaw                 stops or restarts time for a still
// Anchors: post (the hero's post), air (a third along the first flight route), road (a fifth along the main road),
// hero, enemy:<Name>.
string[] a = System.IO.File.ReadAllText("Tools/PlayTest/s4_arg.txt").Trim().Split(' ');
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var level = context.Level;
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
System.Func<UnityEngine.Vector3[], float, UnityEngine.Vector3> along = (route, share) =>
{
    float total = 0f;
    for (int i = 1; i < route.Length; i++) { total += (route[i] - route[i - 1]).magnitude; }
    float want = total * share;
    for (int i = 1; i < route.Length; i++)
    {
        float leg = (route[i] - route[i - 1]).magnitude;
        if (want <= leg) { return UnityEngine.Vector3.Lerp(route[i - 1], route[i], want / leg); }
        want -= leg;
    }

    return route[route.Length - 1];
};
System.Func<string, UnityEngine.Vector3> anchor = name =>
{
    if (name == "post") { return context.Hero.StandAt(context.Hero.PostIndex); }
    if (name == "hero") { return context.Hero.transform.position; }
    if (name == "road") { return along(level.Road.Route(0), 0.2f); }
    if (name == "air")
    {
        for (int p = 0; p < level.Paths.Length; p++)
        {
            if (level.Paths[p].Flying) { return along(level.Road.Route(p), 0.35f); }
        }
    }

    if (name.StartsWith("enemy:"))
    {
        string wanted = name.Substring(6);
        foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
        {
            if (enemy.IsAlive && enemy.Definition.name == wanted) { return enemy.Position; }
        }
    }

    return context.Hero.transform.position;
};
float Arg(int i, float fallback) => a.Length > i ? float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture) : fallback;

switch (a[0])
{
    case "dialogue":
    {
        var dialogue = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Levels.DialogueDefinition>("Assets/_Project/Data/Story/Dialogues/" + a[1] + ".asset");
        if (dialogue == null) { return "no dialogue " + a[1]; }
        context.Dialogue.Clear();
        context.Dialogue.Enqueue(dialogue);
        return "playing " + a[1];
    }
    case "spawn":
    {
        var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + a[1] + ".asset");
        var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
        var spawner = (Game.Levels.EnemySpawner)typeof(Game.Levels.LevelController).GetField("_spawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(controller);
        int count = (int)Arg(2, 1f);
        for (int i = 0; i < count; i++) { spawner.Spawn(definition, 0, Game.Levels.SpawnSource.Gate); }
        return "spawned " + count + " " + a[1];
    }
    case "tower":
    {
        Game.Towers.TowerDefinition tower = null;
        foreach (var t in context.AllTowers) { if (t.Id == a[1]) { tower = t; } }
        if (tower == null) { return "no tower " + a[1]; }
        UnityEngine.Vector3 at = anchor(a[2]);
        int count = (int)Arg(3, 1f);
        var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
        var grid = (Game.Levels.BuildGrid)typeof(Game.Levels.PlacementController).GetField("_buildGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(placement);
        var create = typeof(Game.Levels.PlacementController).GetMethod("CreateTower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cells = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
        for (int x = 0; x < level.GridSize.x; x++)
        {
            for (int y = 0; y < level.GridSize.y; y++)
            {
                var c = new UnityEngine.Vector2Int(x, y);
                if (grid.CanBuild(c)) { cells.Add(c); }
            }
        }

        cells.Sort((p, q) => (level.Grid.CellToWorld(p) - at).sqrMagnitude.CompareTo((level.Grid.CellToWorld(q) - at).sqrMagnitude));
        int built = 0;
        for (int i = 0; i < cells.Count && built < count; i++)
        {
            create.Invoke(placement, new object[] { tower, cells[i], false });
            built++;
        }

        return "built " + built + " " + a[1];
    }
    case "gadget":
    {
        Game.PathObjects.PathObjectDefinition definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.PathObjects.PathObjectDefinition>("Assets/_Project/Data/PathObjects/" + a[1] + ".asset");
        UnityEngine.Vector3 at = anchor(a[2]);
        UnityEngine.Vector2Int best = UnityEngine.Vector2Int.zero;
        float bestDistance = float.MaxValue;
        foreach (UnityEngine.Vector2Int c in level.Road.Cells)
        {
            float d = (level.Grid.CellToWorld(c) - at).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = c; }
        }

        var go = new UnityEngine.GameObject("PathObject_" + definition.DisplayName);
        go.transform.position = level.Grid.CellToWorld(best);
        var pathObject = go.AddComponent<Game.PathObjects.PathObject>();
        pathObject.Initialize(definition, context.Targets, new UnityEngine.Vector3[0], 0);
        go.AddComponent<Game.PathObjects.PathObjectView>().Initialize(pathObject);
        context.PathObjectSpawned.Raise(pathObject);
        return "laid " + a[1] + " at " + best;
    }
    case "ability":
        context.HeroAbilityRequested.Raise((int)Arg(1, 0f));
        return "pressed ability " + a[1] + " money " + context.Wallet.Money;
    case "focus":
        rig.SetView(anchor(a[1]), Arg(2, 6f));
        return "focus " + a[1];
    case "rift":
    {
        var sum = UnityEngine.Vector3.zero;
        int n = 0;
        UnityEngine.Vector2Int first = UnityEngine.Vector2Int.zero;
        foreach (var cell in level.Terrain.Cells)
        {
            if (cell.Value == Game.Levels.TerrainKind.Rift)
            {
                if (n == 0) { first = cell.Key; }
                if ((cell.Key - first).sqrMagnitude < 16) { sum += level.Grid.CellToWorld(cell.Key); n++; }
            }
        }

        if (n == 0) { return "no rift"; }
        rig.SetView(sum / n, Arg(1, 6f));
        return "rift of " + n + " cells at " + (sum / n).ToString("0.0");
    }
    case "freeze":
        UnityEngine.Time.timeScale = 0f;
        return "frozen";
    case "thaw":
        UnityEngine.Time.timeScale = 1f;
        return "thawed";
}

return "unknown command " + a[0];
