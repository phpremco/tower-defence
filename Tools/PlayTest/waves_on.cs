// Switches the wave timeline back on after no_waves.cs, so the level runs its waves again.
var controller = UnityEngine.Object.FindAnyObjectByType<Game.Levels.LevelController>();
controller.enabled = true;
return "waves on";
