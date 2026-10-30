// Launches one drone of every kind the level offers (filling the purse before each), so all three can be watched.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var sb = new System.Text.StringBuilder();
foreach (Game.Drones.DroneDefinition drone in context.Level.Drones)
{
    context.Wallet.Add(10000);
    context.DroneRequested.Raise(drone);
    sb.Append(drone.name).Append('=').Append(context.ActiveDrones(drone)).Append(' ');
}

return "launched: " + sb + " money left " + context.Wallet.Money;
