// Round 65: small field actions for stills. Tools/PlayTest/r65_act_arg.txt holds one line:
//   "tower <TowerId> <x> <y> [upgrades]"  builds that tower on the cell through the real placement path (money added first)
//   "select <x> <y>"                      puts the cursor on the tower of that cell and opens its menu (range ring, info line)
//   "hand <TowerId> <x> <y>"              picks the card and puts the placing cursor on the cell (range ring of the preview)
//   "spawn <Enemy> <pathIndex> [count] [Gate|Nest]"  spawns creatures on a path, or out of a nest of their element
//   "probe"                               towers with their cell, height, range and the status and info lines of the HUD
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) return "no context";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string[] a = System.IO.File.ReadAllText("Tools/PlayTest/r65_act_arg.txt").Trim().Split(' ');
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var type = typeof(Game.Levels.PlacementController);
var cursorField = type.GetField("_cursor", flags);
var refresh = type.GetMethod("Refresh", flags);
var towers = (System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, Game.Towers.Tower>)type.GetField("_towers", flags).GetValue(placement);
System.Func<string, Game.Towers.TowerDefinition> byId = id =>
{
    foreach (var t in c.AvailableTowers) if (t.name == id) return t;
    return UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/" + id + ".asset");
};
if (a[0] == "tower")
{
    var wanted = byId(a[1]);
    if (wanted == null) return "no tower " + a[1];
    var cell = new UnityEngine.Vector2Int(int.Parse(a[2]), int.Parse(a[3]));
    bool can = (bool)type.GetMethod("CanBuildTower", flags).Invoke(placement, new object[] { cell });
    if (!can) return "cell " + cell + " is not a build cell";
    c.Wallet.Add(wanted.BuildCost);
    c.SelectedPlacedTower = null;
    cursorField.SetValue(placement, cell);
    c.SelectTower(wanted);
    type.GetMethod("TryPlace", flags).Invoke(placement, null);
    if (!towers.TryGetValue(cell, out var built)) return "could not build " + wanted.name + " at " + cell;
    int ups = a.Length > 4 ? int.Parse(a[4]) : 0;
    for (int i = 0; i < ups && built.CanUpgrade; i++) built.Upgrade();
    c.SelectTower(null);
    c.Mode = Game.Levels.PlacementMode.Browsing;
    refresh.Invoke(placement, null);
    return wanted.name + " at " + cell + " y " + built.transform.position.y.ToString("0.00", inv) + " range " + built.Range.ToString("0.00", inv) + " level " + built.LevelIndex;
}
if (a[0] == "select")
{
    var cell = new UnityEngine.Vector2Int(int.Parse(a[1]), int.Parse(a[2]));
    if (!towers.TryGetValue(cell, out var built)) return "no tower at " + cell;
    cursorField.SetValue(placement, cell);
    c.SelectedPlacedTower = built;
    c.Mode = Game.Levels.PlacementMode.TowerMenu;
    refresh.Invoke(placement, null);
    return "selected " + built.Definition.name + " range " + built.Range.ToString("0.00", inv);
}
if (a[0] == "hand")
{
    var wanted = byId(a[1]);
    var cell = new UnityEngine.Vector2Int(int.Parse(a[2]), int.Parse(a[3]));
    c.Wallet.Add(wanted.BuildCost);
    c.SelectedPlacedTower = null;
    cursorField.SetValue(placement, cell);
    c.SelectTower(wanted);
    refresh.Invoke(placement, null);
    return "hand " + wanted.name + " on " + cell;
}
if (a[0] == "spawn")
{
    var enemy = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Enemies.EnemyDefinition>("Assets/_Project/Data/Enemies/" + a[1] + ".asset");
    if (enemy == null) return "no enemy " + a[1];
    var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
    var spawner = (Game.Levels.EnemySpawner)typeof(Game.Levels.LevelController).GetField("_spawner", flags).GetValue(controller);
    int count = a.Length > 3 ? int.Parse(a[3]) : 1;
    var source = a.Length > 4 ? (Game.Levels.SpawnSource)System.Enum.Parse(typeof(Game.Levels.SpawnSource), a[4]) : Game.Levels.SpawnSource.Gate;
    for (int i = 0; i < count; i++) spawner.Spawn(enemy, int.Parse(a[2]), source);
    return "spawned " + count + " " + enemy.name + " on path " + a[2] + " from " + source;
}
var sb = new System.Text.StringBuilder();
var seen = new System.Collections.Generic.HashSet<Game.Towers.Tower>();
foreach (var pair in towers)
{
    if (!seen.Add(pair.Value)) continue;
    sb.Append(pair.Value.Definition.name + "@" + pair.Key + " y=" + pair.Value.transform.position.y.ToString("0.00", inv) + " range=" + pair.Value.Range.ToString("0.00", inv) + "; ");
}
sb.Append(" status: " + c.Status);
return sb.ToString();
