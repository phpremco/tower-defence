// Round 37 (Steam screenshots): builds a varied, upgraded defence along the main road like a player would, through the
// bot's build path (BalanceProbeTests.TryBuyAt: buildable cells only, the level's grid). Tools/PlayTest/r37_def_arg.txt:
// "<from share> <to share> <towers> <seed> [air]": cells beside the road between those shares of its length, every
// tower asset in turn (no blockers, no supports; "air" puts air-capable guns first), each upgraded 0..2 times. Fills
// the purse first and leaves plenty in it. Editor only.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var args = System.IO.File.ReadAllText("Tools/PlayTest/r37_def_arg.txt").Trim().Split(' ');
float from = float.Parse(args[0], inv), to = float.Parse(args[1], inv);
int wanted = int.Parse(args[2]); int seed = int.Parse(args[3]);
bool air = args.Length > 4 && args[4] == "air";
var defs = new System.Collections.Generic.List<Game.Towers.TowerDefinition>();
foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:TowerDefinition", new[] { "Assets/_Project/Data/Towers" }))
{
    var d = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (d == null || d.BlocksGroundEnemies || d.IsSupport || d.name.StartsWith("Base")) continue;
    defs.Add(d);
}
if (air) defs.Sort((a, b) => ((b.Targets & Game.Core.TargetLayers.Air) != 0 ? 1 : 0).CompareTo((a.Targets & Game.Core.TargetLayers.Air) != 0 ? 1 : 0));
var rng = new System.Random(seed);
if (!air) { for (int i = defs.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = defs[i]; defs[i] = defs[j]; defs[j] = t; } }
context.Wallet.Add(200000);
var grid = new Game.Levels.BuildGrid(context.Level);
var road = context.Level.MainRoad;
var used = new System.Collections.Generic.HashSet<UnityEngine.Vector2Int>();
foreach (var t in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None)) used.Add(context.Level.Grid.WorldToCell(t.transform.position));
int a = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(from * (road.Length - 1)), 0, road.Length - 1);
int b = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(to * (road.Length - 1)), 0, road.Length - 1);
var candidates = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
for (int r = a; r <= b; r++)
{
    for (int dx = -4; dx <= 4; dx++)
    {
        for (int dy = -4; dy <= 4; dy++)
        {
            if (UnityEngine.Mathf.Max(UnityEngine.Mathf.Abs(dx), UnityEngine.Mathf.Abs(dy)) < 2) continue;
            var cell = road[r] + new UnityEngine.Vector2Int(dx, dy);
            if (!candidates.Contains(cell)) candidates.Add(cell);
        }
    }
}
for (int i = candidates.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = candidates[i]; candidates[i] = candidates[j]; candidates[j] = t; }
int placed = 0, upgrades = 0; var sb = new System.Text.StringBuilder();
foreach (var cell in candidates)
{
    if (placed >= wanted) break;
    if (!grid.CanBuild(cell) || used.Contains(cell)) continue;
    bool crowded = false;
    foreach (var u in used) if (UnityEngine.Mathf.Abs(u.x - cell.x) <= 1 && UnityEngine.Mathf.Abs(u.y - cell.y) <= 1) crowded = true;
    if (crowded) continue;
    var definition = defs[placed % defs.Count];
    if (!context.Wallet.TrySpend(definition.BuildCost)) break;
    var go = new UnityEngine.GameObject("Tower_" + definition.DisplayName);
    go.transform.position = context.Level.Grid.CellToWorld(cell);
    var tower = go.AddComponent<Game.Towers.Tower>();
    go.AddComponent<Game.Towers.TowerView>();
    tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
    tower.Register(context.Structures);
    used.Add(cell);
    int ups = rng.Next(3);
    for (int u = 0; u < ups && tower.CanUpgrade; u++) { if (context.Wallet.TrySpend(tower.NextUpgradeCost)) { tower.Upgrade(); upgrades++; } }
    sb.Append(definition.name).Append(' ');
    placed++;
}
context.Wallet.Add(50000);
return "placed " + placed + " upgrades " + upgrades + " of " + candidates.Count + " cells: " + sb;
