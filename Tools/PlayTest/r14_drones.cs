// Round 14: every drone with its activity, home flag, position and home.
var sb = new System.Text.StringBuilder();
foreach (var drone in UnityEngine.Object.FindObjectsByType<Game.Drones.Drone>(UnityEngine.FindObjectsSortMode.None))
{
    sb.Append(drone.Definition.name).Append(' ').Append(drone.Activity).Append(drone.IsHome ? " home" : "").Append(" at ").Append(drone.Position.ToString("0.0")).Append(" home ").Append(drone.Home.ToString("0.0")).Append(" | ");
}
return sb.ToString() + " t=" + UnityEngine.Time.time.ToString("0.0");
