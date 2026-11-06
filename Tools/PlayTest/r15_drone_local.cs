// Round 15: every drone's home and position in the base's own frame (x across, z towards the road), with its activity,
// so idle spots can be checked against the module bounds (r15_base_modules.cs, there in the export frame: negate x and z).
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var centre = Game.Levels.LevelVisualBuilder.BasePosition(context.Level);
var facing = Game.Levels.LevelVisualBuilder.BaseFacing(context.Level);
var back = UnityEngine.Quaternion.Inverse(facing);
var sb = new System.Text.StringBuilder();
foreach (var drone in UnityEngine.Object.FindObjectsByType<Game.Drones.Drone>(UnityEngine.FindObjectsSortMode.None))
{
    var home = back * (drone.Home - centre);
    var at = back * (drone.Position - centre);
    sb.Append(drone.Definition.name).Append(' ').Append(drone.Activity).Append(" home ").Append(home.x.ToString("0.00")).Append(',').Append(home.z.ToString("0.00")).Append(" at ").Append(at.x.ToString("0.00")).Append(',').Append(at.z.ToString("0.00")).Append(" | ");
}
return sb.ToString();
