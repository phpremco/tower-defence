// Round 49 smoke material fix: frames the play camera on the hero at the half height in zoom_size.txt (time keeps running).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
if (pan != null) { pan.enabled = false; }
var camera = UnityEngine.Camera.main;
var centre = context.Hero.transform.position + UnityEngine.Vector3.up * 0.5f;
var ray = camera.transform.forward;
rig.SetView(centre - ray * (centre.y / ray.y), zoom);
camera.orthographicSize = zoom;
return "hero at " + context.Hero.transform.position.ToString("0.0") + " down " + context.Hero.IsDown;
