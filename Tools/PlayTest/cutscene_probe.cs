// Reports the cutscene state: shot, time, dialogue, fade, finishing.
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player; scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
var q = player.Sequencer;
string line = q.IsFinished ? "finished" : ("shot " + q.ShotIndex + " " + q.Current.Action + " t=" + q.LocalTime.ToString("0.00") + " p=" + q.Progress.ToString("0.00") + " waiting=" + q.WaitingForDialogue);
return line + " dialogue=" + (player.Dialogue.Current != null ? player.Dialogue.Current.Id + "#" + player.Dialogue.LineIndex : "none") + " fade=" + player.Overlay.FadeAlpha.ToString("0.00") + " finishing=" + player.IsFinishing;
