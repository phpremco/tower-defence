// Upgrades every tower that is not a gate by one level, so upgrade looks can be checked.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

int count = 0;
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>())
{
    if (!tower.Definition.BlocksGroundEnemies && tower.CanUpgrade)
    {
        tower.Upgrade();
        count++;
    }
}

return "upgraded " + count + " towers";
