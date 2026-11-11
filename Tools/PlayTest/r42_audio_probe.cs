// Round 42: the cutscene's shot and clock plus every playing AudioSource (name, clip, volume), compact, for the intro sound run.
var sb = new System.Text.StringBuilder();
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) sb.Append("[no cutscene; scene ").Append(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name).Append("] ");
else sb.Append("[").Append(player.Sequencer.IsFinished ? "finished" : player.Sequencer.ShotIndex + " " + player.Sequencer.Current.Action + " t=" + player.Sequencer.LocalTime.ToString("0.0")).Append("] ");
foreach (var s in UnityEngine.Object.FindObjectsByType<UnityEngine.AudioSource>(UnityEngine.FindObjectsSortMode.None))
{
    if (!s.isPlaying) continue;
    sb.Append(s.gameObject.name).Append("=").Append(s.clip != null ? s.clip.name : "oneshot").Append("@").Append(s.volume.ToString("0.00")).Append(" ");
}
return sb.ToString();
