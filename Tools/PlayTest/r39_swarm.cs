// Round 39: the brood during a played run: shot, time, dialogue, the swarm's retreat clock and how many creatures stand.
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player; scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
var q = player.Sequencer;
var set = player.CurrentSet;
string swarm = "no field";
if (set is Game.UI.FieldSet)
{
    var s = (Game.UI.FieldSwarm)typeof(Game.UI.FieldSet).GetField("_swarm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(set);
    int active = 0;
    foreach (UnityEngine.Transform c in s.Root) if (c.gameObject.activeSelf && c.GetComponent<Game.Enemies.Enemy>() != null) active++;
    swarm = "retreat " + (float.IsPositiveInfinity(s.RetreatClock) ? "held" : s.RetreatClock.ToString("0.0")) + " creatures " + active + " swarmActive " + s.Root.gameObject.activeSelf;
}
return (q.IsFinished ? "finished" : "shot " + q.ShotIndex + " " + q.Current.Action + " t=" + q.LocalTime.ToString("0.0")) + " dialogue=" + (player.Dialogue.Current != null ? player.Dialogue.Current.Id + "#" + player.Dialogue.LineIndex : "none") + " | " + swarm;
