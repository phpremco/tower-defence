// Builds a pulse rifle on the free cell between the hero and the camera, the spot where a tower hides the hero.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var camera = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>().GetComponentInChildren<UnityEngine.Camera>();
UnityEngine.Vector3 towards = camera.transform.forward;
towards.y = 0f;
towards.Normalize();
UnityEngine.Vector3 hero = context.Hero.transform.position;
var grid = context.Level.Grid;
Game.Towers.TowerDefinition definition = context.AllTowers[0];
var candidates = new[] { hero - towards, hero - towards + UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, towards), hero - towards - UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, towards) };
foreach (UnityEngine.Vector3 candidate in candidates)
{
    UnityEngine.Vector2Int cell = grid.WorldToCell(candidate);
    if (!grid.Contains(cell) || context.Level.Terrain.IsLiquid(cell) || System.Array.IndexOf(context.Level.MainRoad, cell) >= 0)
    {
        continue;
    }

    var go = new UnityEngine.GameObject("ProbeTower_ByHero");
    go.transform.position = grid.CellToWorld(cell) + UnityEngine.Vector3.up * context.Level.Terrain.HeightAt(cell);
    var tower = go.AddComponent<Game.Towers.Tower>();
    go.AddComponent<Game.Towers.TowerView>();
    tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
    tower.Register(context.Structures);
    return "built at " + cell + " towards " + towards.ToString("0.0");
}

return "no free cell by the hero";
