// Hurts every living creature by a share of its health (and damages the first towers a little) so the health bars show.
int hurt = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
{
    if (!enemy.IsAlive) continue;
    enemy.Health.TakeDamage(enemy.Health.Max * (0.2f + 0.5f * (hurt % 3) / 2f));
    hurt++;
}
int towers = 0;
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>())
{
    if (towers >= 3 || tower.Definition.BlocksGroundEnemies) continue;
    tower.Health.TakeDamage(tower.Health.Max * 0.35f);
    towers++;
}
return "hurt " + hurt + " creatures, " + towers + " towers";
