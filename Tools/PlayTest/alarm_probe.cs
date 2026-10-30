// Reports which clip the boss alarm plays.
var music = UnityEngine.Object.FindAnyObjectByType<Game.Boot.MusicPlayer>();
var alarm = (UnityEngine.AudioClip)typeof(Game.Boot.MusicPlayer).GetField("_alarm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(music);
return "alarm clip=" + (alarm != null ? alarm.name + " " + alarm.length.ToString("0.0") + "s" : "none");
