// Opening cutscene: Mercer's head in the reaction shot (13) against the camera: the angle between the head's forward
// (+Z of the Head bone and of the root) and the direction to the camera, at the progress in Tools/PlayTest/cut_arg.txt.
UnityEditor.EditorApplication.isPaused = true;
var player = UnityEngine.Object.FindAnyObjectByType<Game.UI.CutscenePlayer>();
if (player == null) return "no player";
var parts = System.IO.File.ReadAllText("Tools/PlayTest/cut_arg.txt").Trim().Split(' ');
int shot = int.Parse(parts[0]); float p = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
player.Seek(shot, p); UnityEditor.EditorApplication.Step(); player.Seek(shot, p);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sets = (System.Collections.IDictionary)typeof(Game.UI.CutscenePlayer).GetField("_sets", flags).GetValue(player);
var field = sets[Game.Levels.CutsceneSet.Field];
var layout = (Game.UI.FieldLayout)field.GetType().GetField("_layout", flags).GetValue(field);
var hero = field.GetType().GetField("_hero", flags).GetValue(field);
var root = (UnityEngine.Transform)hero.GetType().GetProperty("Root").GetValue(hero);
var neck = (UnityEngine.Transform)hero.GetType().GetField("_neck", flags).GetValue(hero);
var cam = UnityEngine.Camera.main.transform;
var toCam = cam.position - neck.position; toCam.y = 0f;
System.Func<UnityEngine.Vector3, string> ang = v => { v.y = 0; return UnityEngine.Vector3.SignedAngle(v, toCam, UnityEngine.Vector3.up).ToString("0"); };
var sb = new System.Text.StringBuilder();
sb.Append("head " + neck.position.ToString("0.00") + " cam " + cam.position.ToString("0.00") + " dist " + (cam.position - neck.position).magnitude.ToString("0.00"));
sb.Append(" | rootFwd->cam " + ang(root.forward) + " headZ->cam " + ang(neck.forward) + " headY->cam " + ang(neck.up) + " headX " + ang(neck.right) + " headNegY " + ang(-neck.up) + " headNegZ " + ang(-neck.forward));
var gate = layout.GateCentre - neck.position; gate.y = 0;
sb.Append(" | rootFwd->gate " + UnityEngine.Vector3.SignedAngle(new UnityEngine.Vector3(root.forward.x,0,root.forward.z), gate, UnityEngine.Vector3.up).ToString("0"));
sb.Append(" | entrance->gate " + (layout.GateCentreDistance).ToString("0.0") + " dig " + layout.DigSite.ToString("0.0") + " gateC " + layout.GateCentre.ToString("0.0") + " entr " + layout.Entrance.ToString("0.0") + " guard " + layout.GuardPost.ToString("0.0"));
return sb.ToString();
