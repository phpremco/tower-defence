// Night 1, item 4: for every built tower, the drawn model's bounds against the tower's logical position and its cell.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var sb = new System.Text.StringBuilder();
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>())
{
    if (tower.Definition.BlocksGroundEnemies) continue;
    bool any = false; UnityEngine.Bounds b = default;
    foreach (var r in tower.GetComponentsInChildren<UnityEngine.Renderer>())
    {
        if (!(r is UnityEngine.MeshRenderer || r is UnityEngine.SkinnedMeshRenderer) || !r.enabled) continue;
        if (any) b.Encapsulate(r.bounds); else { b = r.bounds; any = true; }
        sb.Append("  ").Append(r.name).Append(" ").Append(r.bounds.center.ToString("0.00")).Append(" size ").Append(r.bounds.size.ToString("0.00")).AppendLine();
    }
    var cell = context.Level.Grid.WorldToCell(tower.Position);
    sb.Insert(0, tower.name + " pos " + tower.Position.ToString("0.00") + " cell " + cell + " cellCentre " + context.Level.Grid.CellToWorld(cell).ToString("0.00") + " bounds " + b.center.ToString("0.00") + " size " + b.size.ToString("0.00") + "\n");
}
return sb.ToString();
