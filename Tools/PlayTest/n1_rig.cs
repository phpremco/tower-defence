// Night 1, item 6: centres the camera on the colony rig at the zoom in n1_rig_arg.txt (CameraPanZoom zoom: bigger is
// closer) and reports where it stands and what it shows.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) return "no context";
var rig = UnityEngine.GameObject.Find("ColonyRig");
if (rig == null) return "no rig";
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/n1_rig_arg.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var pan = UnityEngine.Object.FindAnyObjectByType<Game.Levels.CameraPanZoom>();
if (zoom > 0f) pan.SetView(rig.transform.position + UnityEngine.Vector3.up * 1.2f, zoom);
int renderers = rig.GetComponentsInChildren<UnityEngine.Renderer>().Length;
var screen = UnityEngine.Camera.main.WorldToViewportPoint(rig.transform.position);
return "rig at " + rig.transform.position.ToString("0.0") + " renderers " + renderers + " children " + rig.transform.childCount + " viewport " + screen.ToString("0.00") + " play zoom " + pan.PlayZoom.ToString("0.0");
