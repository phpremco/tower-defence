// Reports the boss laser state: whether a boss is alive with its beam on, whether the beam renderers are enabled,
// and how many boss lasers the sound player still counts. Optional argument "kill" in enemy_arg.txt kills every boss first.
string arg = System.IO.File.Exists("Tools/PlayTest/enemy_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim() : "";
var sb = new System.Text.StringBuilder();
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive && enemy.Definition.IsBoss)
    {
        var abilities = enemy.GetComponent<Game.Enemies.BossAbilities>();
        sb.Append("boss=").Append(enemy.Definition.name).Append(" laserActive=").Append(abilities != null && abilities.LaserActive).Append(' ');
        if (arg == "kill")
        {
            enemy.TakeDamage(1000000f);
            sb.Append("killed ");
        }
    }
}

foreach (var line in UnityEngine.Object.FindObjectsByType<UnityEngine.LineRenderer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
    if (line.name == "BossBeam")
    {
        sb.Append("beamEnabled=").Append(line.enabled).Append(' ');
    }
}

var sfx = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
if (sfx != null)
{
    sb.Append("hums=").Append(typeof(Game.Boot.SfxPlayer).GetField("_bossLasers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sfx));
}

return sb.Length > 0 ? sb.ToString() : "no boss, no beam";
