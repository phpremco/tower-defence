// Hurts every living enemy by the share in Tools/PlayTest/damage_arg.txt (default 0.4), so health bars show.
float share = System.IO.File.Exists("Tools/PlayTest/damage_arg.txt") ? float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/damage_arg.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture) : 0.4f;
int hurt = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive) continue;
    enemy.TakeDamage(enemy.Health.Max * share);
    hurt++;
}

return "hurt " + hurt + " enemies by " + share;
