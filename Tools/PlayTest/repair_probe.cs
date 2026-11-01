// Lists every repair drone with its activity, position and current job, so the dispatcher can be watched.
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder();
foreach (var drone in UnityEngine.Object.FindObjectsByType<Game.Drones.Drone>(UnityEngine.FindObjectsSortMode.None))
{
    var target = typeof(Game.Drones.Drone).GetField("_repairTarget", flags).GetValue(drone) as Game.Combat.IStructure;
    sb.Append(drone.Definition.Kind).Append(' ').Append(drone.Activity).Append(" at ").Append(drone.transform.position.ToString("0.0")).Append(" pace ").Append(drone.Pace.ToString("0.00"));
    if (target != null) { sb.Append(" job ").Append(target.GetType().Name).Append('@').Append(target.Position.ToString("0")).Append('=').Append(target.HealthFraction.ToString("0.00")); }
    sb.Append(" | ");
}
return sb.ToString();
