// Round 16: hides ("hide") or shows ("show", from r16_furrow_arg.txt) the boss's renderers for a still of the ground under it.
bool hide = System.IO.File.ReadAllText("Tools/PlayTest/r16_furrow_arg.txt").Trim() == "hide";
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss"; }
int n = 0;
foreach (var r in brain.GetComponentsInChildren<UnityEngine.Renderer>(true)) { if (!(r is UnityEngine.ParticleSystemRenderer)) { r.enabled = !hide; n++; } }
return (hide ? "hid " : "showed ") + n;
