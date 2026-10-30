// Upgrades every gate one level, so the gate gun can be checked.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

int count = 0;
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>())
{
    if (tower.Definition.BlocksGroundEnemies && tower.LevelIndex < tower.Definition.MaxLevelIndex)
    {
        tower.Upgrade();
        count++;
    }
}

return "upgraded " + count + " gates";
