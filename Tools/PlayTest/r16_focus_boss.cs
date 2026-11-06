// Round 16: centres the camera on the continent boss at the zoom in r16_focus.txt ("<zoom>" or "<zoom> <behind m>": the
// view moves that far back along its heading, so the trail behind it shows); "play" uses the level's play zoom.
var spec = System.IO.File.ReadAllText("Tools/PlayTest/r16_focus.txt").Trim().Split(' ');
var brain = UnityEngine.Object.FindAnyObjectByType<Game.Bosses.ContinentBossBrain>();
if (brain == null) { return "no boss"; }
var enemy = brain.GetComponent<Game.Enemies.Enemy>();
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
float size = spec[0] == "play" && pan != null ? pan.PlayZoom : float.Parse(spec[0], System.Globalization.CultureInfo.InvariantCulture);
float behind = spec.Length > 1 ? float.Parse(spec[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;
if (pan != null) { pan.enabled = false; }
var focus = brain.transform.position - enemy.Heading * behind;
rig.SetView(focus, size);
return "focus boss " + brain.State + " at " + brain.transform.position.ToString("0.0") + " heading " + enemy.Heading.ToString("0.00") + " zoom " + size.ToString("0.00");
