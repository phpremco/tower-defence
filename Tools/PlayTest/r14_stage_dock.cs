// Round 14: stages a docking moment for a still: the first bay's drone is put back over its home and held there, the bays
// stop animating and bay 1 is posed with its lid open and the drone the share in r14_stage.txt of the way down (the same
// pose code the bays run). Step a frame or two before the still so the drone's view applies the pose.
var view = UnityEngine.Object.FindAnyObjectByType<Game.Levels.BaseDockView>();
if (view == null) { return "no bays"; }
float depthShare = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r14_stage.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var type = typeof(Game.Levels.BaseDockView);
var drones = (Game.Drones.Drone[])type.GetField("_drones", flags).GetValue(view);
var open = (float[])type.GetField("_open", flags).GetValue(view);
var depth = (float[])type.GetField("_depth", flags).GetValue(view);
var drone = drones[0];
if (drone == null) { return "no drone in bay 1"; }
drone.enabled = false;
drone.transform.position = drone.Home;
view.enabled = false;
open[0] = 1f;
depth[0] = depthShare;
type.GetMethod("PoseLid", flags).Invoke(view, new object[] { 0, 1f });
type.GetMethod("PoseDrone", flags).Invoke(view, new object[] { 0 });
return "staged bay 1 open, " + drone.Definition.name + " depth " + depthShare;
