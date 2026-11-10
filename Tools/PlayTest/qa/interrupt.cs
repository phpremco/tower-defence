// QA round 32: what a lost window focus does in a player build (the editor skips the message itself).
UnityEngine.Object.FindAnyObjectByType<Game.Levels.TimeController>().PauseForInterruption();
return "interrupted, timeScale " + UnityEngine.Time.timeScale;
