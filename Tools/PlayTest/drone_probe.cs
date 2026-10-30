// Reports money and the number of active drones per kind.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context))
{
    return "no context";
}

var sb = new System.Text.StringBuilder();
sb.Append("money=").Append(context.Wallet.Money);
foreach (var drone in context.Level.Drones)
{
    sb.Append(' ').Append(drone.name).Append('=').Append(context.ActiveDrones(drone));
}

return sb.ToString();
