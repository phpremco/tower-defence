// Kills every living enemy on the field, so a single creature can be spawned and watched on its own.
int killed = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive)
    {
        enemy.TakeDamage(1000000f);
        killed++;
    }
}

return "killed " + killed;
