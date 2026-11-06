// Round 14: knocks the share in r14_hurt.txt off the base's health, so a repair drone has a short job beside the bays.
if (!Game.Core.Services.TryGet(out Game.Levels.LevelContext context)) { return "no context"; }
float share = float.Parse(System.IO.File.ReadAllText("Tools/PlayTest/r14_hurt.txt").Trim(), System.Globalization.CultureInfo.InvariantCulture);
context.Base.TakeDamage(context.BaseHealth.Max * share);
return "base at " + context.BaseHealth.Fraction.ToString("0.00");
