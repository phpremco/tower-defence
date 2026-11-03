// Seeks the playing cutscene to "shot progress" from Tools/PlayTest/cut_arg.txt (shot index from 0, progress 0..1) and reports the shot.
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no cutscene player";
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
int shot = int.Parse(parts[0]);
float progress = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
player.Seek(shot, progress);
var s = player.Sequencer.Current;
var cam = UnityEngine.Camera.main;
return "shot " + shot + " " + s.Set + "/" + s.Action + " t=" + player.Sequencer.LocalTime.ToString("0.00") + " cam " + cam.transform.position + " fov " + cam.fieldOfView.ToString("0");
