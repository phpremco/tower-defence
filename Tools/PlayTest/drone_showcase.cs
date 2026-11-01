// Builds a big display model of the drone kind in Tools/PlayTest/zoom_arg.txt over an empty patch and zooms on it.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim();
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var old = UnityEngine.GameObject.Find("DroneShowcase");
if (old != null) { UnityEngine.Object.Destroy(old); }
foreach (Game.Drones.DroneDefinition drone in context.Level.Drones)
{
    if (drone.Kind.ToString() != wanted) { continue; }
    var holder = new UnityEngine.GameObject("DroneShowcase");
    var road = context.Level.MainRoad;
    UnityEngine.Vector3 spot = context.Level.Road.CentreOf(road[road.Length - 1]) + new UnityEngine.Vector3(0f, 0.4f, 4f);
    holder.transform.position = spot;
    holder.transform.localScale = UnityEngine.Vector3.one * 3f;
    holder.transform.rotation = UnityEngine.Quaternion.Euler(0f, -35f, 0f);
    var rig = Game.Drones.DroneModelBuilder.Build(holder.transform, drone);
    foreach (var rotor in rig.Rotors) { rotor.Rotate(0f, 37f, 0f, UnityEngine.Space.Self); }
    UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>().SetView(spot, 9f);
    return "showing " + drone.name + " at " + spot.ToString("0.0");
}
return "no " + wanted;
