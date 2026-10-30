Game.Core.Services.TryGet(out Game.Levels.LevelContext context);
var sb = new System.Text.StringBuilder();
sb.Append("base at " + context.Base.Position.ToString("0.0") + " hero at " + context.Hero.transform.position.ToString("0.0"));
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>()) { if (tower.Definition.BlocksGroundEnemies) sb.Append(" gate " + tower.Position.ToString("0.0") + " hp " + tower.Health.Current.ToString("0")); }
return sb.ToString();
