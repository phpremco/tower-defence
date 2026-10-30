// Places a pulse rifle through the real placement path (so it gets the road clearance) on the free cell between
// the hero and the camera, where a tower used to sit on top of the hero.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var placement = UnityEngine.Object.FindAnyObjectByType<Game.Levels.PlacementController>();
var create = typeof(Game.Levels.PlacementController).GetMethod("CreateTower", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var camera = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>().GetComponentInChildren<UnityEngine.Camera>();
UnityEngine.Vector3 towards = camera.transform.forward;
towards.y = 0f;
towards.Normalize();
UnityEngine.Vector3 hero = context.Hero.transform.position;
var grid = context.Level.Grid;
Game.Towers.TowerDefinition definition = context.AllTowers[0];
UnityEngine.Vector3 side = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, towards);
var candidates = new[] { hero - towards, hero - towards + side, hero - towards - side, hero + towards, hero + towards + side };
foreach (UnityEngine.Vector3 candidate in candidates)
{
    UnityEngine.Vector2Int cell = grid.WorldToCell(candidate);
    if (!grid.Contains(cell) || context.Level.Terrain.IsLiquid(cell) || System.Array.IndexOf(context.Level.MainRoad, cell) >= 0)
    {
        continue;
    }

    var tower = (Game.Towers.Tower)create.Invoke(placement, new object[] { definition, cell, false });
    return "placed at " + cell + " standing at " + tower.transform.position.ToString("0.00") + " (cell centre " + grid.CellToWorld(cell).ToString("0.00") + ")";
}

return "no free cell by the hero";
