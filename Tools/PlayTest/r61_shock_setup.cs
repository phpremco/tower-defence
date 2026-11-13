// Round 61: stages the hero's shockwave for stills. Reads Tools/PlayTest/r61_shock_arg.txt:
//   "towers"            places a Cannon (else the dearest tower the level sells) and a Gun Turret beside the hero through the
//                       real placement path, so their shots are in the same frame as the wave;
//   "near"              reports how many living creatures stand inside the shockwave's radius of the hero;
//   "fire"              uses the hero's first ability (the shockwave) through the level's own request event.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
string arg = System.IO.File.ReadAllText("Tools/PlayTest/r61_shock_arg.txt").Trim();
var hero = context.Hero;
if (arg == "near")
{
    int near = 0;
    foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
    {
        if (enemy.IsAlive && UnityEngine.Vector3.Distance(enemy.Position, hero.transform.position) < 3f) { near++; }
    }

    return "near " + near;
}

if (arg == "fire")
{
    context.HeroAbilityRequested.Raise(0);
    return "requested ability 0 at t " + UnityEngine.Time.time.ToString("0.00");
}

var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var create = typeof(Game.Levels.PlacementController).GetMethod("CreateTower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var camera = UnityEngine.Camera.main;
UnityEngine.Vector3 towards = camera.transform.forward;
towards.y = 0f;
towards.Normalize();
UnityEngine.Vector3 side = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, towards);
UnityEngine.Vector3 at = hero.transform.position;
var grid = context.Level.Grid;
Game.Towers.TowerDefinition heavy = null;
Game.Towers.TowerDefinition light = null;
foreach (var tower in context.AllTowers)
{
    if (tower.IsSupport || tower.BlocksGroundEnemies) { continue; }
    if (tower.name == "Cannon" || (heavy == null && tower.SplashRadius > 0f)) { heavy = tower; }
    if (tower.name == "PulseRifle") { light = tower; }
}

var sb = new System.Text.StringBuilder();
var wanted = new[] { heavy, light, light };
int placed = 0;
var offsets = new[] { side * 2f - towards, -side * 2f - towards, side * 2f + towards, -side * 2f + towards, side * 3f, -side * 3f, towards * 2f + side, towards * 2f - side };
foreach (UnityEngine.Vector3 offset in offsets)
{
    if (placed >= wanted.Length) { break; }
    if (wanted[placed] == null) { placed++; continue; }
    UnityEngine.Vector2Int cell = grid.WorldToCell(at + offset * grid.CellSize);
    if (!grid.Contains(cell) || !new Game.Levels.BuildGrid(context.Level).CanBuild(cell)) { continue; }
    create.Invoke(placement, new object[] { wanted[placed], cell, false });
    sb.Append(wanted[placed].name).Append(" at ").Append(cell).Append("; ");
    placed++;
}

return sb.Length > 0 ? sb.ToString() : "no free cell by the hero";
