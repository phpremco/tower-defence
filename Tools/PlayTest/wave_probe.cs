// Reports the wave number, countdown, living enemies by name and bosses alive.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var counts = new System.Collections.Generic.SortedDictionary<string, int>();
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (enemy.IsAlive)
    {
        counts[enemy.Definition.name] = (counts.TryGetValue(enemy.Definition.name, out int have) ? have : 0) + 1;
    }
}

var sb = new System.Text.StringBuilder();
sb.Append("wave=").Append(context.WaveNumber).Append(" countdown=").Append(context.NextWaveCountdown.ToString("0.0")).Append(" bosses=").Append(context.BossesAlive).Append(" outcome=").Append(context.Outcome).Append(" alive:");
foreach (var pair in counts)
{
    sb.Append(' ').Append(pair.Value).Append('x').Append(pair.Key);
}

return sb.ToString();
