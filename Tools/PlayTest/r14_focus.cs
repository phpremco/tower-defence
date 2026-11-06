// Round 14: centres the camera on a part of the base, named in r14_focus.txt as "<what> <zoom>": base, bays, pad,
// front (the road end and the hardpoints) or play (the base at the level's own play zoom; the zoom number is ignored).
string[] spec = System.IO.File.ReadAllText("Tools/PlayTest/r14_focus.txt").Trim().Split(' ');
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var level = context.Level;
var art = level.Theme.BaseArt;
var centre = Game.Levels.LevelVisualBuilder.BasePosition(level);
var facing = Game.Levels.LevelVisualBuilder.BaseFacing(level);
UnityEngine.Vector3 focus = centre;
if (spec[0] == "bays") { focus = Game.Levels.BaseArtDefinition.ToWorld((art.DockPoints[0] + art.DockPoints[3]) * 0.5f, centre, facing); }
else if (spec[0] == "pad") { focus = Game.Levels.BaseArtDefinition.ToWorld(new UnityEngine.Vector3(-1.58f, 1.2f, -1.56f), centre, facing); }
else if (spec[0] == "front") { focus = Game.Levels.BaseArtDefinition.ToWorld(new UnityEngine.Vector3(0f, 0f, 1.6f), centre, facing); }
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
float size = float.Parse(spec[1], System.Globalization.CultureInfo.InvariantCulture);
if (spec[0] == "play" && pan != null) { size = pan.PlayZoom; }
if (pan != null) { pan.enabled = false; }
rig.SetView(focus, size);
return "focus " + spec[0] + " at " + focus.ToString("0.0") + " zoom " + size.ToString("0.00");
