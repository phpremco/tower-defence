// Places one probe tower of every tower asset (not only the level's roster) beside the road, close to the spawn,
// then skips to the first wave. Editor only: the definitions are loaded straight from the asset database.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

UnityEngine.Vector2Int[] road = context.Level.MainRoad;
string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TowerDefinition", new[] { "Assets/_Project/Data/Towers" });
int placed = 0;
int index = 0;
foreach (string guid in guids)
{
    var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Towers.TowerDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
    if (definition == null || definition.BlocksGroundEnemies)
    {
        continue;
    }

    int w = 1 + (index / 4) * 2;
    if (w >= road.Length - 1)
    {
        break;
    }

    UnityEngine.Vector2Int cell = road[w] + (index % 2 == 0 ? UnityEngine.Vector2Int.up : UnityEngine.Vector2Int.down) * (1 + (index % 4) / 2);
    index++;
    if (!context.Level.Grid.Contains(cell) || context.Level.Terrain.IsLiquid(cell))
    {
        continue;
    }

    var go = new UnityEngine.GameObject("ProbeTower_" + definition.DisplayName);
    go.transform.position = context.Level.Grid.CellToWorld(cell);
    var tower = go.AddComponent<Game.Towers.Tower>();
    go.AddComponent<Game.Towers.TowerView>();
    tower.Initialize(definition, context.Targets, context.Projectiles, context.ModifiersFor(definition));
    tower.Register(context.Structures);
    placed++;
}

context.SkipWaveRequested.Raise();
return "placed " + placed + " of " + guids.Length + ", wave " + context.WaveNumber;
