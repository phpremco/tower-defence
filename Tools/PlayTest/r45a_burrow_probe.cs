// Round 45a: the burrows closing in a played run: the swarm clock, each burrow's close time, and every playing burrow-close
// source with how far its slam (0.15 s before the file's end) lies ahead, next to how far the burrow's shut frame lies ahead.
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var set = player.CurrentSet as Game.UI.FieldSet;
if (set == null) return "no field";
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var swarm = (Game.UI.FieldSwarm)typeof(Game.UI.FieldSet).GetField("_swarm", flags).GetValue(set);
float clock = (float)typeof(Game.UI.FieldSwarm).GetField("_lastClock", flags).GetValue(swarm);
var sb = new System.Text.StringBuilder();
sb.Append(player.Sequencer.Current.Action).Append(" t=").Append(player.Sequencer.LocalTime.ToString("0.00")).Append(" clock=").Append(clock.ToString("0.00")).Append(" closes=");
for (int i = 0; i < swarm.BurrowCount; i++) sb.Append(swarm.CloseTimeOf(i).ToString("0.00")).Append(i + 1 < swarm.BurrowCount ? "/" : "");
foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None))
{
    if (!s.isPlaying || s.clip == null || !s.clip.name.StartsWith("threat_burrow_close")) continue;
    float slamIn = s.clip.length - 0.15f - s.time;
    sb.Append(" | ").Append(s.gameObject.name).Append("=").Append(s.clip.name).Append(" len=").Append(s.clip.length.ToString("0.00")).Append(" at=").Append(s.time.ToString("0.00")).Append(" slamIn=").Append(slamIn.ToString("0.00"));
    sb.Append(" -> slam at clock ").Append((clock + slamIn).ToString("0.00"));
}
return sb.ToString();
