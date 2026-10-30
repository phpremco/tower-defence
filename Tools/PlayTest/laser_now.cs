// Makes every living boss fire its laser on the next frame by zeroing the laser ability's timer.
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
int fired = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    var abilities = enemy.IsAlive ? enemy.GetComponent<Game.Enemies.BossAbilities>() : null;
    if (abilities == null)
    {
        continue;
    }

    var timers = (float[])typeof(Game.Enemies.BossAbilities).GetField("_timers", flags).GetValue(abilities);
    var definitions = enemy.Definition.Abilities;
    for (int i = 0; i < definitions.Length && i < timers.Length; i++)
    {
        if (definitions[i].Kind == Game.Enemies.BossAbilityKind.Laser)
        {
            timers[i] = 0f;
            fired++;
        }
    }
}

return "lasers primed: " + fired;
