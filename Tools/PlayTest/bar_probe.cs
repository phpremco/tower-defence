// Reports every living enemy's health bar: whether it is shown, where it sits in the world and how big it is.
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder();
foreach (var view in UnityEngine.Object.FindObjectsByType<Game.Enemies.EnemyView>(UnityEngine.FindObjectsSortMode.None))
{
    var enemy = view.GetComponent<Game.Enemies.Enemy>();
    if (enemy == null || !enemy.IsAlive) { continue; }
    var bar = (UnityEngine.Transform)typeof(Game.Enemies.EnemyView).GetField("_bar", flags).GetValue(view);
    var plate = (UnityEngine.Transform)typeof(Game.Enemies.EnemyView).GetField("_barPlate", flags).GetValue(view);
    sb.Append(enemy.Definition.name).Append(" hp=").Append(enemy.Health.Fraction.ToString("0.00")).Append(" armor=").Append(enemy.Health.ArmorFraction.ToString("0.00"));
    if (bar == null) { sb.Append(" bar=null; "); continue; }
    sb.Append(" barActive=").Append(bar.gameObject.activeSelf).Append(" plateActive=").Append(plate != null && plate.gameObject.activeSelf)
      .Append(" pos=").Append(bar.position.ToString("0.00")).Append(" scale=").Append(bar.lossyScale.ToString("0.00")).Append(" enemyPos=").Append(enemy.Position.ToString("0.00")).Append("; ");
}
return sb.Length == 0 ? "none" : sb.ToString();
