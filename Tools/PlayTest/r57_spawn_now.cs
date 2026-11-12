// Round 57: makes every living boss with a Spawn power call it soon: its timer is set to the Spawn clip's lead (the clip's
// moment) plus a little, so the view starts the clip ahead of the power as in play (zeroing it, as volley_now.cs does, skips
// the clip), and the Sealed One opens its maw and throws the moment's sparks from its socket.
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
int fired = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    var abilities = enemy.IsAlive ? enemy.GetComponent<Game.Enemies.BossAbilities>() : null;
    if (abilities == null) { continue; }
    var timers = (float[])typeof(Game.Enemies.BossAbilities).GetField("_timers", flags).GetValue(abilities);
    var definitions = enemy.Definition.Abilities;
    for (int i = 0; i < definitions.Length && i < timers.Length; i++)
    {
        if (definitions[i].Kind == Game.Enemies.BossAbilityKind.Spawn) { int clip = enemy.Definition.Art != null ? enemy.Definition.Art.IndexOfTrigger("Spawn") : -1; timers[i] = (clip >= 0 ? enemy.Definition.Art.Clips[clip].EventSeconds : 0f) + 0.3f; fired++; }
    }
}

return "spawns primed: " + fired;
