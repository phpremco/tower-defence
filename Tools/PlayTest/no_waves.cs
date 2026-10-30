// Switches the wave timeline off, so enemies spawned by hand can be watched without the level ending.
var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
controller.enabled = false;
return "waves off";
