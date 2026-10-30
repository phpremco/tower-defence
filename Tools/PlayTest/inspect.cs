// Reports what is alive on the field: enemies with their lanes and pace, effects, particles, audio and the wave state.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var sb = new System.Text.StringBuilder();
sb.Append("wave ").Append(context.WaveNumber).Append('/').Append(context.WaveCount);
sb.Append(" base ").Append(context.BaseHealth.Current).Append(" money ").Append(context.Wallet.Money);
int enemies = 0;
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>())
{
    if (!enemy.IsAlive)
    {
        continue;
    }

    enemies++;
    if (enemies <= 6)
    {
        sb.Append("\n  ").Append(enemy.Definition.name).Append(" at ").Append(enemy.Position.ToString("0.00")).Append(" speed x").Append(enemy.SpeedMultiplier.ToString("0.00"));
    }
}

sb.Append("\nenemies alive: ").Append(enemies);
sb.Append("\ndeath effects active: ").Append(UnityEngine.Object.FindObjectsByType<Game.Enemies.EnemyDeathEffect>().Length);
sb.Append("\nimpact flashes active: ").Append(UnityEngine.Object.FindObjectsByType<Game.Combat.ImpactFlash>().Length);
sb.Append("\nparticle systems: ").Append(UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>().Length);
int playing = 0;
foreach (var source in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>())
{
    if (source.isPlaying)
    {
        playing++;
    }
}

sb.Append("\naudio sources playing: ").Append(playing);
return sb.ToString();
