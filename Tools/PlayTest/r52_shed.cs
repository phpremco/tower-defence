// Round 52 (49G import): chips the first living Shardback's armour with small hits until exactly one of its six prism
// clusters has come off (armour fraction just under 5/6), the way tower fire does it in play. Reports armour and health.
const int Clusters = 6;
const float Hit = 0.01f;
const int MaxHits = 400;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != "Shardback") continue;
    float target = (Clusters - 1f) / Clusters;
    int hits = 0;
    while (enemy.Health.ArmorFraction >= target && enemy.IsAlive && hits < MaxHits)
    {
        enemy.TakeDamage(enemy.Health.ArmorMax * Hit);
        hits++;
    }

    return "hits " + hits + " armour " + enemy.Health.ArmorFraction.ToString("0.000") + " health " + enemy.Health.Fraction.ToString("0.000");
}

return "no living Shardback";
