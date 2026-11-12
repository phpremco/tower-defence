// Round 54: steps the paused editor (at most 90 frames per call) until the RuinGate has been opening for the number of
// clip frames (30 per second) in Tools/PlayTest/r54_until.txt; stays paused so the next still shows exactly that moment.
// With fixed 1/30 s steps (r17_fixed_step.cs) one step is one clip frame.
float frame = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r54_until.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
UnityEditor.EditorApplication.isPaused = true;
var gate = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.RuinGateView>();
if (gate == null) { return "no RuinGate"; }
int stepped = 0;
while (gate.Opened * 30f < frame - 0.01f && stepped < 90) { UnityEditor.EditorApplication.Step(); stepped++; }
string state = "";
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain != null) { state = " boss " + brain.State; }
return (gate.Opened * 30f >= frame - 0.01f ? "reached" : "waiting") + " gate frame " + (gate.Opened * 30f).ToString("0.0") + " after " + stepped + state;
