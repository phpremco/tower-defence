// Wrecks every gate on the field, to see the collapse animation and the wreck look.
int wrecked = 0;
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>())
{
    if (tower.Definition.BlocksGroundEnemies && !tower.IsWrecked)
    {
        tower.TakeDamage(100000f);
        wrecked++;
    }
}

return "wrecked gates: " + wrecked;
