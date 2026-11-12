// Round 55: the way the Shattered walks from its ruin entrance to the road (the same planner BackToRoad uses, run from the
// entrance cell), as offsets from the entrance in metres, with the camera's flat facing, so the gate's axis can be chosen
// against both. Reads the brain's field by reflection (a probe only).
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss brain"; }
var field = (Game.Bosses.BossField)typeof(Game.Bosses.ContinentBossBrain).GetProperty("Field", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(brain);
Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
var level = context.Level;
var spot = level.Grid.CellToWorld(level.BossEntrance.Cell);
var points = new System.Collections.Generic.List<UnityEngine.Vector3>();
bool ok = Game.Bosses.BossRoutes.WalkToRoad(field, spot, true, points);
var toCamera = -UnityEngine.Camera.main.transform.forward; toCamera.y = 0f; toCamera.Normalize();
var sb = new System.Text.StringBuilder();
sb.Append("entrance ").Append(level.BossEntrance.Cell).Append(" world ").Append(spot.ToString("0.00")).Append(" toCamera ").Append(toCamera.ToString("0.00"));
sb.Append(" cell ").Append(level.Grid.CellSize.ToString("0.00")).Append(" route ").Append(ok).Append(':');
foreach (var p in points) { var d = p - spot; sb.Append(" (").Append(d.x.ToString("0.0")).Append(',').Append(d.z.ToString("0.0")).Append(')'); }
sb.Append(" | giant at ").Append((brain.transform.position - spot).ToString("0.00"));
var build = new Game.Levels.BuildGrid(level);
var c = level.BossEntrance.Cell;
sb.Append(" | map (row +z first; R road, B build, . other, E entrance):");
for (int dz = 6; dz >= -6; dz--)
{
    sb.Append(" /");
    for (int dx = -8; dx <= 8; dx++)
    {
        var cell = new UnityEngine.Vector2Int(c.x + dx, c.y + dz);
        sb.Append(dx == 0 && dz == 0 ? 'E' : build.IsRoad(cell) ? 'R' : build.CanBuild(cell) ? 'B' : '.');
    }
}

return sb.ToString();
