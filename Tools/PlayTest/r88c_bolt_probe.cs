// Round 88c: every enemy bolt in the air with where it is, where it left and where it lands, and the towers' places.
var inv = System.Globalization.CultureInfo.InvariantCulture;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var type = typeof(Game.Enemies.EnemyBoltView);
var text = new System.Text.StringBuilder();
text.Append("dt ").Append(UnityEngine.Time.deltaTime.ToString("0.000", inv)).Append(" scale ").Append(UnityEngine.Time.timeScale.ToString("0.0", inv)).Append("; ");
foreach (var bolt in UnityEngine.Object.FindObjectsByType<Game.Enemies.EnemyBoltView>(UnityEngine.FindObjectsSortMode.None))
{
    if (!bolt.InFlight) continue;
    var from = (UnityEngine.Vector3)type.GetField("_from", flags).GetValue(bolt);
    var to = (UnityEngine.Vector3)type.GetField("_to", flags).GetValue(bolt);
    float age = (float)type.GetField("_age", flags).GetValue(bolt);
    float seconds = (float)type.GetField("_seconds", flags).GetValue(bolt);
    text.Append("bolt at ").Append(bolt.transform.position.ToString("0.0")).Append(" from ").Append(from.ToString("0.0")).Append(" to ").Append(to.ToString("0.0")).Append(" age ").Append(age.ToString("0.00", inv)).Append('/').Append(seconds.ToString("0.00", inv)).Append("; ");
}
foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None))
{
    if (!tower.Definition.BlocksGroundEnemies) text.Append(tower.Definition.name).Append(' ').Append(tower.transform.position.ToString("0.0")).Append("; ");
}
return text.ToString();
