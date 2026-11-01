// Launches as many repair drones as the level allows, so the repair crew can be watched sharing its work.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
foreach (Game.Drones.DroneDefinition drone in context.Level.Drones)
{
    if (drone.Kind != Game.Drones.DroneKind.Repair) { continue; }
    for (int i = 0; i < 3; i++) { context.Wallet.Add(10000); context.DroneRequested.Raise(drone); }
    return "repair drones: " + context.ActiveDrones(drone);
}
return "no repair drone";
