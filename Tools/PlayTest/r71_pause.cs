// Round 71: "on" pauses the running level (the pause menu opens), "off" resumes it.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r71_pause_arg.txt").Trim();
var time = UnityEngine.Object.FindAnyObjectByType<Game.Levels.TimeController>();
if (time == null || !Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no level"; }
if ((mode == "on") != context.Paused) { time.TogglePause(); }
return "paused " + context.Paused + ", timeScale " + UnityEngine.Time.timeScale;
