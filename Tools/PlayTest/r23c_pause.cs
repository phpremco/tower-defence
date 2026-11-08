// Round 23c: "confirm" pauses the level and opens the pause menu's quit question; "close" resumes. For the text sweep.
string mode = System.IO.File.ReadAllText("Tools/PlayTest/r23c_pause.txt").Trim();
var time = UnityEngine.Object.FindAnyObjectByType<Game.Levels.TimeController>();
var menu = UnityEngine.Object.FindAnyObjectByType<Game.UI.PauseMenuView>();
if (time == null || menu == null) { return "no time or menu"; }
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
if (mode == "confirm")
{
    if (!context.Paused) { time.TogglePause(); }
    typeof(Game.UI.PauseMenuView).GetMethod("ShowConfirm", flags).Invoke(menu, null);
    return "quit question open";
}
if (context.Paused) { time.TogglePause(); }
return "resumed";
