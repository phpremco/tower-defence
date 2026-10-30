var time = UnityEngine.Object.FindAnyObjectByType<Game.Levels.TimeController>();
time.CycleSpeed(); time.CycleSpeed();
return "speed " + time.CurrentSpeed;
