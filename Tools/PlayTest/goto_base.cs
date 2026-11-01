// Jumps to the base (upgrade) screen. Play mode must be running from Boot. Gives some points first so rows are buyable.
UnityEngine.Time.timeScale = 1f;
var progress = Game.Core.Services.Get<Game.Economy.ProgressService>();
int research = int.Parse(System.IO.File.ReadAllText("Tools/PlayTest/points_arg.txt").Trim());
progress.AddRewards(research, research);
UnityEngine.SceneManagement.SceneManager.LoadScene(Game.Core.SceneNames.Base);
return "loading base with +" + research + " research and power (not saved until a purchase)";
