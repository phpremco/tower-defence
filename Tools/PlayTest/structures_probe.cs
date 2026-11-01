// Lists every structure that is not a drone with its health share.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
var sb = new System.Text.StringBuilder();
for (int i = 0; i < context.Structures.Count; i++)
{
    var s = context.Structures[i];
    if (s is Game.Drones.Drone) { continue; }
    sb.Append(s.GetType().Name).Append('@').Append(s.Position.ToString("0")).Append('=').Append(s.HealthFraction.ToString("0.00")).Append(s.BlocksGround ? "B " : " ");
}
return sb.ToString();
