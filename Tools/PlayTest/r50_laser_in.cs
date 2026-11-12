// Round 50: sets every living boss's laser timer to the seconds in Tools/PlayTest/r50_laser_in.txt (laser_now.cs fires at
// once, which starts the aiming clip on its release frame; a lead longer than the clip's shows the whole aim).
float seconds = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r50_laser_in.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
int primed = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    var abilities = enemy.IsAlive ? enemy.GetComponent<Game.Enemies.BossAbilities>() : null;
    if (abilities == null) { continue; }
    var timers = (float[])typeof(Game.Enemies.BossAbilities).GetField("_timers", flags).GetValue(abilities);
    var definitions = enemy.Definition.Abilities;
    for (int i = 0; i < definitions.Length && i < timers.Length; i++)
    {
        if (definitions[i].Kind == Game.Enemies.BossAbilityKind.Laser) { timers[i] = seconds; primed++; }
    }
}

return "lasers due in " + seconds + " s: " + primed;
