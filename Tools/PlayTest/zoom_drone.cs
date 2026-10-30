// Zooms the camera onto the drone whose kind (Repair, Harvest or Attack) is in Tools/PlayTest/zoom_arg.txt, at the
// factor in zoom_size.txt, and reports what it is doing.
string wanted = System.IO.File.ReadAllText("Tools/PlayTest/zoom_arg.txt").Trim();
float zoom = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
foreach (var drone in UnityEngine.Object.FindObjectsByType<Game.Drones.Drone>(UnityEngine.FindObjectsSortMode.None))
{
    if (drone.Definition.Kind.ToString() == wanted)
    {
        rig.SetView(drone.transform.position, zoom);
        return "zoomed on " + drone.name + " at " + drone.transform.position.ToString("0.0") + " work " + (drone.WorkPoint.HasValue ? drone.WorkPoint.Value.ToString("0.0") : "none") + " carrying " + drone.IsCarrying;
    }
}

return "no " + wanted + " drone";
