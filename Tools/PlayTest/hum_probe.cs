// Reports which clip the boss laser loop plays.
var sfx = UnityEngine.Object.FindAnyObjectByType<Game.Boot.SfxPlayer>();
var loop = (UnityEngine.AudioSource)typeof(Game.Boot.SfxPlayer).GetField("_bossLaserLoop", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(sfx);
return "bossLaserLoop clip=" + (loop.clip != null ? loop.clip.name : "none") + " loop=" + loop.loop + " length=" + (loop.clip != null ? loop.clip.length.ToString("0.0") : "0");
