// Round 88a: the repair beam against its drone. "hurt": every tower takes 60 % of its health (so the repair drone has work).
// "probe" (default): per working drone its place, the middle of its drawn body, its tool socket, the start of the beam,
// and how far the start of the beam lies from the vertical line through the middle of the body (in metres; 0 = from the middle).
// "zoom": the camera on the first working drone at the play zoom times Tools/PlayTest/zoom_size.txt.
string mode = System.IO.File.Exists("Tools/PlayTest/r88_drone_arg.txt") ? System.IO.File.ReadAllText("Tools/PlayTest/r88_drone_arg.txt").Trim() : "probe";
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var inv = System.Globalization.CultureInfo.InvariantCulture;
if (mode == "hurt")
{
    int hurt = 0;
    foreach (var tower in UnityEngine.Object.FindObjectsByType<Game.Towers.Tower>(UnityEngine.FindObjectsSortMode.None))
    {
        if (tower.Definition.BlocksGroundEnemies || tower.IsWrecked) { continue; }
        tower.TakeDamage(tower.Health.Current * 0.6f);
        hurt++;
    }
    return "hurt " + hurt + " towers";
}
var sb = new System.Text.StringBuilder();
foreach (var drone in UnityEngine.Object.FindObjectsByType<Game.Drones.Drone>(UnityEngine.FindObjectsSortMode.None))
{
    if (!drone.WorkPoint.HasValue) { sb.Append(drone.Definition.Kind).Append(' ').Append(drone.Activity).Append(" idle | "); continue; }
    if (mode == "zoom")
    {
        var rig = UnityEngine.Object.FindAnyObjectByType<Game.Levels.FixedCameraRig>();
        var pan = rig.GetComponent<Game.Levels.CameraPanZoom>();
        if (pan != null) { pan.enabled = false; }
        float factor = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/zoom_size.txt").Trim(), inv);
        rig.SetView((drone.transform.position + drone.WorkPoint.Value) * 0.5f, (pan != null ? pan.PlayZoom : 10f) * factor);
        return "zoomed on " + drone.Definition.Kind + " at " + drone.transform.position.ToString("0.0");
    }
    var view = drone.GetComponentInChildren<Game.Drones.DroneView>();
    var bounds = new UnityEngine.Bounds(); bool any = false;
    foreach (var r in view.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>()) { if (!any) { bounds = r.bounds; any = true; } else { bounds.Encapsulate(r.bounds); } }
    UnityEngine.Transform beam = null;
    foreach (UnityEngine.Transform child in view.transform.parent) { if (child.name == "ToolBeam" && child.gameObject.activeSelf) { beam = child; } }
    UnityEngine.Transform tool = null;
    foreach (var t in view.GetComponentsInChildren<UnityEngine.Transform>()) { if (t.name == "Beam" || t.name == "Mine") { tool = t; } }
    sb.Append(drone.Definition.Kind).Append(' ').Append(drone.Activity).Append(" drone ").Append(drone.transform.position.ToString("0.00")).Append(" pace ").Append(drone.Pace.ToString("0.00"))
      .Append(" body ").Append(bounds.center.ToString("0.00")).Append(" tool ").Append(tool != null ? tool.position.ToString("0.00") : "none")
      .Append(" beam ").Append(beam != null ? beam.position.ToString("0.00") : "none").Append(" work ").Append(drone.WorkPoint.Value.ToString("0.00"));
    if (beam != null)
    {
        float dx = beam.position.x - bounds.center.x, dz = beam.position.z - bounds.center.z;
        sb.Append(" beam off the body axis ").Append(UnityEngine.Mathf.Sqrt(dx * dx + dz * dz).ToString("0.00", inv)).Append(" m, beam scale ").Append(beam.localScale.ToString("0.00"));
        var mf = beam.GetComponent<UnityEngine.MeshFilter>();
        if (mf != null) { sb.Append(" mesh bounds ").Append(mf.sharedMesh.bounds.center.ToString("0.00")).Append(' ').Append(mf.sharedMesh.bounds.size.ToString("0.00")); }
    }
    sb.Append(" | ");
}
return sb.Length > 0 ? sb.ToString() : "no drones";
