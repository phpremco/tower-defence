// Round 88c: builds guns beside the road ahead of the living level boss, through the real placement path, so a still
// can show its volley landing on them. Tools/PlayTest/r88c_towers_arg.txt: "<TowerId> <count> <least cells off> <most cells off>".
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext c)) return "no context";
string[] a = System.IO.File.ReadAllText("Tools/PlayTest/r88c_towers_arg.txt").Trim().Split(' ');
var inv = System.Globalization.CultureInfo.InvariantCulture;
int wanted = int.Parse(a[1]);
float least = float.Parse(a[2], inv), most = float.Parse(a[3], inv);
Game.Enemies.Enemy boss = null;
foreach (var e in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None)) if (e.IsAlive && e.Definition.IsBoss) boss = e;
if (boss == null) return "no boss";
Game.Towers.TowerDefinition definition = null;
foreach (var t in c.AvailableTowers) if (t.name == a[0]) definition = t;
if (definition == null) definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>("Assets/_Project/Data/Towers/" + a[0] + ".asset");
if (definition == null) return "no tower " + a[0];
var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var type = typeof(Game.Levels.PlacementController);
var towers = (System.Collections.Generic.Dictionary<UnityEngine.Vector2Int, Game.Towers.Tower>)type.GetField("_towers", flags).GetValue(placement);
float cell = c.Level.CellSize;
// A little ahead of the boss, so it walks towards the guns while the still is taken.
UnityEngine.Vector3 centre = boss.Position + boss.Heading * (1.5f * cell);
UnityEngine.Vector2Int origin = c.Level.Grid.WorldToCell(centre);
int built = 0;
var text = new System.Text.StringBuilder();
int reach = UnityEngine.Mathf.CeilToInt(most) + 1;
for (int dx = -reach; dx <= reach && built < wanted; dx++)
{
    for (int dy = -reach; dy <= reach && built < wanted; dy++)
    {
        var at = new UnityEngine.Vector2Int(origin.x + dx, origin.y + dy);
        float off = UnityEngine.Vector3.Distance(c.Level.Grid.CellToWorld(at), centre) / cell;
        if (off < least || off > most || towers.ContainsKey(at)) continue;
        // Not two guns side by side: the still should show three separate hits.
        bool crowded = false;
        foreach (var other in towers.Keys) crowded |= UnityEngine.Mathf.Abs(other.x - at.x) + UnityEngine.Mathf.Abs(other.y - at.y) < 2;
        if (crowded || !(bool)type.GetMethod("CanBuildTower", flags).Invoke(placement, new object[] { at })) continue;
        c.Wallet.Add(definition.BuildCost);
        c.SelectedPlacedTower = null;
        type.GetField("_cursor", flags).SetValue(placement, at);
        c.SelectTower(definition);
        type.GetMethod("TryPlace", flags).Invoke(placement, null);
        if (towers.ContainsKey(at)) { built++; text.Append(' ').Append(at.x).Append(',').Append(at.y); }
    }
}
c.SelectTower(null);
c.Mode = Game.Levels.PlacementMode.Browsing;
type.GetMethod("Refresh", flags).Invoke(placement, null);
return "built " + built + " " + definition.name + " at" + text + " round the boss at " + boss.Position.ToString("0.0");
