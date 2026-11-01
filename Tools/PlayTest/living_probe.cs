// Lists every living enemy with its path progress, position and whether it is rolling, to see who actually moves.
var sb = new System.Text.StringBuilder();
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive) { continue; }
    sb.Append(enemy.Definition.name).Append(" progress=").Append(enemy.PathProgress.ToString("0.00")).Append(" pos=").Append(enemy.Position.ToString("0.0")).Append(" rolling=").Append(enemy.IsRolling).Append("; ");
}
return sb.Length == 0 ? "none alive" : sb.ToString();
