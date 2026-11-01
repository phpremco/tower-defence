// Damages every gate and base turret in the level so the repair drones have work; the amount is in Tools/PlayTest/damage_arg.txt.
float amount = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/damage_arg.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
int hit = 0;
var sb = new System.Text.StringBuilder();
for (int i = 0; i < context.Structures.Count; i++)
{
    var s = context.Structures[i];
    if (s is Game.Drones.Drone) { continue; }
    s.TakeDamage(amount * (1f + hit * 0.35f));
    sb.Append(s.GetType().Name).Append('@').Append(s.Position.ToString("0")).Append('=').Append(s.HealthFraction.ToString("0.00")).Append(' ');
    hit++;
}
return "damaged " + hit + ": " + sb;
