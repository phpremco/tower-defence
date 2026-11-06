// Round 19: steps the paused editor (at most 90 frames per call, eval's 5 s limit) until the continent boss is in the
// state named in Tools/PlayTest/r19_until.txt ("<state> <extra frames>"), then that many frames more; it stays paused,
// so a still taken next shows exactly that frame. Reports the state and how far it got.
var spec = System.IO.File.ReadAllText("Tools/PlayTest/r19_until.txt").Trim().Split(' ');
int extra = spec.Length > 1 ? int.Parse(spec[1]) : 0;
UnityEditor.EditorApplication.isPaused = true;
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss"; }
int stepped = 0;
while (brain.State != spec[0] && stepped < 90) { UnityEditor.EditorApplication.Step(); stepped++; }
if (brain.State != spec[0]) { return "waiting (" + brain.State + ") after " + stepped; }
for (int i = 0; i < extra; i++) { UnityEditor.EditorApplication.Step(); }
System.IO.File.WriteAllText("Tools/PlayTest/r19_until.txt", spec[0] + " 0");
return "reached " + spec[0] + " +" + extra + " (" + brain.State + ") t=" + UnityEngine.Time.time.ToString("0.00");
