// Round 14: launches the level's repair, harvest and attack drone (filling the purse first), whatever the level offers in its bar.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var sb = new System.Text.StringBuilder();
foreach (var drone in new[] { context.Level.RepairDrone, context.Level.AttackDrone, context.Level.HarvestDrone })
{
    if (drone == null) { continue; }
    context.Wallet.Add(10000);
    context.DroneRequested.Raise(drone);
    sb.Append(drone.name).Append('=').Append(context.ActiveDrones(drone)).Append(' ');
}
return "launched " + sb;
