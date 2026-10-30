// Places probe towers beside the main road (one kind per lane) and skips to the first wave.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

UnityEngine.Vector2Int[] road = context.Level.MainRoad;
int placed = 0;
int index = 0;
foreach (Game.Towers.TowerDefinition definition in context.AllTowers)
{
    if (definition.BlocksGroundEnemies)
    {
        continue;
    }

    for (int w = 1; w < road.Length - 1 && placed < 14; w += 2)
    {
        UnityEngine.Vector2Int cell = road[w] + (index % 2 == 0 ? UnityEngine.Vector2Int.up : UnityEngine.Vector2Int.down) * (1 + index / 2);
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

    index++;
}

context.SkipWaveRequested.Raise();
return "placed " + placed + ", wave " + context.WaveNumber;
