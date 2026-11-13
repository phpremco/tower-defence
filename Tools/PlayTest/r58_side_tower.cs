// Round 58: places one probe tower (the level's first available tower) beside the living boss named in enemy_arg.txt, at the
// offset "dx dz" (world units) from Tools/PlayTest/r58_side_offset.txt, so its laser has a target well off its heading.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
string[] offset = System.IO.File.ReadAllText("Tools/PlayTest/r58_side_offset.txt").Trim().Split(' ');
var delta = new UnityEngine.Vector3(float.Parse(offset[0], System.Globalization.CultureInfo.InvariantCulture), 0f, float.Parse(offset[1], System.Globalization.CultureInfo.InvariantCulture));
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != wanted) { continue; }
    var definition = context.AvailableTowers[0];
    UnityEngine.Vector2Int cell = context.Level.Grid.WorldToCell(enemy.Position + delta);
    var go = new UnityEngine.GameObject("ProbeTower_" + definition.DisplayName);
    go.transform.position = context.Level.Grid.CellToWorld(cell);
    var tower = go.AddComponent<Game.Towers.Tower>();
    go.AddComponent<Game.Towers.TowerView>();
    tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
    tower.Register(context.Structures);
    return "placed " + definition.DisplayName + " at " + cell + " " + go.transform.position.ToString("0.0") + " boss at " + enemy.Position.ToString("0.0");
}

return "no living " + wanted;
