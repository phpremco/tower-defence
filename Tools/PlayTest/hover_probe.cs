// Builds a pulse rifle on a build cell beside the road, then puts the cursor in placing mode on the next cell
// along the road, so a screenshot shows the built tower next to the hover marker and the preview.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var type = typeof(Game.Levels.PlacementController);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var create = type.GetMethod("CreateTower", flags);
var move = type.GetMethod("MoveCursor", flags);
var grid = context.Level.Grid;
Game.Levels.RoadCorridor road = context.Level.Road;
Game.Towers.TowerDefinition definition = context.AvailableTowers[0];

// Walk the main road and take the first first-row cell whose left-hand neighbour (away from the second row)
// and the cell after it are both free build cells.
UnityEngine.Vector2Int[] main = context.Level.MainRoad;
foreach (UnityEngine.Vector2Int cell in Game.Levels.LevelValidator.LandPathCells(main))
{
    UnityEngine.Vector3 along = road.DirectionAt(cell);
    var step = new UnityEngine.Vector2Int(UnityEngine.Mathf.RoundToInt(along.x), UnityEngine.Mathf.RoundToInt(along.z));
    var left = new UnityEngine.Vector2Int(-step.y, step.x);
    UnityEngine.Vector2Int first = cell + left;
    UnityEngine.Vector2Int second = first + step;
    if (!grid.Contains(first) || !grid.Contains(second) || road.IsRoad(first) || road.IsRoad(second)
        || System.Array.IndexOf(context.Level.BlockedCells, first) >= 0 || System.Array.IndexOf(context.Level.BlockedCells, second) >= 0
        || context.Level.Terrain.IsLiquid(first) || context.Level.Terrain.IsLiquid(second) || cell == main[0])
    {
        continue;
    }

    var tower = (Game.Towers.Tower)create.Invoke(placement, new object[] { definition, first, false });
    context.SelectedTower = definition;
    context.Mode = Game.Levels.PlacementMode.Placing;
    move.Invoke(placement, new object[] { second });
    return "tower at " + first + " " + tower.transform.position.ToString("0.00") + ", cursor at " + second;
}

return "no suitable cells";
