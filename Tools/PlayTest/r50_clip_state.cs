// Round 50: for every living enemy named in enemy_arg.txt (first word), what its model plays: the cycle (clip, rate, time)
// and the clip on top (clip, time, weight share), whether it moved since the last call, and the height of its left wing tip
// bone over the body (Hand_L or Fin_L_A3) so a still can be taken mid-beat.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/enemy_arg.txt").Trim().Split(' ')[0];
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder();
foreach (var enemy in UnityEngine.Object.FindObjectsByType<Game.Enemies.Enemy>(UnityEngine.FindObjectsSortMode.None))
{
    if (!enemy.IsAlive || enemy.Definition.name != wanted) { continue; }
    var view = enemy.GetComponent<Game.Enemies.EnemyView>();
    var model = (Game.Enemies.CreatureModel)typeof(Game.Enemies.EnemyView).GetField("_model", flags).GetValue(view);
    if (model == null) { sb.Append("no model; "); continue; }
    var player = model.Player;
    var clips = model.Art.Clips;
    sb.Append(wanted).Append(" pos ").Append(enemy.Position.ToString("0.00"));
    sb.Append(" cycle=").Append(player.Cycle >= 0 ? clips[player.Cycle].Clip.name : "-").Append(" x").Append(player.CycleRate.ToString("0.00"));
    sb.Append(" once=").Append(player.Once >= 0 ? clips[player.Once].Clip.name + "@" + player.OnceTime.ToString("0.00") : "none");
    var wing = model.FindBone("Hand_L") ?? model.FindBone("Fin_L_A3");
    var body = model.FindBone("Body");
    if (wing != null && body != null) { sb.Append(" wingTip-body y=").Append((wing.position.y - body.position.y).ToString("0.00")); }
    var abilities = enemy.GetComponent<Game.Enemies.BossAbilities>();
    if (abilities != null) { sb.Append(" laser=").Append(abilities.LaserActive); }
    sb.Append("; ");
}

return sb.Length == 0 ? "no living " + wanted : sb.ToString();
