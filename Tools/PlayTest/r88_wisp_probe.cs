// Round 88a: every living creature with its layer and health, and every tower with what it aims at (does the flak take the Wisp?).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var sb = new System.Text.StringBuilder();
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive) { continue; }
    sb.Append(enemy.Definition.name).Append(' ').Append(enemy.Layer).Append(" hp ").Append(enemy.Health.Current.ToString("0")).Append('/').Append(enemy.Health.Max.ToString("0")).Append(" at ").Append(enemy.Position.ToString("0.0")).Append(" | ");
}
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None))
{
    if (tower.Definition.BlocksGroundEnemies) { continue; }
    var best = context.Targets.FindBest(tower.Position, tower.Range, tower.Definition.Targets);
    sb.Append(tower.Definition.name).Append(" (").Append(tower.Definition.Targets).Append(") aims at ").Append(best is Game.Enemies.Enemy e ? e.Definition.name : "nothing").Append(" | ");
}
return sb.ToString();
